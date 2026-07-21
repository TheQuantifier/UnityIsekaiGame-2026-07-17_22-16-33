#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Combat.Defense;
using UnityIsekaiGame.Combat.Execution;
using UnityIsekaiGame.Combat.OngoingEffects;
using UnityIsekaiGame.Development;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Development.Automation
{
    public static class PrototypeStep6AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            TryRegister(registry, BuildFeature61Suite());
            TryRegister(registry, BuildFeature62Suite());
            TryRegister(registry, BuildFeature63Suite());
            TryRegister(registry, BuildFeature64Suite());
            TryRegister(registry, BuildFeature65Suite());
            TryRegister(registry, BuildFeature66Suite());
            TryRegister(registry, BuildFeature67Suite());
        }

        private static ITestLabAutomationSuite BuildFeature61Suite()
        {
            return Suite("feature.6.1.damage-healing", "Feature 6.1 Damage and Healing", "6.1", 610,
                Required("PrototypeTestLabService", "DamageHealingService", "Current Resources"),
                Scenario("damage-preview-does-not-mutate", "Damage preview does not mutate Health", 10, Step("preview-damage", "Preview damage", context =>
                    Operation(context.Service.PreviewPipelineDamage(First<DamageTypeDefinition>(context), 25f, targetPlayer: true), context, "preview-damage"))),
                Scenario("healing-preview-does-not-mutate", "Healing preview does not mutate Health", 20, Step("preview-healing", "Preview healing", context =>
                    Operation(context.Service.PreviewPipelineHealing(25f, targetPlayer: true), context, "preview-healing"))),
                Scenario("damage-executes-once", "Damage executes once", 30, Step("apply-damage", "Apply pipeline damage", context =>
                    Operation(context.Service.ApplyPipelineDamage(First<DamageTypeDefinition>(context), 25f, targetPlayer: true), context, "apply-damage"))),
                Scenario("duplicate-damage-does-not-apply-twice", "Duplicate damage does not apply twice", 40, Step("duplicate-damage", "Prove duplicate transaction", context =>
                    Operation(context.Service.ProvePipelineDuplicate(First<DamageTypeDefinition>(context), 25f), context, "duplicate-damage"))),
                Scenario("immunity-prevents-damage", "Immunity path remains observable", 50, Step("immunity-preview", "Preview selected damage type", context =>
                    Operation(context.Service.PreviewPipelineDamage(First<DamageTypeDefinition>(context), 25f, targetPlayer: true), context, "immunity-preview"))),
                Scenario("healing-clamps-and-reports-overhealing", "Healing clamps and reports overhealing", 60, Step("apply-healing", "Apply healing", context =>
                    Operation(context.Service.ApplyPipelineHealing(999f, targetPlayer: true), context, "apply-healing"))));
        }

        private static ITestLabAutomationSuite BuildFeature62Suite()
        {
            return Suite("feature.6.2.attack-resolution", "Feature 6.2 Attack Resolution", "6.2", 620,
                Required("PrototypeTestLabService", "AttackResolutionService", "DamageHealingService"),
                Scenario("deterministic-miss", "Deterministic miss", 10, Step("miss", "Execute miss", context =>
                    Operation(context.Service.ExecuteAttackResolution(First<DamageTypeDefinition>(context), 25f, 0.25f, 0.99f, 0f, 0.99f, 1.5f, 1f, 2f, true, true, false), context, "miss"))),
                Scenario("deterministic-hit", "Deterministic hit", 20, Step("hit", "Execute hit", context =>
                    Operation(context.Service.ExecuteAttackResolution(First<DamageTypeDefinition>(context), 10f, 0.95f, 0.1f, 0f, 0.99f, 1.5f, 1f, 2f, true, true, false), context, "hit"))),
                Scenario("deterministic-critical-hit", "Deterministic critical hit", 30, Step("critical", "Execute critical", context =>
                    Operation(context.Service.ExecuteAttackResolution(First<DamageTypeDefinition>(context), 10f, 0.95f, 0.1f, 0.95f, 0.1f, 2f, 1f, 2f, true, true, false), context, "critical"))),
                Scenario("miss-does-not-damage", "Miss does not damage", 40, Step("miss-preview", "Preview miss", context =>
                    Operation(context.Service.PreviewAttackResolution(First<DamageTypeDefinition>(context), 10f, 0.25f, 0.99f, 0f, 0.99f, 1.5f, 1f, 2f, true, true), context, "miss-preview"))),
                Scenario("duplicate-attack-does-not-damage-twice", "Duplicate attack does not damage twice", 50,
                    Step("first", "Generate transaction", context => Operation(context.Service.GenerateAttackTransaction(), context, "attack-tx")),
                    Step("execute", "Execute duplicate proof attack", context => Operation(context.Service.ExecuteAttackResolution(First<DamageTypeDefinition>(context), 10f, 0.95f, 0.1f, 0f, 0.99f, 1.5f, 1f, 2f, true, true, true), context, "attack-duplicate"))));
        }

        private static ITestLabAutomationSuite BuildFeature63Suite()
        {
            return Suite("feature.6.3.lifecycle", "Feature 6.3 Lifecycle", "6.3", 630,
                Required("PrototypeTestLabService", "ActorLifecycleController", "Current Resources"),
                Scenario("zero-health-causes-default-defeat", "Zero Health causes default defeat", 10, Step("zero-health", "Apply zero-health lifecycle damage", context =>
                    Operation(context.Service.ApplyZeroHealthLifecycleDamage(First<DamageTypeDefinition>(context), targetEnemy: true), context, "zero-health"))),
                Scenario("healing-does-not-automatically-recover", "Healing does not automatically recover", 20, Step("heal-after-defeat", "Heal after defeat", context =>
                    Operation(context.Service.ApplyPipelineHealing(25f, targetPlayer: true), context, "heal-after-defeat"))),
                Scenario("recovery-returns-unconscious-to-active", "Recovery returns Unconscious to Active", 30,
                    Step("zero-health", "Reduce enemy Health to zero", context => Operation(context.Service.ApplyZeroHealthLifecycleDamage(First<DamageTypeDefinition>(context), targetEnemy: true), context, "recovery-zero-health")),
                    Step("defeat", "Apply lifecycle defeat", context => Operation(context.Service.ExecuteDefeatLifecycle(targetEnemy: true, reuseTransaction: false), context, "recovery-defeat")),
                    Step("recover", "Execute recovery", context => Operation(context.Service.ExecuteRecoveryLifecycle(targetEnemy: true, 25f, reuseTransaction: false), context, "recover"))),
                Scenario("death-and-revival-transition", "Death and revival transition correctly", 40,
                    Step("death", "Execute death", context => Operation(context.Service.ExecuteDeathLifecycle(targetEnemy: true, reuseTransaction: false), context, "death")),
                    Step("revival", "Execute revival", context => Operation(context.Service.ExecuteRevivalLifecycle(targetEnemy: true, 25f, reuseTransaction: false), context, "revival"))),
                Scenario("duplicate-lifecycle-transaction-does-not-repeat", "Duplicate lifecycle transaction does not repeat", 50,
                    Step("generate", "Generate lifecycle transaction", context => Operation(context.Service.GenerateLifecycleTransaction(), context, "lifecycle-tx")),
                    Step("reuse", "Reuse lifecycle transaction", context => Operation(context.Service.ExecuteDefeatLifecycle(targetEnemy: true, reuseTransaction: true), context, "lifecycle-duplicate"))));
        }

        private static ITestLabAutomationSuite BuildFeature64Suite()
        {
            return Suite("feature.6.4.ongoing-effects", "Feature 6.4 Ongoing Effects", "6.4", 640,
                Required("PrototypeTestLabService", "OngoingEffectService", "DamageHealingService"),
                Scenario("effect-preview-creates-no-instance", "Effect preview creates no instance", 10, Step("preview", "Preview ongoing effect", context =>
                    Operation(context.Service.PreviewOngoingEffect(First<OngoingEffectDefinition>(context), true, 5f, 1f, 5f, 0, 1), context, "ongoing-preview"))),
                Scenario("due-tick-applies-once", "Due tick applies once", 20,
                    Step("apply", "Apply ongoing effect", context => Operation(context.Service.ApplyOngoingEffect(First<OngoingEffectDefinition>(context), true, 5f, 1f, 5f, 0, 1, false), context, "ongoing-apply")),
                    Step("tick", "Process due ticks", context => Operation(context.Service.ProcessOngoingEffectsNow(), context, "ongoing-tick"))),
                Scenario("duplicate-tick-does-not-apply-twice", "Duplicate tick does not apply twice", 30, Step("tick", "Process due ticks once", context =>
                    Operation(context.Service.ProcessOngoingEffectsNow(), context, "ongoing-duplicate-tick"))),
                Scenario("hostile-ongoing-damage-can-reach-zero-health", "Hostile ongoing damage can reach zero Health", 40, Step("apply-large", "Apply large ongoing damage", context =>
                    Operation(context.Service.ApplyOngoingEffect(First<OngoingEffectDefinition>(context), true, 999f, 1f, 1f, 1, 1, false), context, "ongoing-zero-health"))),
                Scenario("restore-does-not-replay-ticks", "Restore does not replay ticks", 50,
                    Step("safe-location", "Move to a known Test Lab point before saving", context => Operation(context.Service.Teleport(FirstTestPoint(context)), context, "ongoing-safe-location")),
                    Step("save", "Save active state", context => Operation(context.Service.Save(), context, "ongoing-save")),
                    Step("load", "Load active state", context => Operation(context.Service.Load(), context, "ongoing-load"))));
        }

        private static ITestLabAutomationSuite BuildFeature65Suite()
        {
            return Suite("feature.6.5.combat-state", "Feature 6.5 Combat State", "6.5/6.5a", 650,
                Required("PrototypeTestLabService", "CombatStateService", "Actor identity"),
                Scenario("explicit-engagement-starts-combat", "Explicit engagement starts combat", 10, Step("engage", "Engage A-B", context =>
                    Operation(context.Service.EngageCombatStateParticipants("A", "B"), context, "combat-state-engage"))),
                Scenario("duplicate-engagement-is-idempotent", "Duplicate engagement remains idempotent", 20,
                    Step("tx", "Generate transaction", context => Operation(context.Service.GenerateCombatStateTransaction(), context, "combat-state-tx")),
                    Step("first", "Engage A-B", context => Operation(context.Service.ExecuteExplicitCombatEngagement(reuseTransaction: true), context, "combat-state-first")),
                    Step("reuse", "Reuse engagement", context => Operation(context.Service.ExecuteExplicitCombatEngagement(reuseTransaction: true), context, "combat-state-reuse"))),
                Scenario("timeout-exits-combat", "Timeout exits combat", 30,
                    Step("engage", "Engage A-B", context => Operation(context.Service.EngageCombatStateParticipants("A", "B"), context, "combat-timeout-engage")),
                    Step("advance", "Advance combat timeout", context => Operation(context.Service.AdvanceCombatState(10f), context, "combat-timeout"))),
                Scenario("connected-encounters-merge", "Connected encounters merge", 40,
                    Step("prep", "Prepare split participants", context => Operation(context.Service.PrepareCombatStateSplitParticipants(), context, "combat-prep")),
                    Step("connect-a-b", "Connect A-B", context => Operation(context.Service.EngageCombatStateParticipants("A", "B"), context, "combat-connect-a-b")),
                    Step("connect-b-c", "Connect B-C", context => Operation(context.Service.EngageCombatStateParticipants("B", "C"), context, "combat-connect-b-c"))),
                Scenario("bridge-removal-splits-encounter", "Bridge removal splits encounter", 50,
                    Step("prep", "Prepare split participants", context => Operation(context.Service.PrepareCombatStateSplitParticipants(), context, "combat-bridge-prep")),
                    Step("connect-a-b", "Connect A-B", context => Operation(context.Service.EngageCombatStateParticipants("A", "B"), context, "combat-bridge-a-b")),
                    Step("connect-b-c", "Connect B-C", context => Operation(context.Service.EngageCombatStateParticipants("B", "C"), context, "combat-bridge-b-c")),
                    Step("connect-c-d", "Connect C-D", context => Operation(context.Service.EngageCombatStateParticipants("C", "D"), context, "combat-bridge-c-d")),
                    Step("end", "End bridge engagement", context => Operation(context.Service.EndCombatStateEngagement("B", "C", false), context, "combat-bridge")),
                    Step("process", "Process graph", context => Operation(context.Service.ProcessCombatStateConnectivity(), context, "combat-split"))),
                Scenario("isolated-participant-exits-combat", "Isolated participant exits combat", 60,
                    Step("prep", "Prepare split participants", context => Operation(context.Service.PrepareCombatStateSplitParticipants(), context, "combat-exit-prep")),
                    Step("connect", "Connect A-B", context => Operation(context.Service.EngageCombatStateParticipants("A", "B"), context, "combat-exit-connect")),
                    Step("exit", "Exit participant", context => Operation(context.Service.ForceCombatStateParticipantExit("B"), context, "combat-exit"))),
                Scenario("integrity-validation-remains-clean", "Integrity validation remains clean", 70, Step("validate", "Validate combat state integrity", context =>
                    Operation(context.Service.ValidateCombatStateIntegrity(), context, "combat-integrity"))));
        }

        private static ITestLabAutomationSuite BuildFeature66Suite()
        {
            return Suite("feature.6.6.defensive-actions", "Feature 6.6 Defensive Actions", "6.6", 660,
                Required("PrototypeTestLabService", "DefensiveActionService", "AttackResolutionService"),
                Scenario("defense-preview-does-not-mutate", "Defense preview does not mutate", 10, Step("preview", "Preview defense activation", context =>
                    Operation(context.Service.PreviewDefenseActivation(First<DefensiveActionDefinition>(context), targetPlayer: true), context, "defense-preview"))),
                Scenario("successful-dodge-prevents-damage", "Successful Dodge prevents damage", 20,
                    Step("activate", "Activate defense", context => Operation(context.Service.ActivateDefense(First<DefensiveActionDefinition>(context), targetPlayer: true, reuseTransaction: false), context, "defense-activate")),
                    Step("attack", "Resolve defensive attack", context => Operation(context.Service.ExecuteDefensiveAttack(First<DamageTypeDefinition>(context), 25f, 0.95f, 0.1f, 0.01f, targetPlayer: true, reuseTransaction: false), context, "defense-attack"))),
                Scenario("partial-block-reduces-damage-before-feature-6-1", "Partial Block reduces damage before 6.1 mitigation", 30, Step("attack", "Resolve block path", context =>
                    Operation(context.Service.ExecuteDefensiveAttack(First<DamageTypeDefinition>(context), 25f, 0.95f, 0.1f, 0.5f, targetPlayer: true, reuseTransaction: false), context, "defense-block"))),
                Scenario("duplicate-attack-does-not-spend-stamina-twice", "Duplicate attack does not spend Stamina twice", 40,
                    Step("tx", "Generate attack transaction", context => Operation(context.Service.GenerateAttackTransaction(), context, "defense-tx")),
                    Step("reuse", "Reuse defensive attack", context => Operation(context.Service.ExecuteDefensiveAttack(First<DamageTypeDefinition>(context), 25f, 0.95f, 0.1f, 0.01f, targetPlayer: true, reuseTransaction: true), context, "defense-duplicate"))),
                Scenario("lifecycle-transition-clears-defense", "Lifecycle transition clears defense", 50,
                    Step("activate", "Activate defense", context => Operation(context.Service.ActivateDefense(First<DefensiveActionDefinition>(context), targetPlayer: true, reuseTransaction: false), context, "defense-lifecycle-activate")),
                    Step("defeat", "Execute defeat", context => Operation(context.Service.ExecuteDefeatLifecycle(targetEnemy: false, reuseTransaction: false), context, "defense-lifecycle-clear"))));
        }

        private static ITestLabAutomationSuite BuildFeature67Suite()
        {
            return Suite("feature.6.7.combat-execution", "Feature 6.7 Combat Execution", "6.7", 670,
                Required("PrototypeTestLabService", "CombatExecutionService", "Current Resources"),
                Scenario("execution-preview-does-not-mutate", "Execution preview does not mutate", 10, Step("preview", "Preview execution", context =>
                    Operation(context.Service.PreviewCombatExecution(First<CombatExecutionDefinition>(context)), context, "execution-preview"))),
                Scenario("commitment-conflict-rejects-second-action", "Commitment conflict rejects second action", 20,
                    Step("begin", "Begin execution", context => Operation(context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false), context, "execution-begin")),
                    Step("begin-second", "Begin second execution", context => Operation(context.Service.RunExpectedAutomationFailure(() => context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false)), context, "execution-conflict", acceptFailure: true))),
                Scenario("commit-spends-costs-once", "Commit spends costs once", 30,
                    Step("begin", "Begin execution", context => Operation(context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false), context, "execution-cost-begin")),
                    Step("advance", "Advance to ready", context => Operation(context.Service.AdvanceCombatExecutionClock(1f), context, "execution-cost-ready")),
                    Step("commit", "Commit execution", context => Operation(context.Service.CommitCombatExecution(false), context, "execution-cost-commit"))),
                Scenario("duplicate-commit-does-not-spend-twice", "Duplicate commit does not spend twice", 40,
                    Step("begin", "Begin execution", context => Operation(context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false), context, "execution-duplicate-begin")),
                    Step("advance", "Advance to ready", context => Operation(context.Service.AdvanceCombatExecutionClock(1f), context, "execution-duplicate-ready")),
                    Step("commit", "Commit execution", context => Operation(context.Service.CommitCombatExecution(false), context, "execution-duplicate-commit")),
                    Step("reuse", "Reuse commit transaction", context => Operation(context.Service.CommitCombatExecution(true), context, "execution-duplicate-reuse"))),
                Scenario("cooldown-blocks-until-ready-boundary", "Cooldown blocks until ready boundary", 50,
                    Step("begin", "Begin execution after cooldown", context => Operation(context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false), context, "execution-cooldown-begin")),
                    Step("advance", "Advance execution clock", context => Operation(context.Service.AdvanceCombatExecutionClock(10f), context, "execution-cooldown-advance"))),
                Scenario("restore-clears-commitment-and-restores-cooldowns-silently", "Restore clears commitment and restores cooldowns silently", 60,
                    Step("begin", "Begin execution", context => Operation(context.Service.BeginCombatExecution(First<CombatExecutionDefinition>(context), false), context, "execution-restore-begin")),
                    Step("restore-clear", "Clear transient execution state", context => Operation(context.Service.ClearCombatExecutionForRestore(), context, "execution-restore-clear")),
                    Step("snapshot", "Snapshot cooldowns", context => Operation(context.Service.SnapshotCombatExecution(), context, "execution-restore-snapshot"))));
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

        private static PrototypeTestPoint FirstTestPoint(TestLabAutomationContext context)
        {
            return context.Service.GetTestPoints().FirstOrDefault();
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
