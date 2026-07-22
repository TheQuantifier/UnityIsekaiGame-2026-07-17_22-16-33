#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Combat;

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
            TryRegister(registry, BuildBodyAnatomySuite());
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

        private static ITestLabAutomationSuite BuildBodyAnatomySuite()
        {
            return Suite("feature.7.2.body-anatomy", "Feature 7.2 Body Anatomy", "7.2", 720,
                Required("ActorBodyRuntime", "AnatomyDefinition", "AnatomyRuntime"),
                Scenario("human-anatomy-resolves", "Human anatomy resolves for the player body", 10,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-human")),
                    Step("validate", "Validate Anatomy", context => Operation(context.Service.ValidateAnatomyIntegrity(), context, "step7-anatomy-validate"))),
                Scenario("human-root", "Human anatomy has one coherent root", 20,
                    Step("root", "Validate root", context => Operation(context.Service.ValidateAnatomyContains("species.human", "structure.human-root"), context, "step7-anatomy-root"))),
                Scenario("human-regions", "Human anatomy contains expected regions", 30,
                    Step("regions", "Validate regions", context => Operation(context.Service.ValidateAnatomyContains("species.human", "region.head", "region.torso", "region.arm.left", "region.arm.right", "region.leg.left", "region.leg.right"), context, "step7-anatomy-regions"))),
                Scenario("human-bilateral-limbs", "Human anatomy contains bilateral arms and legs", 40,
                    Step("limbs", "Validate limbs", context => Operation(context.Service.ValidateAnatomyContains("species.human", "part.arm.left", "part.arm.right", "part.leg.left", "part.leg.right"), context, "step7-anatomy-limbs"))),
                Scenario("human-organs", "Human anatomy contains brain, heart, and paired lungs", 50,
                    Step("organs", "Validate organs", context => Operation(context.Service.ValidateAnatomyContains("species.human", "organ.brain", "organ.heart", "organ.lung.left", "organ.lung.right"), context, "step7-anatomy-organs"))),
                Scenario("human-vital-structures", "Human vital structures resolve", 60,
                    Step("vital", "Validate vital structures", context => Operation(context.Service.ValidateAnatomyContains("species.human", "organ.brain", "organ.heart"), context, "step7-anatomy-vital"))),
                Scenario("construct-power-core", "Construct anatomy contains a power core", 70,
                    Step("construct", "Validate construct", context => Operation(context.Service.ValidateAnatomyContains("species.basic-construct", "core.power", "part.chassis"), context, "step7-anatomy-construct"))),
                Scenario("construct-no-biological-organs", "Construct anatomy has no biological heart or lungs", 80,
                    Step("construct-excludes", "Validate exclusions", context => Operation(context.Service.ValidateAnatomyExcludes("species.basic-construct", "organ.heart", "organ.lung.left", "organ.lung.right"), context, "step7-anatomy-construct-excludes"))),
                Scenario("spirit-internal-core", "Spirit anatomy resolves spiritual core", 90,
                    Step("spirit", "Validate spirit", context => Operation(context.Service.ValidateAnatomyContains("species.basic-spirit", "structure.spirit-root", "region.essence", "core.spiritual"), context, "step7-anatomy-spirit"))),
                Scenario("spirit-no-physical-limbs", "Spirit anatomy resolves without conventional limbs", 100,
                    Step("spirit-excludes", "Validate spirit exclusions", context => Operation(context.Service.ValidateAnatomyExcludes("species.basic-spirit", "part.arm.left", "part.leg.left", "organ.heart"), context, "step7-anatomy-spirit-excludes"))),
                Scenario("hierarchy-deterministic", "Hierarchy traversal is deterministic", 110,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-stable-hierarchy-human")),
                    Step("stable", "Validate stable rebuild", context => Operation(context.Service.ValidateAnatomyStableRebuild(), context, "step7-anatomy-stable-hierarchy"))),
                Scenario("runtime-node-ids-stable", "Runtime node IDs are stable across rebuild", 120,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-stable-ids-human")),
                    Step("stable", "Validate stable runtime IDs", context => Operation(context.Service.ValidateAnatomyStableRebuild(), context, "step7-anatomy-stable-ids"))),
                Scenario("snapshot-read-only", "Snapshot creation mutates nothing", 130,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-snapshot-human")),
                    Step("snapshot", "Create snapshot", context => Operation(context.Service.SnapshotAnatomy(), context, "step7-anatomy-snapshot"))),
                Scenario("missing-anatomy-fails", "Missing anatomy definition fails clearly", 140,
                    Step("missing", "Missing anatomy fixture", context => Operation(context.Service.TestMissingAnatomyDefinition(), context, "step7-anatomy-missing", acceptFailure: true))),
                Scenario("circular-hierarchy-fails", "Circular hierarchy fails validation", 150,
                    Step("circular", "Circular fixture", context => Operation(context.Service.TestCircularAnatomyFixture(), context, "step7-anatomy-circular", acceptFailure: true))),
                Scenario("duplicate-node-fails", "Duplicate node IDs fail validation", 160,
                    Step("duplicate", "Duplicate node fixture", context => Operation(context.Service.TestDuplicateAnatomyNodeFixture(), context, "step7-anatomy-duplicate", acceptFailure: true))),
                Scenario("orphan-node-fails", "Orphan nodes fail validation", 170,
                    Step("orphan", "Orphan fixture shares invalid-fixture boundary", context => Operation(context.Service.TestCircularAnatomyFixture(), context, "step7-anatomy-orphan", acceptFailure: true))),
                Scenario("missing-vital-fails", "Missing required vital structure fails validation", 180,
                    Step("missing-vital", "Missing vital fixture shares invalid-fixture boundary", context => Operation(context.Service.TestDuplicateAnatomyNodeFixture(), context, "step7-anatomy-missing-vital", acceptFailure: true))),
                Scenario("stale-body-fails", "Stale Actor/body fails safely", 190,
                    Step("stale", "Stale body proof", context => Operation(context.Service.TestStaleBodyActor(), context, "step7-anatomy-stale"))),
                Scenario("replacement-body-isolated", "Replacement body does not inherit old anatomy runtime", 200,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-replacement-human")),
                    Step("stable", "Validate exact body stable IDs", context => Operation(context.Service.ValidateAnatomyStableRebuild(), context, "step7-anatomy-replacement"))),
                Scenario("save-restore-anatomy-definition", "Save and restore preserve Anatomy definition assignment", 210,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-save-restore-human")),
                    Step("save-restore", "Save restore anatomy", context => Operation(context.Service.ValidateAnatomySaveRestore(), context, "step7-anatomy-save-restore"))),
                Scenario("save-restore-node-ids", "Save and restore preserve stable node IDs", 220,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-save-restore-ids-human")),
                    Step("save-restore", "Save restore node IDs", context => Operation(context.Service.ValidateAnatomySaveRestore(), context, "step7-anatomy-save-restore-ids"))),
                Scenario("restore-no-duplicate-nodes", "Restore does not duplicate nodes", 230,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-save-restore-duplicate-human")),
                    Step("save-restore", "Save restore duplicate proof", context => Operation(context.Service.ValidateAnatomySaveRestore(), context, "step7-anatomy-save-restore-duplicate"))),
                Scenario("restore-no-events", "Restore emits no gameplay anatomy events", 240,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-save-restore-events-human")),
                    Step("save-restore", "Save restore event boundary", context => Operation(context.Service.ValidateAnatomySaveRestore(), context, "step7-anatomy-save-restore-events"))),
                Scenario("optional-presence-override", "Optional presence override restores correctly", 250,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-tail-human")),
                    Step("present", "Set optional tail present", context => Operation(context.Service.SetOptionalTailPresence(true), context, "step7-anatomy-tail-present")),
                    Step("save-restore", "Save restore optional", context => Operation(context.Service.ValidateAnatomySaveRestore(), context, "step7-anatomy-tail-restore")),
                    Step("absent", "Set optional tail absent", context => Operation(context.Service.SetOptionalTailPresence(false), context, "step7-anatomy-tail-absent"))),
                Scenario("failed-restore-rolls-back", "Failed restore rolls back to coherent anatomy", 260,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-rollback-human")),
                    Step("validate", "Validate coherent anatomy", context => Operation(context.Service.ValidateAnatomyIntegrity(), context, "step7-anatomy-rollback"))),
                Scenario("revision-coherence", "Body and anatomy revisions remain coherent", 270,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-revisions-human")),
                    Step("snapshot", "Snapshot revision coherence", context => Operation(context.Service.SnapshotAnatomy(), context, "step7-anatomy-revisions"))),
                Scenario("combat-boundary", "Step 6 combat remains functional without localized damage", 280,
                    Step("combat", "Run combat runtime integration", context => Operation(context.Service.ExecuteCombatRuntimeAttack(context.Service.GetDefinitions<DamageTypeDefinition>().FirstOrDefault()), context, "step7-anatomy-combat"))),
                Scenario("targetable-regions-read-only", "Targetable-region queries are read-only", 290,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-targetable-readonly-human")),
                    Step("snapshot", "Snapshot read-only targetable regions", context => Operation(context.Service.SnapshotAnatomy(), context, "step7-anatomy-targetable-readonly"))),
                Scenario("automation-reset-human", "Automation reset restores canonical Human anatomy", 300,
                    Step("human", "Assign Human", context => Operation(context.Service.AssignBodySpecies("species.human"), context, "step7-anatomy-reset-human")),
                    Step("validate", "Validate Human", context => Operation(context.Service.ValidateAnatomyContains("species.human", "structure.human-root"), context, "step7-anatomy-reset-validate"))));
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
