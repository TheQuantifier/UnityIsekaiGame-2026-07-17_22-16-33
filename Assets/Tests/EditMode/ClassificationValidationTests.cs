using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class ClassificationValidationTests
    {
        [Test]
        public void Validate_AcceptsValidCategoryHierarchyAndAssignments()
        {
            CategoryDefinition item = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            CategoryDefinition weapon = ClassificationTestFactory.CreateCategory("item.weapon", "Weapon", CategoryDomain.Item, item);
            TagDefinition prototype = ClassificationTestFactory.CreateTag("tag.prototype", "Prototype");
            TestClassifiedDefinition sword = ScriptableObject.CreateInstance<TestClassifiedDefinition>();
            sword.Initialize("item.prototype-sword", "Prototype Sword", CategoryDomain.Item, weapon, prototype);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(item, weapon, prototype, sword);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.False, report.GetSummary());
        }

        [Test]
        public void Validate_ReportsSelfParentCategory()
        {
            CategoryDefinition item = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            SetCategoryParent(item, item);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(item);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("cannot be its own parent"));
        }

        [Test]
        public void Validate_ReportsCircularCategoryHierarchy()
        {
            CategoryDefinition item = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            CategoryDefinition weapon = ClassificationTestFactory.CreateCategory("item.weapon", "Weapon", CategoryDomain.Item, item);
            SetCategoryParent(item, weapon);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(item, weapon);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("circular parent hierarchy"));
        }

        [Test]
        public void Validate_ReportsMissingAssignedCategoryFromCatalog()
        {
            CategoryDefinition weapon = ClassificationTestFactory.CreateCategory("item.weapon", "Weapon", CategoryDomain.Item);
            TestClassifiedDefinition sword = ScriptableObject.CreateInstance<TestClassifiedDefinition>();
            sword.Initialize("item.prototype-sword", "Prototype Sword", CategoryDomain.Item, weapon);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(sword);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("references category"));
        }

        [Test]
        public void Validate_ReportsDuplicateTagAssignment()
        {
            CategoryDefinition item = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            TagDefinition prototype = ClassificationTestFactory.CreateTag("tag.prototype", "Prototype");
            TestClassifiedDefinition sword = ScriptableObject.CreateInstance<TestClassifiedDefinition>();
            sword.Initialize("item.prototype-sword", "Prototype Sword", CategoryDomain.Item, item, prototype, prototype);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(item, prototype, sword);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.WarningCount, Is.GreaterThan(0));
            Assert.That(report.GetSummary(), Does.Contain("duplicate tag"));
        }

        [Test]
        public void Validate_ReportsWrongDomainCategory()
        {
            CategoryDefinition spell = ClassificationTestFactory.CreateCategory("ability.spell", "Spell", CategoryDomain.Ability);
            TestClassifiedDefinition sword = ScriptableObject.CreateInstance<TestClassifiedDefinition>();
            sword.Initialize("item.prototype-sword", "Prototype Sword", CategoryDomain.Item, spell);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(spell, sword);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("expected 'Item'"));
        }

        [Test]
        public void Validate_ReportsMissingAssignedTagFromCatalog()
        {
            CategoryDefinition item = ClassificationTestFactory.CreateCategory("item", "Item", CategoryDomain.Item);
            TagDefinition prototype = ClassificationTestFactory.CreateTag("tag.prototype", "Prototype");
            TestClassifiedDefinition sword = ScriptableObject.CreateInstance<TestClassifiedDefinition>();
            sword.Initialize("item.prototype-sword", "Prototype Sword", CategoryDomain.Item, item, prototype);

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(item, sword);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("references tag"));
        }

        [Test]
        public void Validate_ReportsLegacyStringTags()
        {
            TestLegacyTaggedDefinition person = ScriptableObject.CreateInstance<TestLegacyTaggedDefinition>();
            person.Initialize("person.prototype-npc", "Prototype NPC", "old_role_tag");

            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(person);

            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.WarningCount, Is.GreaterThan(0));
            Assert.That(report.GetSummary(), Does.Contain("legacy raw role tag"));
        }

        private static void SetCategoryParent(CategoryDefinition category, CategoryDefinition parent)
        {
            UnityEditor.SerializedObject serializedCategory = new UnityEditor.SerializedObject(category);
            serializedCategory.FindProperty("parentCategory").objectReferenceValue = parent;
            serializedCategory.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class TestClassifiedDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition
        {
            private string id;
            private string displayName;
            private CategoryDomain domain;
            private CategoryDefinition primaryCategory;
            private TagDefinition[] tags;

            public string Id => id;
            public string DisplayName => displayName;
            public CategoryDefinition PrimaryCategory => primaryCategory;
            public CategoryDomain ClassificationDomain => domain;
            public IReadOnlyList<TagDefinition> Tags => tags;

            public void Initialize(
                string definitionId,
                string definitionDisplayName,
                CategoryDomain definitionDomain,
                CategoryDefinition definitionPrimaryCategory,
                params TagDefinition[] definitionTags)
            {
                id = definitionId;
                displayName = definitionDisplayName;
                domain = definitionDomain;
                primaryCategory = definitionPrimaryCategory;
                tags = definitionTags;
            }
        }

        private sealed class TestLegacyTaggedDefinition : ScriptableObject, IGameDefinition, ILegacyStringTaggedDefinition
        {
            private string id;
            private string displayName;
            private string[] tags;

            public string Id => id;
            public string DisplayName => displayName;
            public IReadOnlyList<string> LegacyTags => tags;
            public string LegacyTagLabel => "role";

            public void Initialize(string definitionId, string definitionDisplayName, params string[] definitionTags)
            {
                id = definitionId;
                displayName = definitionDisplayName;
                tags = definitionTags;
            }
        }
    }
}
