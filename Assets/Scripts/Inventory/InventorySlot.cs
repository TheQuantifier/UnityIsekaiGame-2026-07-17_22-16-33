using System;
using UnityEngine;

namespace UnityIsekaiGame.Inventory
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public int AvailableStackSpace
        {
            get
            {
                if (IsEmpty || item == null)
                {
                    return 0;
                }

                return Mathf.Max(0, item.MaximumStackSize - quantity);
            }
        }

        public bool CanStack(ItemDefinition candidate)
        {
            return !IsEmpty && item == candidate && item.Stackable && AvailableStackSpace > 0;
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
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);

            if (quantity == 0)
            {
                item = null;
            }
        }

        internal void Clear()
        {
            item = null;
            quantity = 0;
        }

        internal bool Remove(int amount)
        {
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
