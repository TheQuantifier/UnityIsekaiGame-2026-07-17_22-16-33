using System;
using UnityEngine;

namespace UnityIsekaiGame.Stats
{
    public class ActorStats : MonoBehaviour, IActorStats
    {
        [SerializeField, Min(1f)] protected float baseMaximumHealth = 100f;
        [SerializeField, Min(0f)] protected float baseMaximumStamina = 100f;
        [SerializeField, Min(0f)] protected float baseMaximumMana = 100f;
        [SerializeField, Min(0f)] protected float baseAttackPower = 0f;
        [SerializeField, Min(0f)] protected float baseDefense = 0f;
        [SerializeField, Min(0f)] protected float baseMovementSpeed = 0f;

        private readonly RuntimeStatCollection runtimeStats = new RuntimeStatCollection();
        private bool baseStatsConfigured;

        public float MaximumHealth => Mathf.Max(1f, GetRuntimeStatValue(StatType.MaximumHealth));
        public float MaximumStamina => Mathf.Max(0f, GetRuntimeStatValue(StatType.MaximumStamina));
        public float MaximumMana => Mathf.Max(0f, GetRuntimeStatValue(StatType.MaximumMana));
        public float AttackPower => Mathf.Max(0f, GetRuntimeStatValue(StatType.AttackPower));
        public float Defense => Mathf.Max(0f, GetRuntimeStatValue(StatType.Defense));
        public float MovementSpeed => Mathf.Max(0f, GetRuntimeStatValue(StatType.MovementSpeed));
        public event Action StatsChanged;

        protected virtual void Awake()
        {
            EnsureBaseStatsConfigured();
        }

        protected virtual void OnEnable()
        {
            EnsureBaseStatsConfigured();
            runtimeStats.StatChanged += OnRuntimeStatChanged;
        }

        protected virtual void OnDisable()
        {
            runtimeStats.StatChanged -= OnRuntimeStatChanged;
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

            runtimeStats.SetBaseValue(StatType.MaximumHealth, baseMaximumHealth);
            runtimeStats.SetBaseValue(StatType.MaximumStamina, baseMaximumStamina);
            runtimeStats.SetBaseValue(StatType.MaximumMana, baseMaximumMana);
            runtimeStats.SetBaseValue(StatType.AttackPower, baseAttackPower);
            runtimeStats.SetBaseValue(StatType.Defense, baseDefense);
            runtimeStats.SetBaseValue(StatType.MovementSpeed, baseMovementSpeed);
            baseStatsConfigured = true;
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
    }
}
