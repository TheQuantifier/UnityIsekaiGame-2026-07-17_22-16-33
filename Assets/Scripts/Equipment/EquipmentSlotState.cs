using System;
using UnityEngine;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotState
    {
        [SerializeField] private EquipmentSlotType slotType;
        [SerializeField] private ItemDefinition item;

        public EquipmentSlotType SlotType => slotType;
        public ItemDefinition Item => item;
        public bool IsEmpty => item == null;

        internal void Initialize(EquipmentSlotType type)
        {
            slotType = type;
        }

        internal void SetItem(ItemDefinition newItem)
        {
            item = newItem;
        }

        internal void Clear()
        {
            item = null;
        }
    }
}
