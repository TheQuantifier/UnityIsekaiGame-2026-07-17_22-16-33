using System;
using UnityEngine;

namespace UnityIsekaiGame.Equipment
{
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField, Min(1f)] private float baseMaximumHealth = 100f;
        [SerializeField, Min(1f)] private float baseMaximumStamina = 100f;
        [SerializeField, Min(1f)] private float baseMaximumMana = 100f;
        [SerializeField, Min(0f)] private float baseAttackPower = 5f;
        [SerializeField, Min(0f)] private float baseDefense = 0f;

        private StatModifiers equipmentModifiers;

        public float MaximumHealth => Mathf.Max(1f, baseMaximumHealth + equipmentModifiers.MaximumHealth);
        public float MaximumStamina => Mathf.Max(1f, baseMaximumStamina + equipmentModifiers.MaximumStamina);
        public float MaximumMana => Mathf.Max(1f, baseMaximumMana + equipmentModifiers.MaximumMana);
        public float AttackPower => Mathf.Max(0f, baseAttackPower + equipmentModifiers.AttackPower);
        public float Defense => Mathf.Max(0f, baseDefense + equipmentModifiers.Defense);
        public event Action StatsChanged;

        private void Awake()
        {
            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }

            Recalculate();
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged += OnEquipmentChanged;
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged -= OnEquipmentChanged;
            }
        }

        private void OnValidate()
        {
            baseMaximumHealth = Mathf.Max(1f, baseMaximumHealth);
            baseMaximumStamina = Mathf.Max(1f, baseMaximumStamina);
            baseMaximumMana = Mathf.Max(1f, baseMaximumMana);
            baseAttackPower = Mathf.Max(0f, baseAttackPower);
            baseDefense = Mathf.Max(0f, baseDefense);
        }

        private void OnEquipmentChanged()
        {
            Recalculate();
            StatsChanged?.Invoke();
        }

        private void Recalculate()
        {
            equipmentModifiers = default;

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

                equipmentModifiers += slot.Item.Equipment.StatModifiers;
            }
        }
    }
}
