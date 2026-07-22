#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityIsekaiGame.Development.Automation
{
    public static class PrototypeStep7AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            TryRegister(registry, BuildBodySpeciesSuite());
        }

        private static ITestLabAutomationSuite BuildBodySpeciesSuite()
        {
            return Suite("feature.7.1.body-species", "Feature 7.1 Body and Species", "7.1", 710,
                Required("ActorBodyRuntime", "SpeciesDefinition", "BiologicalClassificationDefinition", "BodyFormDefinition"),
                Scenario("player-body-snapshot-resolves", "Player body snapshot resolves", 10,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-human")),
                    Step("validate", "Validate body", context => Operation(context.Service.ValidateBodyIntegrity(), context, "step7-validate"))),
                Scenario("human-capabilities", "Human grants living humanoid capabilities", 20,
                    Step("assign", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-human-caps")),
                    Step("reapply", "Reapply Human", context => Operation(context.Service.ReapplyBodySpecies(), context, "step7-human-duplicate"))),
                Scenario("preview-does-not-mutate", "Preview assignment does not mutate", 30,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-preview-baseline")),
                    Step("preview", "Preview Undead Human", context => Operation(context.Service.PreviewBodySpecies("species.undead-human"), context, "step7-preview-undead")),
                    Step("validate", "Validate after preview", context => Operation(context.Service.ValidateBodyIntegrity(), context, "step7-preview-validate"))),
                Scenario("undead-construct-spirit", "Alternate Species assignment paths", 40,
                    Step("undead", "Assign Undead Human", context => Operation(context.Service.AssignBodySpecies("species.undead-human"), context, "step7-undead")),
                    Step("construct", "Assign Construct", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step7-construct")),
                    Step("spirit", "Assign Spirit", context => Operation(context.Service.AssignBodySpecies("species.basic-spirit"), context, "step7-spirit")),
                    Step("restore-human", "Restore Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-restore-human"))),
                Scenario("missing-species-fails-clearly", "Missing Species fails clearly", 50,
                    Step("missing", "Preview missing Species", context => Operation(context.Service.TestMissingBodySpecies(), context, "step7-missing", acceptFailure: true))),
                Scenario("save-restore-preserves-species", "Save and load preserves body Species", 60,
                    Step("assign", "Assign Construct", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step7-save-assign")),
                    Step("save", "Save", context => Operation(context.Service.Save(), context, "step7-save")),
                    Step("load", "Load", context => Operation(context.Service.Load(), context, "step7-load")),
                    Step("validate", "Validate restored body", context => Operation(context.Service.ValidateBodyIntegrity(), context, "step7-load-validate")),
                    Step("reset", "Reset Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-reset-human"))));
        }

        private static ITestLabAutomationSuite Suite(string suiteId, string displayName, string feature, int order, IReadOnlyList<string> required, params ITestLabAutomationScenario[] scenarios)
        {
            return new TestLabAutomationSuite(suiteId, displayName, feature, $"{displayName} runtime integration scenarios.", order, TestLabAutomationCategory.Standard, includeInRunAll: true, requiredServices: required, scenarios: scenarios);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(scenarioId, displayName, displayName, order, order <= 30 ? TestLabAutomationCategory.Quick : TestLabAutomationCategory.Standard, includeInQuickRun: order <= 30, steps: steps);
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, displayName, action);
        }

        private static IReadOnlyList<string> Required(params string[] services)
        {
            return services.ToArray();
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
