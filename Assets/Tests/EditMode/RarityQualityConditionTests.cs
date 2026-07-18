using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class RarityQualityConditionTests
    {
        [Test]
        public void DefinitionCatalog_AcceptsValidRarityQualityAndConditionDefinitions()
        {
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(CreateValidDefinitions());

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.False, report.GetSummary());
        }

        [Test]
        public void RarityRankComparison_UsesConfiguredOrdering()
        {
            RarityDefinition common = ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0);
            RarityDefinition rare = ClassificationTestFactory.CreateRarity("rarity.rare", "Rare", 2);

            Assert.That(RarityQualityConditionUtility.CompareRarityRank(common, rare), Is.LessThan(0));
            Assert.That(RarityQualityConditionUtility.CompareRarityRank(rare, common), Is.GreaterThan(0));
            Assert.That(RarityQualityConditionUtility.CompareRarityRank(null, common), Is.LessThan(0));
        }

        [Test]
        public void RarityIds_RemainGloballyUnique()
        {
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(
                ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0),
                ClassificationTestFactory.CreateRarity("rarity.common", "Duplicate Common", 1));

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("Duplicate definition ID 'rarity.common'"));
        }

        [Test]
        public void QualityRankComparison_UsesConfiguredOrdering()
        {
            QualityDefinition poor = ClassificationTestFactory.CreateQuality("quality.poor", "Poor", 0);
            QualityDefinition masterwork = ClassificationTestFactory.CreateQuality("quality.masterwork", "Masterwork", 3);

            Assert.That(RarityQualityConditionUtility.CompareQualityRank(poor, masterwork), Is.LessThan(0));
            Assert.That(RarityQualityConditionUtility.CompareQualityRank(masterwork, poor), Is.GreaterThan(0));
        }

        [Test]
        public void ConditionResolution_HandlesBoundariesDeterministically()
        {
            IReadOnlyList<ConditionDefinition> conditions = CreateConditionBands();

            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 0f).Id, Is.EqualTo("condition.broken"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 0.01f).Id, Is.EqualTo("condition.damaged"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 0.31f).Id, Is.EqualTo("condition.worn"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 0.61f).Id, Is.EqualTo("condition.good"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 0.91f).Id, Is.EqualTo("condition.excellent"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 1f).Id, Is.EqualTo("condition.excellent"));
        }

        [Test]
        public void ConditionResolution_ClampsValuesOutsideValidRange()
        {
            IReadOnlyList<ConditionDefinition> conditions = CreateConditionBands();

            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, -1f).Id, Is.EqualTo("condition.broken"));
            Assert.That(RarityQualityConditionUtility.ResolveConditionOrNull(conditions, 2f).Id, Is.EqualTo("condition.excellent"));
        }

        [Test]
        public void ConditionValidation_ReportsOverlappingRanges()
        {
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(
                ClassificationTestFactory.CreateCondition("condition.broken", "Broken", 0, 0f, 0.5f),
                ClassificationTestFactory.CreateCondition("condition.damaged", "Damaged", 1, 0.4f, 1f));

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("overlaps"));
        }

        [Test]
        public void ConditionValidation_ReportsGapsInRanges()
        {
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(
                ClassificationTestFactory.CreateCondition("condition.broken", "Broken", 0, 0f, 0.25f),
                ClassificationTestFactory.CreateCondition("condition.good", "Good", 1, 0.5f, 1f));

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("gap"));
        }

        [Test]
        public void ConditionValidation_ReportsReversedRanges()
        {
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(
                ClassificationTestFactory.CreateCondition("condition.bad", "Bad", 0, 0.8f, 0.2f));

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("minimum normalized value is greater"));
        }

        [Test]
        public void ConditionDefinition_CanMarkBrokenStateAsUnusableMetadata()
        {
            ConditionDefinition broken = ClassificationTestFactory.CreateCondition("condition.broken", "Broken", 0, 0f, 0.01f, unusable: true);

            Assert.That(RarityQualityConditionUtility.IsUnusable(broken), Is.True);
        }

        [Test]
        public void RuntimeItemMetadata_IsOptionalAndClampsCondition()
        {
            QualityDefinition fine = ClassificationTestFactory.CreateQuality("quality.fine", "Fine", 2);
            ItemInstanceMetadata noMetadata = ItemInstanceMetadata.WithoutInstanceState();
            ItemInstanceMetadata metadata = ItemInstanceMetadata.WithQualityAndCondition(fine, 2f);

            Assert.That(noMetadata.HasQuality, Is.False);
            Assert.That(noMetadata.HasCondition, Is.False);
            Assert.That(metadata.Quality, Is.SameAs(fine));
            Assert.That(metadata.HasCondition, Is.True);
            Assert.That(metadata.ConditionNormalized, Is.EqualTo(1f));
        }

        [Test]
        public void RuntimeItemMetadata_DoesNotMutateSharedDefinitions()
        {
            QualityDefinition masterwork = ClassificationTestFactory.CreateQuality("quality.masterwork", "Masterwork", 3);

            ItemInstanceMetadata metadata = ItemInstanceMetadata.WithQualityAndCondition(masterwork, 0.25f);

            Assert.That(metadata.Quality.Id, Is.EqualTo("quality.masterwork"));
            Assert.That(masterwork.Id, Is.EqualTo("quality.masterwork"));
            Assert.That(metadata.Quality, Is.SameAs(masterwork));
        }

        [Test]
        public void ItemRarityReference_IsValidatedAgainstCatalog()
        {
            CategoryDefinition itemCategory = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            RarityDefinition common = ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0, isDefault: true);
            TestRarityItem item = ScriptableObject.CreateInstance<TestRarityItem>();
            item.Initialize("item.test", "Test Item", common, itemCategory);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(ClassificationTestFactory.CreateCatalog(itemCategory, common, item));

            Assert.That(report.HasErrors, Is.False, report.GetSummary());
            Assert.That(RarityQualityConditionUtility.GetRarity(item), Is.SameAs(common));
        }

        [Test]
        public void MissingItemRarity_IsWarningOnly()
        {
            CategoryDefinition itemCategory = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            TestRarityItem item = ScriptableObject.CreateInstance<TestRarityItem>();
            item.Initialize("item.test", "Test Item", null, itemCategory);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(ClassificationTestFactory.CreateCatalog(itemCategory, item));

            Assert.That(report.HasErrors, Is.False, report.GetSummary());
            Assert.That(report.WarningCount, Is.GreaterThan(0));
            Assert.That(report.GetSummary(), Does.Contain("has no rarity assigned"));
        }

        [Test]
        public void Catalog_LooksUpRarityQualityAndConditionByIdAndType()
        {
            RarityDefinition common = ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0);
            QualityDefinition standard = ClassificationTestFactory.CreateQuality("quality.standard", "Standard", 1);
            ConditionDefinition excellent = ClassificationTestFactory.CreateCondition("condition.excellent", "Excellent", 4, 0.91f, 1f);
            DefinitionRegistry registry = ClassificationTestFactory.CreateCatalog(common, standard, excellent).CreateRegistry();

            Assert.That(registry.TryGet("rarity.common", out RarityDefinition foundRarity), Is.True);
            Assert.That(registry.TryGet("quality.standard", out QualityDefinition foundQuality), Is.True);
            Assert.That(registry.TryGet("condition.excellent", out ConditionDefinition foundCondition), Is.True);
            Assert.That(foundRarity, Is.SameAs(common));
            Assert.That(foundQuality, Is.SameAs(standard));
            Assert.That(foundCondition, Is.SameAs(excellent));
        }

        [Test]
        public void StackCompatibility_RemainsDefinitionOnlyForCurrentInventory()
        {
            TestRarityItem item = ScriptableObject.CreateInstance<TestRarityItem>();
            item.Initialize("item.stackable", "Stackable Item", null, stackable: true);
            TestRarityItem sameIdDifferentDefinition = ScriptableObject.CreateInstance<TestRarityItem>();
            sameIdDifferentDefinition.Initialize("item.stackable", "Stackable Item", null, stackable: true);

            Assert.That(RarityQualityConditionUtility.CanShareDefinitionOnlyStack(item, item), Is.True);
            Assert.That(RarityQualityConditionUtility.CanShareDefinitionOnlyStack(item, sameIdDifferentDefinition), Is.False);
        }

        private static ScriptableObject[] CreateValidDefinitions()
        {
            List<ScriptableObject> definitions = new List<ScriptableObject>();
            definitions.Add(ClassificationTestFactory.CreateRarity("rarity.common", "Common", 0, isDefault: true));
            definitions.Add(ClassificationTestFactory.CreateRarity("rarity.uncommon", "Uncommon", 1));
            definitions.Add(ClassificationTestFactory.CreateQuality("quality.poor", "Poor", 0));
            definitions.Add(ClassificationTestFactory.CreateQuality("quality.standard", "Standard", 1, isDefault: true));
            definitions.AddRange(CreateConditionBands());
            return definitions.ToArray();
        }

        private static List<ConditionDefinition> CreateConditionBands()
        {
            return new List<ConditionDefinition>
            {
                ClassificationTestFactory.CreateCondition("condition.broken", "Broken", 0, 0f, 0.01f, unusable: true),
                ClassificationTestFactory.CreateCondition("condition.damaged", "Damaged", 1, 0.01f, 0.31f),
                ClassificationTestFactory.CreateCondition("condition.worn", "Worn", 2, 0.31f, 0.61f),
                ClassificationTestFactory.CreateCondition("condition.good", "Good", 3, 0.61f, 0.91f),
                ClassificationTestFactory.CreateCondition("condition.excellent", "Excellent", 4, 0.91f, 1f, isDefault: true)
            };
        }

        private sealed class TestRarityItem : ScriptableObject, IInventoryItemDefinition, IHasRarity
        {
            private string id;
            private string displayName;
            private RarityDefinition rarity;
            private CategoryDefinition primaryCategory;
            private bool stackable;

            public string Id => id;
            public string DisplayName => displayName;
            public CategoryDefinition PrimaryCategory => primaryCategory;
            public CategoryDomain ClassificationDomain => CategoryDomain.Item;
            public IReadOnlyList<TagDefinition> Tags => System.Array.Empty<TagDefinition>();
            public string Description => string.Empty;
            public Sprite Icon => null;
            public bool Stackable => stackable;
            public int MaximumStackSize => stackable ? 10 : 1;
            public RarityDefinition Rarity => rarity;

            public void Initialize(
                string itemId,
                string itemDisplayName,
                RarityDefinition itemRarity,
                CategoryDefinition itemCategory = null,
                bool stackable = true)
            {
                id = itemId;
                displayName = itemDisplayName;
                rarity = itemRarity;
                primaryCategory = itemCategory;
                this.stackable = stackable;
            }
        }
    }
}
