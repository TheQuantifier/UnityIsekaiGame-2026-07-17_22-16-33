using System;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotState
    {
        [SerializeField] private EquipmentSlotType slotType;
        [SerializeField] private ItemDefinition item;
        [NonSerialized] private ItemInstance itemInstance;

        public EquipmentSlotType SlotType => slotType;
        public ItemDefinition Item => itemInstance != null ? itemInstance.Definition as ItemDefinition : item;
        public ItemInstance ItemInstance => itemInstance;
        public bool IsStateful => itemInstance != null;
        public bool IsEmpty => Item == null;

        internal void Initialize(EquipmentSlotType type)
        {
            slotType = type;
        }

        internal void SetItem(ItemDefinition newItem)
        {
            itemInstance = null;
            item = newItem;
        }

        internal void SetInstance(ItemInstance newItemInstance)
        {
            if (newItemInstance == null || newItemInstance.Definition is not ItemDefinition)
            {
                Clear();
                return;
            }

            itemInstance = newItemInstance;
            item = null;
        }

        internal void Clear()
        {
            itemInstance = null;
            item = null;
        }
    }
}
