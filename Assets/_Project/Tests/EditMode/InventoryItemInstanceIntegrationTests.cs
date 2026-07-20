using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class InventoryItemInstanceIntegrationTests
    {
        private const string SwordInstanceId = "11111111-1111-4111-8111-111111111111";

        [Test]
        public void AddItemOrInstances_CreatesSeparatePersistentEntriesForAlwaysInstancedItems()
        {
            ScriptableObject sword = CreateItem("item.prototype-sword", "Prototype Sword", ItemInstanceMode.AlwaysInstanced, false, 1, true);
            Component inventory = CreateInventory(4);

            object result = Invoke(inventory, "AddItemOrInstances", sword, 2);

            Assert.That(Get<bool>(result, "AddedAll"), Is.True);
            Assert.That(Get<bool>(GetSlot(inventory, 0), "IsStateful"), Is.True);
            Assert.That(Get<bool>(GetSlot(inventory, 1), "IsStateful"), Is.True);
            Assert.That(Get<string>(Get<object>(GetSlot(inventory, 0), "ItemInstance"), "InstanceId"), Is.Not.EqualTo(Get<string>(Get<object>(GetSlot(inventory, 1), "ItemInstance"), "InstanceId")));
            Assert.That(ItemInstanceId.IsValid(Get<string>(Get<object>(GetSlot(inventory, 0), "ItemInstance"), "InstanceId")), Is.True);

            UnityEngine.Object.DestroyImmediate(inventory.gameObject);
        }

        [Test]
        public void EquipAndUnequip_PreservesItemInstanceIdentity()
        {
            ScriptableObject sword = CreateItem("item.prototype-sword", "Prototype Sword", ItemInstanceMode.AlwaysInstanced, false, 1, true);
            Component inventory = CreateInventory(4);
            Component equipment = inventory.gameObject.AddComponent(RequiredType("UnityIsekaiGame.Equipment.PlayerEquipment"));
            AssignObject(equipment, "inventory", inventory);

            ItemInstance swordInstance = ItemInstance.CreateStateful((IInventoryItemDefinition)sword, ItemInstanceMetadata.WithCondition(0.73f), SwordInstanceId);
            Invoke(inventory, "AddItemInstance", swordInstance);

            object equipResult = Invoke(equipment, "EquipFromInventorySlot", 0);
            object mainHand = Invoke(equipment, "GetSlot", MainHandValue());

            Assert.That(Get<bool>(equipResult, "Succeeded"), Is.True);
            Assert.That(Get<object>(mainHand, "ItemInstance"), Is.SameAs(swordInstance));
            Assert.That(Get<bool>(GetSlot(inventory, 0), "IsEmpty"), Is.True);

            object unequipResult = Invoke(equipment, "Unequip", MainHandValue());

            Assert.That(Get<bool>(unequipResult, "Succeeded"), Is.True);
            Assert.That(Get<object>(GetSlot(inventory, 0), "ItemInstance"), Is.SameAs(swordInstance));
            Assert.That(Get<bool>(mainHand, "IsEmpty"), Is.True);

            UnityEngine.Object.DestroyImmediate(inventory.gameObject);
        }

        [Test]
        public void InventorySaveRestore_RestoresStacksAndStatefulInstances()
        {
            ScriptableObject potion = CreateItem("item.health-potion", "Health Potion", ItemInstanceMode.DefinitionOnly, true, 10);
            ScriptableObject sword = CreateItem("item.prototype-sword", "Prototype Sword", ItemInstanceMode.AlwaysInstanced, false, 1, true);
            Component inventory = CreateInventory(4);
            Invoke(inventory, "AddItem", potion, 3);
            Invoke(inventory, "AddItemInstance", ItemInstance.CreateStateful((IInventoryItemDefinition)sword, ItemInstanceMetadata.WithCondition(0.5f), SwordInstanceId));
            object saveData = Invoke(inventory, "CreateSaveData");
            DefinitionRegistry registry = new DefinitionRegistry(new IGameDefinition[] { (IGameDefinition)potion, (IGameDefinition)sword });

            Component restored = CreateInventory(1);
            object restoreResult = Invoke(restored, "TryRestoreFromSaveData", saveData, registry);

            Assert.That(Get<bool>(restoreResult, "Succeeded"), Is.True);
            Assert.That(Get<int>(restored, "SlotCapacity"), Is.EqualTo(4));
            Assert.That(Get<object>(GetSlot(restored, 0), "Item"), Is.SameAs(potion));
            Assert.That(Get<int>(GetSlot(restored, 0), "Quantity"), Is.EqualTo(3));
            Assert.That(Get<string>(Get<object>(GetSlot(restored, 1), "ItemInstance"), "InstanceId"), Is.EqualTo(SwordInstanceId));
            Assert.That(Get<float>(Get<object>(Get<object>(GetSlot(restored, 1), "ItemInstance"), "Metadata"), "ConditionNormalized"), Is.EqualTo(0.5f));

            UnityEngine.Object.DestroyImmediate(inventory.gameObject);
            UnityEngine.Object.DestroyImmediate(restored.gameObject);
        }

        [Test]
        public void InventoryRestore_FailureDoesNotChangeExistingSlots()
        {
            ScriptableObject potion = CreateItem("item.health-potion", "Health Potion", ItemInstanceMode.DefinitionOnly, true, 10);
            Component inventory = CreateInventory(2);
            Invoke(inventory, "AddItem", potion, 2);
            object badSave = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Inventory.InventorySaveData"));
            SetField(badSave, "slotCapacity", 2);
            IList entries = (IList)badSave.GetType().GetField("entries").GetValue(badSave);
            object badEntry = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Inventory.InventoryEntrySaveData"));
            SetField(badEntry, "mode", Enum.Parse(RequiredType("UnityIsekaiGame.Inventory.InventoryEntrySaveMode"), "DefinitionStack"));
            SetField(badEntry, "definitionId", "item.missing");
            SetField(badEntry, "quantity", 1);
            entries.Add(badEntry);

            object result = Invoke(inventory, "TryRestoreFromSaveData", badSave, new DefinitionRegistry(new IGameDefinition[] { (IGameDefinition)potion }));

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            Assert.That(Get<object>(GetSlot(inventory, 0), "Item"), Is.SameAs(potion));
            Assert.That(Get<int>(GetSlot(inventory, 0), "Quantity"), Is.EqualTo(2));

            UnityEngine.Object.DestroyImmediate(inventory.gameObject);
        }

        private static Component CreateInventory(int slotCapacity)
        {
            GameObject gameObject = new GameObject("Inventory Test");
            Component inventory = gameObject.AddComponent(RequiredType("UnityIsekaiGame.Inventory.PlayerInventory"));
            AssignInt(inventory, "slotCapacity", slotCapacity);
            return inventory;
        }

        private static ScriptableObject CreateItem(
            string id,
            string displayName,
            ItemInstanceMode instanceMode,
            bool stackable,
            int maximumStackSize,
            bool equippable = false)
        {
            Type itemDefinitionType = RequiredType("UnityIsekaiGame.Inventory.ItemDefinition");
            ScriptableObject item = ScriptableObject.CreateInstance(itemDefinitionType);
            SerializedObject serializedItem = new SerializedObject(item);
            serializedItem.FindProperty("itemId").stringValue = id;
            serializedItem.FindProperty("displayName").stringValue = displayName;
            serializedItem.FindProperty("instanceMode").enumValueIndex = (int)instanceMode;
            serializedItem.FindProperty("stackable").boolValue = stackable;
            serializedItem.FindProperty("maximumStackSize").intValue = maximumStackSize;

            SerializedProperty equipment = serializedItem.FindProperty("equipment");
            equipment.FindPropertyRelative("equippable").boolValue = equippable;
            equipment.FindPropertyRelative("slotType").enumValueIndex = Convert.ToInt32(MainHandValue());

            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static object GetSlot(Component inventory, int slotIndex)
        {
            IReadOnlyList<object> slots = ReadSlots(inventory);
            return slots[slotIndex];
        }

        private static IReadOnlyList<object> ReadSlots(Component inventory)
        {
            List<object> slots = new List<object>();
            foreach (object slot in (IEnumerable)Get<object>(inventory, "Slots"))
            {
                slots.Add(slot);
            }

            return slots;
        }

        private static object MainHandValue()
        {
            return Enum.Parse(RequiredType("UnityIsekaiGame.Equipment.EquipmentSlotType"), "MainHand");
        }

        private static Type RequiredType(string fullName)
        {
            Type type = TestTypeResolver.RequiredType(fullName);
            Assert.That(type, Is.Not.Null, $"Expected runtime type {fullName} to exist in loaded project assemblies.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName).Invoke(target, args);
        }

        private static T Get<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName).GetValue(target);
        }

        private static void AssignInt(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName).SetValue(target, value);
        }
    }
}
