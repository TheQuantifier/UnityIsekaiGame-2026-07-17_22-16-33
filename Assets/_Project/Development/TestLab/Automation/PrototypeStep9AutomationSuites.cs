#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Development.Automation
{
    public static class PrototypeStep9AutomationSuites
    {
        private const string SwordId = "item.prototype-sword";
        private const string PotionId = "item.health-potion";

        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            TryRegister(registry, BuildItemIdentitySuite());
        }

        private static ITestLabAutomationSuite BuildItemIdentitySuite()
        {
            return Suite("feature.9.1.item-identity-instance-state", "Feature 9.1 Item Identity and Instance State", "9.1", 910,
                Required("ItemInstanceIdentityRuntime", "ItemIdentityInventoryBridge", "PlayerItemIdentitySynchronizer", "ItemInstanceRecordData", "ItemInstanceRuntimeSaveData"),
                Scenario("distinct-instances", "Two items sharing one definition keep distinct identities", 10,
                    Step("step9-items-distinct", "Create distinct item instances", DistinctInstances)),
                Scenario("ownership-custody", "Ownership and custody mutate independently", 20,
                    Step("step9-items-ownership", "Transfer ownership and custody", OwnershipCustody)),
                Scenario("location-validation", "Location validation rejects duplicate world placement", 30,
                    Step("step9-items-location", "Validate world placement identity", LocationValidation)),
                Scenario("persistence-round-trip", "Item identity persistence round-trips state", 40,
                    Step("step9-items-persistence", "Save and restore item identity", PersistenceRoundTrip)),
                Scenario("inventory-equipment-migration", "Legacy inventory and equipment saves migrate into item identity", 50,
                    Step("step9-items-migration", "Migrate inventory and equipment graph", InventoryEquipmentMigration)),
                Scenario("inventory-equipment-synchronization", "Current inventory and equipment projection synchronizes item identity", 55,
                    Step("step9-items-sync", "Synchronize inventory and equipment graph", InventoryEquipmentSynchronization)),
                Scenario("access-subject", "Item projections expose stable Step 8 subject references", 60,
                    Step("step9-items-access", "Project item information subject", AccessSubject)));
        }

        private static TestLabAutomationStepResult DistinctInstances(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-distinct", failure);
            }

            ItemInstanceOperationResult first = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "a"), ownerPersonId: "person.prototype.owner-a");
            ItemInstanceOperationResult second = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "b"), ownerPersonId: "person.prototype.owner-b");
            if (!first.Succeeded || !second.Succeeded)
            {
                return Fail(context, "step9-items-distinct", $"{first.Status}/{second.Status} {first.Message} {second.Message}");
            }

            bool valid = first.Snapshot.ItemInstanceId != second.Snapshot.ItemInstanceId
                && first.Snapshot.ItemDefinitionId == SwordId
                && runtime.QueryByDefinition(SwordId).Count == 2;
            return valid
                ? Pass(context, "step9-items-distinct", $"First={first.Snapshot.ItemInstanceId} Second={second.Snapshot.ItemInstanceId}")
                : Fail(context, "step9-items-distinct", "Distinct item identity failed.");
        }

        private static TestLabAutomationStepResult OwnershipCustody(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-ownership", failure);
            }

            string id = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "owned"), ownerPersonId: "person.owner", custodianPersonId: "person.borrower").Snapshot.ItemInstanceId;
            ItemInstanceOperationResult custody = runtime.TransferCustody(id, custodianPersonId: "person.carrier");
            ItemInstanceOperationResult ownership = runtime.TransferOwnership(id, ItemOwnershipKind.PersonOwned, ownerPersonId: "person.new-owner");
            bool valid = custody.Succeeded
                && ownership.Succeeded
                && ownership.Snapshot.OwnerPersonId == "person.new-owner"
                && ownership.Snapshot.CustodianPersonId == "person.carrier";
            return valid
                ? Pass(context, "step9-items-ownership", $"Owner={ownership.Snapshot.OwnerPersonId} Custodian={ownership.Snapshot.CustodianPersonId}")
                : Fail(context, "step9-items-ownership", $"{custody.Status}/{ownership.Status} {custody.Message} {ownership.Message}");
        }

        private static TestLabAutomationStepResult LocationValidation(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-location", failure);
            }

            string first = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "placed-a")).Snapshot.ItemInstanceId;
            string second = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "placed-b")).Snapshot.ItemInstanceId;
            ItemInstanceOperationResult placed = runtime.SetWorldPlacement(first, context.ScenarioContext.ScopedId("placement", "sword"), "world-entity.prototype.sword-a", "scene.prototype");
            ItemInstanceOperationResult duplicate = runtime.SetWorldPlacement(second, context.ScenarioContext.ScopedId("placement", "sword"), "world-entity.prototype.sword-b", "scene.prototype");
            bool valid = placed.Succeeded && !duplicate.Succeeded && duplicate.Status == ItemInstanceOperationStatus.InvalidLocation;
            return valid
                ? Pass(context, "step9-items-location", duplicate.Message)
                : Fail(context, "step9-items-location", $"{placed.Status}/{duplicate.Status} {placed.Message} {duplicate.Message}");
        }

        private static TestLabAutomationStepResult PersistenceRoundTrip(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-persistence", failure);
            }

            string id = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "persist"), creatorPersonId: "person.smith", ownerPersonId: "person.owner").Snapshot.ItemInstanceId;
            runtime.Rename(id, "Persistent Prototype Sword");
            runtime.AssignMakerMarkAndSerial(id, "smith.mark", "P-0001");
            runtime.SetQuality(id, ItemQualityTier.Fine, ItemQualitySource.Authored, 0.8f);
            ItemInstanceRuntimeSaveData saveData = runtime.CreateSaveData();
            ItemInstanceIdentityRuntime restored = new ItemInstanceIdentityRuntime();
            ItemInstanceOperationResult restore = restored.RestoreFromSaveData(saveData, context.ScenarioContext.Runtimes.DefinitionRegistry);
            ItemInstanceSnapshot snapshot = null;
            bool valid = restore.Succeeded
                && restored.TryGetSnapshot(id, out snapshot)
                && snapshot.CustomName == "Persistent Prototype Sword"
                && snapshot.MakerMark == "smith.mark"
                && snapshot.QualityTier == ItemQualityTier.Fine;
            return valid
                ? Pass(context, "step9-items-persistence", $"Restored={id} Revision={snapshot.Revision}")
                : Fail(context, "step9-items-persistence", restore.Message);
        }

        private static TestLabAutomationStepResult AccessSubject(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-access", failure);
            }

            string id = runtime.CreateItem(sword, itemInstanceId: RunGuid(context, "subject"), ownerPersonId: "person.owner").Snapshot.ItemInstanceId;
            ItemInstanceProjection projection = runtime.Project(id, ItemProjectionAudience.PublicInspection);
            InformationSubjectReferenceData subject = projection.Snapshot.CreateInformationSubject();
            bool valid = !projection.Denied
                && subject.subjectType == InformationSubjectType.Custom
                && subject.subjectId == id
                && subject.parentSubjectId == SwordId
                && subject.tags.Contains("item.instance")
                && subject.tags.Contains(ItemInformationSubject.ItemInstanceSubjectTag)
                && ItemInformationSubject.ProtectedFields.Contains("serial")
                && ItemInformationSubject.ProtectedFields.Contains("provenance");
            return valid
                ? Pass(context, "step9-items-access", $"Subject={subject.subjectId} Parent={subject.parentSubjectId}")
                : Fail(context, "step9-items-access", "Item access subject projection was invalid.");
        }

        private static TestLabAutomationStepResult InventoryEquipmentMigration(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out _, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-migration", failure);
            }

            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            if (!registry.TryGet(PotionId, out ItemDefinition potion))
            {
                return Fail(context, "step9-items-migration", $"Item definition '{PotionId}' is missing.");
            }

            string inventorySword = RunGuid(context, "migration-inventory-sword");
            string equippedSword = RunGuid(context, "migration-equipped-sword");
            PlayerInventoryEquipmentSaveData legacy = new PlayerInventoryEquipmentSaveData
            {
                inventory = new InventorySaveData
                {
                    slotCapacity = 3,
                    entries =
                    {
                        new InventoryEntrySaveData { mode = InventoryEntrySaveMode.DefinitionStack, definitionId = potion.ItemId, quantity = 2 },
                        new InventoryEntrySaveData { mode = InventoryEntrySaveMode.StatefulInstance, itemInstance = StatefulSave(sword, inventorySword, 0.7f) }
                    }
                },
                equipment = new EquipmentSaveData
                {
                    slots =
                    {
                        new EquipmentSlotSaveData { slotType = EquipmentSlotType.MainHand, mode = EquipmentEntrySaveMode.StatefulInstance, itemInstance = StatefulSave(sword, equippedSword, 1f) }
                    }
                }
            };

            ItemIdentityInventoryBridgeResult migration = ItemIdentityInventoryBridge.MigrateInventoryEquipmentSave(legacy, registry, "person.prototype.player", context.ScenarioContext.Namespace);
            if (!migration.Succeeded)
            {
                return Fail(context, "step9-items-migration", $"{migration.Status}: {migration.Message}");
            }

            ItemIdentityInventoryBridgeResult audit = ItemIdentityInventoryBridge.ValidateInventoryEquipmentProjection(legacy, migration.SaveData, "person.prototype.player");
            bool valid = audit.Succeeded
                && migration.SaveData.records.Count == 3
                && migration.SaveData.records.Any(record => record.itemInstanceId == inventorySword && record.location.kind == ItemLocationKind.Inventory)
                && migration.SaveData.records.Any(record => record.itemInstanceId == equippedSword && record.location.kind == ItemLocationKind.Equipped)
                && migration.SaveData.records.Any(record => record.itemDefinitionId == PotionId && record.classification == ItemInstanceClassification.Fungible && record.stackQuantity == 2);
            return valid
                ? Pass(context, "step9-items-migration", $"Migrated={migration.SaveData.records.Count} Audit={audit.Status}")
                : Fail(context, "step9-items-migration", $"{audit.Status}: {audit.Message}");
        }

        private static TestLabAutomationStepResult InventoryEquipmentSynchronization(TestLabAutomationContext context)
        {
            if (!TryCreateRuntime(context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure))
            {
                return Fail(context, "step9-items-sync", failure);
            }

            string swordId = RunGuid(context, "sync-sword");
            PlayerInventoryEquipmentSaveData projection = new PlayerInventoryEquipmentSaveData
            {
                inventory = new InventorySaveData
                {
                    slotCapacity = 1,
                    entries =
                    {
                        new InventoryEntrySaveData { mode = InventoryEntrySaveMode.StatefulInstance, definitionId = sword.ItemId, itemInstanceId = swordId }
                    }
                },
                equipment = new EquipmentSaveData()
            };

            ItemIdentityInventoryBridgeResult first = ItemIdentityInventoryBridge.SynchronizeInventoryEquipmentRuntime(
                runtime,
                projection,
                context.ScenarioContext.Runtimes.DefinitionRegistry,
                "person.prototype.player",
                context.ScenarioContext.Namespace);
            projection.inventory.entries.Clear();
            projection.equipment.slots.Add(new EquipmentSlotSaveData { slotType = EquipmentSlotType.MainHand, mode = EquipmentEntrySaveMode.StatefulInstance, definitionId = sword.ItemId, itemInstanceId = swordId });
            ItemIdentityInventoryBridgeResult second = ItemIdentityInventoryBridge.SynchronizeInventoryEquipmentRuntime(
                runtime,
                projection,
                context.ScenarioContext.Runtimes.DefinitionRegistry,
                "person.prototype.player",
                context.ScenarioContext.Namespace);
            ItemIdentityInventoryBridgeResult audit = ItemIdentityInventoryBridge.ValidateSynchronizedProjection(
                projection,
                runtime.CreateSaveData(),
                context.ScenarioContext.Runtimes.DefinitionRegistry,
                "person.prototype.player",
                context.ScenarioContext.Namespace);

            ItemInstanceSnapshot snapshot = null;
            bool valid = first.Succeeded
                && second.Succeeded
                && audit.Succeeded
                && runtime.TryGetSnapshot(swordId, out snapshot)
                && snapshot.LocationKind == ItemLocationKind.Equipped
                && snapshot.Data.location.equipmentSlotId == EquipmentSlotType.MainHand.ToString();
            return valid
                ? Pass(context, "step9-items-sync", $"Sync={first.Status}/{second.Status} Location={snapshot.LocationKind}/{snapshot.Data.location.equipmentSlotId}")
                : Fail(context, "step9-items-sync", $"First={first.Status}:{first.Message} Second={second.Status}:{second.Message} Audit={audit.Status}:{audit.Message}");
        }

        private static bool TryCreateRuntime(TestLabAutomationContext context, out ItemInstanceIdentityRuntime runtime, out ItemDefinition sword, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.ItemInstances;
            sword = null;
            failure = string.Empty;
            if (runtime == null)
            {
                failure = "Item instance runtime is missing from the Test Lab runtime bundle.";
                return false;
            }

            DefinitionRegistry registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (registry == null)
            {
                failure = "Definition registry is missing.";
                return false;
            }

            if (!registry.TryGet(SwordId, out sword))
            {
                failure = $"Item definition '{SwordId}' is missing.";
                return false;
            }

            return true;
        }

        private static string RunGuid(TestLabAutomationContext context, string slug)
        {
            string seed = $"{context?.RunId}.{context?.CurrentSuiteId}.{context?.CurrentScenarioId}.{slug}";
            return DeterministicGuid(seed);
        }

        private static string DeterministicGuid(string seed)
        {
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed ?? string.Empty));
            return new Guid(bytes).ToString("D");
        }

        private static ITestLabAutomationSuite Suite(string suiteId, string displayName, string feature, int order, System.Collections.Generic.IReadOnlyList<string> required, params ITestLabAutomationScenario[] scenarios)
        {
            return new TestLabAutomationSuite(suiteId, displayName, feature, $"{displayName} runtime integration scenarios.", order, TestLabAutomationCategory.Standard, includeInRunAll: true, requiredServices: required, scenarios: scenarios);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                order <= 20 ? TestLabAutomationCategory.Quick : TestLabAutomationCategory.Standard,
                includeInQuickRun: order <= 20,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Items,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[] { SwordId, PotionId });
        }

        private static ItemInstanceSaveData StatefulSave(ItemDefinition item, string instanceId, float condition)
        {
            return new ItemInstanceSaveData
            {
                definitionId = item.ItemId,
                instanceId = instanceId,
                hasCondition = true,
                conditionNormalized = condition
            };
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, displayName, action);
        }

        private static System.Collections.Generic.IReadOnlyList<string> Required(params string[] services)
        {
            return services.ToArray();
        }

        private static TestLabAutomationStepResult Pass(TestLabAutomationContext context, string stepId, string diagnostics)
        {
            string transactionId = context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, stepId);
            return new TestLabAutomationStepResult(stepId, stepId, TestLabAutomationStatus.Passed, "OperationSucceeded", "Succeeded", "Succeeded", string.Empty, transactionId, diagnostics);
        }

        private static TestLabAutomationStepResult Fail(TestLabAutomationContext context, string stepId, string diagnostics)
        {
            string transactionId = context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, stepId);
            return new TestLabAutomationStepResult(stepId, stepId, TestLabAutomationStatus.Failed, "OperationSucceeded", "Succeeded", "Failed", string.Empty, transactionId, diagnostics);
        }

        private static void TryRegister(TestLabAutomationRegistry registry, ITestLabAutomationSuite suite)
        {
            registry.TryRegister(suite, out _);
        }
    }
}
#endif
