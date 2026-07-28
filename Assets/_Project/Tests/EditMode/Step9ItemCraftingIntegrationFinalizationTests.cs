using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Inventory.Crafting;
using UnityIsekaiGame.Inventory.Composition;
using UnityIsekaiGame.Inventory.Durability;
using UnityIsekaiGame.Inventory.Integration;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Inventory.Production;
using UnityIsekaiGame.Inventory.Quality;

namespace UnityIsekaiGame.Tests
{
    public sealed class Step9ItemCraftingIntegrationFinalizationTests
    {
        private const string CatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";
        private const string SwordId = "item.prototype-sword";

        [Test]
        public void AuthorityMapAndPersistenceDependenciesAreCompleteAndAcyclic()
        {
            Assert.That(Step9IntegrationValidator.AuthorityMap.Select(entry => entry.Domain).Distinct().Count(), Is.EqualTo(Step9IntegrationValidator.AuthorityMap.Count));

            Step9IntegrationValidationReport report = Step9IntegrationValidator.ValidateRuntimeGraph(new Step9IntegrationRuntimeSnapshot(), PrototypeRegistry());

            Assert.That(report.Diagnostics.Where(diagnostic => diagnostic.Domain == Step9IntegrationDiagnosticDomain.Authority && diagnostic.Severity == Step9IntegrationDiagnosticSeverity.Error), Is.Empty);
            Assert.That(report.Diagnostics.Where(diagnostic => diagnostic.Domain == Step9IntegrationDiagnosticDomain.Persistence && diagnostic.Severity == Step9IntegrationDiagnosticSeverity.Error), Is.Empty);
        }

        [Test]
        public void PrototypeCatalogProvidesStep9DefinitionsWithoutIntegrationErrors()
        {
            DefinitionRegistry registry = PrototypeRegistry();

            Step9IntegrationValidationReport report = Step9IntegrationValidator.ValidateDefinitions(registry);

            Assert.That(report.ErrorCount, Is.Zero, string.Join("\n", report.Diagnostics));
        }

        [Test]
        public void RuntimeGraphDetectsCrossRuntimeConflictsAndMissingReferences()
        {
            DefinitionRegistry registry = PrototypeRegistry();
            ItemInstanceRuntimeSaveData items = new ItemInstanceRuntimeSaveData
            {
                records =
                {
                    Item("item.instance.a", ItemLifecycleState.Active, ItemLocationKind.Equipped, "person.owner", "main-hand"),
                    Item("item.instance.b", ItemLifecycleState.Active, ItemLocationKind.Equipped, "person.owner", "main-hand"),
                    Item("item.instance.dead", ItemLifecycleState.Salvaged, ItemLocationKind.Inventory, "person.owner", "")
                }
            };
            ItemCompositionRuntimeSaveData compositions = new ItemCompositionRuntimeSaveData
            {
                records =
                {
                    new ItemCompositionRecordData
                    {
                        compositionId = "composition.a",
                        itemInstanceId = "item.instance.a",
                        sourceItemDefinitionId = SwordId,
                        components =
                        {
                            new ItemComponentEntryData { componentEntryId = "component.shared", componentItemInstanceId = "item.instance.missing" }
                        }
                    }
                }
            };
            ItemDurabilityRuntimeSaveData durability = new ItemDurabilityRuntimeSaveData
            {
                records =
                {
                    new ItemDurabilityRecordData
                    {
                        durabilityRecordId = "durability.a",
                        itemInstanceId = "item.instance.a",
                        itemDefinitionId = SwordId,
                        currentDurability = 150f,
                        maximumDurability = 100f
                    }
                }
            };

            Step9IntegrationValidationReport report = Step9IntegrationValidator.ValidateRuntimeGraph(
                new Step9IntegrationRuntimeSnapshot(itemInstances: items, itemCompositions: compositions, itemDurability: durability),
                registry);

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Diagnostics.Any(diagnostic => diagnostic.Code == "DuplicateExclusiveLocation"), Is.True);
            Assert.That(report.Diagnostics.Any(diagnostic => diagnostic.Code == "TerminalItemHasActiveLocation"), Is.True);
            Assert.That(report.Diagnostics.Any(diagnostic => diagnostic.Code == "TrackedComponentMissing"), Is.True);
            Assert.That(report.Diagnostics.Any(diagnostic => diagnostic.Code == "InvalidDurabilityRange"), Is.True);
        }

