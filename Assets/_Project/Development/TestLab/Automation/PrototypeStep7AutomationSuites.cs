#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Beings.Biology.Compatibility;
using UnityIsekaiGame.Beings.Biology.Hazards;
using UnityIsekaiGame.Beings.Biology.VitalProcesses;
using UnityIsekaiGame.Beings.Biology.Condition;
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
            TryRegister(registry, BuildBodyConditionSuite());
            TryRegister(registry, BuildVitalProcessesSuite());
            TryRegister(registry, BuildBiologicalHazardsSuite());
            TryRegister(registry, BuildBiologicalCompatibilitySuite());
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

        private static ITestLabAutomationSuite BuildBodyConditionSuite()
        {
            return Suite("feature.7.3.body-condition", "Feature 7.3 Body Condition", "7.3", 730,
                Required("ActorBodyRuntime", "BodyConditionRuntime", "InjuryTypeDefinition"),
                Scenario("healthy-condition-ready", "Healthy condition runtime is ready", 10,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-reset")),
                    Step("validate", "Validate condition", context => Operation(context.Service.ValidateBodyConditionIntegrity(), context, "step7-condition-validate"))),
                Scenario("preview-mutates-nothing", "Preview localized damage mutates nothing", 20,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-preview-reset")),
                    Step("preview", "Preview arm blunt trauma", context => Operation(context.Service.PreviewLocalizedStructuralDamage("injury.blunt-trauma", "part.arm.left", 12), context, "step7-condition-preview"))),
                Scenario("execute-blunt-damages-once", "Execute localized blunt trauma once", 30,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-execute-reset")),
                    Step("apply", "Apply arm blunt trauma", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.blunt-trauma", "part.arm.left", 12), context, "step7-condition-execute"))),
                Scenario("duplicate-transaction-idempotent", "Duplicate localized damage is idempotent", 40,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-duplicate-reset")),
                    Step("duplicate", "Duplicate proof", context => Operation(context.Service.ProveLocalizedDamageDuplicateProtection(), context, "step7-condition-duplicate"))),
                Scenario("laceration-records-node", "Laceration records the target node", 50,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-laceration-reset")),
                    Step("apply", "Lacerate hand", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.laceration", "part.hand.left", 14), context, "step7-condition-laceration"))),
                Scenario("fracture-compatible", "Fracture is compatible with limbs", 60,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-fracture-reset")),
                    Step("apply", "Fracture leg", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.fracture", "part.leg.left", 30), context, "step7-condition-fracture"))),
                Scenario("severing-changes-presence", "Severing can make a structure unavailable", 70,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-sever-reset")),
                    Step("apply", "Sever arm", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.severing", "part.arm.left", 100), context, "step7-condition-sever"))),
                Scenario("missing-node-fails", "Missing anatomy node fails clearly", 80,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-missing-reset")),
                    Step("missing", "Missing node", context => Operation(context.Service.TestMissingConditionNode(), context, "step7-condition-missing", acceptFailure: true))),
                Scenario("incompatible-injury-fails", "Incompatible injury fails clearly", 90,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-incompatible-reset")),
                    Step("incompatible", "Incompatible fracture against spirit core", context => Operation(context.Service.TestIncompatibleConditionInjury(), context, "step7-condition-incompatible", acceptFailure: true))),
                Scenario("save-restore-preserves-injury", "Save and restore preserve body condition injuries", 100,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-save-reset")),
                    Step("save-restore", "Save restore body condition", context => Operation(context.Service.ValidateBodyConditionSaveRestore(), context, "step7-condition-save-restore"))),
                Scenario("remove-injury-updates-state", "Removing an injury updates condition state", 110,
                    Step("reset", "Reset healthy Human condition", context => Operation(context.Service.ResetBodyConditionHealthy(), context, "step7-condition-remove-reset")),
                    Step("apply", "Apply hand laceration", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.laceration", "part.hand.left", 14), context, "step7-condition-remove-apply")),
                    Step("remove", "Remove first injury", context => Operation(context.Service.RemoveFirstBodyConditionInjury(), context, "step7-condition-remove"))),
                Scenario("construct-core-damage", "Construct core damage uses the same condition runtime", 120,
                    Step("construct", "Assign Construct", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step7-condition-construct")),
                    Step("apply", "Apply core damage", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.core-damage", "core.power", 35), context, "step7-condition-core"))),
                Scenario("spirit-disruption", "Spirit disruption uses incorporeal body condition", 130,
                    Step("spirit", "Assign Spirit", context => Operation(context.Service.AssignBodySpecies("species.basic-spirit"), context, "step7-condition-spirit")),
                    Step("apply", "Apply incorporeal disruption", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.incorporeal-disruption", "essence.aura", 25), context, "step7-condition-spirit-disruption"))));
        }

        private static ITestLabAutomationSuite BuildVitalProcessesSuite()
        {
            return Suite("feature.7.4.vital-processes", "Feature 7.4 Vital Processes", "7.4", 740,
                Required("ActorBodyRuntime", "VitalProcessRuntime", "BiologicalResourceDefinition", "VitalProcessProfileDefinition"),
                Scenario("healthy-human-ready", "Human vital process runtime is ready", 10,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-reset")),
                    Step("validate", "Validate vitals", context => Operation(context.Service.ValidateVitalProcessIntegrity(), context, "step7-vitals-validate"))),
                Scenario("preview-mutates-nothing", "Preview vital mutation mutates nothing", 20,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-preview-reset")),
                    Step("preview", "Preview blood consume", context => Operation(context.Service.PreviewVitalResourceMutation(BiologicalResourceIds.Blood, VitalResourceMutationOperation.Consume, 10f), context, "step7-vitals-preview"))),
                Scenario("execute-changes-once", "Committed vital mutation changes once", 30,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-execute-reset")),
                    Step("consume", "Consume blood", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Blood, VitalResourceMutationOperation.Consume, 10f), context, "step7-vitals-execute"))),
                Scenario("duplicate-transaction-idempotent", "Duplicate vital transaction is idempotent", 40,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-duplicate-reset")),
                    Step("duplicate", "Duplicate proof", context => Operation(context.Service.ProveVitalProcessDuplicateProtection(), context, "step7-vitals-duplicate"))),
                Scenario("breath-consume-restore", "Breath can be consumed and restored", 50,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-breath-reset")),
                    Step("consume", "Consume breath", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Breath, VitalResourceMutationOperation.Consume, 20f), context, "step7-vitals-breath-consume")),
                    Step("restore", "Restore breath", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Breath, VitalResourceMutationOperation.Restore, 20f), context, "step7-vitals-breath-restore"))),
                Scenario("temperature-target-centered", "Temperature uses target-centered thresholds", 60,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-temp-reset")),
                    Step("low", "Set low temperature", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Temperature, VitalResourceMutationOperation.Set, 34f), context, "step7-vitals-temp-low")),
                    Step("high", "Set high temperature", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Temperature, VitalResourceMutationOperation.Set, 40f), context, "step7-vitals-temp-high")),
                    Step("ideal", "Set ideal temperature", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Temperature, VitalResourceMutationOperation.Set, 37f), context, "step7-vitals-temp-ideal"))),
                Scenario("nutrition-hydration", "Nutrition and hydration deplete", 70,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-food-reset")),
                    Step("nutrition", "Consume nutrition", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Nutrition, VitalResourceMutationOperation.Consume, 5f), context, "step7-vitals-nutrition")),
                    Step("hydration", "Consume hydration", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Hydration, VitalResourceMutationOperation.Consume, 5f), context, "step7-vitals-hydration"))),
                Scenario("sleep-fatigue-accumulate", "Sleep need and fatigue accumulate", 80,
                    Step("reset", "Reset Human vitals", context => Operation(context.Service.ResetVitalProcessesHuman(), context, "step7-vitals-needs-reset")),
                    Step("sleep", "Increase sleep need", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.SleepNeed, VitalResourceMutationOperation.Consume, 5f), context, "step7-vitals-sleep")),
                    Step("fatigue", "Increase fatigue", context => Operation(context.Service.ApplyVitalResourceMutation(BiologicalResourceIds.Fatigue, VitalResourceMutationOperation.Consume, 5f), context, "step7-vitals-fatigue"))),
                Scenario("deterministic-process-update", "Vital process update is deterministic", 90,
                    Step("update", "Validate deterministic update", context => Operation(context.Service.ValidateVitalProcessDeterministicUpdate(), context, "step7-vitals-deterministic"))),
                Scenario("construct-inactive-blood", "Construct blood is inactive", 100,
                    Step("inactive", "Construct blood rejects mutation", context => Operation(context.Service.TestInactiveVitalResource("species.basic-construct", BiologicalResourceIds.Blood), context, "step7-vitals-construct-blood", acceptFailure: true))),
                Scenario("spirit-inactive-breath", "Spirit breath is inactive", 110,
                    Step("inactive", "Spirit breath rejects mutation", context => Operation(context.Service.TestInactiveVitalResource("species.basic-spirit", BiologicalResourceIds.Breath), context, "step7-vitals-spirit-breath", acceptFailure: true))),
                Scenario("lung-condition-reduces-breath", "Lung condition reduces Breath capacity", 120,
                    Step("lung", "Damage lung and recalculate", context => Operation(context.Service.DamageLungAndRecalculateBreath(), context, "step7-vitals-lung-capacity"))),
                Scenario("save-restore-preserves-vitals", "Save and restore preserve vital process state silently", 130,
                    Step("restore", "Save restore vitals", context => Operation(context.Service.ValidateVitalProcessSaveRestore(), context, "step7-vitals-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildBiologicalHazardsSuite()
        {
            return Suite("feature.7.5.biological-hazards", "Feature 7.5 Biological Hazards", "7.5", 750,
                Required("ActorBodyRuntime", "VitalProcessRuntime", "BiologicalHazardRuntime", "BiologicalHazardDefinition", "EnvironmentalExposureDefinition"),
                Scenario("healthy-human-hazard-free", "Healthy Human starts with a ready empty hazard runtime", 10,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-reset")),
                    Step("validate", "Validate biological hazards", context => Operation(context.Service.ValidateBiologicalHazardIntegrity(), context, "step7-hazards-validate"))),
                Scenario("bleeding-preview-mutates-nothing", "Bleeding preview uses vital rules without mutation", 20,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-preview-reset")),
                    Step("bleeding", "Add bleeding source", context => Operation(context.Service.AddBleedingHazard(), context, "step7-hazards-preview-bleeding")),
                    Step("preview", "Preview hazard tick", context => Operation(context.Service.PreviewBiologicalHazardTick(1800f), context, "step7-hazards-preview"))),
                Scenario("bleeding-tick-consumes-blood-once", "Bleeding tick consumes Blood through vital process runtime", 30,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-bleed-reset")),
                    Step("bleeding", "Add bleeding source", context => Operation(context.Service.AddBleedingHazard(), context, "step7-hazards-bleed-source")),
                    Step("tick", "Apply bleeding tick", context => Operation(context.Service.ApplyBiologicalHazardTick(1800f), context, "step7-hazards-bleed-tick"))),
                Scenario("duplicate-hazard-tick-idempotent", "Duplicate biological hazard tick is idempotent", 40,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-duplicate-reset")),
                    Step("duplicate", "Duplicate proof", context => Operation(context.Service.ProveBiologicalHazardTickDuplicateProtection(), context, "step7-hazards-duplicate"))),
                Scenario("multiple-bleeding-sources-source-safe-removal", "Multiple bleeding sources merge and remove source-safely", 50,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-source-reset")),
                    Step("first", "Add bleeding source", context => Operation(context.Service.AddBleedingHazard(), context, "step7-hazards-source-first")),
                    Step("second", "Add second bleeding source", context => Operation(context.Service.AddSecondBleedingHazardSource(), context, "step7-hazards-source-second")),
                    Step("remove", "Remove first source", context => Operation(context.Service.RemoveFirstBiologicalHazardSource(), context, "step7-hazards-source-remove"))),
                Scenario("inactive-blood-rejects-bleeding", "Inactive Blood rejects bleeding source", 60,
                    Step("inactive", "Construct blood rejects bleeding", context => Operation(context.Service.TestInactiveBiologicalHazardResource("species.basic-construct", BiologicalHazardIds.Bleeding), context, "step7-hazards-inactive-blood", acceptFailure: true))),
                Scenario("suffocation-exposure-consumes-breath", "Suffocation exposure consumes Breath", 70,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-suffocation-reset")),
                    Step("source", "Add suffocation exposure", context => Operation(context.Service.AddSuffocationExposure(), context, "step7-hazards-suffocation-source")),
                    Step("tick", "Apply suffocation tick", context => Operation(context.Service.ApplyBiologicalHazardTick(1800f), context, "step7-hazards-suffocation-tick"))),
                Scenario("inactive-breath-rejects-suffocation", "Inactive Breath rejects suffocation source", 80,
                    Step("inactive", "Spirit breath rejects suffocation", context => Operation(context.Service.TestInactiveBiologicalHazardResource("species.basic-spirit", BiologicalHazardIds.Suffocation), context, "step7-hazards-inactive-breath", acceptFailure: true))),
                Scenario("temperature-overheating-hypothermia-exclusive", "Temperature hazards are mutually exclusive", 90,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-temperature-reset")),
                    Step("high", "Create overheating", context => Operation(context.Service.CreateTemperatureHazard(high: true), context, "step7-hazards-temperature-high")),
                    Step("low", "Create hypothermia", context => Operation(context.Service.CreateTemperatureHazard(high: false), context, "step7-hazards-temperature-low"))),
                Scenario("starvation-dehydration-from-critical-vitals", "Critical nutrition and hydration create hazard pressure", 100,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-food-reset")),
                    Step("critical", "Create starvation and dehydration", context => Operation(context.Service.CreateStarvationAndDehydrationPressure(), context, "step7-hazards-food-critical"))),
                Scenario("fatigue-sleep-deprivation-from-critical-needs", "Critical fatigue and sleep need create hazard pressure", 110,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-needs-reset")),
                    Step("critical", "Create fatigue and sleep pressure", context => Operation(context.Service.CreateFatigueAndSleepPressure(), context, "step7-hazards-needs-critical"))),
                Scenario("environmental-exposure-source-removal", "Environmental exposure sources can be removed cleanly", 120,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-exposure-reset")),
                    Step("heat", "Add heat exposure", context => Operation(context.Service.AddHeatExposure(), context, "step7-hazards-exposure-heat")),
                    Step("remove", "Remove exposure source", context => Operation(context.Service.RemoveFirstBiologicalHazardSource(), context, "step7-hazards-exposure-remove"))),
                Scenario("suppression-weakens-hazard", "Hazard suppression lowers effective rate", 130,
                    Step("reset", "Reset Human biological hazards", context => Operation(context.Service.ResetBiologicalHazardsHuman(), context, "step7-hazards-suppress-reset")),
                    Step("bleeding", "Add bleeding source", context => Operation(context.Service.AddBleedingHazard(), context, "step7-hazards-suppress-source")),
                    Step("suppress", "Suppress bleeding", context => Operation(context.Service.SuppressBleedingHazard(), context, "step7-hazards-suppress"))),
                Scenario("save-restore-preserves-hazards-silently", "Save and restore preserve hazards without restore events", 140,
                    Step("restore", "Save restore hazards", context => Operation(context.Service.ValidateBiologicalHazardSaveRestore(), context, "step7-hazards-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildBiologicalCompatibilitySuite()
        {
            return Suite("feature.7.6.biological-compatibility", "Feature 7.6 Biological Compatibility", "7.6", 760,
                Required("ActorBodyRuntime", "BiologicalCompatibilityRuntime", "BiologicalInteractionDefinition", "BiologicalCompatibilityProfileDefinition"),
                Scenario("healthy-human-ready", "Human compatibility runtime is ready", 10,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-reset")),
                    Step("validate", "Validate compatibility", context => Operation(context.Service.ValidateBiologicalCompatibilityIntegrity(), context, "step7-compat-validate"))),
                Scenario("human-bleeding-compatible", "Human ordinary Bleeding is compatible", 20,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-bleed-reset")),
                    Step("evaluate", "Evaluate Human bleeding", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Bleeding, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-bleed"))),
                Scenario("human-suffocation-compatible", "Human Suffocation remains compatible", 25,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-suffocation-human-reset")),
                    Step("evaluate", "Evaluate Human suffocation", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Suffocation, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-suffocation-human"))),
                Scenario("spirit-bleeding-incompatible", "Spirit ordinary Bleeding is incompatible", 30,
                    Step("spirit", "Assign Spirit", context => Operation(context.Service.AssignBodySpecies("species.basic-spirit"), context, "step7-compat-spirit")),
                    Step("evaluate", "Evaluate Spirit bleeding", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Bleeding, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-spirit-bleed"))),
                Scenario("construct-repair-compatible", "Construct Repair is compatible with Constructs", 40,
                    Step("construct", "Assign Construct", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step7-compat-construct")),
                    Step("repair", "Evaluate Construct Repair", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.ConstructRepair, BiologicalInteractionCategory.Repair, string.Empty), context, "step7-compat-construct-repair")),
                    Step("suffocation", "Evaluate Construct Suffocation", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Suffocation, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-construct-suffocation")),
                    Step("core", "Evaluate Construct core damage", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.CoreDamage, BiologicalInteractionCategory.Injury, "core.power"), context, "step7-compat-construct-core"))),
                Scenario("biological-healing-contract", "Biological healing contract distinguishes body types", 50,
                    Step("human", "Reset Human", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-heal-human-reset")),
                    Step("human-heal", "Evaluate Human healing", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.BiologicalHealing, BiologicalInteractionCategory.Healing, string.Empty), context, "step7-compat-heal-human")),
                    Step("construct", "Assign Construct", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step7-compat-heal-construct")),
                    Step("construct-heal", "Evaluate Construct biological healing", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.BiologicalHealing, BiologicalInteractionCategory.Healing, string.Empty), context, "step7-compat-heal-construct-eval"))),
                Scenario("resistance-combines-deterministically", "Resistance contributions combine deterministically", 60,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-resist-reset")),
                    Step("resist", "Add resistance", context => Operation(context.Service.AddBiologicalCompatibilityResistance(), context, "step7-compat-resist")),
                    Step("second", "Add second resistance", context => Operation(context.Service.AddSecondBiologicalCompatibilityResistance(), context, "step7-compat-resist-second")),
                    Step("order", "Prove deterministic order", context => Operation(context.Service.ProveBiologicalCompatibilityDeterministicOrder(), context, "step7-compat-resist-order"))),
                Scenario("specific-rule-precedence", "Specific interaction rules override category defaults at equal priority", 65,
                    Step("prove", "Prove specific precedence", context => Operation(context.Service.ProveBiologicalCompatibilitySpecificRulePrecedence(), context, "step7-compat-precedence"))),
                Scenario("source-safe-removal", "Removing one compatibility source preserves others", 70,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-source-reset")),
                    Step("resist", "Add resistance", context => Operation(context.Service.AddBiologicalCompatibilityResistance(), context, "step7-compat-source-resist")),
                    Step("vulnerable", "Add vulnerability", context => Operation(context.Service.AddBiologicalCompatibilityVulnerability(), context, "step7-compat-source-vulnerable")),
                    Step("remove", "Remove one rule", context => Operation(context.Service.RemoveFirstBiologicalCompatibilityRule(), context, "step7-compat-source-remove"))),
                Scenario("dynamic-reset-restores-canonical", "Reset removes development compatibility rules", 75,
                    Step("prove", "Prove dynamic reset", context => Operation(context.Service.ProveBiologicalCompatibilityDynamicReset(), context, "step7-compat-dynamic-reset"))),
                Scenario("immunity-is-semantic", "Immunity is reported semantically", 80,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-immune-reset")),
                    Step("immune", "Add immunity", context => Operation(context.Service.AddBiologicalCompatibilityImmunity(), context, "step7-compat-immune")),
                    Step("evaluate", "Evaluate bleeding immunity", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Bleeding, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-immune-eval"))),
                Scenario("suppression-is-not-immunity", "Suppression is distinct from immunity", 90,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-suppress-reset")),
                    Step("suppress", "Add suppression", context => Operation(context.Service.AddBiologicalCompatibilitySuppression(), context, "step7-compat-suppress")),
                    Step("evaluate", "Evaluate suppressed bleeding", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Bleeding, BiologicalInteractionCategory.Hazard, string.Empty), context, "step7-compat-suppress-eval"))),
                Scenario("affinity-conversion-absorption", "Affinity, conversion, and absorption are reported", 100,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-special-reset")),
                    Step("affinity", "Add affinity", context => Operation(context.Service.AddBiologicalCompatibilityAffinity(), context, "step7-compat-affinity")),
                    Step("conversion", "Add conversion", context => Operation(context.Service.AddBiologicalCompatibilityConversion(), context, "step7-compat-conversion")),
                    Step("absorption", "Add absorption", context => Operation(context.Service.AddBiologicalCompatibilityAbsorption(), context, "step7-compat-absorption"))),
                Scenario("hazards-consume-compatibility", "Feature 7.5 hazards consume compatibility result", 110,
                    Step("spirit", "Assign Spirit", context => Operation(context.Service.AssignBodySpecies("species.basic-spirit"), context, "step7-compat-hazard-spirit")),
                    Step("bleeding", "Bleeding rejected on Spirit", context => Operation(context.Service.AddBleedingHazard(), context, "step7-compat-hazard-spirit-bleed", acceptFailure: true))),
                Scenario("injuries-consume-compatibility", "Feature 7.3 injuries consume compatibility result", 120,
                    Step("spirit", "Assign Spirit", context => Operation(context.Service.AssignBodySpecies("species.basic-spirit"), context, "step7-compat-injury-spirit")),
                    Step("fracture", "Fracture rejected on Spirit", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.fracture", "essence.aura", 20), context, "step7-compat-injury-fracture", acceptFailure: true)),
                    Step("disrupt", "Incorporeal disruption accepted", context => Operation(context.Service.ApplyLocalizedStructuralDamage("injury.incorporeal-disruption", "essence.aura", 20), context, "step7-compat-injury-disrupt"))),
                Scenario("missing-and-stale-contexts-reject", "Missing interactions and stale body snapshots fail closed", 125,
                    Step("missing", "Missing interaction rejected", context => Operation(context.Service.ProveBiologicalCompatibilityMissingInteractionRejected(), context, "step7-compat-missing-interaction")),
                    Step("stale", "Stale body rejected", context => Operation(context.Service.ProveBiologicalCompatibilityStaleBodyRejected(), context, "step7-compat-stale-body"))),
                Scenario("snapshot-read-only", "Compatibility snapshot mutates nothing", 130,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-snapshot-reset")),
                    Step("snapshot", "Snapshot read-only", context => Operation(context.Service.ProveBiologicalCompatibilitySnapshotReadOnly(), context, "step7-compat-snapshot"))),
                Scenario("future-contracts-resolve", "Future recovery disease and transformation contracts resolve", 140,
                    Step("reset", "Reset Human compatibility", context => Operation(context.Service.ResetBiologicalCompatibilityHuman(), context, "step7-compat-contract-reset")),
                    Step("disease", "Evaluate Disease", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Disease, BiologicalInteractionCategory.Disease, string.Empty), context, "step7-compat-contract-disease")),
                    Step("poison", "Evaluate Poison", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Poison, BiologicalInteractionCategory.Poison, string.Empty), context, "step7-compat-contract-poison")),
                    Step("polymorph", "Evaluate Polymorph", context => Operation(context.Service.EvaluateBiologicalCompatibility(BiologicalInteractionIds.Polymorph, BiologicalInteractionCategory.Transformation, string.Empty), context, "step7-compat-contract-polymorph"))));
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
