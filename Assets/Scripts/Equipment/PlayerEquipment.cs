using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Equipment
{
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private List<EquipmentSlotState> slots = new List<EquipmentSlotState>();

        public IReadOnlyList<EquipmentSlotState> Slots => slots;
        public event Action EquipmentChanged;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }

            EnsureSlots();
        }

        private void OnValidate()
        {
            EnsureSlots();
        }

        public EquipmentOperationResult EquipFromInventorySlot(int inventorySlotIndex)
        {
            if (inventory == null)
            {
                return EquipmentOperationResult.Failure("No inventory is assigned.");
            }

            InventorySlot inventorySlot = inventory.GetSlot(inventorySlotIndex);
            if (inventorySlot == null || inventorySlot.IsEmpty)
            {
                return EquipmentOperationResult.Failure("Selected inventory slot is empty.");
            }

            ItemDefinition item = inventorySlot.Item;
            if (item == null || !item.IsEquippable)
            {
                string itemName = item == null ? "Item" : item.DisplayName;
                return EquipmentOperationResult.Failure($"{itemName} cannot be equipped.");
            }

            EquipmentSlotState equipmentSlot = GetSlot(item.Equipment.SlotType);
            if (equipmentSlot == null)
            {
                return EquipmentOperationResult.Failure("Equipment slot is not supported.");
            }

            ItemDefinition replacedItem = equipmentSlot.Item;
            if (replacedItem != null && !inventory.CanAddItemAfterRemovingFromSlot(replacedItem, 1, inventorySlotIndex, 1))
            {
                return EquipmentOperationResult.Failure($"No inventory room to unequip {replacedItem.DisplayName}.");
            }

            if (!inventory.RemoveItemAt(inventorySlotIndex, 1))
            {
                return EquipmentOperationResult.Failure($"Could not remove {item.DisplayName} from inventory.");
            }

            if (replacedItem != null)
            {
                InventoryAddResult addResult = inventory.AddItem(replacedItem, 1);
                if (!addResult.AddedAll)
                {
                    inventory.AddItem(item, 1);
                    return EquipmentOperationResult.Failure($"No inventory room to unequip {replacedItem.DisplayName}.");
                }
            }

            equipmentSlot.SetItem(item);
            EquipmentChanged?.Invoke();

            string message = replacedItem == null
                ? $"Equipped {item.DisplayName}."
                : $"Equipped {item.DisplayName} and unequipped {replacedItem.DisplayName}.";
            Debug.Log(message);
            return EquipmentOperationResult.Success(message);
        }

        public EquipmentOperationResult Unequip(EquipmentSlotType slotType)
        {
            if (inventory == null)
            {
                return EquipmentOperationResult.Failure("No inventory is assigned.");
            }

            EquipmentSlotState slot = GetSlot(slotType);
            if (slot == null || slot.IsEmpty)
            {
                return EquipmentOperationResult.Failure($"{FormatSlotName(slotType)} is empty.");
            }

            ItemDefinition item = slot.Item;
            if (!inventory.CanAddItem(item, 1))
            {
                return EquipmentOperationResult.Failure($"No inventory room to unequip {item.DisplayName}.");
            }

            InventoryAddResult result = inventory.AddItem(item, 1);
            if (!result.AddedAll)
            {
                return EquipmentOperationResult.Failure($"No inventory room to unequip {item.DisplayName}.");
            }

            slot.Clear();
            EquipmentChanged?.Invoke();

            string message = $"Unequipped {item.DisplayName}.";
            Debug.Log(message);
            return EquipmentOperationResult.Success(message);
        }

        public EquipmentSlotState GetSlot(EquipmentSlotType slotType)
        {
            EnsureSlots();

            foreach (EquipmentSlotState slot in slots)
            {
                if (slot.SlotType == slotType)
                {
                    return slot;
                }
            }

            return null;
        }

        private void EnsureSlots()
        {
            slots ??= new List<EquipmentSlotState>();

            Array values = Enum.GetValues(typeof(EquipmentSlotType));
            while (slots.Count < values.Length)
            {
                slots.Add(new EquipmentSlotState());
            }

            if (slots.Count > values.Length)
            {
                slots.RemoveRange(values.Length, slots.Count - values.Length);
            }

            for (int i = 0; i < values.Length; i++)
            {
                slots[i].Initialize((EquipmentSlotType)values.GetValue(i));
            }
        }

        private static string FormatSlotName(EquipmentSlotType slotType)
        {
            return slotType switch
            {
                EquipmentSlotType.MainHand => "Main Hand",
                EquipmentSlotType.OffHand => "Off Hand",
                _ => slotType.ToString()
            };
        }
    }
}
