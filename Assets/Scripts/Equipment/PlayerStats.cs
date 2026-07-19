using System;
using UnityEngine;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.Equipment
{
    public sealed class PlayerStats : MonoBehaviour, IRuntimeStatReceiver
    {
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField, Min(1f)] private float baseMaximumHealth = 100f;
        [SerializeField, Min(1f)] private float baseMaximumStamina = 100f;
        [SerializeField, Min(1f)] private float baseMaximumMana = 100f;
        [SerializeField, Min(0f)] private float baseAttackPower = 5f;
        [SerializeField, Min(0f)] private float baseDefense = 0f;

        private readonly RuntimeStatCollection runtimeStats = new RuntimeStatCollection();
        private bool baseStatsConfigured;

        public float MaximumHealth => Mathf.Max(1f, GetRuntimeStatValue(StatType.MaximumHealth));
        public float MaximumStamina => Mathf.Max(1f, GetRuntimeStatValue(StatType.MaximumStamina));
        public float MaximumMana => Mathf.Max(1f, GetRuntimeStatValue(StatType.MaximumMana));
        public float AttackPower => Mathf.Max(0f, GetRuntimeStatValue(StatType.AttackPower));
        public float Defense => Mathf.Max(0f, GetRuntimeStatValue(StatType.Defense));
        public event Action StatsChanged;

        private void Awake()
        {
            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }

            EnsureBaseStatsConfigured();
            RecalculateEquipmentModifiers();
        }

        private void OnEnable()
        {
            EnsureBaseStatsConfigured();

            if (equipment != null)
            {
                equipment.EquipmentChanged += OnEquipmentChanged;
            }

            runtimeStats.StatChanged += OnRuntimeStatChanged;
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged -= OnEquipmentChanged;
            }

            runtimeStats.StatChanged -= OnRuntimeStatChanged;
        }

        private void OnValidate()
        {
            baseMaximumHealth = Mathf.Max(1f, baseMaximumHealth);
            baseMaximumStamina = Mathf.Max(1f, baseMaximumStamina);
            baseMaximumMana = Mathf.Max(1f, baseMaximumMana);
            baseAttackPower = Mathf.Max(0f, baseAttackPower);
            baseDefense = Mathf.Max(0f, baseDefense);
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

        private void OnEquipmentChanged()
        {
            RecalculateEquipmentModifiers();
            StatsChanged?.Invoke();
        }

        private void OnRuntimeStatChanged(StatType statType, float value)
        {
            StatsChanged?.Invoke();
        }

        private float GetRuntimeStatValue(StatType statType)
        {
            EnsureBaseStatsConfigured();
            return runtimeStats.GetValue(statType);
        }

        private void EnsureBaseStatsConfigured()
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
            runtimeStats.SetBaseValue(StatType.MovementSpeed, 0f);
            baseStatsConfigured = true;
        }

        private void RecalculateEquipmentModifiers()
        {
            RemoveEquipmentModifiers();

            if (equipment == null)
            {
                return;
            }

            foreach (EquipmentSlotState slot in equipment.Slots)
            {
                if (slot == null || slot.IsEmpty || slot.Item == null || !slot.Item.IsEquippable)
                {
                    continue;
                }

                RegisterEquipmentModifiers(slot);
            }
        }

        private void RemoveEquipmentModifiers()
        {
            Array values = Enum.GetValues(typeof(EquipmentSlotType));
            for (int i = 0; i < values.Length; i++)
            {
                EquipmentSlotType slotType = (EquipmentSlotType)values.GetValue(i);
                runtimeStats.RemoveModifiersFromSource(CreateEquipmentSource(slotType));
            }
        }

        private void RegisterEquipmentModifiers(EquipmentSlotState slot)
        {
            StatModifierSource source = CreateEquipmentSource(slot.SlotType);
            StatModifiers modifiers = slot.Item.Equipment.StatModifiers;
            AddFlatEquipmentModifier(source, StatType.MaximumHealth, modifiers.MaximumHealth);
            AddFlatEquipmentModifier(source, StatType.MaximumStamina, modifiers.MaximumStamina);
            AddFlatEquipmentModifier(source, StatType.MaximumMana, modifiers.MaximumMana);
            AddFlatEquipmentModifier(source, StatType.AttackPower, modifiers.AttackPower);
            AddFlatEquipmentModifier(source, StatType.Defense, modifiers.Defense);
        }

        private void AddFlatEquipmentModifier(StatModifierSource source, StatType statType, float value)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            runtimeStats.AddModifier(new RuntimeStatModifier(statType, StatModifierOperation.FlatAdd, value, source));
        }

        private static StatModifierSource CreateEquipmentSource(EquipmentSlotType slotType)
        {
            return new StatModifierSource(StatModifierSourceType.Equipment, $"equipment.slot.{slotType}");
        }
    }
}
