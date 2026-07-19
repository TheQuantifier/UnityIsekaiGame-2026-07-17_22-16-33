using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;
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

            ItemInstance replacedInstance = equipmentSlot.ItemInstance;
            ItemDefinition replacedItem = equipmentSlot.Item;
            if (!CanReturnEquippedItemToInventory(replacedItem, replacedInstance, inventorySlotIndex))
            {
                return EquipmentOperationResult.Failure($"No inventory room to unequip {replacedItem.DisplayName}.");
            }

            if (!inventory.TryExtractSlotItem(inventorySlotIndex, out ItemDefinition extractedItem, out ItemInstance extractedInstance, out string extractFailureReason))
            {
                return EquipmentOperationResult.Failure(string.IsNullOrWhiteSpace(extractFailureReason) ? $"Could not remove {item.DisplayName} from inventory." : extractFailureReason);
            }

            if (!TryReturnEquippedItemToInventory(replacedItem, replacedInstance))
            {
                RestoreExtractedInventoryItem(extractedItem, extractedInstance);
                return EquipmentOperationResult.Failure($"No inventory room to unequip {replacedItem.DisplayName}.");
            }

            if (extractedInstance != null)
            {
                equipmentSlot.SetInstance(extractedInstance);
            }
            else
            {
                equipmentSlot.SetItem(extractedItem);
            }

            EquipmentChanged?.Invoke();

            string message = replacedItem == null
                ? $"Equipped {extractedItem.DisplayName}."
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

            ItemInstance itemInstance = slot.ItemInstance;
            ItemDefinition item = slot.Item;
            if (itemInstance != null)
            {
                if (!inventory.CanAddItemInstance(itemInstance))
                {
                    return EquipmentOperationResult.Failure($"No inventory room to unequip {item.DisplayName}.");
                }

                InventoryInstanceOperationResult instanceResult = inventory.AddItemInstance(itemInstance);
                if (!instanceResult.Succeeded)
                {
                    return EquipmentOperationResult.Failure($"No inventory room to unequip {item.DisplayName}.");
                }

                slot.Clear();
                EquipmentChanged?.Invoke();

                string instanceMessage = $"Unequipped {item.DisplayName}.";
                Debug.Log(instanceMessage);
                return EquipmentOperationResult.Success(instanceMessage);
            }

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

        public EquipmentSaveData CreateSaveData()
        {
            EnsureSlots();

            EquipmentSaveData saveData = new EquipmentSaveData();
            foreach (EquipmentSlotState slot in slots)
            {
                EquipmentSlotSaveData entry = new EquipmentSlotSaveData
                {
                    slotType = slot.SlotType
                };

                if (slot.IsEmpty)
                {
                    entry.mode = EquipmentEntrySaveMode.Empty;
                }
                else if (slot.IsStateful)
                {
                    entry.mode = EquipmentEntrySaveMode.StatefulInstance;
                    entry.itemInstance = ItemInstanceSerializationUtility.CreateSaveData(slot.ItemInstance);
                }
                else
                {
                    entry.mode = EquipmentEntrySaveMode.DefinitionOnly;
                    entry.definitionId = slot.Item.ItemId;
                }

                saveData.slots.Add(entry);
            }

            return saveData;
        }

        public EquipmentRestoreResult TryRestoreFromSaveData(EquipmentSaveData saveData, DefinitionRegistry registry)
        {
            if (saveData == null)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.MissingSaveData, "Equipment save data is missing.");
            }

            Dictionary<EquipmentSlotType, EquipmentSlotState> restoredBySlot = CreateEmptySlotMap();
            HashSet<EquipmentSlotType> restoredSlots = new HashSet<EquipmentSlotType>();
            HashSet<string> instanceIds = new HashSet<string>();
            IReadOnlyList<EquipmentSlotSaveData> savedSlots = saveData.slots;
            if (savedSlots == null)
            {
                savedSlots = Array.Empty<EquipmentSlotSaveData>();
            }

            for (int i = 0; i < savedSlots.Count; i++)
            {
                EquipmentSlotSaveData entry = savedSlots[i];
                if (entry == null)
                {
                    continue;
                }

                if (!restoredSlots.Add(entry.slotType))
                {
                    return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.DuplicateSlot, $"Equipment save data contains duplicate {entry.slotType} slots.");
                }

                if (!restoredBySlot.TryGetValue(entry.slotType, out EquipmentSlotState restoredSlot))
                {
                    return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.WrongSlotType, $"Equipment slot '{entry.slotType}' is not supported.");
                }

                EquipmentRestoreResult entryResult = TryApplyRestoredSlot(entry, registry, instanceIds, restoredSlot);
                if (!entryResult.Succeeded)
                {
                    return entryResult;
                }
            }

            slots = new List<EquipmentSlotState>(restoredBySlot.Count);
            Array values = Enum.GetValues(typeof(EquipmentSlotType));
            for (int i = 0; i < values.Length; i++)
            {
                slots.Add(restoredBySlot[(EquipmentSlotType)values.GetValue(i)]);
            }

            EquipmentChanged?.Invoke();
            return EquipmentRestoreResult.Success();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DevelopmentClearEquipment()
        {
            EnsureSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i]?.Clear();
            }

            EquipmentChanged?.Invoke();
        }
