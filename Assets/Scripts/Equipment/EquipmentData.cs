using System;
using UnityEngine;

namespace UnityIsekaiGame.Equipment
{
    [Serializable]
    public sealed class EquipmentData
    {
        [SerializeField] private bool equippable;
        [SerializeField] private EquipmentSlotType slotType;
        [SerializeField] private StatModifiers statModifiers;

        public bool Equippable => equippable;
        public EquipmentSlotType SlotType => slotType;
        public StatModifiers StatModifiers => statModifiers;
    }
}
