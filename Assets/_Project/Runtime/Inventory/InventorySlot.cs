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
        [SerializeField] private string itemInstanceId;

        public ItemDefinition Item => item;
        public int Quantity => Mode == InventorySlotMode.StatefulInstance ? 1 : quantity;
        public string ItemInstanceId => itemInstanceId ?? string.Empty;
        public bool HasItemIdentity => !string.IsNullOrWhiteSpace(ItemInstanceId);
        public InventorySlotMode Mode
        {
            get
            {
                if (item == null || quantity <= 0)
                {
                    return InventorySlotMode.Empty;
                }

                if (!item.Stackable && HasItemIdentity)
                {
                    return InventorySlotMode.StatefulInstance;
                }

                return InventorySlotMode.DefinitionStack;
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
            SetIdentity(newItem, string.Empty, newQuantity);
        }

        internal void SetIdentity(ItemDefinition newItem, string newItemInstanceId, int newQuantity)
        {
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);
            itemInstanceId = newItemInstanceId ?? string.Empty;

            if (quantity == 0)
            {
                item = null;
                itemInstanceId = string.Empty;
            }
        }

        internal void Clear()
        {
            item = null;
            quantity = 0;
            itemInstanceId = string.Empty;
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