#endif

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

        private bool CanReturnEquippedItemToInventory(ItemDefinition item, ItemInstance itemInstance, int removingInventorySlotIndex)
        {
            if (item == null)
            {
                return true;
            }

            if (itemInstance != null)
            {
                return inventory.CanAddItemInstanceAfterRemovingFromSlot(itemInstance, removingInventorySlotIndex);
            }

            return inventory.CanAddItemAfterRemovingFromSlot(item, 1, removingInventorySlotIndex, 1);
        }

        private bool TryReturnEquippedItemToInventory(ItemDefinition item, ItemInstance itemInstance)
        {
            if (item == null)
            {
                return true;
            }

            if (itemInstance != null)
            {
                return inventory.AddItemInstance(itemInstance).Succeeded;
            }

            return inventory.AddItem(item, 1).AddedAll;
        }

        private void RestoreExtractedInventoryItem(ItemDefinition item, ItemInstance itemInstance)
        {
            if (itemInstance != null)
            {
                inventory.AddItemInstance(itemInstance);
                return;
            }

            inventory.AddItem(item, 1);
        }

        private static Dictionary<EquipmentSlotType, EquipmentSlotState> CreateEmptySlotMap()
        {
            Dictionary<EquipmentSlotType, EquipmentSlotState> slotMap = new Dictionary<EquipmentSlotType, EquipmentSlotState>();
            Array values = Enum.GetValues(typeof(EquipmentSlotType));
            for (int i = 0; i < values.Length; i++)
            {
                EquipmentSlotType slotType = (EquipmentSlotType)values.GetValue(i);
                EquipmentSlotState slot = new EquipmentSlotState();
                slot.Initialize(slotType);
                slotMap.Add(slotType, slot);
            }

            return slotMap;
        }

        private static EquipmentRestoreResult TryApplyRestoredSlot(
            EquipmentSlotSaveData entry,
            DefinitionRegistry registry,
            HashSet<string> instanceIds,
            EquipmentSlotState restoredSlot)
        {
            if (entry.mode == EquipmentEntrySaveMode.Empty)
            {
                return EquipmentRestoreResult.Success();
            }

            if (entry.mode == EquipmentEntrySaveMode.DefinitionOnly)
            {
                return TryApplyRestoredDefinitionItem(entry, registry, restoredSlot);
            }

            if (entry.itemInstance == null)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.InvalidItemInstance, "Equipment stateful item entry has no item instance save data.");
            }

            ItemInstanceRestoreResult instanceResult = ItemInstanceSerializationUtility.Restore(entry.itemInstance, registry);
            if (!instanceResult.Succeeded)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.InvalidItemInstance, instanceResult.Message);
            }

            if (instanceResult.ItemInstance.Definition is not ItemDefinition item)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.WrongDefinitionType, $"Definition '{instanceResult.ItemInstance.DefinitionId}' is not an ItemDefinition asset.");
            }

            EquipmentRestoreResult compatibilityResult = ValidateSlotCompatibility(item, entry.slotType);
            if (!compatibilityResult.Succeeded)
            {
                return compatibilityResult;
            }

            if (instanceResult.ItemInstance.RequiresPersistentIdentity && !instanceResult.ItemInstance.HasPersistentIdentity)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.InvalidItemInstance, $"Item instance '{instanceResult.ItemInstance.DefinitionId}' requires a persistent instance ID.");
            }

            if (instanceResult.ItemInstance.HasPersistentIdentity && !instanceIds.Add(instanceResult.ItemInstance.InstanceId))
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.DuplicateInstanceId, $"Duplicate item instance ID '{instanceResult.ItemInstance.InstanceId}' found in equipment save data.");
            }

            restoredSlot.SetInstance(instanceResult.ItemInstance);
            return EquipmentRestoreResult.Success();
        }

        private static EquipmentRestoreResult TryApplyRestoredDefinitionItem(
            EquipmentSlotSaveData entry,
            DefinitionRegistry registry,
            EquipmentSlotState restoredSlot)
        {
            if (string.IsNullOrWhiteSpace(entry.definitionId))
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.MissingDefinitionId, "Equipment entry has no definition ID.");
            }

            if (registry == null || !registry.TryGet(entry.definitionId, out ItemDefinition item))
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.MissingItemDefinition, $"Item definition '{entry.definitionId}' was not found.");
            }

            EquipmentRestoreResult compatibilityResult = ValidateSlotCompatibility(item, entry.slotType);
            if (!compatibilityResult.Succeeded)
            {
                return compatibilityResult;
            }

            restoredSlot.SetItem(item);
            return EquipmentRestoreResult.Success();
        }

        private static EquipmentRestoreResult ValidateSlotCompatibility(ItemDefinition item, EquipmentSlotType slotType)
        {
            if (item == null || !item.IsEquippable)
            {
                string itemName = item == null ? "Item" : item.DisplayName;
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.WrongDefinitionType, $"{itemName} cannot be equipped.");
            }

            if (item.Equipment.SlotType != slotType)
            {
                return EquipmentRestoreResult.Failure(EquipmentRestoreStatus.WrongSlotType, $"{item.DisplayName} belongs in {FormatSlotName(item.Equipment.SlotType)}, not {FormatSlotName(slotType)}.");
            }

            return EquipmentRestoreResult.Success();
        }
    }
}
