using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Beings;
using UnityIsekaiGame.Combat;

namespace UnityIsekaiGame.Stats
{
    public class ActorStats : MonoBehaviour, IActorStats, IDamageResistanceReceiver
    {
        [SerializeField] private ActorProfileDefinition actorProfile;
        [SerializeField, Min(1f)] protected float baseMaximumHealth = 100f;
        [SerializeField, Min(0f)] protected float baseMaximumStamina = 100f;
        [SerializeField, Min(0f)] protected float baseMaximumMana = 100f;
        [SerializeField, Min(0f)] protected float baseAttackPower = 0f;
        [SerializeField, Min(0f)] protected float baseDefense = 0f;
        [SerializeField, Min(0f)] protected float baseMovementSpeed = 0f;

        private readonly RuntimeStatCollection runtimeStats = new RuntimeStatCollection();
        private readonly RuntimeResistanceCollection runtimeResistances = new RuntimeResistanceCollection();
        private bool baseStatsConfigured;

        public float MaximumHealth => Mathf.Max(1f, GetRuntimeStatValue(StatType.MaximumHealth));
        public float MaximumStamina => Mathf.Max(0f, GetRuntimeStatValue(StatType.MaximumStamina));
        public float MaximumMana => Mathf.Max(0f, GetRuntimeStatValue(StatType.MaximumMana));
        public float AttackPower => Mathf.Max(0f, GetRuntimeStatValue(StatType.AttackPower));
        public float Defense => Mathf.Max(0f, GetRuntimeStatValue(StatType.Defense));
        public float MovementSpeed => Mathf.Max(0f, GetRuntimeStatValue(StatType.MovementSpeed));
        public ActorProfileDefinition ActorProfile => actorProfile;
        public bool IsInitialized => baseStatsConfigured;
        public ActorProfileInitializationResult LastInitializationResult { get; private set; }
        public bool HasProfileLegacyConflict => actorProfile != null
            && (!Mathf.Approximately(actorProfile.BaseMaximumHealth, baseMaximumHealth)
                || !Mathf.Approximately(actorProfile.BaseMaximumStamina, baseMaximumStamina)
                || !Mathf.Approximately(actorProfile.BaseMaximumMana, baseMaximumMana)
                || !Mathf.Approximately(actorProfile.BaseAttackPower, baseAttackPower)
                || !Mathf.Approximately(actorProfile.BaseDefense, baseDefense)
                || !Mathf.Approximately(actorProfile.BaseMovementSpeed, baseMovementSpeed));
        public event Action StatsChanged;
        public event Action<DamageTypeDefinition, float> ResistanceChanged;

        protected virtual void Awake()
        {
            EnsureBaseStatsConfigured();
        }

        protected virtual void OnEnable()
        {
            EnsureBaseStatsConfigured();
            runtimeStats.StatChanged += OnRuntimeStatChanged;
            runtimeResistances.ResistanceChanged += OnRuntimeResistanceChanged;
        }

        protected virtual void OnDisable()
        {
            runtimeStats.StatChanged -= OnRuntimeStatChanged;
            runtimeResistances.ResistanceChanged -= OnRuntimeResistanceChanged;
        }

        protected virtual void OnValidate()
        {
            baseMaximumHealth = Mathf.Max(1f, baseMaximumHealth);
            baseMaximumStamina = Mathf.Max(0f, baseMaximumStamina);
            baseMaximumMana = Mathf.Max(0f, baseMaximumMana);
            baseAttackPower = Mathf.Max(0f, baseAttackPower);
            baseDefense = Mathf.Max(0f, baseDefense);
            baseMovementSpeed = Mathf.Max(0f, baseMovementSpeed);
        }

        public ActorProfileInitializationResult TryInitializeBaseStats()
        {
            if (baseStatsConfigured)
            {
                LastInitializationResult = new ActorProfileInitializationResult(
                    ActorProfileInitializationStatus.AlreadyInitialized,
                    $"{name} actor stats are already initialized.");
                return LastInitializationResult;
            }

            EnsureBaseStatsConfigured();
            return LastInitializationResult;
        }

        public bool HasStat(StatType statType)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.HasStat(statType);
        }

