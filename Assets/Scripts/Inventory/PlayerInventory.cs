using System;
using System.Collections.Generic;
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

        public InventorySlot GetSlot(int slotIndex)
        {
            EnsureSlotCapacity();
            return slotIndex >= 0 && slotIndex < slots.Count ? slots[slotIndex] : null;
        }

        public bool CanAddItem(ItemDefinition item, int quantity)
        {
            return GetAddableQuantity(item, quantity) >= quantity;
        }

        public bool CanAddItemAfterRemovingFromSlot(ItemDefinition item, int quantity, int removeSlotIndex, int removeQuantity)
        {
            return GetAddableQuantity(item, quantity, removeSlotIndex, removeQuantity) >= quantity;
        }

        public bool RemoveItemAt(int slotIndex, int quantity)
        {
            EnsureSlotCapacity();

            if (slotIndex < 0 || slotIndex >= slots.Count || quantity <= 0)
            {
                return false;
            }

            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.IsEmpty || slot.Quantity < quantity)
            {
                return false;
            }

            bool removed = slot.Remove(quantity);
            if (removed)
            {
                InventoryChanged?.Invoke();
            }

            return removed;
        }

        public ItemUseResult UseItem(int slotIndex, GameObject user)
        {
            EnsureSlotCapacity();

            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return ItemUseResult.Failure("No inventory slot selected.");
            }

            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.IsEmpty)
            {
                return ItemUseResult.Failure("Selected slot is empty.");
            }

            ItemDefinition item = slot.Item;
            if (item == null || !item.IsUsable)
            {
                string itemName = item == null ? "Item" : item.DisplayName;
                return ItemUseResult.Failure($"{itemName} cannot be used.");
            }

            ItemUseContext context = new ItemUseContext(user, this, slotIndex, item);
            IReadOnlyList<ItemUseEffect> effects = item.UseEffects;

            for (int i = 0; i < effects.Count; i++)
            {
                ItemUseEffect effect = effects[i];
                if (effect == null)
                {
                    return ItemUseResult.Failure($"{item.DisplayName} has a missing use effect.");
                }

                if (!effect.CanUse(in context, out string failureReason))
                {
                    return ItemUseResult.Failure(string.IsNullOrWhiteSpace(failureReason) ? $"{item.DisplayName} cannot be used right now." : failureReason);
                }
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i].Apply(in context);
            }

            slot.Remove(1);
            InventoryChanged?.Invoke();

            string message = $"Used {item.DisplayName}.";
            Debug.Log(message);
            return ItemUseResult.Success(message);
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

        private int GetAddableQuantity(ItemDefinition item, int quantity, int removeSlotIndex = -1, int removeQuantity = 0)
        {
            if (item == null || quantity <= 0)
            {
                return 0;
            }

            EnsureSlotCapacity();

            int remainingQuantity = quantity;
            bool createsEmptySlot = false;

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    createsEmptySlot = true;
                    continue;
                }

                int simulatedQuantity = slot.Quantity;
                ItemDefinition simulatedItem = slot.Item;

                if (i == removeSlotIndex)
                {
                    simulatedQuantity = Mathf.Max(0, simulatedQuantity - removeQuantity);
                    if (simulatedQuantity == 0)
                    {
                        simulatedItem = null;
                        createsEmptySlot = true;
                    }
                }

                if (simulatedItem == null)
                {
                    continue;
                }

                if (item.Stackable && simulatedItem == item)
                {
                    remainingQuantity -= Mathf.Min(remainingQuantity, Mathf.Max(0, item.MaximumStackSize - simulatedQuantity));
                }

                if (remainingQuantity <= 0)
                {
                    return quantity;
                }
            }

            if (!createsEmptySlot)
            {
                return quantity - remainingQuantity;
            }

            foreach (InventorySlot slot in slots)
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                if (slot != null && !slot.IsEmpty)
                {
                    continue;
                }

                remainingQuantity -= Mathf.Min(remainingQuantity, item.MaximumStackSize);
            }

            if (remainingQuantity > 0 && removeSlotIndex >= 0 && removeSlotIndex < slots.Count)
            {
                InventorySlot removedFromSlot = slots[removeSlotIndex];
                if (removedFromSlot != null && removedFromSlot.Quantity <= removeQuantity)
                {
                    remainingQuantity -= Mathf.Min(remainingQuantity, item.MaximumStackSize);
                }
            }

            return quantity - Mathf.Max(0, remainingQuantity);
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
