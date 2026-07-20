using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.Stats
{
    public sealed class RuntimeStatCollection
    {
        private readonly Dictionary<StatType, float> baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatModifierSource, List<RuntimeStatModifier>> modifiersBySource = new Dictionary<StatModifierSource, List<RuntimeStatModifier>>();
        private readonly Dictionary<StatType, float> cachedValues = new Dictionary<StatType, float>();

        public event Action<StatType, float> StatChanged;

        public void SetBaseValue(StatType statType, float value)
        {
            float clampedValue = Mathf.Max(0f, value);
            float previous = GetValue(statType);
            baseValues[statType] = clampedValue;
            NotifyIfChanged(statType, previous);
        }

        public bool HasStat(StatType statType)
        {
            return baseValues.ContainsKey(statType);
        }

        public float GetValue(StatType statType)
        {
            if (!baseValues.TryGetValue(statType, out float baseValue))
            {
                return 0f;
            }

            float flatAdd = 0f;
            float percentAdd = 0f;
            float multiplicative = 1f;

            foreach (List<RuntimeStatModifier> sourceModifiers in modifiersBySource.Values)
            {
                for (int i = 0; i < sourceModifiers.Count; i++)
                {
                    RuntimeStatModifier modifier = sourceModifiers[i];
                    if (modifier.StatType != statType)
                    {
                        continue;
                    }

                    switch (modifier.Operation)
                    {
                        case StatModifierOperation.FlatAdd:
                            flatAdd += modifier.Value;
                            break;
                        case StatModifierOperation.PercentAdd:
                            percentAdd += modifier.Value;
                            break;
                        case StatModifierOperation.Multiplicative:
                            multiplicative *= modifier.Value;
                            break;
                    }
                }
            }

            float value = (baseValue + flatAdd) * (1f + percentAdd) * multiplicative;
            return Mathf.Max(0f, value);
        }

        public bool AddModifier(RuntimeStatModifier modifier)
        {
            if (!modifier.IsValid || !HasStat(modifier.StatType))
            {
                return false;
            }

            if (!modifiersBySource.TryGetValue(modifier.Source, out List<RuntimeStatModifier> sourceModifiers))
            {
                sourceModifiers = new List<RuntimeStatModifier>();
                modifiersBySource.Add(modifier.Source, sourceModifiers);
            }

            for (int i = 0; i < sourceModifiers.Count; i++)
            {
                RuntimeStatModifier existing = sourceModifiers[i];
                if (existing.StatType == modifier.StatType && existing.Operation == modifier.Operation && existing.Priority == modifier.Priority)
                {
                    return false;
                }
            }

            float previous = GetValue(modifier.StatType);
            sourceModifiers.Add(modifier);
            NotifyIfChanged(modifier.StatType, previous);
            return true;
        }

        public bool RemoveModifiersFromSource(StatModifierSource source)
        {
            if (!modifiersBySource.TryGetValue(source, out List<RuntimeStatModifier> sourceModifiers))
            {
                return false;
            }

            HashSet<StatType> changedStats = new HashSet<StatType>();
            Dictionary<StatType, float> previousValues = new Dictionary<StatType, float>();
            for (int i = 0; i < sourceModifiers.Count; i++)
            {
                StatType statType = sourceModifiers[i].StatType;
                if (changedStats.Add(statType))
                {
                    previousValues.Add(statType, GetValue(statType));
                }
            }

            modifiersBySource.Remove(source);
            foreach (StatType statType in changedStats)
            {
                NotifyIfChanged(statType, previousValues[statType]);
            }

            return true;
        }

        public void ClearModifiers()
        {
            List<StatModifierSource> sources = new List<StatModifierSource>(modifiersBySource.Keys);
            for (int i = 0; i < sources.Count; i++)
            {
                RemoveModifiersFromSource(sources[i]);
            }
        }

        private void NotifyIfChanged(StatType statType, float previous)
        {
            float current = GetValue(statType);
            cachedValues[statType] = current;
            if (!Mathf.Approximately(previous, current))
            {
                StatChanged?.Invoke(statType, current);
            }
        }
    }
}