        [Test]
        public void SaveSchemaValidationRejectsUnsupportedStep9VersionBeforeRuntimeRestore()
        {
            DefinitionRegistry registry = PrototypeRegistry();
            ItemInstanceRuntimeSaveData items = new ItemInstanceRuntimeSaveData { schemaVersion = ItemInstanceRuntimeSaveData.CurrentSchemaVersion + 1 };

            Step9IntegrationValidationReport report = Step9IntegrationValidator.ValidateRuntimeGraph(new Step9IntegrationRuntimeSnapshot(itemInstances: items), registry);

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Diagnostics.Any(diagnostic => diagnostic.Code == "UnsupportedSchemaVersion" && diagnostic.SubjectId == "ItemInstanceIdentityRuntime"), Is.True);
        }

        [Test]
        public void CanonicalFingerprintIsDeterministicAndOrderIndependent()
        {
            ItemInstanceRuntimeSaveData first = new ItemInstanceRuntimeSaveData
            {
                records =
                {
                    Item("item.instance.b", ItemLifecycleState.Active, ItemLocationKind.Inventory, "person.owner", ""),
                    Item("item.instance.a", ItemLifecycleState.Active, ItemLocationKind.Inventory, "person.owner", "")
                }
            };
            ItemInstanceRuntimeSaveData second = first.Clone();
            second.records.Reverse();

            string firstFingerprint = Step9IntegrationValidator.CreateCanonicalFingerprint(new Step9IntegrationRuntimeSnapshot(itemInstances: first));
            string secondFingerprint = Step9IntegrationValidator.CreateCanonicalFingerprint(new Step9IntegrationRuntimeSnapshot(itemInstances: second));

            Assert.That(firstFingerprint, Is.EqualTo(secondFingerprint));
        }

        [Test]
        public void RuntimeSnapshotClonesInputsAndRemainsImmutableAfterSourceMutation()
        {
            ItemInstanceRuntimeSaveData source = new ItemInstanceRuntimeSaveData
            {
                records = { Item("item.instance.a", ItemLifecycleState.Active, ItemLocationKind.Inventory, "person.owner", "") }
            };
            Step9IntegrationRuntimeSnapshot snapshot = new Step9IntegrationRuntimeSnapshot(itemInstances: source);

            source.records[0].itemInstanceId = "item.instance.changed";
            string fingerprint = Step9IntegrationValidator.CreateCanonicalFingerprint(snapshot);

            Assert.That(fingerprint, Is.EqualTo(Step9IntegrationValidator.CreateCanonicalFingerprint(snapshot.Clone())));
            Assert.That(snapshot.ItemInstances.records.Single().itemInstanceId, Is.EqualTo("item.instance.a"));
        }

        private static ItemInstanceRecordData Item(string itemId, ItemLifecycleState lifecycle, ItemLocationKind location, string holderOrOwner, string slot)
        {
            return new ItemInstanceRecordData
            {
                itemInstanceId = itemId,
                itemDefinitionId = SwordId,
                classification = ItemInstanceClassification.IndividuallyTracked,
                stackQuantity = 1,
                lifecycleState = lifecycle,
                location = location switch
                {
                    ItemLocationKind.Equipped => new ItemLocationStateData { kind = location, equipmentHolderId = holderOrOwner, equipmentSlotId = slot },
                    ItemLocationKind.Inventory => new ItemLocationStateData { kind = location, inventoryOwnerId = holderOrOwner },
                    _ => new ItemLocationStateData { kind = location }
                }
            };
        }

        private static DefinitionRegistry PrototypeRegistry()
        {
            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Prototype catalog is missing at {CatalogPath}.");
            return catalog.CreateRegistry();
        }
    }
}
