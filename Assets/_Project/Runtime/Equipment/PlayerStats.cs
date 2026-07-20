using System;
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.Equipment
{
    public sealed class PlayerStats : ActorStats
    {
        private const float DefaultPlayerAttackPower = 5f;

        [SerializeField] private PlayerEquipment equipment;

        private void Reset()
        {
            baseAttackPower = DefaultPlayerAttackPower;
        }

        protected override void Awake()
        {
            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }

            if (Mathf.Approximately(baseAttackPower, 0f))
            {
                baseAttackPower = DefaultPlayerAttackPower;
            }

            base.Awake();
            RecalculateEquipmentModifiers();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (equipment != null)
            {
                equipment.EquipmentChanged += OnEquipmentChanged;
            }
        }

        protected override void OnDisable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged -= OnEquipmentChanged;
            }

            base.OnDisable();
        }

        private void OnEquipmentChanged()
        {
            RecalculateEquipmentModifiers();
            NotifyStatsChanged();
        }

        public void RefreshEquipmentModifiers()
        {
            RecalculateEquipmentModifiers();
            NotifyStatsChanged();
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
                StatModifierSource source = CreateEquipmentSource(slotType);
                RemoveModifiersFromSource(source);
                RemoveResistanceModifiersFromSource(source);
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
            RegisterEquipmentResistanceModifiers(source, slot.Item.Equipment.ResistanceModifiers);
        }

        private void AddFlatEquipmentModifier(StatModifierSource source, StatType statType, float value)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            AddModifier(new RuntimeStatModifier(statType, StatModifierOperation.FlatAdd, value, source));
        }

        private static StatModifierSource CreateEquipmentSource(EquipmentSlotType slotType)
        {
            return new StatModifierSource(StatModifierSourceType.Equipment, $"equipment.slot.{slotType}");
        }

        private void RegisterEquipmentResistanceModifiers(StatModifierSource source, System.Collections.Generic.IReadOnlyList<ResistanceModifierDefinition> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                ResistanceModifierDefinition modifier = modifiers[i];
                if (modifier == null || !modifier.IsValid)
                {
                    continue;
                }

                AddResistanceModifier(modifier.CreateRuntimeModifier(source));
            }
        }
    }
}
