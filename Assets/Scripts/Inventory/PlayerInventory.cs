using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnityIsekaiGame.Inventory
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int slotCapacity = 16;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SlotCapacity => slotCapacity;
        public event Action InventoryChanged;

        private void Awake()
        {
            EnsureSlotCapacity();
        }

        private void OnValidate()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            EnsureSlotCapacity();
        }

        public InventoryAddResult AddItem(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return new InventoryAddResult(InventoryAddStatus.None, Mathf.Max(0, quantity), 0);
            }

            EnsureSlotCapacity();

            int requestedQuantity = quantity;
            int remainingQuantity = quantity;

            if (item.Stackable)
            {
                remainingQuantity = AddToExistingStacks(item, remainingQuantity);
            }

            remainingQuantity = AddToNewStacks(item, remainingQuantity);

            int addedQuantity = requestedQuantity - remainingQuantity;
            InventoryAddStatus status = GetAddStatus(requestedQuantity, addedQuantity);

            LogAddResult(item, new InventoryAddResult(status, requestedQuantity, addedQuantity));

            if (addedQuantity > 0)
            {
                InventoryChanged?.Invoke();
            }

            return new InventoryAddResult(status, requestedQuantity, addedQuantity);
        }

        private int AddToExistingStacks(ItemDefinition item, int remainingQuantity)
        {
            foreach (InventorySlot slot in slots)
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                if (!slot.CanStack(item))
                {
                    continue;
                }

                remainingQuantity -= slot.AddToStack(remainingQuantity);
            }

            return remainingQuantity;
        }

        private int AddToNewStacks(ItemDefinition item, int remainingQuantity)
        {
            foreach (InventorySlot slot in slots)
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                if (!slot.IsEmpty)
                {
                    continue;
                }

                int quantityForSlot = Mathf.Min(remainingQuantity, item.MaximumStackSize);
                slot.Set(item, quantityForSlot);
                remainingQuantity -= quantityForSlot;
            }

            return remainingQuantity;
        }

        private void EnsureSlotCapacity()
        {
            slots ??= new List<InventorySlot>();

            while (slots.Count < slotCapacity)
            {
                slots.Add(new InventorySlot());
            }

            if (slots.Count > slotCapacity)
            {
                slots.RemoveRange(slotCapacity, slots.Count - slotCapacity);
            }
        }

        private static InventoryAddStatus GetAddStatus(int requestedQuantity, int addedQuantity)
        {
            if (addedQuantity <= 0)
            {
                return InventoryAddStatus.None;
            }

            return addedQuantity >= requestedQuantity ? InventoryAddStatus.All : InventoryAddStatus.Partial;
        }

        private static void LogAddResult(ItemDefinition item, InventoryAddResult result)
        {
            if (result.Status == InventoryAddStatus.None)
            {
                Debug.Log($"Inventory full. Could not add {result.RequestedQuantity} x {item.ItemId}.");
                return;
            }

            Debug.Log($"Item added: {item.ItemId}. Quantity added: {result.AddedQuantity}.");

            if (result.Status == InventoryAddStatus.Partial)
            {
                Debug.Log($"Partial pickup: {result.RemainingQuantity} x {item.ItemId} could not fit.");
            }
        }
    }
}
