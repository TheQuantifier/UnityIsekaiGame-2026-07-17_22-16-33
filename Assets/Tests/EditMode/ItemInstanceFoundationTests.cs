using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class ItemInstanceFoundationTests
    {
        private const string DeterministicInstanceId = "11111111-2222-3333-4444-555555555555";

        [Test]
        public void DefinitionOnlyInstanceCreation_UsesDefinitionWithoutPersistentId()
        {
            TestItemDefinition potion = CreateItem("item.health-potion", stackable: true);

            ItemInstanceCreationResult result = ItemInstanceFactory.CreateDefinitionOnly(potion);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ItemInstance.Definition, Is.SameAs(potion));
            Assert.That(result.ItemInstance.HasPersistentIdentity, Is.False);
            Assert.That(result.ItemInstance.HasMetadata, Is.False);
        }

        [Test]
        public void StatefulInstanceCreation_GeneratesUniquePersistentId()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false, mode: ItemInstanceMode.OptionalInstance);
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);

            ItemInstanceCreationResult first = ItemInstanceFactory.CreateStateful(sword, ItemInstanceMetadata.WithQuality(fine));
            ItemInstanceCreationResult second = ItemInstanceFactory.CreateStateful(sword, ItemInstanceMetadata.WithQuality(fine));

            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(ItemInstanceId.IsValid(first.ItemInstance.InstanceId), Is.True);
            Assert.That(first.ItemInstance.InstanceId, Is.Not.EqualTo(second.ItemInstance.InstanceId));
        }

        [Test]
        public void StatefulInstanceCreation_AcceptsDeterministicSuppliedId()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false, mode: ItemInstanceMode.OptionalInstance);

            ItemInstanceCreationResult result = ItemInstanceFactory.CreateStateful(sword, ItemInstanceMetadata.WithCondition(0.72f), DeterministicInstanceId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ItemInstance.InstanceId, Is.EqualTo(DeterministicInstanceId));
        }

        [Test]
        public void InstanceCloningWithNewIdentity_CopiesMetadataWithoutSharingIdentity()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false, mode: ItemInstanceMode.OptionalInstance);
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);
            ItemInstance original = ItemInstance.CreateStateful(sword, ItemInstanceMetadata.WithQualityAndCondition(fine, 0.72f), DeterministicInstanceId);

            ItemInstance clone = original.CloneWithNewIdentity();

            Assert.That(clone.Definition, Is.SameAs(original.Definition));
            Assert.That(clone.InstanceId, Is.Not.EqualTo(original.InstanceId));
            Assert.That(clone.Metadata.Quality, Is.SameAs(fine));
            Assert.That(clone.Metadata.ConditionNormalized, Is.EqualTo(0.72f));
        }

        [Test]
        public void MetadataMutation_DoesNotMutateDefinitionAsset()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            ItemInstance instance = ItemInstance.CreateDefinitionOnly(sword);

            instance.SetCondition(0.25f);

            Assert.That(instance.Metadata.HasCondition, Is.True);
            Assert.That(sword.Id, Is.EqualTo("item.prototype-sword"));
            Assert.That(sword.Stackable, Is.False);
        }

        [Test]
        public void SaveDataCreation_StoresStableIdsAndPresenceFlags()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);
            ItemInstance instance = ItemInstance.CreateStateful(sword, ItemInstanceMetadata.WithQualityAndCondition(fine, 0.72f), DeterministicInstanceId);

            ItemInstanceSerializationResult result = ItemInstanceSerializationUtility.TryCreateSaveData(instance);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.SaveData.definitionId, Is.EqualTo("item.prototype-sword"));
            Assert.That(result.SaveData.instanceId, Is.EqualTo(DeterministicInstanceId));
            Assert.That(result.SaveData.hasQuality, Is.True);
            Assert.That(result.SaveData.qualityId, Is.EqualTo("quality.fine"));
            Assert.That(result.SaveData.hasCondition, Is.True);
            Assert.That(result.SaveData.conditionNormalized, Is.EqualTo(0.72f));
        }

        [Test]
        public void Restoration_PreservesIdentityAndMetadata()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(sword, fine).CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("item.prototype-sword", DeterministicInstanceId, "quality.fine", 0.72f);

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ItemInstance.Definition, Is.SameAs(sword));
            Assert.That(result.ItemInstance.InstanceId, Is.EqualTo(DeterministicInstanceId));
            Assert.That(result.ItemInstance.Metadata.Quality, Is.SameAs(fine));
            Assert.That(result.ItemInstance.Metadata.ConditionNormalized, Is.EqualTo(0.72f));
        }

        [Test]
        public void Restoration_FailsForMissingItemDefinition()
        {
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog().CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("item.missing", DeterministicInstanceId);

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(ItemInstanceRestoreStatus.MissingItemDefinition));
        }

        [Test]
        public void Restoration_FailsForWrongDefinitionType()
        {
            RarityDefinition rarity = ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(rarity).CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("rarity.common", DeterministicInstanceId);

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(ItemInstanceRestoreStatus.WrongDefinitionType));
        }

        [Test]
        public void Restoration_FailsForMissingQualityDefinition()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(sword).CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("item.prototype-sword", DeterministicInstanceId, "quality.missing");

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(ItemInstanceRestoreStatus.MissingQualityDefinition));
        }

        [Test]
        public void Restoration_FailsForInvalidConditionData()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(sword).CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("item.prototype-sword", DeterministicInstanceId, conditionNormalized: 2f);

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(ItemInstanceRestoreStatus.InvalidConditionValue));
        }

        [Test]
        public void Restoration_FailsForMalformedInstanceId()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(sword).CreateRegistry();
            ItemInstanceSaveData saveData = CreateSaveData("item.prototype-sword", "not-a-guid");

            ItemInstanceRestoreResult result = ItemInstanceSerializationUtility.Restore(saveData, registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(ItemInstanceRestoreStatus.InvalidInstanceId));
        }

        [Test]
        public void StackCompatibility_AllowsSameDefinitionDefinitionOnlyInstances()
        {
            TestItemDefinition potion = CreateItem("item.health-potion", stackable: true);

            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                ItemInstance.CreateDefinitionOnly(potion),
                ItemInstance.CreateDefinitionOnly(potion)), Is.True);
        }

        [Test]
        public void StackCompatibility_RejectsDifferentDefinitions()
        {
            TestItemDefinition potion = CreateItem("item.health-potion", stackable: true);
            TestItemDefinition ore = CreateItem("item.prototype-iron-ore", stackable: true);

            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                ItemInstance.CreateDefinitionOnly(potion),
                ItemInstance.CreateDefinitionOnly(ore)), Is.False);
        }

        [Test]
        public void StackCompatibility_RejectsDifferentQuality()
        {
            TestItemDefinition ore = CreateItem("item.prototype-iron-ore", stackable: true);
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);
            QualityDefinition poor = ClassificationTestFactory.CreateQuality("quality.poor", "Poor", 0);

            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                new ItemInstance(ore, null, ItemInstanceMetadata.WithQuality(fine)),
                new ItemInstance(ore, null, ItemInstanceMetadata.WithQuality(poor))), Is.False);
        }

        [Test]
        public void StackCompatibility_RejectsDifferentCondition()
        {
            TestItemDefinition ore = CreateItem("item.prototype-iron-ore", stackable: true);

            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                new ItemInstance(ore, null, ItemInstanceMetadata.WithCondition(0.7f)),
                new ItemInstance(ore, null, ItemInstanceMetadata.WithCondition(0.8f))), Is.False);
        }

        [Test]
        public void StackCompatibility_RejectsPersistentOrStatefulIdentity()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: true, mode: ItemInstanceMode.AlwaysInstanced);

            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                new ItemInstance(sword, DeterministicInstanceId),
                ItemInstance.CreateDefinitionOnly(sword)), Is.False);
            Assert.That(ItemInstanceStackingPolicy.CanShareStack(
                new ItemInstance(sword, null, ItemInstanceMetadata.WithCondition(0.8f)),
                ItemInstance.CreateDefinitionOnly(sword)), Is.False);
        }

        [Test]
        public void CurrentDefinitionOnlyStackBehavior_RemainsReferenceBased()
        {
            TestItemDefinition potion = CreateItem("item.health-potion", stackable: true);
            TestItemDefinition sameIdDifferentAsset = CreateItem("item.health-potion", stackable: true);

            Assert.That(RarityQualityConditionUtility.CanShareDefinitionOnlyStack(potion, potion), Is.True);
            Assert.That(RarityQualityConditionUtility.CanShareDefinitionOnlyStack(potion, sameIdDifferentAsset), Is.False);
        }

        [Test]
        public void RuntimeValidation_DetectsDuplicatedInstanceIdsInSuppliedCollection()
        {
            TestItemDefinition sword = CreateItem("item.prototype-sword", stackable: false);
            ItemInstance first = new ItemInstance(sword, DeterministicInstanceId);
            ItemInstance second = new ItemInstance(sword, DeterministicInstanceId);

            DefinitionValidationReport report = ItemInstanceValidationUtility.ValidateUniqueInstanceIds(new[] { first, second });

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("Duplicate item instance ID"));
        }

        [Test]
        public void StaticValidation_WarnsWhenAlwaysInstancedItemIsStackable()
        {
            CategoryDefinition itemCategory = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            TestItemDefinition relic = CreateItem("item.unique-relic", stackable: true, mode: ItemInstanceMode.AlwaysInstanced, itemCategory: itemCategory);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(ClassificationTestFactory.CreateCatalog(itemCategory, relic));

            Assert.That(report.WarningCount, Is.GreaterThan(0));
            Assert.That(report.GetSummary(), Does.Contain("always-instanced but is configured as stackable"));
        }

        private static ItemInstanceSaveData CreateSaveData(
            string definitionId,
            string instanceId,
            string qualityId = null,
            float? conditionNormalized = null)
        {
            return new ItemInstanceSaveData
            {
                definitionId = definitionId,
                instanceId = instanceId,
                hasQuality = !string.IsNullOrWhiteSpace(qualityId),
                qualityId = qualityId,
                hasCondition = conditionNormalized.HasValue,
                conditionNormalized = conditionNormalized.GetValueOrDefault()
            };
        }

        private static TestItemDefinition CreateItem(
            string id,
            bool stackable,
            ItemInstanceMode mode = ItemInstanceMode.DefinitionOnly,
            CategoryDefinition itemCategory = null)
        {
            TestItemDefinition item = ScriptableObject.CreateInstance<TestItemDefinition>();
            item.Initialize(id, id, stackable, mode, itemCategory);
            return item;
        }

        private sealed class TestItemDefinition : ScriptableObject, IInventoryItemDefinition, IItemInstancePolicy
        {
            private string id;
            private string displayName;
            private bool stackable;
            private ItemInstanceMode instanceMode;
            private CategoryDefinition primaryCategory;

            public string Id => id;
            public string DisplayName => displayName;
            public string Description => string.Empty;
            public Sprite Icon => null;
            public CategoryDefinition PrimaryCategory => primaryCategory;
            public CategoryDomain ClassificationDomain => CategoryDomain.Item;
            public IReadOnlyList<TagDefinition> Tags => System.Array.Empty<TagDefinition>();
            public bool Stackable => stackable;
            public int MaximumStackSize => stackable ? 10 : 1;
            public ItemInstanceMode InstanceMode => instanceMode;

            public void Initialize(
                string itemId,
                string itemDisplayName,
                bool itemStackable,
                ItemInstanceMode itemInstanceMode,
                CategoryDefinition itemCategory)
            {
                id = itemId;
                displayName = itemDisplayName;
                stackable = itemStackable;
                instanceMode = itemInstanceMode;
                primaryCategory = itemCategory;
            }
        }
    }
}
