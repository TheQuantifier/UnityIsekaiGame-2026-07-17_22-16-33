#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;

namespace UnityIsekaiGame.Development.Automation
{
    public static class PrototypeStep3AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            TryRegister(registry, BuildRuntimeTaxonomySuite());
        }

        private static ITestLabAutomationSuite BuildRuntimeTaxonomySuite()
        {
            return Suite("feature.3.runtime-taxonomy", "Step 3 Runtime Taxonomy", "3.x", 300,
                Required("PrototypeTestLabService", "Inventory", "QuestLog", "ContractJournal"),
                Scenario("item-instance-equipment-flow", "Item instances and equipment flow", 10,
                    Step("clear-inventory", "Clear inventory", context => Operation(context.Service.ClearInventory(true), context, "step3-clear-inventory")),
                    Step("grant-stack", "Grant stackable item", context => Operation(context.Service.GrantItem(FirstStackableItem(context), 2), context, "step3-grant-stack")),
                    Step("grant-instance", "Grant stateful item", context => Operation(context.Service.GrantStatefulItem(FirstStatefulItem(context)), context, "step3-grant-instance")),
                    Step("grant-equippable", "Grant equippable item", context => Operation(context.Service.GrantStatefulItem(FirstEquippableItem(context)), context, "step3-grant-equippable")),
                    Step("equip", "Equip compatible item", context => Operation(context.Service.EquipFirstCompatible(FirstEquippableItem(context)), context, "step3-equip")),
                    Step("unequip", "Unequip all", context => Operation(context.Service.UnequipAll(true), context, "step3-unequip"))),
                Scenario("status-damage-and-vitals-flow", "Status, damage type, and vitals flow", 20,
                    Step("restore", "Restore vitals", context => Operation(context.Service.RestoreVitals(), context, "step3-restore-vitals")),
                    Step("status", "Apply status", context => Operation(context.Service.ApplyStatus(First<StatusEffectDefinition>(context), toEnemy: false), context, "step3-apply-status")),
                    Step("damage", "Apply typed damage", context => Operation(context.Service.ApplyTypedDamage(First<DamageTypeDefinition>(context), 5f, targetEnemy: false, sourcePlayer: false), context, "step3-typed-damage")),
                    Step("remove-status", "Remove status", context => Operation(context.Service.RemoveStatus(First<StatusEffectDefinition>(context), fromEnemy: false), context, "step3-remove-status"))),
                Scenario("quest-contract-objective-signals", "Quest and contract objective signals", 30,
                    Step("clear-quests", "Clear quest log", context => Operation(context.Service.ClearQuestLog(true), context, "step3-clear-quests")),
                    Step("clear-contracts", "Clear contract journal", context => Operation(context.Service.ClearContractJournal(true), context, "step3-clear-contracts")),
                    Step("start-quest", "Start quest", context => Operation(context.Service.StartQuest(First<QuestDefinition>(context)), context, "step3-start-quest")),
                    Step("report-talk", "Report talk", context => Operation(context.Service.ReportTalk(First<PersonDefinition>(context)), context, "step3-report-talk")),
                    Step("report-reach", "Report reach", context => Operation(context.Service.ReportReach(First<PlaceDefinition>(context)), context, "step3-report-reach")),
                    Step("accept-contract", "Accept contract", context => Operation(context.Service.AcceptContract(First<ContractDefinition>(context)), context, "step3-accept-contract")),
                    Step("report-defeat", "Report defeat", context => Operation(context.Service.ReportDefeat("prototype_enemy"), context, "step3-report-defeat"))),
                Scenario("location-and-world-entity-diagnostics", "Location and world entity diagnostics", 40,
                    Step("location", "Validate current location", context => Operation(context.Service.ValidateCurrentLocation(), context, "step3-location")),
                    Step("world-entities", "Refresh world entity diagnostics", context => Operation(context.Service.RefreshWorldEntityDiagnostics(), context, "step3-world-entities"))));
        }

        private static ITestLabAutomationSuite Suite(string suiteId, string displayName, string feature, int order, IReadOnlyList<string> required, params ITestLabAutomationScenario[] scenarios)
        {
            return new TestLabAutomationSuite(
                suiteId,
                displayName,
                feature,
                $"{displayName} runtime integration scenarios.",
                order,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: required,
                scenarios: scenarios);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                order <= 30 ? TestLabAutomationCategory.Quick : TestLabAutomationCategory.Standard,
                includeInQuickRun: order <= 30,
                steps: steps);
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, displayName, action);
        }

        private static IReadOnlyList<string> Required(params string[] services)
        {
            return services.ToArray();
        }

        private static T First<T>(TestLabAutomationContext context)
            where T : class, IGameDefinition
        {
            return context.Service.GetDefinitions<T>().FirstOrDefault();
        }

        private static ItemDefinition FirstStackableItem(TestLabAutomationContext context)
        {
            return context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault(item => item != null && item.Stackable)
                ?? context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault();
        }

        private static ItemDefinition FirstStatefulItem(TestLabAutomationContext context)
        {
            return context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault(item => item != null && item.InstanceMode != ItemInstanceMode.DefinitionOnly)
                ?? context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault();
        }

        private static ItemDefinition FirstEquippableItem(TestLabAutomationContext context)
        {
            return context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault(item => item != null && item.IsEquippable)
                ?? context.Service.GetDefinitions<ItemDefinition>().FirstOrDefault();
        }

        private static TestLabAutomationStepResult Operation(PrototypeTestLabOperation operation, TestLabAutomationContext context, string operationId, bool acceptFailure = false)
        {
            string transactionId = context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, operationId);
            if (acceptFailure)
            {
                return operation.Succeeded
                    ? TestLabAssertions.Fail(operationId, operation.OperationName, "OperationFailed", "Failure", operation.Code, operation.Message, string.Empty, transactionId)
                    : TestLabAssertions.Pass(operationId, operation.OperationName, $"Expected rejection observed: {operation.Code} {operation.Message}");
            }

            return operation.Succeeded
                ? new TestLabAutomationStepResult(operationId, operation.OperationName, TestLabAutomationStatus.Passed, "OperationSucceeded", "Succeeded", operation.Code, string.Empty, transactionId, operation.Message)
                : new TestLabAutomationStepResult(operationId, operation.OperationName, TestLabAutomationStatus.Failed, "OperationSucceeded", "Succeeded", operation.Code, string.Empty, transactionId, operation.Message);
        }

        private static void TryRegister(TestLabAutomationRegistry registry, ITestLabAutomationSuite suite)
        {
            registry.TryRegister(suite, out _);
        }
    }
}
#endif
