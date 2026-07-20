using System;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Inventory
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int quantity;
        [NonSerialized] private ItemInstance itemInstance;

        public ItemDefinition Item => itemInstance != null ? itemInstance.Definition as ItemDefinition : item;
        public int Quantity => Mode == InventorySlotMode.StatefulInstance ? 1 : quantity;
        public ItemInstance ItemInstance => itemInstance;
        public InventorySlotMode Mode
        {
            get
            {
                if (itemInstance != null)
                {
                    return InventorySlotMode.StatefulInstance;
                }

                return item == null || quantity <= 0 ? InventorySlotMode.Empty : InventorySlotMode.DefinitionStack;
            }
        }
        public bool IsStateful => Mode == InventorySlotMode.StatefulInstance;
        public bool IsEmpty => Mode == InventorySlotMode.Empty;

        public int AvailableStackSpace
        {
            get
            {
                if (Mode != InventorySlotMode.DefinitionStack || item == null)
                {
                    return 0;
                }

                return Mathf.Max(0, item.MaximumStackSize - quantity);
            }
        }

        public bool CanStack(ItemDefinition candidate)
        {
            return Mode == InventorySlotMode.DefinitionStack && item == candidate && item.Stackable && AvailableStackSpace > 0;
        }

        internal int AddToStack(int amount)
        {
            if (item == null || amount <= 0)
            {
                return 0;
            }

            int added = Mathf.Min(amount, AvailableStackSpace);
            quantity += added;
            return added;
        }

        internal void Set(ItemDefinition newItem, int newQuantity)
        {
            itemInstance = null;
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);

            if (quantity == 0)
            {
                item = null;
            }
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
            quantity = 0;
        }

        internal void Clear()
        {
            itemInstance = null;
            item = null;
            quantity = 0;
        }

        internal bool Remove(int amount)
        {
            if (Mode == InventorySlotMode.StatefulInstance)
            {
                if (amount != 1)
                {
                    return false;
                }

                Clear();
                return true;
            }

            if (amount <= 0 || IsEmpty)
            {
                return false;
            }

            quantity = Mathf.Max(0, quantity - amount);
            if (quantity == 0)
            {
                item = null;
            }

            return true;
        }
    }
}
