using System;
using UnityEngine;

namespace UnityIsekaiGame.Stats
{
    [Serializable]
    public sealed class StatModifierDefinition
    {
        [SerializeField] private StatType statType;
        [SerializeField] private StatModifierOperation operation;
        [SerializeField] private float value;
        [SerializeField] private bool scaleWithStacks = true;
        [SerializeField] private int priority;

        public StatType StatType => statType;
        public StatModifierOperation Operation => operation;
        public float Value => value;
        public bool ScaleWithStacks => scaleWithStacks;
        public int Priority => priority;

        public bool IsValid => !float.IsNaN(value) && !float.IsInfinity(value);

        public RuntimeStatModifier CreateRuntimeModifier(StatModifierSource source, int stackCount)
        {
            float effectiveValue = scaleWithStacks ? value * Mathf.Max(1, stackCount) : value;
            return new RuntimeStatModifier(statType, operation, effectiveValue, source, priority);
        }
    }
}