        public float GetStatValue(StatType statType)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.GetValue(statType);
        }

        public bool AddModifier(RuntimeStatModifier modifier)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.AddModifier(modifier);
        }

        public bool RemoveModifiersFromSource(StatModifierSource source)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.RemoveModifiersFromSource(source);
        }

        public float GetDirectResistance(DamageTypeDefinition damageType)
        {
            EnsureBaseStatsConfigured();
            return runtimeResistances.GetDirectResistance(damageType);
        }

        public float GetEffectiveResistance(DamageTypeDefinition damageType)
        {
            EnsureBaseStatsConfigured();
            return runtimeResistances.GetEffectiveResistance(damageType);
        }

        public bool AddResistanceModifier(RuntimeResistanceModifier modifier)
        {
            EnsureBaseStatsConfigured();
            return runtimeResistances.AddModifier(modifier);
        }

        public bool RemoveResistanceModifiersFromSource(StatModifierSource source)
        {
            EnsureBaseStatsConfigured();
            return runtimeResistances.RemoveModifiersFromSource(source);
        }

        public IReadOnlyList<RuntimeResistanceModifier> GetResistanceModifiers()
        {
            EnsureBaseStatsConfigured();
            return runtimeResistances.GetModifiers();
        }

        protected void NotifyStatsChanged()
        {
            StatsChanged?.Invoke();
        }

        protected void EnsureBaseStatsConfigured()
        {
            if (baseStatsConfigured)
            {
                return;
            }

            if (actorProfile != null && actorProfile.HasValidBaseStats)
            {
                ApplyBaseValues(
                    actorProfile.BaseMaximumHealth,
                    actorProfile.BaseMaximumStamina,
                    actorProfile.BaseMaximumMana,
                    actorProfile.BaseAttackPower,
                    actorProfile.BaseDefense,
                    actorProfile.BaseMovementSpeed);
                ApplyBaseResistances(actorProfile.BaseResistances);
                LastInitializationResult = new ActorProfileInitializationResult(
                    ActorProfileInitializationStatus.InitializedFromProfile,
                    $"{name} actor stats initialized from profile '{actorProfile.Id}'.");
            }
            else
            {
                ApplyBaseValues(
                    baseMaximumHealth,
                    baseMaximumStamina,
                    baseMaximumMana,
                    baseAttackPower,
                    baseDefense,
                    baseMovementSpeed);
                runtimeResistances.ClearBaseResistances();
                LastInitializationResult = new ActorProfileInitializationResult(
                    actorProfile == null ? ActorProfileInitializationStatus.InitializedFromLegacyFallback : ActorProfileInitializationStatus.InvalidProfile,
                    actorProfile == null
                        ? $"{name} actor stats initialized from legacy fallback fields."
                        : $"{name} actor stats profile '{actorProfile.Id}' is invalid; legacy fallback fields were used.");
            }

            baseStatsConfigured = true;
        }

        private void ApplyBaseValues(
            float maximumHealth,
            float maximumStamina,
            float maximumMana,
            float attackPower,
            float defense,
            float movementSpeed)
        {
            runtimeStats.SetBaseValue(StatType.MaximumHealth, maximumHealth);
            runtimeStats.SetBaseValue(StatType.MaximumStamina, maximumStamina);
            runtimeStats.SetBaseValue(StatType.MaximumMana, maximumMana);
            runtimeStats.SetBaseValue(StatType.AttackPower, attackPower);
            runtimeStats.SetBaseValue(StatType.Defense, defense);
            runtimeStats.SetBaseValue(StatType.MovementSpeed, movementSpeed);
        }

        private void ApplyBaseResistances(IReadOnlyList<ResistanceModifierDefinition> baseResistances)
        {
            runtimeResistances.ClearBaseResistances();
            if (baseResistances == null)
            {
                return;
            }

            for (int i = 0; i < baseResistances.Count; i++)
            {
                ResistanceModifierDefinition resistance = baseResistances[i];
                if (resistance != null && resistance.IsValid)
                {
                    runtimeResistances.SetBaseResistance(resistance.DamageType, resistance.Resistance);
                }
            }
        }

        private float GetRuntimeStatValue(StatType statType)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.GetValue(statType);
        }

        private void OnRuntimeStatChanged(StatType statType, float value)
        {
            NotifyStatsChanged();
        }

        private void OnRuntimeResistanceChanged(DamageTypeDefinition damageType, float value)
        {
            ResistanceChanged?.Invoke(damageType, value);
        }
    }
}
