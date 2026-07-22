#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityIsekaiGame.Beings.Biology;
using UnityIsekaiGame.Development.Automation;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.CharacterSystem;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Combat.CombatState;
using UnityIsekaiGame.Combat.Contributions;
using UnityIsekaiGame.Combat.Defense;
using UnityIsekaiGame.Combat.Execution;
using UnityIsekaiGame.Combat.Integration;
using UnityIsekaiGame.Combat.OngoingEffects;
using UnityIsekaiGame.Combat.Reactions;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Requirements;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.Traits;
using UnityIsekaiGame.WorldEntities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabService
    {
        public const int DefaultHistoryLimit = 40;
        private const string PrototypeCatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";
        private const string DevelopmentStatusSource = "development.prototype-test-lab";

        private readonly List<PrototypeTestLabOperation> history = new List<PrototypeTestLabOperation>();
        private readonly HashSet<string> pendingConfirmations = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<Type, List<IGameDefinition>> selectorCache = new Dictionary<Type, List<IGameDefinition>>();
        private readonly DamageHealingService damageHealingService = new DamageHealingService();
        private readonly DefensiveActionService defensiveActionService = new DefensiveActionService();
        private readonly AttackResolutionService attackResolutionService;
        private CombatReactionService combatReactionService;
        private CombatContributionService combatContributionService;
        private CombatExecutionService combatExecutionService = new CombatExecutionService();
        private CombatRuntimeFacade combatRuntimeFacade;
        private readonly TestLabAutomationRegistry automationRegistry = new TestLabAutomationRegistry();
        private readonly TestLabAutomationReportExporter automationReportExporter = new TestLabAutomationReportExporter();
        private TestLabAutomationRunner automationRunner;
        private TestLabAutomationResult lastAutomationResult;
        private readonly List<TestLabScenarioResult> automationBatchScenarios = new List<TestLabScenarioResult>();
        private DateTime automationBatchStartedAtUtc;
        private string automationBatchRunId;
        private TestLabAutomationRunMode automationBatchMode;
        private bool automationBatchCancelled;
        private int automationBatchCounter;
        private bool automationBatchRunning;
        private bool suppressExpectedAutomationWarnings;
        private PrototypeTestLabContext context;
        private DefinitionRegistry registry;
        private int historyLimit = DefaultHistoryLimit;
        private string lastSpawnedWorldEntityId;
        private ItemDefinition lastSpawnedWorldEntityItem;
        private string lastDestroyedWorldEntityId;
        private ItemDefinition lastDestroyedWorldEntityItem;
        private string lastWorldEntityOperationMessage;
        private string lastAttackTransactionId;
        private string lastDefenseActivationTransactionId;
        private string lastCombatStateTransactionId;
        private string lastCombatStateSplitTransactionId;
        private string lastCombatExecutionBeginTransactionId;
        private string lastCombatExecutionCommitTransactionId;
        private string lastCombatExecutionInstanceId;
        private string lastLifecycleTransactionId;
        private string lastOngoingEffectTransactionId;
        private DamageApplicationResult lastContributionDamageSource;
        private HealingApplicationResult lastContributionHealingSource;
        private string lastContributionCreditTargetActorId;
        private float combatStateClockSeconds;
        private float combatExecutionClockSeconds;
        private float ongoingEffectClockSeconds;
        private readonly Dictionary<string, GameObject> combatStateTestActors = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        public event Action HistoryChanged;

        public IReadOnlyList<PrototypeTestLabOperation> History => history;
        public DefinitionRegistry Registry => registry;
        public string CurrentSlotId => context?.Persistence == null ? PersistenceService.PrototypeSlotId : context.Persistence.PrototypeSlotId;

        public PrototypeTestLabService()
        {
            attackResolutionService = new AttackResolutionService(damageHealingService, defensiveActionService);
        }

        public void Configure(PrototypeTestLabContext newContext)
        {
            context = newContext;
            combatExecutionService = context?.Persistence == null ? combatExecutionService : context.Persistence.CombatExecution;
            registry = CreateRegistry(context?.DefinitionCatalog);
            context?.IdentityProgression?.RegisterDefinitionCache(registry);
            if (EnsureResources(out CharacterResourceCollection resources))
            {
                resources.Configure(registry, context.PlayerCalculatedStats, PersistenceService.LocalPlayerId);
            }

            context?.PlayerSkills?.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            if (EnsureTraits(out CharacterTraitCollection traits))
            {
                traits.Configure(registry, context.PlayerCalculatedStats, context.PlayerSkills, PersistenceService.LocalPlayerId);
            }

            EnsureCharacterSystem(out _);
            EnsureLifecycleRuntime(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject, ref context.PlayerLifecycle, needsResource: true);
            EnsureLifecycleRuntime(context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject, ref context.EnemyLifecycle, needsResource: true);
            EnsureCombatStateRuntime();
            EnsureOngoingEffectRuntime(targetEnemy: false);
            EnsureOngoingEffectRuntime(targetEnemy: true);
            EnsureCombatReactionRuntime();
            EnsureCombatContributionRuntime();
            combatRuntimeFacade = null;
            EnsureCombatRuntimeFacade();
            EnsureAutomation();
            selectorCache.Clear();
        }

        public IReadOnlyList<ITestLabAutomationSuite> GetAutomationSuites()
        {
            EnsureAutomation();
            return automationRegistry.Suites;
        }

        public IReadOnlyList<ITestLabAutomationScenario> GetAutomationScenarios(string suiteId)
        {
            EnsureAutomation();
            return automationRegistry.TryGetSuite(suiteId, out ITestLabAutomationSuite suite)
                ? suite.Scenarios
                : Array.Empty<ITestLabAutomationScenario>();
        }

        public string BuildAutomationSummary()
        {
            EnsureAutomation();
            TestLabAutomationValidationResult validation = TestLabAutomationValidation.Validate(automationRegistry);
            if (lastAutomationResult == null)
            {
                return $"{validation.ToSummary()}\nSuites: {automationRegistry.Suites.Count}\nNo automation run yet.";
            }

            List<string> lines = new List<string>
            {
                validation.ToSummary(),
                $"Run: {lastAutomationResult.RunId} ({lastAutomationResult.RunMode}) Cancelled={lastAutomationResult.Cancelled}",
                $"Scenarios: {lastAutomationResult.PassedScenarios} passed, {lastAutomationResult.FailedScenarios} failed, {lastAutomationResult.ErrorScenarios} error, {lastAutomationResult.SkippedScenarios} skipped, {lastAutomationResult.CancelledScenarios} cancelled.",
                $"Steps: {lastAutomationResult.TotalSteps}. Elapsed: {lastAutomationResult.Elapsed.TotalSeconds:0.###}s."
            };

            foreach (TestLabScenarioResult scenario in lastAutomationResult.Scenarios)
            {
                lines.Add($"{scenario.Status}: {scenario.SuiteId}/{scenario.ScenarioId} - {scenario.DisplayName}");
                TestLabAutomationStepResult failedStep = scenario.Steps.FirstOrDefault(step => step.Status == TestLabAutomationStatus.Failed || step.Status == TestLabAutomationStatus.Error);
                if (failedStep != null)
                {
                    lines.Add($"  Failed Step: {failedStep.StepId} Expected='{failedStep.Expected}' Actual='{failedStep.Actual}' Tx='{failedStep.TransactionId}'");
                    if (!string.IsNullOrWhiteSpace(failedStep.Diagnostics))
                    {
                        lines.Add($"  Diagnostics: {failedStep.Diagnostics}");
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        public PrototypeTestLabOperation ValidateAutomationRegistration()
        {
            EnsureAutomation();
            TestLabAutomationValidationResult validation = TestLabAutomationValidation.Validate(automationRegistry);
            string message = validation.ToSummary();
            if (validation.Errors.Count > 0)
            {
                message += Environment.NewLine + string.Join(Environment.NewLine, validation.Errors);
            }

            if (validation.Warnings.Count > 0)
            {
                message += Environment.NewLine + string.Join(Environment.NewLine, validation.Warnings);
            }

            return Record(validation.Succeeded, "Validate Test Lab Automation", validation.Succeeded ? "Valid" : "Invalid", message);
        }

        public PrototypeTestLabOperation RunAutomationScenario(string suiteId, string scenarioId, bool stopOnFirstFailure)
        {
            EnsureAutomation();
            lastAutomationResult = automationRunner.RunScenario(suiteId, scenarioId, CreateAutomationOptions(stopOnFirstFailure));
            return Record(!lastAutomationResult.HasFailures, "Run Automation Scenario", lastAutomationResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation RunAutomationSuite(string suiteId, bool stopOnFirstFailure)
        {
            EnsureAutomation();
            lastAutomationResult = automationRunner.RunSuite(suiteId, CreateAutomationOptions(stopOnFirstFailure));
            return Record(!lastAutomationResult.HasFailures, "Run Automation Suite", lastAutomationResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation RunAutomationQuick(bool stopOnFirstFailure)
        {
            EnsureAutomation();
            lastAutomationResult = automationRunner.RunAll(quickOnly: true, CreateAutomationOptions(stopOnFirstFailure));
            return Record(!lastAutomationResult.HasFailures, "Run Quick Automation", lastAutomationResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation RunAutomationAll(bool stopOnFirstFailure)
        {
            EnsureAutomation();
            lastAutomationResult = automationRunner.RunAll(quickOnly: false, CreateAutomationOptions(stopOnFirstFailure));
            return Record(!lastAutomationResult.HasFailures, "Run All Automation", lastAutomationResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation RerunFailedAutomation(bool stopOnFirstFailure)
        {
            EnsureAutomation();
            lastAutomationResult = automationRunner.RerunFailed(CreateAutomationOptions(stopOnFirstFailure));
            return Record(!lastAutomationResult.HasFailures, "Rerun Failed Automation", lastAutomationResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation BeginAutomationBatch(TestLabAutomationRunMode runMode)
        {
            EnsureAutomation();
            automationBatchCounter++;
            automationBatchRunId = $"ui-batch-{automationBatchCounter:0000}";
            automationBatchMode = runMode;
            automationBatchStartedAtUtc = DateTime.UtcNow;
            automationBatchCancelled = false;
            automationBatchScenarios.Clear();
            UpdateAutomationBatchResult();
            return RecordSuccess("Begin Automation Batch", $"Started {runMode} automation batch {automationBatchRunId}.");
        }

        public PrototypeTestLabOperation RunAutomationScenarioInBatch(string suiteId, string scenarioId, bool stopOnFirstFailure)
        {
            EnsureAutomation();
            if (string.IsNullOrWhiteSpace(automationBatchRunId))
            {
                BeginAutomationBatch(TestLabAutomationRunMode.SelectedScenario);
            }

            automationBatchRunning = true;
            TestLabAutomationResult scenarioResult;
            try
            {
                scenarioResult = automationRunner.RunScenario(suiteId, scenarioId, CreateAutomationOptions(stopOnFirstFailure));
            }
            finally
            {
                automationBatchRunning = false;
            }

            automationBatchScenarios.AddRange(scenarioResult.Scenarios);
            UpdateAutomationBatchResult();
            LogAutomationScenarioFailures(scenarioResult);
            return Record(!scenarioResult.HasFailures, "Run Automation Batch Scenario", scenarioResult.HasFailures ? "Failed" : "Passed", FormatAutomationRun(scenarioResult));
        }

        public PrototypeTestLabOperation CompleteAutomationBatch(bool cancelled)
        {
            automationBatchCancelled = cancelled;
            UpdateAutomationBatchResult();
            string status = cancelled ? "Cancelled" : lastAutomationResult != null && lastAutomationResult.HasFailures ? "Failed" : "Passed";
            return Record(status == "Passed", "Complete Automation Batch", status, FormatAutomationRun(lastAutomationResult));
        }

        public PrototypeTestLabOperation CancelAutomation()
        {
            EnsureAutomation();
            automationBatchCancelled = true;
            automationRunner.Cancel();
            return RecordSuccess("Cancel Automation", "Cancellation requested. The current synchronous scenario will finish its current step before remaining scenarios are marked cancelled.");
        }

        public PrototypeTestLabOperation ClearAutomationResults()
        {
            lastAutomationResult = null;
            return RecordSuccess("Clear Automation Results", "Automation result summary cleared.");
        }

        public PrototypeTestLabOperation ResetAutomationRuntimeState()
        {
            RestoreVitals();
            ResetLifecycleForAutomation(context?.PlayerLifecycle, PersistenceService.LocalPlayerId);
            ResetLifecycleForAutomation(context?.EnemyLifecycle, string.Empty);
            ClearTemporaryStatuses();
            defensiveActionService.ClearTransientStateForRestore();
            combatExecutionService.RestoreFromSaveData(new CombatExecutionSaveData
            {
                schemaVersion = CombatExecutionSaveData.CurrentSchemaVersion,
                playerId = PersistenceService.LocalPlayerId,
                personId = context?.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId,
                cooldowns = new List<CombatExecutionCooldownSaveData>()
            }, PersistenceService.LocalPlayerId, out _, restoring: true);
            EnsureCombatStateRuntime().ClearTransientStateForRestore();
            EnsureOngoingEffectRuntime(targetEnemy: false)?.ClearTransientStateForRestore();
            EnsureOngoingEffectRuntime(targetEnemy: true)?.ClearTransientStateForRestore();
            EnsureCombatReactionRuntime()?.ClearTransientStateForRestore();
            EnsureCombatReactionRuntime()?.ClearAllSources();
            EnsureCombatContributionRuntime()?.ClearTransientStateForRestore();
            ongoingEffectClockSeconds = 0f;
            combatStateClockSeconds = 0f;
            combatExecutionClockSeconds = 0f;
            lastAttackTransactionId = string.Empty;
            lastDefenseActivationTransactionId = string.Empty;
            lastCombatStateTransactionId = string.Empty;
            lastCombatStateSplitTransactionId = string.Empty;
            lastCombatExecutionBeginTransactionId = string.Empty;
            lastCombatExecutionCommitTransactionId = string.Empty;
            lastCombatExecutionInstanceId = string.Empty;
            lastLifecycleTransactionId = string.Empty;
            lastOngoingEffectTransactionId = string.Empty;
            lastContributionDamageSource = null;
            lastContributionHealingSource = null;
            lastContributionCreditTargetActorId = string.Empty;
            ResetEnemy();
            return RecordSuccess("Reset Automation Runtime", "Runtime automation baseline restored without expected optional-action warnings.");
        }

        private static void ResetLifecycleForAutomation(ActorLifecycleController lifecycle, string playerId)
        {
            if (lifecycle == null)
            {
                return;
            }

            lifecycle.RestoreFromSaveData(new ActorLifecycleSaveData
            {
                schemaVersion = ActorLifecycleSaveData.CurrentSchemaVersion,
                playerId = playerId ?? string.Empty,
                personId = string.Empty,
                actorId = lifecycle.ActorId,
                policyId = string.Empty,
                lifecycleState = ActorLifecycleState.Active.ToString()
            }, playerId ?? string.Empty, lifecycle.ActorId, out _, restoring: true);
        }

        public PrototypeTestLabOperation RunExpectedAutomationFailure(Func<PrototypeTestLabOperation> action)
        {
            if (action == null)
            {
                return RecordFailure("Expected Automation Failure", "No expected-failure action was provided.", "MissingAction");
            }

            suppressExpectedAutomationWarnings = true;
            try
            {
                return action();
            }
            finally
            {
                suppressExpectedAutomationWarnings = false;
            }
        }

        public PrototypeTestLabOperation ExportAutomationJsonReport()
        {
            if (lastAutomationResult == null)
            {
                return RecordFailure("Export Automation JSON", "Run automation before exporting a report.", "NoResult");
            }

            string path = automationReportExporter.ExportJson(lastAutomationResult);
            return RecordSuccess("Export Automation JSON", $"Exported JSON report to {path}.");
        }

        public PrototypeTestLabOperation ExportAutomationMarkdownReport()
        {
            if (lastAutomationResult == null)
            {
                return RecordFailure("Export Automation Markdown", "Run automation before exporting a report.", "NoResult");
            }

            string path = automationReportExporter.ExportMarkdown(lastAutomationResult);
            return RecordSuccess("Export Automation Markdown", $"Exported Markdown report to {path}.");
        }

        public IReadOnlyList<TDefinition> GetDefinitions<TDefinition>()
            where TDefinition : class, IGameDefinition
        {
            Type type = typeof(TDefinition);
            if (!selectorCache.TryGetValue(type, out List<IGameDefinition> cached))
            {
                cached = registry == null
                    ? new List<IGameDefinition>()
                    : registry.DefinitionsById.Values
                        .Where(definition => definition is TDefinition)
                        .OrderBy(definition => definition.DisplayName)
                        .ThenBy(definition => definition.Id)
                        .ToList();
                selectorCache.Add(type, cached);
            }

            return cached.Cast<TDefinition>().ToList();
        }

        public IReadOnlyList<PrototypeTestPoint> GetTestPoints()
        {
            return UnityEngine.Object.FindObjectsByType<PrototypeTestPoint>(FindObjectsInactive.Exclude)
                .Where(point => point != null && !string.IsNullOrWhiteSpace(point.TestPointId))
                .OrderBy(point => point.TestPointId)
                .ThenBy(point => point.DisplayName)
                .ToList();
        }

        public string BuildOverview()
        {
            if (context == null)
            {
                return "Test Lab context is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                "Prototype Systems Test Lab",
                $"Build Boundary: {(Application.isEditor ? "Editor" : "Development Build")}",
                $"Player: {(context.PlayerTransform == null ? "Missing" : context.PlayerTransform.name)}",
                $"Health: {FormatHealth()}",
                $"Stamina: {FormatResource(context.PlayerStamina == null ? 0f : context.PlayerStamina.CurrentStamina, context.PlayerStamina == null ? 0f : context.PlayerStamina.MaximumStamina)}",
                $"Mana: {FormatResource(context.PlayerMana == null ? 0f : context.PlayerMana.CurrentMana, context.PlayerMana == null ? 0f : context.PlayerMana.MaximumMana)}",
                $"Stats: ATK {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.AttackPower)}, DEF {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.Defense)}",
                $"Base Attributes: {(context.PlayerAttributes == null ? "Missing" : context.PlayerAttributes.AttributeValues.Count.ToString())}",
                $"Skills: {(context.PlayerSkills == null ? "Missing" : context.PlayerSkills.LearnedSkills.Count.ToString())}",
                $"Character System: {FormatCharacterReadinessOneLine()}",
                $"Statuses: {FormatStatuses(context.PlayerStatuses)}",
                $"Inventory: {FormatInventory()}",
                $"Equipped: {CountEquipped()} item(s)",
                $"Selected Spell: {(context.SpellLoadout == null || context.SpellLoadout.SelectedSpell == null ? "None" : FormatDefinition(context.SpellLoadout.SelectedSpell))}",
                $"Quests: {(context.QuestLog == null ? 0 : context.QuestLog.Quests.Count)}",
                $"Contracts: {(context.ContractJournal == null ? 0 : context.ContractJournal.Contracts.Count)}",
                $"Identity: {FormatIdentityOneLine()}",
                $"Enemy: {FormatEnemy()}",
                $"Location: {FormatLocationOneLine()}",
                $"Definitions: {(registry == null ? 0 : registry.Count)}",
                $"Persistence Slot: {CurrentSlotId}",
                $"Modal Active: {PrototypeGameplayModalState.IsModalActive}"
            });
        }

        public string BuildCombatRuntimeSummary()
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            if (facade == null)
            {
                return "Combat runtime facade is unavailable.";
            }

            CombatRuntimeSnapshot snapshot = facade.CreateSnapshot(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Feature 6.10 Combat System Integration");
            builder.AppendLine($"Readiness: {snapshot.Readiness.State} Errors={CountDiagnostics(snapshot.Diagnostics, CombatIntegritySeverity.Error)} Warnings={CountDiagnostics(snapshot.Diagnostics, CombatIntegritySeverity.Warning)}");
            builder.AppendLine($"Actor: {EmptyAs(snapshot.ActorId, "Missing")} Body: {EmptyAs(snapshot.BodyId, "Missing")} Person: {EmptyAs(snapshot.PersonId, "None")} Lifecycle: {snapshot.LifecycleState}");
            builder.AppendLine($"Resources: {string.Join(", ", snapshot.Resources.Select(resource => $"{resource.ResourceId} {resource.Current:0.###}/{resource.Maximum:0.###}"))}");
            builder.AppendLine($"Combat Stats: {string.Join(", ", snapshot.CombatStats.Select(stat => $"{stat.StatId}={stat.Value:0.###}"))}");
            builder.AppendLine($"Transient: Defense={(snapshot.ActiveDefense == null ? "None" : snapshot.ActiveDefense.DefinitionId)} Execution={(snapshot.ActiveExecution == null ? "None" : snapshot.ActiveExecution.DefinitionId)} Ongoing={snapshot.ActiveOngoingEffects.Count} Reactions={snapshot.ReactionSources.Count}");
            builder.AppendLine($"Combat State: InCombat={(snapshot.CombatState != null && snapshot.CombatState.IsInCombat)} Engagements={snapshot.ActiveEngagements.Count} RecentOpponents={snapshot.RecentOpponents.Count}");
            builder.AppendLine($"Contributions: Ledgers={snapshot.ContributionLedgers.Count} ContributionRevision={snapshot.Revisions.ContributionRevision} AggregateRevision={snapshot.Revisions.AggregateRevision}");
            builder.AppendLine($"Last Tx: Root={EmptyAs(snapshot.LastTransactionTrace?.RootTransactionId, "None")} Attack={EmptyAs(snapshot.LastTransactionTrace?.AttackTransactionId, "None")} Damage={EmptyAs(snapshot.LastTransactionTrace?.DamageTransactionId, "None")} Coherent={(snapshot.LastTransactionTrace == null || snapshot.LastTransactionTrace.IsCoherent)}");
            builder.AppendLine("Persistence: combat state, defense windows, reaction sources, contribution ledgers, ongoing timers, and execution commitments are transient unless owned by their dedicated persistence participant. Restore clears transient combat runtime state silently before normal runtime resumes.");
            builder.AppendLine("Compatibility: existing 6.1-6.9 feature services remain callable; 6.10 composes them through one facade and shared DamageHealingService authority.");

            foreach (CombatRuntimeDiagnostic diagnostic in snapshot.Diagnostics.Take(8))
            {
                builder.AppendLine($"{diagnostic.Severity}: {diagnostic.Subsystem}/{diagnostic.Code} {diagnostic.Message}");
            }

            return builder.ToString();
        }

        public PrototypeTestLabOperation ValidateCombatRuntimeIntegrity()
        {
            CombatIntegrityReport report = EnsureCombatRuntimeFacade()?.ValidateIntegrity(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            if (report == null)
            {
                return RecordFailure("Validate 6.10 Combat Runtime", "Combat runtime facade is missing.", "MissingFacade");
            }

            string message = report.Diagnostics.Count == 0
                ? "Combat runtime integrity passed with no diagnostics."
                : string.Join(Environment.NewLine, report.Diagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Subsystem}/{diagnostic.Code} {diagnostic.Message}"));
            return Record(report.Passed, "Validate 6.10 Combat Runtime", report.Passed ? "Passed" : "Failed", message);
        }

        public PrototypeTestLabOperation ResetCombatRuntimeIntegration()
        {
            PrototypeTestLabOperation reset = ResetAutomationRuntimeState();
            combatRuntimeFacade = null;
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            CombatReadinessResult readiness = facade?.EvaluateReadiness(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            bool succeeded = reset.Succeeded && readiness != null && readiness.Diagnostics.All(diagnostic => diagnostic.Severity != CombatIntegritySeverity.Error);
            string message = $"Reset={reset.Code}; Readiness={readiness?.State}; Errors={CountDiagnostics(readiness?.Diagnostics, CombatIntegritySeverity.Error)} Warnings={CountDiagnostics(readiness?.Diagnostics, CombatIntegritySeverity.Warning)}.";
            return Record(succeeded, "Reset 6.10 Combat Runtime", succeeded ? "Ready" : "Invalid", message);
        }

        public PrototypeTestLabOperation PreviewCombatRuntimeAttack(DamageTypeDefinition damageType)
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, 25f, 0.95f, 0.1f, 0f, 0.99f, 1.5f, 1f, 2f, targetEnemy: true, sourcePlayer: true, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = facade.PreviewAttack(request);
            return Record(result.Succeeded, "Preview 6.10 Facade Attack", result.Code, FormatAttackResolution(result));
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeAttack(DamageTypeDefinition damageType)
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, 25f, 0.95f, 0.1f, 0f, 0.99f, 1.5f, 1f, 2f, targetEnemy: true, sourcePlayer: true, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = facade.ExecuteAttack(request);
            return Record(result.Succeeded, "Execute 6.10 Facade Attack", result.Code, $"{FormatAttackResolution(result)}\n{FormatCombatTransactionTrace(facade.LastTransactionTrace)}");
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeMiss(DamageTypeDefinition damageType)
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, 25f, 0.25f, 0.99f, 0f, 0.99f, 1.5f, 1f, 2f, targetEnemy: true, sourcePlayer: true, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = facade.ExecuteAttack(request);
            return Record(result.Succeeded, "Execute 6.10 Facade Miss", result.Code, $"{FormatAttackResolution(result)}\n{FormatCombatTransactionTrace(facade.LastTransactionTrace)}");
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeCritical(DamageTypeDefinition damageType)
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, 10f, 0.95f, 0.1f, 0.95f, 0.1f, 2f, 1f, 2f, targetEnemy: true, sourcePlayer: true, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = facade.ExecuteAttack(request);
            return Record(result.Succeeded, "Execute 6.10 Facade Critical", result.Code, $"{FormatAttackResolution(result)}\n{FormatCombatTransactionTrace(facade.LastTransactionTrace)}");
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeDefense(DamageTypeDefinition damageType, bool block)
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            DefensiveActionDefinition defense = FindDefensiveAction(block ? "block" : "dodge") ?? GetDefinitions<DefensiveActionDefinition>().FirstOrDefault();
            if (!EnsureCompatibleDefenseEquipment(defense, out string equipmentFailure))
            {
                return RecordFailure(block ? "Execute 6.10 Block Flow" : "Execute 6.10 Dodge Flow", equipmentFailure, DefensiveActionResultCode.IncompatibleEquipment);
            }

            if (!TryBuildDefenseActivationRequest(defense, targetPlayer: true, reuseTransaction: false, out DefenseActivationRequest activation, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            DefenseActivationResult activationResult = facade.ActivateDefense(activation);
            if (!activationResult.Succeeded)
            {
                return Record(false, block ? "Execute 6.10 Block Flow" : "Execute 6.10 Dodge Flow", activationResult.Code, FormatDefenseActivation(activationResult));
            }

            AttackResolutionRequest attack = CreateDefensiveAttackRequest(damageType, 25f, 0.95f, 0.1f, 0.01f, targetPlayer: true, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = facade.ExecuteAttack(attack);
            return Record(result.Succeeded, block ? "Execute 6.10 Block Flow" : "Execute 6.10 Dodge Flow", result.Code, $"{FormatDefenseActivation(activationResult)}\n{FormatAttackResolution(result)}");
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeOngoingDamage(OngoingEffectDefinition definition, DamageTypeDefinition damageType)
        {
            PrototypeTestLabOperation apply = ApplyOngoingEffect(definition, targetEnemy: true, amount: 5f, interval: 1f, duration: 3f, tickCount: 1, stacks: 1, reuseTransaction: false);
            if (!apply.Succeeded)
            {
                return apply;
            }

            return ProcessOngoingEffectsNow();
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeReaction(CombatReactionDefinition definition)
        {
            CombatReactionDefinition selected = definition ?? GetDefinitions<CombatReactionDefinition>().FirstOrDefault(candidate => candidate.SupportsTrigger(CombatReactionTriggerType.DamageApplied));
            PrototypeTestLabOperation register = RegisterCombatReaction(selected, ownerPlayer: false);
            if (!register.Succeeded)
            {
                return register;
            }

            return ExecuteCombatReactionDamage(selected);
        }

        public PrototypeTestLabOperation ExecuteCombatRuntimeContribution(DamageTypeDefinition damageType)
        {
            PrototypeTestLabOperation record = RecordDamageContribution(damageType, reuseTransaction: false);
            if (!record.Succeeded)
            {
                return record;
            }

            return ResolveDefeatContributionCredit();
        }

        public PrototypeTestLabOperation SimulateCombatRuntimeRestoreClear()
        {
            CombatRuntimeFacade facade = EnsureCombatRuntimeFacade();
            facade.ClearTransientStateForRestore(ResolveActorId(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject));
            facade.MarkReadyAfterRestore();
            CombatRuntimeSnapshot snapshot = facade.CreateSnapshot(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            bool cleared = snapshot.ActiveDefense == null
                && snapshot.ActiveExecution == null
                && snapshot.ActiveOngoingEffects.Count == 0
                && snapshot.ReactionSources.Count == 0
                && snapshot.ContributionLedgers.Count == 0;
            return Record(cleared, "Restore Clear 6.10 Combat Runtime", cleared ? "Cleared" : "StillActive", $"Readiness={snapshot.Readiness.State}; Defense={snapshot.ActiveDefense != null}; Execution={snapshot.ActiveExecution != null}; Ongoing={snapshot.ActiveOngoingEffects.Count}; Reactions={snapshot.ReactionSources.Count}; Ledgers={snapshot.ContributionLedgers.Count}.");
        }

        public string BuildIdentityProgressionSummary()
        {
            if (context?.IdentityProgression == null)
            {
                return "Player identity/progression component is missing.";
            }

            context.IdentityProgression.RegisterDefinitionCache(registry);
            return context.IdentityProgression.BuildDiagnosticSummary();
        }

        public string BuildAttributeCalculatedStatsSummary()
        {
            if (context?.PlayerAttributes == null || context.PlayerCalculatedStats == null)
            {
                return "Player Base Attributes or Calculated Stats component is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                context.PlayerAttributes.BuildDiagnosticSummary(),
                string.Empty,
                context.PlayerCalculatedStats.BuildDiagnosticSummary()
            });
        }

        public string BuildSkillsSummary(bool includeHidden)
        {
            if (context?.PlayerSkills == null)
            {
                return "Player Skill collection component is missing.";
            }

            context.PlayerSkills.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            return context.PlayerSkills.BuildDiagnosticSummary(includeHidden);
        }

        public string BuildTraitsSummary(bool includeHidden)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return "Player Trait collection component is missing.";
            }

            return traits.BuildDiagnosticSummary(includeHidden);
        }

        public string BuildCharacterSystemSummary(bool developmentView)
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character))
            {
                return "Character System coordinator is missing.";
            }

            return character.BuildDiagnosticSummary(developmentView);
        }

        public string BuildBodySpeciesSummary()
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return "Body runtime is missing.";
            }

            BodySnapshot snapshot = body.CreateSnapshot();
            List<string> lines = new List<string>
            {
                "Feature 7.1 Body and Species",
                $"Readiness: {snapshot.Readiness}",
                $"Revision: {snapshot.BodyRevision}",
                $"Actor/body: {snapshot.ActorBodyId}",
                $"Person: {snapshot.PersonId}",
                $"Species: {snapshot.SpeciesDisplayName} ({snapshot.SpeciesId})",
                $"Classification: {snapshot.BiologicalClassificationId}",
                $"Body Form: {snapshot.BodyFormId}",
                $"Defeat Policy: {snapshot.DefeatPolicyId}",
                $"Breathing Required: {snapshot.RequiresBreathing}",
                $"Has Blood: {snapshot.HasBlood}",
                $"Can Become Unconscious: {snapshot.CanBecomeUnconscious}",
                $"Can Die: {snapshot.CanDie}",
                $"Can Be Revived: {snapshot.CanBeRevived}",
                $"Accepts Biological Healing: {snapshot.AcceptsBiologicalHealing}",
                $"Accepts Repair: {snapshot.AcceptsRepair}",
                $"Has Physical Body: {snapshot.HasPhysicalBody}",
                $"Traits: {string.Join(", ", snapshot.SpeciesOwnedTraits.Select(trait => trait.TraitId))}",
                $"Capabilities: {string.Join(", ", snapshot.BiologicalCapabilities.Where(capability => capability.BooleanValue).Select(capability => capability.CapabilityId))}",
                $"Stat Contributions: {string.Join(", ", snapshot.BiologicalStatContributions.Select(stat => $"{stat.StatId} {stat.Direction} {stat.Magnitude:0.###}"))}",
                $"Coherent: {snapshot.Coherent}"
            };

            if (snapshot.Diagnostics.Count > 0)
            {
                lines.AddRange(snapshot.Diagnostics.Select(diagnostic => $"Diagnostic: {diagnostic}"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public PrototypeTestLabOperation PreviewBodySpecies(string speciesId)
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Preview Body Species", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            return RecordBodyResult("Preview Body Species", body.PreviewAssignSpecies(speciesId));
        }

        public PrototypeTestLabOperation AssignBodySpecies(string speciesId)
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Assign Body Species", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            return RecordBodyResult("Assign Body Species", body.AssignSpecies(speciesId, restoring: false, "Test Lab Species assignment"));
        }

        public PrototypeTestLabOperation ReapplyBodySpecies()
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Reapply Body Species", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            return RecordBodyResult("Reapply Body Species", body.AssignSpecies(body.SpeciesDefinitionId, restoring: false, "Test Lab duplicate Species proof"));
        }

        public PrototypeTestLabOperation ValidateBodyIntegrity()
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Validate Body Integrity", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            return body.ValidateBody(out string failureReason)
                ? RecordSuccess("Validate Body Integrity", body.CreateSnapshot().Coherent ? "Body integrity is coherent." : "Body snapshot reports diagnostics.")
                : RecordFailure("Validate Body Integrity", failureReason, BodyOperationResultCode.InvalidConfiguration.ToString());
        }

        public PrototypeTestLabOperation TestMissingBodySpecies()
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Missing Body Species", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            return RecordBodyResult("Missing Body Species", body.PreviewAssignSpecies("species.missing-test-lab"));
        }

        public PrototypeTestLabOperation TestStaleBodyActor()
        {
            if (!EnsureBodyRuntime(out ActorBodyRuntime body))
            {
                return RecordFailure("Stale Body Actor", "Body runtime is missing.", BodyOperationResultCode.MissingActorBody.ToString());
            }

            BodySnapshot snapshot = body.CreateSnapshot();
            bool stale = string.IsNullOrWhiteSpace(snapshot.ActorBodyId) || snapshot.ActorBodyId.Contains("stale", StringComparison.Ordinal);
            return stale
                ? RecordFailure("Stale Body Actor", $"Actor/body '{snapshot.ActorBodyId}' is stale or missing.", BodyOperationResultCode.StaleActorBody.ToString())
                : RecordSuccess("Stale Body Actor", $"Current Actor/body '{snapshot.ActorBodyId}' resolves; replacement-body redirection was not attempted.");
        }

        public PrototypeTestLabOperation InitializeCharacterSystem()
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character, initialize: false))
            {
                return RecordFailure("Initialize Character System", "Character System coordinator is missing.", "MissingCharacterSystem");
            }

            bool succeeded = character.InitializeFromRegistry(registry, restoring: false, addMissingCore: true);
            return Record(succeeded, "Initialize Character System", succeeded ? "Ready" : "Failed", succeeded ? $"Readiness={character.Readiness}, Revision={character.Revision}." : character.LastFailureReason);
        }

        public PrototypeTestLabOperation RebuildCharacterSystem()
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character))
            {
                return RecordFailure("Rebuild Character System", "Character System coordinator is missing.", "MissingCharacterSystem");
            }

            bool succeeded = character.FullRebuild(restoring: false, reason: "TestLabFullRebuild");
            return Record(succeeded, "Rebuild Character System", succeeded ? "Rebuilt" : "Failed", succeeded ? $"Readiness={character.Readiness}, Revision={character.Revision}." : character.LastFailureReason);
        }

        public PrototypeTestLabOperation ValidateCharacterSystemIntegrity()
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character))
            {
                return RecordFailure("Character Integrity", "Character System coordinator is missing.", "MissingCharacterSystem");
            }

            CharacterIntegrityReport report = character.ValidateIntegrity();
            return Record(report.Passed, "Character Integrity", report.Passed ? "Passed" : "Failed", report.GetSummary());
        }

        public PrototypeTestLabOperation SnapshotCharacterSystem()
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character))
            {
                return RecordFailure("Character Snapshot", "Character System coordinator is missing.", "MissingCharacterSystem");
            }

            CharacterFullSnapshot snapshot = character.GetSnapshot(developmentView: true);
            return RecordSuccess("Character Snapshot", $"Captured schema {snapshot.SchemaVersion}, revision {snapshot.Revision}, person {snapshot.Identity.PersonId}, actor {snapshot.Identity.ActorId}.");
        }

        public PrototypeTestLabOperation GrantTrait(TraitDefinition trait, TraitLifecycleState lifecycle, TraitDiscoveryState discovery)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Grant Trait", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Grant Trait", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = trait.Id,
                RequestedLifecycle = lifecycle,
                RequestedDiscovery = discovery,
                SourceCategory = TraitSourceCategory.Development,
                SourceId = "test-lab",
                Reason = "Prototype Test Lab"
            });
            return Record(result.Succeeded, $"Grant Trait {lifecycle}", result.Code, result.Message);
        }

        public PrototypeTestLabOperation GrantTraitDuplicateProof(TraitDefinition trait)
        {
            if (trait == null)
            {
                return RecordFailure("Trait Duplicate Proof", "Trait definition is missing.", "MissingTrait");
            }

            GrantTrait(trait, TraitLifecycleState.Active, TraitDiscoveryState.Discovered);
            return GrantTrait(trait, TraitLifecycleState.Active, TraitDiscoveryState.Discovered);
        }

        public PrototypeTestLabOperation GrantTraitSecondSource(TraitDefinition trait)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Trait Second Source", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Trait Second Source", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = trait.Id,
                RequestedLifecycle = TraitLifecycleState.Active,
                RequestedDiscovery = TraitDiscoveryState.Discovered,
                SourceCategory = TraitSourceCategory.Administrative,
                SourceId = "test-lab.second-source",
                Reason = "Prototype Test Lab second source"
            });
            return Record(result.Succeeded, "Trait Second Source", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RemoveTraitTestLabSource(TraitDefinition trait)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Remove Trait Source", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Remove Trait Source", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.RemoveTraitSource(trait.Id, TraitSourceCategory.Development, "test-lab");
            return Record(result.Succeeded, "Remove Trait Source", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SuppressTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Suppress Trait", collection => collection.SuppressTrait(trait.Id));
        }

        public PrototypeTestLabOperation UnsuppressTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Unsuppress Trait", collection => collection.UnsuppressTrait(trait.Id));
        }

        public PrototypeTestLabOperation ActivateTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Activate Trait", collection => collection.ActivateTrait(trait.Id));
        }

        public PrototypeTestLabOperation SetTraitSuspected(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Suspect Trait", collection => collection.SetDiscoveryState(trait.Id, TraitDiscoveryState.Suspected));
        }

        public PrototypeTestLabOperation SetTraitDiscovered(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Discover Trait", collection => collection.SetDiscoveryState(trait.Id, TraitDiscoveryState.Discovered));
        }

        public PrototypeTestLabOperation ReplaceTrait(TraitDefinition replacement)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Replace Trait", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (replacement == null)
            {
                return RecordFailure("Replace Trait", "Trait definition is missing.", "MissingTrait");
            }

            IReadOnlyList<string> blockers = traits.GetDevelopmentSnapshot()
                .Where(snapshot => snapshot.Definition != null
                    && snapshot.Definition.Id != replacement.Id
                    && (snapshot.Definition.ConflictGroupIds.Any(group => replacement.ConflictGroupIds.Contains(group))
                        || snapshot.Definition.IncompatibleTraits.Any(trait => trait != null && trait.Id == replacement.Id)
                        || replacement.IncompatibleTraits.Any(trait => trait != null && trait.Id == snapshot.Definition.Id)))
                .Select(snapshot => snapshot.Definition.Id)
                .ToList();
            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = replacement.Id,
                RequestedLifecycle = TraitLifecycleState.Active,
                RequestedDiscovery = TraitDiscoveryState.Discovered,
                SourceCategory = TraitSourceCategory.Development,
                SourceId = "test-lab.replace",
                Reason = "Prototype Test Lab replacement",
                AllowConflictReplacement = true,
                TraitsAuthorizedForReplacement = blockers
            });
            return Record(result.Succeeded, "Replace Trait", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RebuildTraitEffects()
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Rebuild Traits", "Player Trait collection component is missing.", "MissingTraits");
            }

            TraitOperationResult result = traits.RebuildTraitEffects();
            return Record(result.Succeeded, "Rebuild Traits", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SnapshotTraitsForPersistence()
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Trait Save Snapshot", "Player Trait collection component is missing.", "MissingTraits");
            }

            PlayerTraitsSaveData saveData = traits.CreateSaveData(PersistenceService.LocalPlayerId, context?.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId);
            bool valid = CharacterTraitCollection.ValidateSaveData(saveData, registry, PersistenceService.LocalPlayerId, out string failureReason);
            return Record(valid, "Trait Save Snapshot", valid ? "Valid" : "Invalid", valid ? $"Captured {saveData.traits.Count} Trait record(s)." : failureReason);
        }

        public PrototypeTestLabOperation EvaluateRequirement(RequirementSetDefinition requirement)
        {
            if (requirement == null)
            {
                return RecordFailure("Evaluate Requirement", "Requirement Set definition is missing.", "MissingRequirement");
            }

            RequirementEvaluationResult result = EnsureCharacterSystem(out CharacterSystemCoordinator character) && character.IsReady
                ? character.Query.EvaluateRequirement(requirement)
                : CapabilityRequirementEvaluator.Evaluate(requirement, BuildRequirementContext(testLab: true));
            string failures = result.Passed ? "All nodes passed." : string.Join("; ", result.TestLabFailureReasons);
            return Record(result.Passed, "Evaluate Requirement", result.Passed ? "Passed" : "Failed", failures);
        }

        public string BuildCurrentResourcesSummary()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return "Player resource collection is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                resources.BuildDiagnosticSummary(),
                string.Empty,
                $"Wrapper Health: {FormatHealth()}",
                $"Wrapper Mana: {FormatResource(context.PlayerMana == null ? 0f : context.PlayerMana.CurrentMana, context.PlayerMana == null ? 0f : context.PlayerMana.MaximumMana)}",
                $"Wrapper Stamina: {FormatResource(context.PlayerStamina == null ? 0f : context.PlayerStamina.CurrentStamina, context.PlayerStamina == null ? 0f : context.PlayerStamina.MaximumStamina)}"
            });
        }

        public PrototypeTestLabOperation ReconcileResources()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Reconcile Resources", "Player resource collection is missing.", "MissingResources");
            }

            int changed = 0;
            foreach (ResourceSnapshot snapshot in resources.GetSnapshots())
            {
                if (resources.ReconcileResource(snapshot.ResourceId))
                {
                    changed++;
                }
            }

            return RecordSuccess("Reconcile Resources", $"Reconciled {resources.GetSnapshots().Count} resource(s); {changed} current value(s) changed.");
        }

        public PrototypeTestLabOperation ProveResourceDuplicateEvent()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Duplicate Proof", "Player resource collection is missing.", "MissingResources");
            }

            string eventId = "resource.test-lab.duplicate-proof";
            ResourceChangeResult first = resources.TrySpend(ResourceIds.Mana, 1f, "test-lab", "Duplicate proof", eventId);
            ResourceChangeResult second = resources.TrySpend(ResourceIds.Mana, 1f, "test-lab", "Duplicate proof", eventId);
            bool passed = first.Succeeded && second.Succeeded && second.AppliedAmount <= CharacterResourceCollection.Epsilon;
            return Record(passed, "Resource Duplicate Proof", passed ? "Passed" : "Failed", $"First={first.AppliedAmount:0.###}, Second={second.AppliedAmount:0.###}, Mana={resources.GetCurrent(ResourceIds.Mana):0.###}/{resources.GetMaximum(ResourceIds.Mana):0.###}");
        }

        public PrototypeTestLabOperation TickResourceRegeneration()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Regen Tick", "Player resource collection is missing.", "MissingResources");
            }

            resources.TrySpend(ResourceIds.Stamina, Mathf.Max(1f, Mathf.Min(5f, resources.GetMaximum(ResourceIds.Stamina))), "test-lab", "Prepare regeneration tick");
            float before = resources.GetCurrent(ResourceIds.Stamina);
            resources.TickResources(1f, Time.time + 2f);
            float after = resources.GetCurrent(ResourceIds.Stamina);
            return RecordSuccess("Resource Regen Tick", $"Stamina {before:0.###} -> {after:0.###}.");
        }

        public PrototypeTestLabOperation SnapshotResourcesForPersistence()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Save Snapshot", "Player resource collection is missing.", "MissingResources");
            }

            PlayerResourcesSaveData saveData = resources.CreateSaveData(PersistenceService.LocalPlayerId, context?.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId);
            bool valid = CharacterResourceCollection.ValidateSaveData(saveData, registry, context?.PlayerCalculatedStats, PersistenceService.LocalPlayerId, out string failureReason);
            return Record(valid, "Resource Save Snapshot", valid ? "Valid" : "Invalid", valid ? $"Captured {saveData.resources.Count} resource record(s)." : failureReason);
        }

        public PrototypeTestLabOperation SimulateSkillAction(SkillDefinition skill, bool executed, bool succeeded, string eventId = "")
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Skill Action", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Skill Action", "Skill definition is missing.", "MissingSkill");
            }

            SkillActionExecutionEvent actionEvent = SkillActionExecutionEvent.Development(
                string.IsNullOrWhiteSpace(eventId) ? $"skill-action.test-lab.{Guid.NewGuid():N}" : eventId,
                skill.NaturalLearning == null ? SkillActionEventCategory.Development : skill.NaturalLearning.ActionCategory,
                skill.NaturalLearning == null ? skill.Id : skill.NaturalLearning.QualifyingEventId,
                executed,
                succeeded);
            SkillOperationResult result = skills.RecordQualifyingAction(actionEvent);
            return Record(result.Succeeded, executed ? succeeded ? "Skill Valid Action" : "Skill Missed Action" : "Skill Blocked Action", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SimulateManySkillActions(SkillDefinition skill, int count)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Skill Multi Action", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Skill Multi Action", "Skill definition is missing.", "MissingSkill");
            }

            int amount = Mathf.Max(1, count);
            for (int i = 0; i < amount; i++)
            {
                SkillActionExecutionEvent actionEvent = SkillActionExecutionEvent.Development(
                    $"skill-action.test-lab.{Guid.NewGuid():N}",
                    skill.NaturalLearning == null ? SkillActionEventCategory.Development : skill.NaturalLearning.ActionCategory,
                    skill.NaturalLearning == null ? skill.Id : skill.NaturalLearning.QualifyingEventId,
                    executed: true,
                    succeeded: true);
                skills.RecordQualifyingAction(actionEvent);
            }

            return RecordSuccess("Skill Multi Action", $"Simulated {amount} qualifying action(s) for {skill.DisplayName}.");
        }

        public PrototypeTestLabOperation TestDuplicateSkillAction(SkillDefinition skill)
        {
            string eventId = $"skill-action.test-lab.duplicate.{Guid.NewGuid():N}";
            SimulateSkillAction(skill, executed: true, succeeded: true, eventId);
            return SimulateSkillAction(skill, executed: true, succeeded: true, eventId);
        }

        public PrototypeTestLabOperation GrantSkill(SkillDefinition skill, SkillGrade grade)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Grant Skill", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Grant Skill", "Skill definition is missing.", "MissingSkill");
            }

            SkillOperationResult result = skills.GrantSkill(skill, grade, SkillAcquisitionSource.Development, "Prototype Test Lab", "test-lab");
            return Record(result.Succeeded, $"Grant Skill {grade}", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AwardSkillXp(SkillDefinition skill, int amount)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Award Skill XP", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Award Skill XP", "Skill definition is missing.", "MissingSkill");
            }

            SkillOperationResult result = skills.AwardSkillUse(skill.Id, amount: Mathf.Max(1, amount));
            return Record(result.Succeeded, "Award Skill XP", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RebuildSkillEffects()
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Rebuild Skills", "Player Skill collection component is missing.", "MissingSkills");
            }

            SkillOperationResult result = skills.RebuildSkillEffects();
            return Record(result.Succeeded, "Rebuild Skills", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ClearSkillDevelopmentState(bool confirmed)
        {
            if (!RequireConfirmation("ClearSkillDevelopmentState", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Clear Skills", "Player Skill collection component is missing.", "MissingSkills");
            }

            SkillOperationResult result = skills.ClearDevelopmentState(confirmed: true);
            return Record(result.Succeeded, "Clear Skills", result.Code, result.Message);
        }

        public string BuildLocationSummary()
        {
            string details = context?.Persistence == null
                ? "Player location persistence is missing."
                : context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return string.Join(Environment.NewLine, new[]
            {
                "Player Location Persistence",
                details,
                "Policy: same-scene restore is supported; cross-scene saves validate clearly and are not restored yet.",
                "Reach Location objectives are suppressed during persistence restore."
            });
        }

        public string BuildWorldEntitySummary()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Persistent World Entities",
                WorldEntityRegistry.BuildDiagnosticReport(),
                $"Last Spawned: {(string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) ? "None" : lastSpawnedWorldEntityId)}",
                $"Last Destroyed: {(string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) ? "None" : lastDestroyedWorldEntityId)}",
                $"Last Result: {(string.IsNullOrWhiteSpace(lastWorldEntityOperationMessage) ? "None" : lastWorldEntityOperationMessage)}"
            });
        }

        public string BuildSaveSlotSummary()
        {
            if (context?.Persistence == null)
            {
                return "Save slot persistence is missing.";
            }

            context.Persistence.EnsureInitialized();
            List<string> lines = new List<string>
            {
                "Save Slots, Autosave, and Load UI",
                context.Persistence.BuildSaveSlotDiagnosticSummary()
            };

            IReadOnlyList<SaveSlotDescriptor> descriptors = context.Persistence.BuildSaveSlotDescriptors();
            for (int i = 0; i < descriptors.Count; i++)
            {
                SaveSlotDescriptor descriptor = descriptors[i];
                lines.Add($"{descriptor.displayName}: {descriptor.compatibilityStatus} | {PrototypeSaveSlotCatalog.FormatLocalTimestamp(descriptor.lastSavedAtUtc)} | {PrototypeSaveSlotCatalog.FormatPlayTime(descriptor.playTimeSeconds)} | Backup={descriptor.backupExists}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string BuildPersistenceIntegrationSummary()
        {
            return context?.Persistence == null
                ? "Persistence integration service is missing."
                : context.Persistence.BuildPersistenceIntegrationDiagnosticSummary();
        }

        public PrototypeTestLabOperation GrantItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Item", "No item definition selected.", "MissingDefinition");
            }

            InventoryAddResult result = context.Inventory.AddItemOrInstances(item, Mathf.Max(1, quantity));
            return Record(result.AddedQuantity > 0, "Grant Item", result.Status.ToString(), $"Requested {result.RequestedQuantity} x {item.DisplayName}; added {result.AddedQuantity}.");
        }

        public PrototypeTestLabOperation GrantStatefulItem(ItemDefinition item)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Stateful Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Stateful Item", "No item definition selected.", "MissingDefinition");
            }

            ItemInstanceCreationResult creation = ItemInstanceFactory.CreateStateful(item, ItemInstanceMetadata.WithoutInstanceState());
            if (!creation.Succeeded)
            {
                return RecordFailure("Grant Stateful Item", creation.Message, creation.Status.ToString());
            }

            InventoryInstanceOperationResult result = context.Inventory.AddItemInstance(creation.ItemInstance);
            return Record(result.Succeeded, "Grant Stateful Item", result.Succeeded ? "Added" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation RemoveItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Remove Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Remove Item", "No item definition selected.", "MissingDefinition");
            }

            bool removed = context.Inventory.RemoveItem(item, Mathf.Max(1, quantity));
            return Record(removed, "Remove Item", removed ? "Removed" : "NotFound", removed ? $"Removed {quantity} x {item.DisplayName}." : $"{item.DisplayName} quantity was not available.");
        }

        public PrototypeTestLabOperation FillInventory(ItemDefinition filler)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Fill Inventory", "Player inventory is missing.", "MissingInventory");
            }

            if (filler == null)
            {
                return RecordFailure("Fill Inventory", "No filler item selected.", "MissingDefinition");
            }

            int safety = context.Inventory.SlotCapacity * Mathf.Max(1, filler.MaximumStackSize);
            int added = 0;
            for (int i = 0; i < safety && context.Inventory.DevelopmentOccupiedSlotCount() < context.Inventory.SlotCapacity; i++)
            {
                InventoryAddResult result = context.Inventory.AddItemOrInstances(filler, 1);
                if (result.AddedQuantity <= 0)
                {
                    break;
                }

                added += result.AddedQuantity;
            }

            return Record(added > 0, "Fill Inventory", added > 0 ? "Filled" : "NoChange", $"Added {added} filler item(s); occupied slots {context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity}.");
        }

        public PrototypeTestLabOperation ClearInventory(bool confirmed)
        {
            if (!RequireConfirmation("ClearInventory", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.Inventory?.DevelopmentClearInventory();
            return RecordSuccess("Clear Inventory", "Inventory cleared. Equipment was preserved.");
        }

        public PrototypeTestLabOperation EquipFirstCompatible(ItemDefinition item)
        {
            if (context?.Inventory == null || context.Equipment == null)
            {
                return RecordFailure("Equip Item", "Inventory or equipment is missing.", "MissingReference");
            }

            for (int i = 0; i < context.Inventory.Slots.Count; i++)
            {
                InventorySlot slot = context.Inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty && slot.Item == item)
                {
                    EquipmentOperationResult result = context.Equipment.EquipFromInventorySlot(i);
                    return Record(result.Succeeded, "Equip Item", result.Succeeded ? "Equipped" : "Failed", result.Message);
                }
            }

            return RecordFailure("Equip Item", "Selected item was not found in inventory.", "NotFound");
        }

        public PrototypeTestLabOperation UnequipAll(bool confirmed)
        {
            if (!RequireConfirmation("UnequipAll", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (context?.Equipment == null)
            {
                return RecordFailure("Unequip All", "Equipment is missing.", "MissingEquipment");
            }

            int changed = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty && context.Equipment.Unequip(slot.SlotType).Succeeded)
                {
                    changed++;
                }
            }

            return RecordSuccess("Unequip All", $"Unequipped {changed} slot(s).");
        }

        public PrototypeTestLabOperation DamagePlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Damage Player", "Player health is missing.", "MissingHealth");
            }

            int applied = context.PlayerHealth.Damage(Mathf.Max(0, amount));
            return RecordSuccess("Damage Player", $"Applied raw test damage {applied}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation HealPlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Heal Player", "Player health is missing.", "MissingHealth");
            }

            int healed = context.PlayerHealth.Heal(Mathf.Max(0, amount));
            return RecordSuccess("Heal Player", $"Healed {healed}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation SetHealth(int value)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Set Health", "Player health is missing.", "MissingHealth");
            }

            bool restored = context.PlayerHealth.TryRestoreForPersistence(Mathf.Clamp(value, 1, context.PlayerHealth.MaximumHealth), out string failureReason);
            return Record(restored, "Set Health", restored ? "Clamped" : "Failed", restored ? $"Health set to {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}." : failureReason);
        }

        public PrototypeTestLabOperation RestoreVitals()
        {
            context?.PlayerHealth?.ResetToMaximum();
            context?.PlayerMana?.RestoreToMaximum();
            context?.PlayerStamina?.RestoreToMaximum();
            return RecordSuccess("Restore Vitals", "Health, mana, and stamina restored to maximum.");
        }

        public PrototypeTestLabOperation AddStrengthTraining()
        {
            return AddAttributeTraining(AttributeIds.Strength, 0.25f, "Strength Base Attribute Training");
        }

        public PrototypeTestLabOperation AddBalancedAttributeTraining()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Balanced Base Attribute Training", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            List<RuntimeAttributeSourceContribution> contributions = new List<RuntimeAttributeSourceContribution>();
            foreach (string attributeId in AttributeIds.AlphaAttributeIds)
            {
                contributions.Add(new RuntimeAttributeSourceContribution
                {
                    attributeId = attributeId,
                    sourceId = "development.test-lab.balanced-training",
                    sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                    amount = 0.1f,
                    removable = false
                });
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                $"development.attribute-growth.{Guid.NewGuid():N}",
                AttributeGrowthEventCategory.Development,
                contributions,
                "Prototype Test Lab",
                out string failureReason);
            return Record(succeeded, "Balanced Base Attribute Training", succeeded ? "Recorded" : "Failed", succeeded ? "Added +0.1 permanent growth to every alpha Base Attribute." : failureReason);
        }

        public PrototypeTestLabOperation SetStrengthAboveHundred()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Set Strength Above 100", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            string sourceId = "development.test-lab.strength-above-100";
            context.PlayerAttributes.RemovePermanentSource(sourceId, out _);
            bool succeeded = context.PlayerAttributes.TryAddPermanentSource(
                sourceId,
                CalculatedStatContributionSourceCategory.Development,
                AttributeIds.Strength,
                100f,
                removable: true,
                out string failureReason);
            return Record(succeeded, "Set Strength Above 100", succeeded ? "Applied" : "Failed", succeeded ? "Strength has a removable +100 permanent development source." : failureReason);
        }

        public PrototypeTestLabOperation AddPhysicalPowerFlat()
        {
            return AddCalculatedContribution(
                "development.test-lab.physical-power-flat",
                CalculatedStatIds.PhysicalPower,
                CalculatedStatContributionKind.Flat,
                CalculatedStatContributionDirection.Improve,
                5f,
                "Add Physical Power");
        }

        public PrototypeTestLabOperation AddPhysicalDefensePenalty()
        {
            return AddCalculatedContribution(
                "development.test-lab.physical-defense-penalty",
                CalculatedStatIds.PhysicalDefense,
                CalculatedStatContributionKind.Flat,
                CalculatedStatContributionDirection.Reduce,
                3f,
                "Add Defense Penalty");
        }

        public PrototypeTestLabOperation ClearFeature52Contributions()
        {
            if (context?.PlayerAttributes != null)
            {
                context.PlayerAttributes.RemovePermanentSource("development.test-lab.strength-above-100", out _);
            }

            bool removedPower = context?.PlayerCalculatedStats != null
                && context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, "development.test-lab.physical-power-flat");
            bool removedDefense = context?.PlayerCalculatedStats != null
                && context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, "development.test-lab.physical-defense-penalty");
            return RecordSuccess("Clear Feature 5.4a Contributions", $"Cleared development Base Attribute/Calculated Stat contributions. Power={removedPower} Defense={removedDefense}.");
        }

        public PrototypeTestLabOperation RecalculateFeature52Stats()
        {
            if (context?.PlayerCalculatedStats == null)
            {
                return RecordFailure("Rebuild Feature 5.4a Stats", "Player Calculated Stats component is missing.", "MissingCalculatedStats");
            }

            context.PlayerCalculatedStats.ForceRecalculateAll();
            return RecordSuccess("Rebuild Feature 5.4a Stats", "Calculated Stat cache rebuilt from Base Attributes and active contributions.");
        }

        public PrototypeTestLabOperation AttemptInvalidAttributeGrowth()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Invalid Base Attribute Growth Proof", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                "development.invalid-growth-proof",
                AttributeGrowthEventCategory.Development,
                new[]
                {
                    new RuntimeAttributeSourceContribution
                    {
                        attributeId = AttributeIds.Strength,
                        sourceId = "development.invalid-growth-proof",
                        sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                        amount = -1f
                    }
                },
                "Prototype Test Lab",
                out string failureReason);
            return Record(!succeeded, "Invalid Base Attribute Growth Proof", succeeded ? "UnexpectedSuccess" : "Rejected", succeeded ? "Invalid negative growth was unexpectedly accepted." : failureReason);
        }

        public PrototypeTestLabOperation DrainMana(float amount)
        {
            VitalChangeResult result = context?.PlayerMana == null
                ? VitalChangeResult.Failure(amount, "Player mana is missing.")
                : context.PlayerMana.Spend(Mathf.Max(0f, amount));
            return Record(result.Succeeded, "Drain Mana", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        private PrototypeTestLabOperation AddAttributeTraining(string attributeId, float amount, string operationName)
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure(operationName, "Player Base Attributes component is missing.", "MissingAttributes");
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                $"development.attribute-growth.{Guid.NewGuid():N}",
                AttributeGrowthEventCategory.Development,
                new[]
                {
                    new RuntimeAttributeSourceContribution
                    {
                        attributeId = attributeId,
                        sourceId = $"development.test-lab.{attributeId}",
                        sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                        amount = amount,
                        removable = false
                    }
                },
                "Prototype Test Lab",
                out string failureReason);
            return Record(succeeded, operationName, succeeded ? "Recorded" : "Failed", succeeded ? $"Added +{amount:0.###} to {attributeId}." : failureReason);
        }

        private PrototypeTestLabOperation AddCalculatedContribution(string sourceId, string statId, CalculatedStatContributionKind kind, CalculatedStatContributionDirection direction, float magnitude, string operationName)
        {
            if (context?.PlayerCalculatedStats == null)
            {
                return RecordFailure(operationName, "Player calculated stats component is missing.", "MissingCalculatedStats");
            }

            context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, sourceId);
            bool succeeded = context.PlayerCalculatedStats.AddContribution(new RuntimeCalculatedStatContribution
            {
                contributionId = sourceId,
                sourceId = sourceId,
                sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                statId = statId,
                kind = (int)kind,
                direction = (int)direction,
                magnitude = magnitude
            }, out string failureReason);
            return Record(succeeded, operationName, succeeded ? "Applied" : "Failed", succeeded ? $"{direction} {statId} by {magnitude:0.###}." : failureReason);
        }

        public PrototypeTestLabOperation DrainStamina(float amount)
        {
            VitalChangeResult result = context?.PlayerStamina == null
                ? VitalChangeResult.Failure(amount, "Player stamina is missing.")
                : context.PlayerStamina.Spend(Mathf.Max(0f, amount), "Development test");
            return Record(result.Succeeded, "Drain Stamina", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ApplyStatus(StatusEffectDefinition status, bool toEnemy)
        {
            StatusEffectController controller = toEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null)
            {
                return RecordFailure("Apply Status", "Target status controller is missing.", "MissingTarget");
            }

            if (status == null)
            {
                return RecordFailure("Apply Status", "No status definition selected.", "MissingDefinition");
            }

            StatusEffectApplicationRequest request = new StatusEffectApplicationRequest(status, null, DevelopmentStatusSource, 0f, string.Empty, Time.time);
            StatusApplicationResult result = controller.ApplyStatus(request);
            return Record(result.Succeeded, "Apply Status", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RemoveStatus(StatusEffectDefinition status, bool fromEnemy)
        {
            StatusEffectController controller = fromEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null || status == null)
            {
                return RecordFailure("Remove Status", "Target status controller or status definition is missing.", "MissingReference");
            }

            bool removed = controller.RemoveStatusesByDefinition(status.Id);
            return Record(removed, "Remove Status", removed ? "Removed" : "NotFound", removed ? $"Removed {status.DisplayName}." : $"{status.DisplayName} was not active.");
        }

        public PrototypeTestLabOperation ClearTemporaryStatuses()
        {
            context?.PlayerStatuses?.ClearTemporaryStatuses();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            return RecordSuccess("Clear Temporary Statuses", "Temporary player and enemy statuses cleared.");
        }

        public PrototypeTestLabOperation ApplyTypedDamage(DamageTypeDefinition damageType, float amount, bool targetEnemy, bool sourcePlayer)
        {
            if (damageType == null)
            {
                return RecordFailure("Apply Typed Damage", "No damage type selected.", "MissingDefinition");
            }

            IDamageable damageable = targetEnemy ? context?.EnemyHealth : context?.PlayerHealth;
            Transform targetTransform = targetEnemy ? context?.EnemyTransform : context?.PlayerTransform;
            GameObject source = sourcePlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (damageable == null || targetTransform == null)
            {
                return RecordFailure("Apply Typed Damage", "Damage target is missing.", "MissingTarget");
            }

            float rawAmount = Mathf.Max(0f, amount);
            DamageComponent component = new DamageComponent(damageType, rawAmount);
            DamagePacket packet = DamagePacket.Single(source, component);
            DamageInfo info = new DamageInfo(rawAmount, source, targetTransform.position, Vector3.forward, DamageType.Physical, packet);
            DamageResult result = damageable.ApplyDamage(in info);
            return Record(result.Applied, "Apply Typed Damage", result.Applied ? "Applied" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation PreviewPipelineDamage(DamageTypeDefinition damageType, float amount, bool targetPlayer)
        {
            if (damageType == null)
            {
                return RecordFailure("Preview 6.1 Damage", "No damage type selected.", "MissingDefinition");
            }

            DamageApplicationRequest request = CreatePipelineDamageRequest(damageType, amount, targetPlayer, string.Empty);
            DamageApplicationResult result = damageHealingService.PreviewDamage(request);
            string message = result.Succeeded
                ? $"{damageType.DisplayName}: requested {result.RequestedAmount:0.###}, defense {result.DefenseApplied:0.###}, resistance {result.ResistanceFraction:0.###}, final {result.FinalDamageAmount:0.###}, Health {result.OldHealth:0.###}->{result.NewHealth:0.###}."
                : result.Message;
            return Record(result.Succeeded, "Preview 6.1 Damage", result.Code, message);
        }

        public PrototypeTestLabOperation ApplyPipelineDamage(DamageTypeDefinition damageType, float amount, bool targetPlayer)
        {
            if (damageType == null)
            {
                return RecordFailure("Apply 6.1 Damage", "No damage type selected.", "MissingDefinition");
            }

            DamageApplicationRequest request = CreatePipelineDamageRequest(damageType, amount, targetPlayer, $"development.damage-healing.{Guid.NewGuid():N}");
            DamageApplicationResult result = damageHealingService.ApplyDamage(request);
            string message = result.Succeeded
                ? $"{damageType.DisplayName}: final {result.FinalDamageAmount:0.###}, Health {result.OldHealth:0.###}->{result.NewHealth:0.###}, Changed={result.HealthChanged}, Immune={result.Immune}, Duplicate={result.Duplicate}."
                : result.Message;
            return Record(result.Succeeded, "Apply 6.1 Damage", result.Code, message);
        }

        public PrototypeTestLabOperation PreviewPipelineHealing(float amount, bool targetPlayer)
        {
            HealingApplicationRequest request = CreatePipelineHealingRequest(amount, targetPlayer, string.Empty);
            HealingApplicationResult result = damageHealingService.PreviewHealing(request);
            string message = result.Succeeded
                ? $"Healing final {result.FinalHealingAmount:0.###}, overheal {result.OverhealAmount:0.###}, Health {result.OldHealth:0.###}->{result.NewHealth:0.###}, Changed={result.HealthChanged}, Duplicate={result.Duplicate}."
                : result.Message;
            return Record(result.Succeeded, "Preview 6.1 Healing", result.Code, message);
        }

        public PrototypeTestLabOperation ApplyPipelineHealing(float amount, bool targetPlayer)
        {
            HealingApplicationRequest request = CreatePipelineHealingRequest(amount, targetPlayer, $"development.damage-healing.{Guid.NewGuid():N}");
            HealingApplicationResult result = damageHealingService.ApplyHealing(request);
            string message = result.Succeeded
                ? $"Healing final {result.FinalHealingAmount:0.###}, overheal {result.OverhealAmount:0.###}, Health {result.OldHealth:0.###}->{result.NewHealth:0.###}, Changed={result.HealthChanged}, Duplicate={result.Duplicate}."
                : result.Message;
            return Record(result.Succeeded, "Apply 6.1 Healing", result.Code, message);
        }

        public PrototypeTestLabOperation ProvePipelineDuplicate(DamageTypeDefinition damageType, float amount)
        {
            if (damageType == null)
            {
                return RecordFailure("6.1 Duplicate Proof", "No damage type selected.", "MissingDefinition");
            }

            string transactionId = $"development.damage-healing.duplicate.{Guid.NewGuid():N}";
            DamageApplicationResult first = damageHealingService.ApplyDamage(CreatePipelineDamageRequest(damageType, amount, targetPlayer: true, transactionId: transactionId));
            DamageApplicationResult second = damageHealingService.ApplyDamage(CreatePipelineDamageRequest(damageType, amount, targetPlayer: true, transactionId: transactionId));
            bool succeeded = first.Succeeded && second.Succeeded && second.Duplicate && !second.HealthChanged;
            string message = $"First={first.Code} changed={first.HealthChanged}; second={second.Code} duplicate={second.Duplicate} changed={second.HealthChanged}.";
            return Record(succeeded, "6.1 Duplicate Proof", succeeded ? "DuplicateProtected" : "UnexpectedResult", message);
        }

        public PrototypeTestLabOperation GenerateAttackTransaction()
        {
            lastAttackTransactionId = AttackDeterministicRoll.NewTransactionId("development.attack-resolution");
            return RecordSuccess("Generate 6.2 Attack Transaction", $"Attack transaction ID: {lastAttackTransactionId}");
        }

        public PrototypeTestLabOperation GenerateLifecycleTransaction()
        {
            lastLifecycleTransactionId = $"development.lifecycle.{Guid.NewGuid():N}";
            return RecordSuccess("Generate 6.3 Lifecycle Transaction", $"Lifecycle transaction ID: {lastLifecycleTransactionId}");
        }

        public PrototypeTestLabOperation PreviewAttackResolution(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float criticalChance, float criticalRoll, float criticalMultiplier, float distance, float maximumRange, bool targetEnemy, bool sourcePlayer)
        {
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, amount, baseHitChance, hitRoll, criticalChance, criticalRoll, criticalMultiplier, distance, maximumRange, targetEnemy, sourcePlayer, transactionId: ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = attackResolutionService.PreviewAttack(request);
            return Record(result.Succeeded, "Preview 6.2 Attack", result.Code, FormatAttackResolution(result));
        }

        public PrototypeTestLabOperation ExecuteAttackResolution(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float criticalChance, float criticalRoll, float criticalMultiplier, float distance, float maximumRange, bool targetEnemy, bool sourcePlayer, bool reuseTransaction)
        {
            AttackResolutionRequest request = CreateAttackResolutionRequest(damageType, amount, baseHitChance, hitRoll, criticalChance, criticalRoll, criticalMultiplier, distance, maximumRange, targetEnemy, sourcePlayer, ResolveAttackTransactionId(reuseTransaction));
            AttackResolutionResult result = attackResolutionService.ExecuteAttack(request);
            return Record(result.Succeeded, reuseTransaction ? "Execute 6.2 Attack Reuse" : "Execute 6.2 Attack", result.Code, FormatAttackResolution(result));
        }

        public PrototypeTestLabOperation ExecuteEnvironmentalAttack(DamageTypeDefinition damageType, float amount, float hitRoll)
        {
            AttackResolutionRequest request = new AttackResolutionRequest(
                ResolveAttackTransactionId(reuse: false),
                AttackSourceType.Environmental,
                null,
                string.Empty,
                context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject,
                ResolveActorId(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject),
                damageType,
                Mathf.Max(0f, amount),
                hitRoll,
                0.5f,
                baseHitChance: 0.95f,
                criticalChance: 0f,
                criticalMultiplier: AttackResolutionRequest.DefaultCriticalMultiplier,
                hasSuppliedDistance: false,
                hasMaximumRange: false,
                originatingActionId: "development.environmental-test");
            AttackResolutionResult result = attackResolutionService.ExecuteAttack(request);
            return Record(result.Succeeded, "Environmental 6.2 Attack", result.Code, FormatAttackResolution(result));
        }

        public string BuildDefensiveActionSummary()
        {
            GameObject player = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            GameObject enemy = context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject;
            EnsureAttackResolutionRuntime(player, needsResource: true);
            EnsureAttackResolutionRuntime(enemy, needsResource: true);
            string playerId = ResolveActorId(player);
            string enemyId = ResolveActorId(enemy);
            string playerDefense = FormatActiveDefense(playerId);
            string enemyDefense = FormatActiveDefense(enemyId);
            string playerStamina = FormatResource(player, ResourceIds.Stamina);
            string enemyStamina = FormatResource(enemy, ResourceIds.Stamina);
            return $"Player Defense: {playerDefense}\nEnemy Defense: {enemyDefense}\nPlayer Stamina: {playerStamina}\nEnemy Stamina: {enemyStamina}";
        }

        public PrototypeTestLabOperation PreviewDefenseActivation(DefensiveActionDefinition definition, bool targetPlayer)
        {
            if (!TryBuildDefenseActivationRequest(definition, targetPlayer, reuseTransaction: false, out DefenseActivationRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            DefenseActivationResult result = defensiveActionService.PreviewActivate(request);
            return Record(result.Succeeded, "Preview 6.6 Defense", result.Code, FormatDefenseActivation(result));
        }

        public PrototypeTestLabOperation ActivateDefense(DefensiveActionDefinition definition, bool targetPlayer, bool reuseTransaction)
        {
            if (!TryBuildDefenseActivationRequest(definition, targetPlayer, reuseTransaction, out DefenseActivationRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            DefenseActivationResult result = defensiveActionService.Activate(request);
            return Record(result.Succeeded, reuseTransaction ? "Activate 6.6 Defense Reuse" : "Activate 6.6 Defense", result.Code, FormatDefenseActivation(result));
        }

        public PrototypeTestLabOperation CancelDefense(bool targetPlayer)
        {
            GameObject target = targetPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (target == null)
            {
                return RecordFailure("Cancel 6.6 Defense", "Defense target is missing.", "MissingTarget");
            }

            DefenseCancellationRequest request = new DefenseCancellationRequest(
                $"development.defense-action.cancel.{Guid.NewGuid():N}",
                ResolveActorId(target),
                target,
                DefenseCancellationReason.Explicit,
                now: Time.time);
            DefenseCancellationResult result = defensiveActionService.Cancel(request);
            return Record(result.Succeeded, "Cancel 6.6 Defense", result.Code, FormatDefenseCancellation(result));
        }

        public PrototypeTestLabOperation PreviewDefensiveAttack(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float defenseRoll, bool targetPlayer)
        {
            AttackResolutionRequest request = CreateDefensiveAttackRequest(damageType, amount, baseHitChance, hitRoll, defenseRoll, targetPlayer, ResolveAttackTransactionId(reuse: false));
            AttackResolutionResult result = attackResolutionService.PreviewAttack(request);
            return Record(result.Succeeded, "Preview 6.6 Defensive Attack", result.Code, FormatAttackResolution(result));
        }

        public PrototypeTestLabOperation ExecuteDefensiveAttack(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float defenseRoll, bool targetPlayer, bool reuseTransaction)
        {
            AttackResolutionRequest request = CreateDefensiveAttackRequest(damageType, amount, baseHitChance, hitRoll, defenseRoll, targetPlayer, ResolveAttackTransactionId(reuseTransaction));
            AttackResolutionResult result = attackResolutionService.ExecuteAttack(request);
            return Record(result.Succeeded, reuseTransaction ? "Execute 6.6 Defensive Attack Reuse" : "Execute 6.6 Defensive Attack", result.Code, FormatAttackResolution(result));
        }

        public string BuildCombatExecutionSummary()
        {
            GameObject player = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            EnsureAttackResolutionRuntime(player, needsResource: true);
            string actorId = ResolveActorId(player);
            CombatExecutionStateSnapshot state = combatExecutionService.GetExecutionState(actorId);
            string active = state == null
                ? "None"
                : $"{state.DefinitionId} Phase={state.Phase} Ready={state.ReadyAt:0.###} RecoveryEnd={state.RecoveryEndsAt:0.###} Instance={state.ExecutionInstanceId}";
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.7 Combat Execution",
                $"Clock: {combatExecutionClockSeconds:0.###}s",
                $"Actor: {(string.IsNullOrWhiteSpace(actorId) ? "None" : actorId)}",
                $"Active: {active}",
                $"Health: {FormatResource(player, ResourceIds.Health)}",
                $"Stamina: {FormatResource(player, ResourceIds.Stamina)}",
                $"Mana: {FormatResource(player, ResourceIds.Mana)}",
                $"Last Begin Tx: {(string.IsNullOrWhiteSpace(lastCombatExecutionBeginTransactionId) ? "None" : lastCombatExecutionBeginTransactionId)}",
                $"Last Commit Tx: {(string.IsNullOrWhiteSpace(lastCombatExecutionCommitTransactionId) ? "None" : lastCombatExecutionCommitTransactionId)}",
                $"Last Instance: {(string.IsNullOrWhiteSpace(lastCombatExecutionInstanceId) ? "None" : lastCombatExecutionInstanceId)}",
                FormatCombatExecutionCooldowns(actorId)
            });
        }

        public PrototypeTestLabOperation PreviewCombatExecution(CombatExecutionDefinition definition)
        {
            if (!TryBuildCombatExecutionBeginRequest(definition, reuseTransaction: false, out CombatExecutionBeginRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatExecutionResult result = combatExecutionService.PreviewBeginExecution(request);
            return Record(result.Succeeded, "Preview 6.7 Execution", result.Code, FormatCombatExecutionResult(result));
        }

        public PrototypeTestLabOperation BeginCombatExecution(CombatExecutionDefinition definition, bool reuseTransaction)
        {
            if (!TryBuildCombatExecutionBeginRequest(definition, reuseTransaction, out CombatExecutionBeginRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatExecutionResult result = combatExecutionService.BeginExecution(request);
            if (result.Succeeded && result.State != null)
            {
                lastCombatExecutionInstanceId = result.State.ExecutionInstanceId;
            }

            return Record(result.Succeeded, reuseTransaction ? "Begin 6.7 Execution Reuse" : "Begin 6.7 Execution", result.Code, FormatCombatExecutionResult(result));
        }

        public PrototypeTestLabOperation CommitCombatExecution(bool reuseTransaction)
        {
            if (!TryBuildCombatExecutionCommitRequest(reuseTransaction, out CombatExecutionCommitRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatExecutionResult result = combatExecutionService.CommitExecution(request);
            return Record(result.Succeeded, reuseTransaction ? "Commit 6.7 Execution Reuse" : "Commit 6.7 Execution", result.Code, FormatCombatExecutionResult(result));
        }

        public PrototypeTestLabOperation CancelCombatExecution()
        {
            if (!TryBuildCombatExecutionCancelRequest(CombatExecutionCancellationReason.PlayerOrAIRequest, out CombatExecutionCancelRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatExecutionResult result = combatExecutionService.CancelExecution(request);
            if (result.Succeeded)
            {
                lastCombatExecutionInstanceId = string.Empty;
            }

            return Record(result.Succeeded, "Cancel 6.7 Execution", result.Code, FormatCombatExecutionResult(result));
        }

        public PrototypeTestLabOperation InterruptCombatExecution()
        {
            if (!TryBuildCombatExecutionCancelRequest(CombatExecutionCancellationReason.InterruptedByDamage, out CombatExecutionCancelRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatExecutionResult result = combatExecutionService.InterruptExecution(request);
            if (result.Succeeded)
            {
                lastCombatExecutionInstanceId = string.Empty;
            }

            return Record(result.Succeeded, "Interrupt 6.7 Execution", result.Code, FormatCombatExecutionResult(result));
        }

        public PrototypeTestLabOperation AdvanceCombatExecutionClock(float seconds)
        {
            float delta = Mathf.Max(0f, seconds);
            combatExecutionClockSeconds += delta;
            IReadOnlyList<CombatExecutionResult> results = combatExecutionService.ProcessExecutionTime(combatExecutionClockSeconds);
            string message = results.Count == 0
                ? $"Advanced 6.7 clock by {delta:0.###}s. No completions."
                : string.Join(Environment.NewLine, results.Select(FormatCombatExecutionResult));
            return RecordSuccess("Advance 6.7 Execution Clock", message);
        }

        public PrototypeTestLabOperation ClearCombatExecutionForRestore()
        {
            combatExecutionService.ClearTransientStateForRestore();
            lastCombatExecutionInstanceId = string.Empty;
            return RecordSuccess("Restore Clear 6.7 Execution", "Cleared transient combat execution commitments without emitting cancellation or interruption state through persistence.");
        }

        public PrototypeTestLabOperation SnapshotCombatExecution()
        {
            CombatExecutionSaveData saveData = combatExecutionService.CreateSaveData(PersistenceService.LocalPlayerId, "person.prototype-player");
            return RecordSuccess("Snapshot 6.7 Execution", $"Cooldown records: {(saveData.cooldowns == null ? 0 : saveData.cooldowns.Count)}. Transient commitments are not saved.");
        }

        public string BuildCombatStateSummary()
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            EnsureCombatStateTestParticipants();
            string playerId = ResolveCombatStateActorId(GetCombatStateTestActor("A"));
            string enemyId = ResolveCombatStateActorId(GetCombatStateTestActor("B"));
            ActorCombatStateSnapshot player = combatState == null ? null : combatState.GetCombatState(playerId);
            ActorCombatStateSnapshot enemy = combatState == null ? null : combatState.GetCombatState(enemyId);
            ActorCombatStateSnapshot c = combatState == null ? null : combatState.GetCombatState(ResolveCombatStateActorId(GetCombatStateTestActor("C")));
            ActorCombatStateSnapshot d = combatState == null ? null : combatState.GetCombatState(ResolveCombatStateActorId(GetCombatStateTestActor("D")));
            CombatEncounterSnapshot encounter = null;
            if (combatState != null)
            {
                encounter = combatState.GetEncounterForActor(playerId);
                if (encounter == null)
                {
                    encounter = combatState.GetEncounterForActor(enemyId);
                }
            }
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.5 Combat State",
                $"Clock: {combatStateClockSeconds:0.###}s Timeout: {(combatState == null ? 10f : combatState.CombatTimeoutSeconds):0.###}s",
                FormatCombatStateSnapshot("A Player", player),
                FormatCombatStateSnapshot("B Enemy", enemy),
                FormatCombatStateSnapshot("C Mock", c),
                FormatCombatStateSnapshot("D Mock", d),
                FormatCombatEncounter(encounter),
                $"Last Combat Tx: {(string.IsNullOrWhiteSpace(lastCombatStateTransactionId) ? "None" : lastCombatStateTransactionId)}",
                $"Last Split Tx: {(string.IsNullOrWhiteSpace(lastCombatStateSplitTransactionId) ? "None" : lastCombatStateSplitTransactionId)}"
            });
        }

        public PrototypeTestLabOperation GenerateCombatStateTransaction()
        {
            lastCombatStateTransactionId = $"development.combat-state.{Guid.NewGuid():N}";
            return RecordSuccess("Generate 6.5 Combat Transaction", lastCombatStateTransactionId);
        }

        public PrototypeTestLabOperation PreviewExplicitCombatEngagement()
        {
            if (!TryBuildCombatEngagementRequest(CombatActivityClassification.ExplicitEngagement, reuseTransaction: false, out CombatStateService service, out CombatEngagementRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatEntryResult result = service.PreviewEnterCombat(request);
            return Record(result.Succeeded, "Preview 6.5 Engagement", result.Code, FormatCombatEntryResult(result));
        }

        public PrototypeTestLabOperation ExecuteExplicitCombatEngagement(bool reuseTransaction)
        {
            if (!TryBuildCombatEngagementRequest(CombatActivityClassification.ExplicitEngagement, reuseTransaction, out CombatStateService service, out CombatEngagementRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CombatEntryResult result = service.EnterCombat(request);
            return Record(result.Succeeded, reuseTransaction ? "Execute 6.5 Engagement Reuse" : "Execute 6.5 Engagement", result.Code, FormatCombatEntryResult(result));
        }

        public PrototypeTestLabOperation ExecuteCombatStateAttack(DamageTypeDefinition damageType, bool miss, bool blocked, bool prevented)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            if (combatState == null)
            {
                return RecordFailure("6.5 Attack", "Combat State service is missing.", "MissingCombatState");
            }

            float hitRoll = miss ? 0.99f : 0.1f;
            float distance = blocked ? 999f : 1f;
            float amount = prevented ? 0f : 10f;
            AttackResolutionRequest request = CreateCombatStateAttackResolutionRequest(damageType, amount, 0.75f, hitRoll, 0f, 0.5f, 1.5f, distance, 2f, ResolveCombatStateTransactionId(reuse: false));
            AttackResolutionResult attack = attackResolutionService.ExecuteAttack(request);
            CombatEntryResult combat = combatState.RecordAttackResult(attack);
            string operation = blocked ? "Blocked 6.5 Attack" : miss ? "Miss 6.5 Attack" : prevented ? "Prevented 6.5 Attack" : "Hit 6.5 Attack";
            bool expectedOutcome = blocked ? !attack.Succeeded && !combat.Succeeded : attack.Succeeded && combat.Succeeded;
            return Record(expectedOutcome, operation, attack.Succeeded ? combat.Code : attack.Code, $"{FormatAttackResolution(attack)} Combat={FormatCombatEntryResult(combat)}");
        }

        public PrototypeTestLabOperation AdvanceCombatState(float deltaSeconds)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            if (combatState == null)
            {
                return RecordFailure("Advance 6.5 Combat", "Combat State service is missing.", "MissingCombatState");
            }

            float delta = Mathf.Max(0f, deltaSeconds);
            combatStateClockSeconds += delta;
            CombatStateProcessResult result = combatState.AdvanceTime(delta);
            return RecordSuccess("Advance 6.5 Combat", FormatCombatProcessResult(result));
        }

        public PrototypeTestLabOperation ForceCombatExit(bool targetEnemy)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            GameObject actor = targetEnemy ? context?.EnemyTransform?.gameObject : context?.PlayerTransform?.gameObject;
            string actorId = ResolveCombatStateActorId(actor);
            CombatExitResult result = combatState == null
                ? null
                : combatState.LeaveCombat(new CombatExitRequest(ResolveCombatStateTransactionId(reuse: false), actorId, actor, CombatExitReason.Forced, authoritative: true));
            return Record(result != null && result.Succeeded, targetEnemy ? "Force Enemy Combat Exit" : "Force Player Combat Exit", result == null ? "MissingCombatState" : result.Code, FormatCombatExitResult(result));
        }

        public PrototypeTestLabOperation EndCurrentCombatEncounter()
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            string playerId = ResolveCombatStateActorId(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            CombatEncounterSnapshot encounter = combatState == null ? null : combatState.GetEncounterForActor(playerId);
            if (combatState == null || encounter == null)
            {
                return RecordFailure("End 6.5 Encounter", "No active combat encounter is available.", "MissingEncounter");
            }

            CombatEncounterSnapshot ended = combatState.EndEncounter(new CombatEncounterEndRequest(ResolveCombatStateTransactionId(reuse: false), encounter.EncounterId, CombatEncounterCompletionReason.Forced, authoritative: true));
            return Record(ended != null && !ended.Active, "End 6.5 Encounter", ended == null ? "MissingEncounter" : "Success", FormatCombatEncounter(ended));
        }

        public PrototypeTestLabOperation PrepareCombatStateSplitParticipants()
        {
            EnsureCombatStateTestParticipants();
            string summary = string.Join(" | ", new[] { "A", "B", "C", "D" }.Select(key => $"{key}={ResolveCombatStateActorId(GetCombatStateTestActor(key))}"));
            return RecordSuccess("Prepare 6.5 Split Participants", summary);
        }

        public PrototypeTestLabOperation EngageCombatStateParticipants(string firstKey, string secondKey)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            GameObject first = GetCombatStateTestActor(firstKey);
            GameObject second = GetCombatStateTestActor(secondKey);
            if (combatState == null || first == null || second == null)
            {
                return RecordFailure($"Engage {firstKey}-{secondKey}", "Combat State service or participant is missing.", "MissingReference");
            }

            CombatEntryResult result = combatState.EnterCombat(new CombatEngagementRequest(
                ResolveCombatStateTransactionId(reuse: false),
                ResolveCombatStateActorId(first),
                first,
                ResolveCombatStateActorId(second),
                second,
                CombatActivityClassification.ExplicitEngagement,
                "development.test-lab.split",
                hostile: true,
                authorityValidated: true));
            return Record(result.Succeeded, $"Engage {firstKey}-{secondKey}", result.Code, FormatCombatEntryResult(result));
        }

        public PrototypeTestLabOperation EndCombatStateEngagement(string firstKey, string secondKey, bool reuseTransaction)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            GameObject first = GetCombatStateTestActor(firstKey);
            GameObject second = GetCombatStateTestActor(secondKey);
            if (combatState == null || first == null || second == null)
            {
                return RecordFailure($"End {firstKey}-{secondKey}", "Combat State service or participant is missing.", "MissingReference");
            }

            CombatEncounterSplitResult result = combatState.EndEngagement(new CombatEngagementEndRequest(
                ResolveCombatStateSplitTransactionId(reuseTransaction),
                string.Empty,
                ResolveCombatStateActorId(first),
                ResolveCombatStateActorId(second),
                CombatExitReason.Forced,
                authoritative: true));
            return Record(result.Succeeded, $"End {firstKey}-{secondKey}", result.Code, FormatCombatSplitResult(result));
        }

        public PrototypeTestLabOperation ProcessCombatStateConnectivity()
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            string actorId = ResolveCombatStateActorId(GetCombatStateTestActor("A"));
            CombatEncounterSnapshot encounter = combatState == null ? null : combatState.GetEncounterForActor(actorId);
            if (combatState == null || encounter == null)
            {
                return RecordFailure("Process 6.5 Connectivity", "No active A encounter is available.", "MissingEncounter");
            }

            CombatEncounterSplitResult result = combatState.ProcessEncounterConnectivity(ResolveCombatStateSplitTransactionId(reuse: false), encounter.EncounterId);
            return Record(result.Succeeded, "Process 6.5 Connectivity", result.Code, FormatCombatSplitResult(result));
        }

        public PrototypeTestLabOperation ForceCombatStateParticipantExit(string key)
        {
            CombatStateService combatState = EnsureCombatStateRuntime();
            GameObject actor = GetCombatStateTestActor(key);
            string actorId = ResolveCombatStateActorId(actor);
            CombatExitResult result = combatState == null
                ? null
                : combatState.LeaveCombat(new CombatExitRequest(ResolveCombatStateTransactionId(reuse: false), actorId, actor, CombatExitReason.Forced, authoritative: true));
            return Record(result != null && result.Succeeded, $"Force {key} Combat Exit", result == null ? "MissingCombatState" : result.Code, FormatCombatExitResult(result));
        }

        public PrototypeTestLabOperation KillCombatStateParticipant(string key)
        {
            GameObject actor = GetCombatStateTestActor(key);
            if (actor == null)
            {
                return RecordFailure($"Kill {key}", "Combat State participant is missing.", "MissingReference");
            }

            ActorLifecycleController lifecycle = actor.GetComponentInParent<ActorLifecycleController>();
            CharacterResourceCollection resources = actor.GetComponentInParent<CharacterResourceCollection>();
            if (lifecycle == null || resources == null)
            {
                return RecordFailure($"Kill {key}", "Combat State participant lifecycle or resources are missing.", "MissingLifecycle");
            }

            ActorLifecycleResult death = lifecycle.ExecuteDeath(new LifecycleDeathRequest($"development.combat-state.kill.{Guid.NewGuid():N}", ResolveCombatStateActorId(GetCombatStateTestActor("A")), GetCombatStateTestActor("A"), ResolveCombatStateActorId(actor), actor, LifecycleTriggerKind.ExplicitDeath));
            CombatStateProcessResult process = EnsureCombatStateRuntime()?.AdvanceTime(0f);
            return Record(death.Succeeded, $"Kill {key}", death.Code, $"{death.Message} Combat={FormatCombatProcessResult(process)}");
        }

        public PrototypeTestLabOperation ValidateCombatStateIntegrity()
        {
            CombatStateIntegrityResult result = EnsureCombatStateRuntime()?.ValidateIntegrity();
            if (result == null)
            {
                return RecordFailure("Validate 6.5 Integrity", "Combat State service is missing.", "MissingCombatState");
            }

            string message = result.Diagnostics.Count == 0 ? "Combat State integrity is valid." : string.Join(" | ", result.Diagnostics);
            return Record(result.Succeeded, "Validate 6.5 Integrity", result.Succeeded ? "Valid" : CombatStateResultCode.IntegrityViolation, message);
        }

        public PrototypeTestLabOperation ResetEnemy()
        {
            context?.EnemyAttack?.ResetCooldown();
            context?.EnemyController?.ResetControllerState();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            context?.EnemyHealth?.ResetToMaximum();
            return RecordSuccess("Reset Enemy", "Enemy health, cooldown, controller state, and temporary statuses reset.");
        }

        public string BuildLifecycleSummary()
        {
            EnsureLifecycleRuntime(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject, ref context.PlayerLifecycle, needsResource: true);
            EnsureLifecycleRuntime(context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject, ref context.EnemyLifecycle, needsResource: true);
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.3 Actor Lifecycle",
                FormatLifecycleSummary("Player", context?.PlayerLifecycle, context?.PlayerResources),
                FormatLifecycleSummary("Enemy", context?.EnemyLifecycle, context?.EnemyTransform == null ? null : context.EnemyTransform.GetComponentInParent<CharacterResourceCollection>()),
                $"Last Lifecycle Tx: {(string.IsNullOrWhiteSpace(lastLifecycleTransactionId) ? "None" : lastLifecycleTransactionId)}"
            });
        }

        public PrototypeTestLabOperation PreviewDefeatLifecycle(bool targetEnemy)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.PreviewDefeat(new DefeatResolutionRequest(string.Empty, "development.test-lab", null, actorId, target, LifecycleTriggerKind.ExplicitDefeat, reason: "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Preview Enemy Defeat" : "Preview Player Defeat", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation ExecuteDefeatLifecycle(bool targetEnemy, bool reuseTransaction)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.ExecuteDefeat(new DefeatResolutionRequest(ResolveLifecycleTransactionId(reuseTransaction), "development.test-lab", null, actorId, target, LifecycleTriggerKind.ExplicitDefeat, reason: "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Defeat Enemy Lifecycle" : "Defeat Player Lifecycle", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation ApplyZeroHealthLifecycleDamage(DamageTypeDefinition damageType, bool targetEnemy)
        {
            if (damageType == null)
            {
                return RecordFailure("Lifecycle Zero Health", "No damage type selected.", "MissingDefinition");
            }

            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController _, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            CharacterResourceCollection targetResources = target.GetComponentInParent<CharacterResourceCollection>();
            if (targetResources == null || !targetResources.TryGetResource(ResourceIds.Health, out ResourceSnapshot health))
            {
                return RecordFailure("Lifecycle Zero Health", "Target Health resource is missing.", "MissingHealth");
            }

            string transactionId = ResolveLifecycleTransactionId(reuse: false);
            float amount = Mathf.Max(1f, health.Current - health.Minimum + 1000f);
            GameObject source = targetEnemy ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            DamageApplicationRequest request = new DamageApplicationRequest(transactionId, ResolveActorId(source), source, actorId, target, damageType, amount, "Prototype Test Lab zero-health lifecycle proof");
            DamageApplicationResult result = damageHealingService.ApplyDamage(request);
            return Record(result.Succeeded, targetEnemy ? "Zero Health Enemy" : "Zero Health Player", result.Code, $"Damage={result.FinalDamageAmount:0.###} Health={result.OldHealth:0.###}->{result.NewHealth:0.###} BecameZero={result.BecameZero} Lifecycle={ActorLifecycleUtility.GetState(target)} Duplicate={result.Duplicate}.");
        }

        public PrototypeTestLabOperation PreviewRecoveryLifecycle(bool targetEnemy, float amount)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.PreviewRecovery(new LifecycleRecoveryRequest(string.Empty, "development.test-lab", null, actorId, target, Mathf.Max(0f, amount), "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Preview Enemy Recovery" : "Preview Player Recovery", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation ExecuteRecoveryLifecycle(bool targetEnemy, float amount, bool reuseTransaction)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.ExecuteRecovery(new LifecycleRecoveryRequest(ResolveLifecycleTransactionId(reuseTransaction), "development.test-lab", null, actorId, target, Mathf.Max(0f, amount), "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Recover Enemy" : "Recover Player", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation PreviewDeathLifecycle(bool targetEnemy)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.PreviewDeath(new LifecycleDeathRequest(string.Empty, "development.test-lab", null, actorId, target, LifecycleTriggerKind.ExplicitDeath, "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Preview Enemy Death" : "Preview Player Death", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation ExecuteDeathLifecycle(bool targetEnemy, bool reuseTransaction)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.ExecuteDeath(new LifecycleDeathRequest(ResolveLifecycleTransactionId(reuseTransaction), "development.test-lab", null, actorId, target, LifecycleTriggerKind.ExplicitDeath, "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Kill Enemy Lifecycle" : "Kill Player Lifecycle", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation PreviewRevivalLifecycle(bool targetEnemy, float amount)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.PreviewRevival(new LifecycleRevivalRequest(string.Empty, "development.test-lab", null, actorId, target, Mathf.Max(0f, amount), "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Preview Enemy Revival" : "Preview Player Revival", result.Code, FormatLifecycleResult(result));
        }

        public PrototypeTestLabOperation ExecuteRevivalLifecycle(bool targetEnemy, float amount, bool reuseTransaction)
        {
            if (!TryResolveLifecycleTarget(targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ActorLifecycleResult result = lifecycle.ExecuteRevival(new LifecycleRevivalRequest(ResolveLifecycleTransactionId(reuseTransaction), "development.test-lab", null, actorId, target, Mathf.Max(0f, amount), "Prototype Test Lab"));
            return Record(result.Succeeded, targetEnemy ? "Revive Enemy" : "Revive Player", result.Code, FormatLifecycleResult(result));
        }

        public string BuildOngoingEffectsSummary()
        {
            OngoingEffectService playerService = EnsureOngoingEffectRuntime(targetEnemy: false);
            OngoingEffectService enemyService = EnsureOngoingEffectRuntime(targetEnemy: true);
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.4 Ongoing Effects",
                $"Clock: {ongoingEffectClockSeconds:0.###}s",
                FormatOngoingEffectTarget("Player", playerService, context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject, context?.PlayerResources, context?.PlayerLifecycle),
                FormatOngoingEffectTarget("Enemy", enemyService, context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject, context?.EnemyTransform == null ? null : context.EnemyTransform.GetComponentInParent<CharacterResourceCollection>(), context?.EnemyLifecycle),
                $"Last Ongoing Tx: {(string.IsNullOrWhiteSpace(lastOngoingEffectTransactionId) ? "None" : lastOngoingEffectTransactionId)}"
            });
        }

        public PrototypeTestLabOperation GenerateOngoingEffectTransaction()
        {
            lastOngoingEffectTransactionId = $"development.ongoing-effect.{Guid.NewGuid():N}";
            return RecordSuccess("Fresh Ongoing Effect Tx", lastOngoingEffectTransactionId);
        }

        public PrototypeTestLabOperation PreviewOngoingEffect(OngoingEffectDefinition definition, bool targetEnemy, float amount, float interval, float duration, int tickCount, int stacks)
        {
            if (!TryBuildOngoingEffectRequest(definition, targetEnemy, amount, interval, duration, tickCount, stacks, reuseTransaction: false, out OngoingEffectService service, out OngoingEffectApplicationRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            OngoingEffectApplicationResult result = service.PreviewApplyOngoingEffect(request);
            return Record(result.Succeeded, targetEnemy ? "Preview Enemy Ongoing Effect" : "Preview Player Ongoing Effect", result.Code, FormatOngoingApplicationResult(result));
        }

        public PrototypeTestLabOperation ApplyOngoingEffect(OngoingEffectDefinition definition, bool targetEnemy, float amount, float interval, float duration, int tickCount, int stacks, bool reuseTransaction)
        {
            if (!TryBuildOngoingEffectRequest(definition, targetEnemy, amount, interval, duration, tickCount, stacks, reuseTransaction, out OngoingEffectService service, out OngoingEffectApplicationRequest request, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            OngoingEffectApplicationResult result = service.ApplyOngoingEffect(request);
            return Record(result.Succeeded, targetEnemy ? "Apply Enemy Ongoing Effect" : "Apply Player Ongoing Effect", result.Code, FormatOngoingApplicationResult(result));
        }

        public PrototypeTestLabOperation AdvanceOngoingEffects(float deltaSeconds)
        {
            float delta = Mathf.Max(0f, deltaSeconds);
            ongoingEffectClockSeconds += delta;
            OngoingEffectProcessResult player = EnsureOngoingEffectRuntime(targetEnemy: false)?.AdvanceTime(delta);
            OngoingEffectProcessResult enemy = EnsureOngoingEffectRuntime(targetEnemy: true)?.AdvanceTime(delta);
            return RecordSuccess("Advance Ongoing Effects", $"Advanced {delta:0.###}s. Player={FormatOngoingProcessResult(player)} Enemy={FormatOngoingProcessResult(enemy)}");
        }

        public PrototypeTestLabOperation ProcessOngoingEffectsNow()
        {
            OngoingEffectProcessResult player = EnsureOngoingEffectRuntime(targetEnemy: false)?.ProcessDueTicks(0f);
            OngoingEffectProcessResult enemy = EnsureOngoingEffectRuntime(targetEnemy: true)?.ProcessDueTicks(0f);
            return RecordSuccess("Process Ongoing Effects", $"Processed due ticks without advancing time. Player={FormatOngoingProcessResult(player)} Enemy={FormatOngoingProcessResult(enemy)}");
        }

        public PrototypeTestLabOperation CancelFirstOngoingEffect(bool targetEnemy, bool preview)
        {
            OngoingEffectService service = EnsureOngoingEffectRuntime(targetEnemy);
            RuntimeOngoingEffectInstance instance = service == null ? null : service.ActiveInstances.FirstOrDefault();
            if (instance == null)
            {
                return RecordFailure("Cancel Ongoing Effect", "No active ongoing effect instance is available.", "MissingInstance");
            }

            OngoingEffectCancellationRequest request = new OngoingEffectCancellationRequest($"development.ongoing-cancel.{Guid.NewGuid():N}", instance.InstanceId, instance.TargetActorId, instance.TargetObject, "Prototype Test Lab");
            OngoingEffectCancellationResult result = preview ? service.PreviewCancelOngoingEffect(request) : service.CancelOngoingEffect(request);
            return Record(result.Succeeded, preview ? "Preview Cancel Ongoing Effect" : "Cancel Ongoing Effect", result.Code, FormatOngoingCancellationResult(result));
        }

        public string BuildCombatReactionSummary()
        {
            CombatReactionService service = EnsureCombatReactionRuntime();
            IReadOnlyList<CombatReactionSourceRegistration> registrations = service == null ? Array.Empty<CombatReactionSourceRegistration>() : service.Registrations;
            string selected = string.Join(Environment.NewLine, GetDefinitions<CombatReactionDefinition>().Select(definition => $"{definition.DisplayName} ({definition.Id}) Triggers={string.Join(",", definition.TriggerTypes)} Op={definition.OperationType} Target={definition.TargetPolicy} Chance={definition.ProcChance:0.###} Priority={definition.Priority}").Take(8));
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.8 Combat Reactions",
                $"Registered Sources: {registrations.Count}",
                registrations.Count == 0 ? "Sources: None" : $"Sources: {string.Join(" | ", registrations.Select(registration => $"{registration.Definition.Id}@{registration.OwnerActorId}:{registration.SourceStableId}:{registration.SourceInstanceId}"))}",
                string.IsNullOrWhiteSpace(selected) ? "Definitions: None" : selected
            });
        }

        public PrototypeTestLabOperation RegisterCombatReaction(CombatReactionDefinition definition, bool ownerPlayer)
        {
            CombatReactionService service = EnsureCombatReactionRuntime();
            GameObject owner = ownerPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (service == null || owner == null || definition == null)
            {
                return RecordFailure("Register 6.8 Reaction", "Combat reaction service, owner, or definition is missing.", CombatReactionResultCode.MissingDefinition);
            }

            EnsureAttackResolutionRuntime(owner, needsResource: true);
            string ownerActorId = ResolveActorId(owner);
            CombatReactionSourceRegistration registration = new CombatReactionSourceRegistration(
                $"development.reaction.{definition.Id}.{ownerActorId}",
                ownerActorId,
                owner,
                CombatReactionSourceKind.Development,
                $"development.{definition.Id}",
                "prototype-test-lab",
                0,
                definition);
            CombatReactionSourceRegistration registered = service.RegisterSource(registration);
            return RecordSuccess("Register 6.8 Reaction", $"Registered {registered.Definition.Id} for {(ownerPlayer ? "player" : "enemy")} actor {ownerActorId}.");
        }

        public PrototypeTestLabOperation ClearCombatReactions()
        {
            CombatReactionService service = EnsureCombatReactionRuntime();
            service?.ClearAllSources();
            service?.ClearTransientStateForRestore();
            return RecordSuccess("Clear 6.8 Reactions", "Combat reaction sources and transient chain state cleared.");
        }

        public PrototypeTestLabOperation PreviewCombatReactionDamage(CombatReactionDefinition definition)
        {
            return RunCombatReactionDamage(definition, execute: false, critical: false, rootId: $"development.reaction.preview.{Guid.NewGuid():N}");
        }

        public PrototypeTestLabOperation ExecuteCombatReactionDamage(CombatReactionDefinition definition)
        {
            return RunCombatReactionDamage(definition, execute: true, critical: false, rootId: $"development.reaction.execute.{Guid.NewGuid():N}");
        }

        public PrototypeTestLabOperation ExecuteCombatReactionCritical(CombatReactionDefinition definition)
        {
            return RunCombatReactionDamage(definition, execute: true, critical: true, rootId: $"development.reaction.critical.{Guid.NewGuid():N}");
        }

        public PrototypeTestLabOperation ExecuteCombatReactionDuplicateProof(CombatReactionDefinition definition)
        {
            string rootId = $"development.reaction.duplicate.{Guid.NewGuid():N}";
            PrototypeTestLabOperation first = RunCombatReactionDamage(definition, execute: true, critical: false, rootId: rootId);
            PrototypeTestLabOperation second = RunCombatReactionDamage(definition, execute: true, critical: false, rootId: rootId);
            bool succeeded = first.Succeeded && second.Succeeded && second.Code == CombatReactionResultCode.Duplicate;
            return Record(succeeded, "Duplicate 6.8 Reaction Proof", second.Code, $"First={first.Code} Second={second.Code}. {second.Message}");
        }

        private PrototypeTestLabOperation RunCombatReactionDamage(CombatReactionDefinition definition, bool execute, bool critical, string rootId)
        {
            if (definition == null)
            {
                return RecordFailure("6.8 Reaction", "Combat reaction definition is missing.", CombatReactionResultCode.MissingDefinition);
            }

            CombatReactionService service = EnsureCombatReactionRuntime();
            GameObject source = context?.PlayerTransform?.gameObject;
            GameObject target = context?.EnemyTransform?.gameObject;
            if (service == null || source == null || target == null)
            {
                return RecordFailure("6.8 Reaction", "Combat reaction service, source, or target is missing.", CombatReactionResultCode.MissingTarget);
            }

            EnsureAttackResolutionRuntime(source, needsResource: true);
            EnsureAttackResolutionRuntime(target, needsResource: true);
            CombatReactionTriggerContext trigger = new CombatReactionTriggerContext(
                critical ? CombatReactionTriggerType.CriticalHit : CombatReactionTriggerType.DamageApplied,
                rootId,
                ResolveActorId(source),
                source,
                ResolveActorId(target),
                target,
                actualDamage: 25f,
                critical: critical,
                damageType: GetDefinitions<DamageTypeDefinition>().FirstOrDefault());
            CombatReactionChainResult result = execute ? service.ExecuteTrigger(trigger) : service.PreviewTrigger(trigger);
            return Record(result.Succeeded, execute ? critical ? "Execute 6.8 Critical Reaction" : "Execute 6.8 Reaction" : "Preview 6.8 Reaction", result.Code, FormatCombatReactionChain(result));
        }

        public string BuildCombatContributionSummary()
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            IReadOnlyList<CombatContributionLedgerSnapshot> ledgers = service == null ? Array.Empty<CombatContributionLedgerSnapshot>() : service.GetLedgerSnapshots();
            string ledgerText = ledgers.Count == 0
                ? "Ledgers: None"
                : string.Join(Environment.NewLine, ledgers.Select(FormatContributionLedger));
            CombatContributionPolicyDefinition policy = GetDefinitions<CombatContributionPolicyDefinition>().FirstOrDefault();
            return string.Join(Environment.NewLine, new[]
            {
                "Feature 6.9 Combat Contribution",
                $"Policy: {(policy == null ? "None" : $"{policy.DisplayName} ({policy.Id}) Window={policy.ContributionWindowSeconds:0.###}s")}",
                $"Clock: {(service == null ? 0f : service.SimulationTime):0.###}s",
                ledgerText
            });
        }

        public PrototypeTestLabOperation PreviewContribution(DamageTypeDefinition damageType)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            DamageApplicationRequest request = CreatePipelineDamageRequest(damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault(), 25f, targetPlayer: false, transactionId: $"development.contribution.preview.{Guid.NewGuid():N}");
            CombatContributionRecordRequest contribution = new CombatContributionRecordRequest(
                request.TransactionId,
                CombatContributionType.DamageApplied,
                request.SourceActorId,
                string.Empty,
                string.Empty,
                request.TargetActorId,
                string.Empty,
                request.RequestedAmount,
                Mathf.Max(0f, request.RequestedAmount),
                0f,
                service == null ? 0f : service.SimulationTime,
                CombatContributionSourceKind.Development,
                preview: true);
            CombatContributionRecordResult result = service == null
                ? CombatContributionRecordResult.Failure(true, CombatContributionResultCode.MissingPolicy, "Contribution service is missing.", 0, 0)
                : service.PreviewContribution(contribution);
            return Record(result.Succeeded, "Preview 6.9 Contribution", result.Code, FormatContributionRecordResult(result));
        }

        public PrototypeTestLabOperation RecordDamageContribution(DamageTypeDefinition damageType, bool reuseTransaction)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            DamageTypeDefinition selected = damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault();
            if (service == null || selected == null)
            {
                return RecordFailure("Record 6.9 Damage", "Contribution service or damage type is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string transactionId = reuseTransaction && !string.IsNullOrWhiteSpace(lastAttackTransactionId)
                ? lastAttackTransactionId
                : $"development.contribution.damage.{Guid.NewGuid():N}";
            lastAttackTransactionId = transactionId;
            DamageApplicationResult damage;
            if (reuseTransaction && lastContributionDamageSource != null)
            {
                damage = lastContributionDamageSource;
            }
            else
            {
                if (!TryPrepareContributionHealth(targetPlayer: false, desiredCurrent: 50f, out string healthMessage))
                {
                    return RecordFailure("Record 6.9 Damage", healthMessage, "MissingHealth");
                }

                damage = damageHealingService.ApplyDamage(CreatePipelineDamageRequest(selected, 25f, targetPlayer: false, transactionId));
                if (damage.Succeeded && !damage.Duplicate)
                {
                    lastContributionDamageSource = damage;
                }
            }

            CombatContributionRecordResult result = service.RecordDamage(damage, sourceKind: CombatContributionSourceKind.Development);
            if (result.Record != null && !string.IsNullOrWhiteSpace(result.Record.TargetActorId))
            {
                lastContributionCreditTargetActorId = result.Record.TargetActorId;
            }

            return Record(result.Succeeded || result.Duplicate, reuseTransaction ? "Record 6.9 Damage Reuse" : "Record 6.9 Damage", result.Code, $"{FormatDamageApplication(damage)} {FormatContributionRecordResult(result)}");
        }

        public PrototypeTestLabOperation RecordHealingContribution(bool reuseTransaction)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            if (service == null)
            {
                return RecordFailure("Record 6.9 Healing", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string transactionId = reuseTransaction && !string.IsNullOrWhiteSpace(lastOngoingEffectTransactionId)
                ? lastOngoingEffectTransactionId
                : $"development.contribution.healing.{Guid.NewGuid():N}";
            lastOngoingEffectTransactionId = transactionId;
            HealingApplicationResult healing;
            if (reuseTransaction && lastContributionHealingSource != null)
            {
                healing = lastContributionHealingSource;
            }
            else
            {
                if (!TryPrepareContributionHealth(targetPlayer: false, desiredCurrent: 50f, out string healthMessage))
                {
                    return RecordFailure("Record 6.9 Healing", healthMessage, "MissingHealth");
                }

                healing = damageHealingService.ApplyHealing(CreatePipelineHealingRequest(25f, targetPlayer: false, transactionId));
                if (healing.Succeeded && !healing.Duplicate)
                {
                    lastContributionHealingSource = healing;
                }
            }

            CombatContributionRecordResult result = service.RecordHealing(healing, sourceKind: CombatContributionSourceKind.Development);
            if (result.Record != null && !string.IsNullOrWhiteSpace(result.Record.BeneficiaryActorId))
            {
                lastContributionCreditTargetActorId = result.Record.BeneficiaryActorId;
            }

            return Record(result.Succeeded || result.Duplicate, reuseTransaction ? "Record 6.9 Healing Reuse" : "Record 6.9 Healing", result.Code, $"{FormatHealingApplication(healing)} {FormatContributionRecordResult(result)}");
        }

        public PrototypeTestLabOperation RecordFullyPreventedDamageContribution(DamageTypeDefinition damageType)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            DamageTypeDefinition selected = damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault();
            if (service == null || selected == null)
            {
                return RecordFailure("Record 6.9 Prevented Damage", "Contribution service or damage type is missing.", CombatContributionResultCode.MissingPolicy);
            }

            DamageApplicationRequest request = CreatePipelineDamageRequest(selected, 25f, targetPlayer: false, $"development.contribution.prevented.{Guid.NewGuid():N}");
            DamageApplicationResult damage = DamageApplicationResult.Create(false, "Prevented", "Damage was fully prevented.", request, request.TargetActorId, 25f, 25f, 25f, 0f, 0f, 0f, 100f, 100f, 0f, 100f, false, false, false, false, false, 0f, null);
            CombatContributionRecordResult result = service.RecordDamage(damage, sourceKind: CombatContributionSourceKind.Development);
            bool expectedZero = result.Code == CombatContributionResultCode.ZeroEffectiveContribution;
            return Record(expectedZero, "Record 6.9 Prevented Damage", result.Code, $"{FormatDamageApplication(damage)} {FormatContributionRecordResult(result)}");
        }

        public PrototypeTestLabOperation RecordOverkillContribution(DamageTypeDefinition damageType)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            DamageTypeDefinition selected = damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault();
            if (service == null || selected == null)
            {
                return RecordFailure("Record 6.9 Overkill", "Contribution service or damage type is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string transactionId = $"development.contribution.overkill.{Guid.NewGuid():N}";
            if (!TryPrepareContributionHealth(targetPlayer: false, desiredCurrent: 50f, out string healthMessage))
            {
                return RecordFailure("Record 6.9 Overkill", healthMessage, "MissingHealth");
            }

            DamageApplicationResult damage = damageHealingService.ApplyDamage(CreatePipelineDamageRequest(selected, 999f, targetPlayer: false, transactionId));
            CombatContributionRecordResult result = service.RecordDamage(damage, sourceKind: CombatContributionSourceKind.Development);
            return Record(result.Succeeded || result.Code == CombatContributionResultCode.ZeroEffectiveContribution, "Record 6.9 Overkill", result.Code, $"{FormatDamageApplication(damage)} {FormatContributionRecordResult(result)}");
        }

        public PrototypeTestLabOperation RecordOngoingDamageContribution()
        {
            return RecordSyntheticContribution(
                "Record 6.9 Ongoing Damage",
                CombatContributionType.OngoingDamageApplied,
                CombatContributionSourceKind.OngoingEffect,
                "ongoing-effect.synthetic",
                requestedAmount: 5f,
                actualAmount: 5f,
                preventedAmount: 0f);
        }

        public PrototypeTestLabOperation RecordReactionDamageContribution()
        {
            return RecordSyntheticContribution(
                "Record 6.9 Reaction Damage",
                CombatContributionType.ReactionDamageApplied,
                CombatContributionSourceKind.Reaction,
                "combat-reaction.synthetic-damage",
                requestedAmount: 5f,
                actualAmount: 5f,
                preventedAmount: 0f);
        }

        public PrototypeTestLabOperation RecordReactionHealingContribution()
        {
            return RecordSyntheticContribution(
                "Record 6.9 Reaction Healing",
                CombatContributionType.ReactionHealingApplied,
                CombatContributionSourceKind.Reaction,
                "combat-reaction.synthetic-healing",
                requestedAmount: 5f,
                actualAmount: 5f,
                preventedAmount: 0f);
        }

        public PrototypeTestLabOperation RecordDefenseContribution(CombatContributionType contributionType)
        {
            string label = contributionType == CombatContributionType.SuccessfulBlock
                ? "Record 6.9 Block"
                : contributionType == CombatContributionType.SuccessfulParry
                    ? "Record 6.9 Parry"
                    : "Record 6.9 Dodge";
            return RecordSyntheticContribution(
                label,
                contributionType,
                CombatContributionSourceKind.Defense,
                $"defense.synthetic.{contributionType}",
                requestedAmount: 25f,
                actualAmount: contributionType == CombatContributionType.SuccessfulBlock ? 5f : 1f,
                preventedAmount: contributionType == CombatContributionType.SuccessfulBlock ? 20f : 25f,
                support: true);
        }

        public PrototypeTestLabOperation AdvanceContributionClock(float seconds)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            service?.AdvanceClock(Mathf.Max(0f, seconds));
            return RecordSuccess("Advance 6.9 Clock", $"Contribution clock advanced by {Mathf.Max(0f, seconds):0.###}s to {(service == null ? 0f : service.SimulationTime):0.###}s.");
        }

        public PrototypeTestLabOperation ResolveDefeatContributionCredit()
        {
            return ResolveContributionCredit(kill: false);
        }

        public PrototypeTestLabOperation ResolveKillContributionCredit()
        {
            return ResolveContributionCredit(kill: true);
        }

        public PrototypeTestLabOperation FinalizeContributionLedger()
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            CombatContributionLedgerSnapshot snapshot = service?.GetLedgerSnapshots().FirstOrDefault();
            CombatContributionLedgerSnapshot finalized = snapshot == null ? null : service.FinalizeLedger(snapshot.LedgerId);
            return Record(finalized != null, "Finalize 6.9 Ledger", finalized == null ? CombatContributionResultCode.MissingTarget : CombatContributionResultCode.Success, finalized == null ? "No contribution ledger exists." : FormatContributionLedger(finalized));
        }

        public PrototypeTestLabOperation ClearCombatContributions()
        {
            EnsureCombatContributionRuntime()?.ClearTransientStateForRestore();
            lastContributionDamageSource = null;
            lastContributionHealingSource = null;
            lastContributionCreditTargetActorId = string.Empty;
            return RecordSuccess("Clear 6.9 Contributions", "Contribution ledgers, credit results, and duplicate keys cleared.");
        }

        public PrototypeTestLabOperation ProveContributionKillCreditLatest()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Latest Kill Credit", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.kill.old", CombatContributionType.DamageApplied, "actor.proof.old", target, string.Empty, 50f));
            service.AdvanceClock(1f);
            service.RecordContribution(SyntheticContribution("proof.kill.latest", CombatContributionType.DamageApplied, "actor.proof.latest", target, string.Empty, 5f));
            CombatCreditResolutionResult credit = service.ResolveKillCredit(BuildContributionLifecycle(target, kill: true, "proof.kill.lifecycle"));
            bool passed = credit.Succeeded && credit.PrimaryContributorActorId == "actor.proof.latest";
            return Record(passed, "Prove 6.9 Latest Kill Credit", credit.Code, FormatCreditResult(credit));
        }

        public PrototypeTestLabOperation ProveContributionAssistCredit()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Assist Credit", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.assist.damage", CombatContributionType.DamageApplied, "actor.proof.assist", target, string.Empty, 10f));
            service.RecordContribution(SyntheticContribution("proof.assist.heal", CombatContributionType.HealingApplied, "actor.proof.healer", string.Empty, target, 6f));
            service.AdvanceClock(1f);
            service.RecordContribution(SyntheticContribution("proof.assist.kill", CombatContributionType.DamageApplied, "actor.proof.primary", target, string.Empty, 5f));
            CombatCreditResolutionResult credit = service.ResolveKillCredit(BuildContributionLifecycle(target, kill: true, "proof.assist.lifecycle"));
            bool passed = credit.Succeeded
                && credit.PrimaryContributorActorId == "actor.proof.primary"
                && credit.Assists.Any(summary => summary.ContributorActorId == "actor.proof.assist")
                && credit.Assists.Any(summary => summary.ContributorActorId == "actor.proof.healer");
            return Record(passed, "Prove 6.9 Assist Credit", credit.Code, FormatCreditResult(credit));
        }

        public PrototypeTestLabOperation ProveContributionHealingOnlyNotPrimary()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Healing Not Primary", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.heal.only", CombatContributionType.HealingApplied, "actor.proof.healer", string.Empty, target, 10f));
            CombatCreditResolutionResult credit = service.ResolveKillCredit(BuildContributionLifecycle(target, kill: true, "proof.heal.lifecycle"));
            bool passed = credit.Succeeded && string.IsNullOrWhiteSpace(credit.PrimaryContributorActorId) && credit.Code == CombatContributionResultCode.NoEligibleContributor;
            return Record(passed, "Prove 6.9 Healing Not Primary", credit.Code, FormatCreditResult(credit));
        }

        public PrototypeTestLabOperation ProveContributionEncounterMerge()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Encounter Merge", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            service.RecordContribution(SyntheticContribution("proof.merge.a", CombatContributionType.DamageApplied, "actor.proof.a", "actor.proof.target", string.Empty, 5f, "encounter.proof.a"));
            service.RecordContribution(SyntheticContribution("proof.merge.b", CombatContributionType.DamageApplied, "actor.proof.b", "actor.proof.target", string.Empty, 5f, "encounter.proof.b"));
            CombatContributionLedgerMergeResult merge = service.MergeEncounterLedgers(new CombatEncounterSnapshot("encounter.proof.a", true, 0f, 0f, new[] { "actor.proof.a", "actor.proof.b", "actor.proof.target" }, Array.Empty<CombatEngagementSnapshot>(), 1L, CombatEncounterCompletionReason.Forced));
            bool passed = merge.Succeeded && merge.Snapshot != null && merge.Snapshot.Records.Count == 2;
            return Record(passed, "Prove 6.9 Encounter Merge", merge.Code, $"Merged={string.Join(",", merge.MergedLedgerIds)} Records={(merge.Snapshot == null ? 0 : merge.Snapshot.Records.Count)}. {merge.Message}");
        }

        public PrototypeTestLabOperation ProveContributionEncounterSplit()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Encounter Split", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            service.RecordContribution(SyntheticContribution("proof.split.a", CombatContributionType.DamageApplied, "actor.proof.a", "actor.proof.target-a", string.Empty, 5f, "encounter.proof.original"));
            service.RecordContribution(SyntheticContribution("proof.split.b", CombatContributionType.DamageApplied, "actor.proof.b", "actor.proof.target-b", string.Empty, 5f, "encounter.proof.original"));
            service.RecordContribution(SyntheticContribution("proof.split.cross", CombatContributionType.DamageApplied, "actor.proof.a", "actor.proof.target-b", string.Empty, 5f, "encounter.proof.original"));
            CombatEncounterSplitResult split = new CombatEncounterSplitResult(true, false, false, "Success", "Proof split.", "proof.split.tx", "encounter.proof.original", "encounter.proof.original", new[] { "encounter.proof.new" }, new[] { "actor.proof.a", "actor.proof.target-a", "actor.proof.b", "actor.proof.target-b" }, new[]
            {
                new CombatEncounterSplitComponentSnapshot("encounter.proof.original", new[] { "actor.proof.a", "actor.proof.target-a" }, Array.Empty<string>(), true, 0f, true),
                new CombatEncounterSplitComponentSnapshot("encounter.proof.new", new[] { "actor.proof.b", "actor.proof.target-b" }, Array.Empty<string>(), false, 0f, true)
            }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CombatExitResult>(), Array.Empty<CombatEncounterSnapshot>(), Array.Empty<CombatParticipantReassignmentResult>(), CombatExitReason.Explicit, 0L, 1L, 0f);
            CombatContributionLedgerPartitionResult partition = service.PartitionEncounterLedgers(split);
            bool passed = partition.Succeeded
                && partition.ComponentSnapshots.Count == 2
                && partition.ComponentSnapshots.All(snapshot => snapshot.Summaries.Count == 1);
            return Record(passed, "Prove 6.9 Encounter Split", partition.Code, $"Components={partition.ComponentSnapshots.Count} Historical={partition.HistoricalSnapshots.SelectMany(snapshot => snapshot.Records).Count()}. {partition.Message}");
        }

        public PrototypeTestLabOperation ProveContributionDuplicateLifecycleCredit()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Duplicate Credit", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.duplicate.damage", CombatContributionType.DamageApplied, "actor.proof.primary", target, string.Empty, 5f));
            ActorLifecycleResult lifecycle = BuildContributionLifecycle(target, kill: true, "proof.duplicate.lifecycle");
            CombatCreditResolutionResult first = service.ResolveKillCredit(lifecycle);
            CombatCreditResolutionResult duplicate = service.ResolveKillCredit(lifecycle);
            bool passed = first.Succeeded && duplicate.Succeeded && duplicate.Duplicate;
            return Record(passed, "Prove 6.9 Duplicate Credit", duplicate.Code, FormatCreditResult(duplicate));
        }

        public PrototypeTestLabOperation ProveContributionRevivalPreservesCredit()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Revival Preserves Credit", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.revival.damage", CombatContributionType.DamageApplied, "actor.proof.primary", target, string.Empty, 5f));
            ActorLifecycleResult lifecycle = BuildContributionLifecycle(target, kill: true, "proof.revival.lifecycle");
            CombatCreditResolutionResult first = service.ResolveKillCredit(lifecycle);
            service.RecordContribution(SyntheticContribution("proof.revival.support", CombatContributionType.RevivalProvided, "actor.proof.healer", string.Empty, target, 10f));
            CombatCreditResolutionResult after = service.ResolveKillCredit(lifecycle, transactionId: "proof.revival.lifecycle.after");
            bool passed = first.Succeeded && after.Duplicate && after.PrimaryContributorActorId == "actor.proof.primary";
            return Record(passed, "Prove 6.9 Revival Preserves Credit", after.Code, FormatCreditResult(after));
        }

        public PrototypeTestLabOperation ProveContributionRewardSafety()
        {
            CombatContributionService service = ResetContributionProofState();
            if (service == null)
            {
                return RecordFailure("Prove 6.9 Reward Safety", "Contribution service is missing.", CombatContributionResultCode.MissingPolicy);
            }

            string target = ResolveContributionTargetActorId();
            service.RecordContribution(SyntheticContribution("proof.reward.damage", CombatContributionType.DamageApplied, "actor.proof.primary", target, string.Empty, 5f));
            CombatCreditResolutionResult credit = service.ResolveKillCredit(BuildContributionLifecycle(target, kill: true, "proof.reward.lifecycle"));
            bool passed = credit.Succeeded && !credit.GrantsConcreteRewards && credit.Contributors.Any(summary => summary.Eligibility.Contains(CombatRewardEligibilityCategory.DiagnosticOnly));
            return Record(passed, "Prove 6.9 Reward Safety", credit.Code, $"{FormatCreditResult(credit)} EligibilityOnly=True ConcreteRewards=False");
        }

        private PrototypeTestLabOperation ResolveContributionCredit(bool kill)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            GameObject target = context?.EnemyTransform?.gameObject;
            string targetActorId = !string.IsNullOrWhiteSpace(lastContributionCreditTargetActorId)
                ? lastContributionCreditTargetActorId
                : ResolveActorId(target);
            if (service == null || target == null)
            {
                return RecordFailure(kill ? "Resolve 6.9 Kill Credit" : "Resolve 6.9 Defeat Credit", "Contribution service or target is missing.", CombatContributionResultCode.MissingTarget);
            }

            ActorLifecycleResult lifecycle = ActorLifecycleResult.Create(
                true,
                false,
                false,
                ActorLifecycleResultCode.Success,
                kill ? "Development kill credit proof." : "Development defeat credit proof.",
                $"development.contribution.lifecycle.{Guid.NewGuid():N}",
                ResolveActorId(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject),
                targetActorId,
                string.Empty,
                kill ? LifecycleTransitionKind.Death : LifecycleTransitionKind.Defeat,
                kill ? LifecycleTriggerKind.ExplicitDeath : LifecycleTriggerKind.HealthDepleted,
                ActorLifecycleState.Active,
                kill ? ActorLifecycleState.Dead : ActorLifecycleState.Defeated,
                DefeatPolicyOutcome.RemainDefeated,
                0f,
                0f,
                0f,
                100f,
                0f,
                0f,
                0f,
                string.Empty,
                0L);
            CombatCreditResolutionResult result = kill ? service.ResolveKillCredit(lifecycle) : service.ResolveDefeatCredit(lifecycle);
            return Record(result.Succeeded, kill ? "Resolve 6.9 Kill Credit" : "Resolve 6.9 Defeat Credit", result.Code, FormatCreditResult(result));
        }

        private CombatContributionService ResetContributionProofState()
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            service?.ClearTransientStateForRestore();
            lastContributionDamageSource = null;
            lastContributionHealingSource = null;
            lastContributionCreditTargetActorId = string.Empty;
            return service;
        }

        private string ResolveContributionTargetActorId()
        {
            GameObject target = context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject;
            string resolved = ResolveActorId(target);
            return string.IsNullOrWhiteSpace(resolved) ? "actor.proof.target" : resolved;
        }

        private CombatContributionRecordRequest SyntheticContribution(string transactionId, CombatContributionType type, string contributorActorId, string targetActorId, string beneficiaryActorId, float actualAmount, string encounterId = "")
        {
            return new CombatContributionRecordRequest(
                transactionId,
                type,
                contributorActorId,
                string.Empty,
                beneficiaryActorId,
                targetActorId,
                encounterId,
                actualAmount,
                actualAmount,
                type == CombatContributionType.SuccessfulBlock || type == CombatContributionType.SuccessfulParry || type == CombatContributionType.SuccessfulDodge ? actualAmount : 0f,
                combatContributionService == null ? 0f : combatContributionService.SimulationTime,
                CombatContributionSourceKind.Development,
                transactionId,
                string.Empty,
                "development.test-lab",
                string.Empty,
                preview: false,
                authorityValidated: true);
        }

        private ActorLifecycleResult BuildContributionLifecycle(string targetActorId, bool kill, string transactionId)
        {
            return ActorLifecycleResult.Create(
                true,
                false,
                false,
                ActorLifecycleResultCode.Success,
                kill ? "Development kill credit proof." : "Development defeat credit proof.",
                transactionId,
                ResolveActorId(context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject),
                targetActorId,
                string.Empty,
                kill ? LifecycleTransitionKind.Death : LifecycleTransitionKind.Defeat,
                kill ? LifecycleTriggerKind.ExplicitDeath : LifecycleTriggerKind.HealthDepleted,
                ActorLifecycleState.Active,
                kill ? ActorLifecycleState.Dead : ActorLifecycleState.Defeated,
                DefeatPolicyOutcome.RemainDefeated,
                0f,
                0f,
                0f,
                100f,
                0f,
                0f,
                0f,
                string.Empty,
                0L);
        }

        private PrototypeTestLabOperation RecordSyntheticContribution(string operationName, CombatContributionType type, CombatContributionSourceKind sourceKind, string originDefinitionId, float requestedAmount, float actualAmount, float preventedAmount, bool support = false)
        {
            CombatContributionService service = EnsureCombatContributionRuntime();
            GameObject contributor = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            GameObject enemy = context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject;
            if (service == null || contributor == null || enemy == null)
            {
                return RecordFailure(operationName, "Contribution service, contributor, or target is missing.", CombatContributionResultCode.MissingTarget);
            }

            string contributorId = ResolveActorId(contributor);
            string enemyId = ResolveActorId(enemy);
            string transactionId = $"development.contribution.synthetic.{Guid.NewGuid():N}";
            bool beneficiaryContribution = type == CombatContributionType.HealingApplied
                || type == CombatContributionType.OngoingHealingApplied
                || type == CombatContributionType.ReactionHealingApplied
                || type == CombatContributionType.RecoveryProvided
                || type == CombatContributionType.RevivalProvided;
            CombatContributionRecordRequest request = new CombatContributionRecordRequest(
                transactionId,
                type,
                contributorId,
                string.Empty,
                beneficiaryContribution ? enemyId : string.Empty,
                beneficiaryContribution ? string.Empty : enemyId,
                string.Empty,
                requestedAmount,
                actualAmount,
                preventedAmount,
                service.SimulationTime,
                sourceKind,
                transactionId,
                string.Empty,
                originDefinitionId,
                string.Empty,
                preview: false,
                authorityValidated: true);
            CombatContributionRecordResult result = service.RecordContribution(request);
            if (result.Record != null)
            {
                if (!string.IsNullOrWhiteSpace(result.Record.TargetActorId))
                {
                    lastContributionCreditTargetActorId = result.Record.TargetActorId;
                }
                else if (!string.IsNullOrWhiteSpace(result.Record.BeneficiaryActorId))
                {
                    lastContributionCreditTargetActorId = result.Record.BeneficiaryActorId;
                }
            }

            return Record(result.Succeeded || result.Duplicate, operationName, result.Code, FormatContributionRecordResult(result));
        }

        private DamageApplicationRequest CreatePipelineDamageRequest(DamageTypeDefinition damageType, float amount, bool targetPlayer, string transactionId)
        {
            GameObject source = targetPlayer ? context?.EnemyTransform?.gameObject : context?.PlayerTransform?.gameObject;
            GameObject target = targetPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            EnsureAttackResolutionRuntime(source, needsResource: false);
            EnsureAttackResolutionRuntime(target, needsResource: true);
            return new DamageApplicationRequest(
                transactionId,
                ResolveActorId(source),
                source,
                ResolveActorId(target),
                target,
                damageType,
                Mathf.Max(0f, amount),
                "Prototype Test Lab");
        }

        private bool TryPrepareContributionHealth(bool targetPlayer, float desiredCurrent, out string message)
        {
            GameObject target = targetPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            EnsureAttackResolutionRuntime(target, needsResource: true);
            CharacterResourceCollection resources = target == null ? null : target.GetComponentInParent<CharacterResourceCollection>();
            if (resources == null || !resources.TryGetResource(ResourceIds.Health, out ResourceSnapshot health))
            {
                message = "Contribution target Health resource is missing.";
                return false;
            }

            float prepared = Mathf.Clamp(desiredCurrent, health.Minimum, health.Maximum);
            ResourceChangeResult result = resources.SetCurrent(ResourceIds.Health, prepared, "development.test-lab", "Prepare contribution automation.", restoration: true);
            message = result == null ? "Contribution Health preparation did not return a result." : result.Message;
            return result != null && result.Succeeded;
        }

        private AttackResolutionRequest CreateDefensiveAttackRequest(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float defenseRoll, bool targetPlayer, string transactionId)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["defense.roll"] = Mathf.Clamp(defenseRoll, 0f, 0.999f).ToString("0.###"),
                ["defense.blockable"] = "true",
                ["defense.parryable"] = "true",
                ["defense.dodgeable"] = "true",
                ["defense.allow-true-active"] = "true"
            };
            return CreateAttackResolutionRequest(
                damageType,
                amount,
                baseHitChance,
                hitRoll,
                criticalChance: 0f,
                criticalRoll: 0.5f,
                criticalMultiplier: AttackResolutionRequest.DefaultCriticalMultiplier,
                distance: 1f,
                maximumRange: 2f,
                targetEnemy: !targetPlayer,
                sourcePlayer: !targetPlayer,
                transactionId,
                metadata);
        }

        private AttackResolutionRequest CreateAttackResolutionRequest(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float criticalChance, float criticalRoll, float criticalMultiplier, float distance, float maximumRange, bool targetEnemy, bool sourcePlayer, string transactionId, IReadOnlyDictionary<string, string> metadata = null)
        {
            GameObject source = sourcePlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            GameObject target = targetEnemy ? context?.EnemyTransform?.gameObject : context?.PlayerTransform?.gameObject;
            EnsureAttackResolutionRuntime(source, needsResource: false);
            EnsureAttackResolutionRuntime(target, needsResource: true);
            return new AttackResolutionRequest(
                transactionId,
                sourcePlayer ? AttackSourceType.Weapon : AttackSourceType.Unarmed,
                source,
                ResolveActorId(source),
                target,
                ResolveActorId(target),
                damageType,
                Mathf.Max(0f, amount),
                hitRoll,
                criticalRoll,
                Mathf.Clamp01(baseHitChance),
                Mathf.Clamp01(criticalChance),
                Mathf.Max(1f, criticalMultiplier),
                hasSuppliedDistance: distance >= 0f,
                suppliedDistance: Mathf.Max(0f, distance),
                hasMaximumRange: maximumRange >= 0f,
                maximumRange: Mathf.Max(0f, maximumRange),
                originatingActionId: "development.attack-resolution-test",
                metadata: metadata);
        }

        private AttackResolutionRequest CreateCombatStateAttackResolutionRequest(DamageTypeDefinition damageType, float amount, float baseHitChance, float hitRoll, float criticalChance, float criticalRoll, float criticalMultiplier, float distance, float maximumRange, string transactionId)
        {
            EnsureCombatStateTestParticipants();
            GameObject source = GetCombatStateTestActor("A");
            GameObject target = GetCombatStateTestActor("B");
            EnsureAttackResolutionRuntime(source, needsResource: false);
            EnsureAttackResolutionRuntime(target, needsResource: true);
            return new AttackResolutionRequest(
                transactionId,
                AttackSourceType.Weapon,
                source,
                ResolveCombatStateActorId(source),
                target,
                ResolveCombatStateActorId(target),
                damageType,
                Mathf.Max(0f, amount),
                hitRoll,
                criticalRoll,
                Mathf.Clamp01(baseHitChance),
                Mathf.Clamp01(criticalChance),
                Mathf.Max(1f, criticalMultiplier),
                hasSuppliedDistance: distance >= 0f,
                suppliedDistance: Mathf.Max(0f, distance),
                hasMaximumRange: maximumRange >= 0f,
                maximumRange: Mathf.Max(0f, maximumRange),
                originatingActionId: "development.combat-state-attack-test");
        }

        private bool TryBuildDefenseActivationRequest(DefensiveActionDefinition definition, bool targetPlayer, bool reuseTransaction, out DefenseActivationRequest request, out PrototypeTestLabOperation failure)
        {
            request = default;
            failure = default;
            if (definition == null)
            {
                failure = RecordFailure("6.6 Defense Activation", "No defensive action selected.", "MissingDefinition");
                return false;
            }

            GameObject target = targetPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (target == null)
            {
                failure = RecordFailure("6.6 Defense Activation", "Defense target is missing.", "MissingTarget");
                return false;
            }

            EnsureAttackResolutionRuntime(target, needsResource: true);
            request = new DefenseActivationRequest(
                ResolveDefenseActivationTransactionId(reuseTransaction),
                ResolveActorId(target),
                target,
                definition,
                sourceEquipmentId: string.Empty,
                sourceActionId: "development.test-lab",
                now: Time.time,
                authorityValidated: true);
            return true;
        }

        private string FormatActiveDefense(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !defensiveActionService.TryGetActiveDefense(actorId, out DefensiveActionStateSnapshot snapshot))
            {
                return "None";
            }

            string expiration = snapshot.HasExpiration ? $" expires={Mathf.Max(0f, snapshot.ExpiresAt - Time.time):0.###}s" : " persistent";
            return $"{snapshot.Definition.DisplayName} ({snapshot.DefinitionId}) state={snapshot.State}{expiration}";
        }

        private static string FormatResource(GameObject owner, string resourceId)
        {
            CharacterResourceCollection resources = owner == null ? null : owner.GetComponentInParent<CharacterResourceCollection>();
            if (resources == null || !resources.TryGetResource(resourceId, out ResourceSnapshot snapshot))
            {
                return "Missing";
            }

            return $"{snapshot.Current:0.###}/{snapshot.Maximum:0.###}";
        }

        private static string FormatDefenseActivation(DefenseActivationResult result)
        {
            if (result == null)
            {
                return "Defense activation result is missing.";
            }

            string state = result.State == null ? "State=None" : $"State={result.State.StateId} action={result.State.DefinitionId} runtime={result.State.State}";
            string stamina = result.StaminaResult == null ? "Stamina=None" : $"Stamina={result.StaminaResult.Code} {result.StaminaResult.OldCurrent:0.###}->{result.StaminaResult.NewCurrent:0.###} duplicate={result.StaminaResult.DuplicateEvent}";
            return $"{state} preview={result.Preview} duplicate={result.Duplicate} {stamina}. {result.Message}";
        }

        private static string FormatDefenseCancellation(DefenseCancellationResult result)
        {
            if (result == null)
            {
                return "Defense cancellation result is missing.";
            }

            string state = result.RemovedState == null ? "State=None" : $"Removed={result.RemovedState.DefinitionId} state={result.RemovedState.StateId}";
            return $"{state} preview={result.Preview} duplicate={result.Duplicate}. {result.Message}";
        }

        private void EnsureAttackResolutionRuntime(GameObject actor, bool needsResource)
        {
            if (!CanMutateRuntimeActor(actor))
            {
                return;
            }

            WorldEntityIdentity identity = actor.GetComponentInParent<WorldEntityIdentity>();
            if (identity == null)
            {
                identity = actor.AddComponent<WorldEntityIdentity>();
                if (identity == null)
                {
                    return;
                }

                identity.TryInitializeRuntime($"entity.local-world.runtime.attack-test-lab.{Guid.NewGuid():N}", "scene.prototype", PersistenceService.LocalWorldId, PersistenceScope.RegionOrScene, "development.attack-resolution", out _);
            }

            CharacterAttributes attributes = actor.GetComponentInParent<CharacterAttributes>();
            if (attributes == null)
            {
                attributes = actor.AddComponent<CharacterAttributes>();
                if (attributes == null)
                {
                    return;
                }
            }

            CalculatedStatCollection stats = actor.GetComponentInParent<CalculatedStatCollection>();
            if (stats == null)
            {
                stats = actor.AddComponent<CalculatedStatCollection>();
                if (stats == null)
                {
                    return;
                }
            }

            attributes.Configure(registry);
            stats.Configure(registry, attributes);
            if (needsResource)
            {
                CharacterResourceCollection resources = actor.GetComponentInParent<CharacterResourceCollection>();
                if (resources == null)
                {
                    resources = actor.AddComponent<CharacterResourceCollection>();
                    if (resources == null)
                    {
                        return;
                    }
                }

                resources.Configure(registry, stats, PersistenceService.LocalPlayerId);
            }
        }

        private void EnsureLifecycleRuntime(GameObject actor, ref ActorLifecycleController lifecycle, bool needsResource)
        {
            if (!CanMutateRuntimeActor(actor))
            {
                return;
            }

            EnsureAttackResolutionRuntime(actor, needsResource);
            lifecycle = lifecycle == null ? actor.GetComponentInParent<ActorLifecycleController>() : lifecycle;
            if (lifecycle == null)
            {
                lifecycle = actor.AddComponent<ActorLifecycleController>();
                if (lifecycle == null)
                {
                    return;
                }
            }

            CharacterResourceCollection resources = actor.GetComponentInParent<CharacterResourceCollection>();
            CharacterSystemCoordinator character = actor.GetComponentInParent<CharacterSystemCoordinator>();
            CharacterTraitCollection traits = actor.GetComponentInParent<CharacterTraitCollection>();
            lifecycle.Configure(null, resources, character, traits);
        }

        private static bool CanMutateRuntimeActor(GameObject actor)
        {
            if (actor == null || !actor.activeInHierarchy)
            {
                return false;
            }

            CharacterSystemCoordinator character = actor.GetComponentInParent<CharacterSystemCoordinator>();
            return character == null || character.isActiveAndEnabled;
        }

        private CombatStateService EnsureCombatStateRuntime()
        {
            GameObject owner = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            CombatStateService service = context?.CombatState;
            if (service == null && owner != null)
            {
                service = owner.GetComponentInParent<CombatStateService>();
            }

            if (service == null && owner != null)
            {
                service = owner.AddComponent<CombatStateService>();
            }

            CombatStatePolicyDefinition policy = registry == null
                ? null
                : registry.DefinitionsById.Values.OfType<CombatStatePolicyDefinition>().OrderBy(definition => definition.Id).FirstOrDefault();
            service?.Configure(policy);
            service?.SetClock(combatStateClockSeconds);
            if (context != null)
            {
                context.CombatState = service;
            }

            return service;
        }

        private CombatRuntimeFacade EnsureCombatRuntimeFacade()
        {
            if (combatRuntimeFacade != null)
            {
                return combatRuntimeFacade;
            }

            GameObject player = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            CombatStateService combatState = EnsureCombatStateRuntime();
            OngoingEffectService ongoing = EnsureOngoingEffectRuntime(targetEnemy: false) ?? EnsureOngoingEffectRuntime(targetEnemy: true);
            CombatReactionService reactions = EnsureCombatReactionRuntime();
            CombatContributionService contributions = EnsureCombatContributionRuntime();
            combatRuntimeFacade = new CombatRuntimeFacade(
                registry,
                player,
                damageHealingService,
                defensiveActionService,
                combatState,
                combatExecutionService,
                ongoing,
                reactions,
                contributions,
                attackResolutionService);
            return combatRuntimeFacade;
        }

        private void EnsureCombatStateTestParticipants()
        {
            EnsureCombatStateRuntime();
            RegisterCombatStateTestActor("A", context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject);
            RegisterCombatStateTestActor("B", context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject);
            EnsureCombatStateMockActor("C");
            EnsureCombatStateMockActor("D");
        }

        private void RegisterCombatStateTestActor(string key, GameObject actor)
        {
            if (actor == null)
            {
                return;
            }

            EnsureAttackResolutionRuntime(actor, needsResource: true);
            if (string.Equals(key, "A", StringComparison.Ordinal))
            {
                EnsureLifecycleRuntime(actor, ref context.PlayerLifecycle, needsResource: true);
            }
            else if (string.Equals(key, "B", StringComparison.Ordinal))
            {
                EnsureLifecycleRuntime(actor, ref context.EnemyLifecycle, needsResource: true);
            }
            else
            {
                ActorLifecycleController lifecycle = actor.GetComponentInParent<ActorLifecycleController>();
                EnsureLifecycleRuntime(actor, ref lifecycle, needsResource: true);
            }

            combatStateTestActors[key] = actor;
        }

        private GameObject EnsureCombatStateMockActor(string key)
        {
            if (combatStateTestActors.TryGetValue(key, out GameObject existing) && existing != null)
            {
                return existing;
            }

            GameObject root = context?.PlayerTransform == null ? null : context.PlayerTransform.root.gameObject;
            GameObject actor = new GameObject($"Combat State Test Actor {key}");
            if (root != null)
            {
                actor.transform.SetParent(root.transform);
                actor.transform.position = context.PlayerTransform.position + context.PlayerTransform.right * (string.Equals(key, "C", StringComparison.Ordinal) ? 2f : 3f);
            }

            WorldEntityIdentity identity = actor.AddComponent<WorldEntityIdentity>();
            identity.TryInitializeRuntime($"entity.local-world.runtime.combat-state-test-lab.{key.ToLowerInvariant()}.{Guid.NewGuid():N}", "scene.prototype", PersistenceService.LocalWorldId, PersistenceScope.SessionOnly, "development.combat-state-test-lab", out _);
            EnsureAttackResolutionRuntime(actor, needsResource: true);
            ActorLifecycleController lifecycle = actor.GetComponentInParent<ActorLifecycleController>();
            EnsureLifecycleRuntime(actor, ref lifecycle, needsResource: true);
            combatStateTestActors[key] = actor;
            return actor;
        }

        private GameObject GetCombatStateTestActor(string key)
        {
            EnsureCombatStateTestParticipants();
            if (combatStateTestActors.TryGetValue(key, out GameObject actor) && actor != null)
            {
                return actor;
            }

            return null;
        }

        private bool TryBuildCombatExecutionBeginRequest(CombatExecutionDefinition definition, bool reuseTransaction, out CombatExecutionBeginRequest request, out PrototypeTestLabOperation failure)
        {
            request = default;
            failure = default;
            GameObject actor = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            EnsureAttackResolutionRuntime(actor, needsResource: true);
            if (definition == null)
            {
                failure = RecordFailure("6.7 Execution", "Combat execution definition is missing.", CombatExecutionResultCode.MissingDefinition);
                return false;
            }

            if (actor == null)
            {
                failure = RecordFailure("6.7 Execution", "Prototype player actor is missing.", CombatExecutionResultCode.MissingActor);
                return false;
            }

            request = new CombatExecutionBeginRequest(
                ResolveCombatExecutionBeginTransactionId(reuseTransaction),
                definition,
                actor,
                ResolveActorId(actor),
                combatExecutionClockSeconds,
                authorityValidated: true);
            return true;
        }

        private bool TryBuildCombatExecutionCommitRequest(bool reuseTransaction, out CombatExecutionCommitRequest request, out PrototypeTestLabOperation failure)
        {
            request = default;
            failure = default;
            GameObject actor = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            EnsureAttackResolutionRuntime(actor, needsResource: true);
            if (actor == null)
            {
                failure = RecordFailure("6.7 Execution", "Prototype player actor is missing.", CombatExecutionResultCode.MissingActor);
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastCombatExecutionInstanceId))
            {
                failure = RecordFailure("6.7 Execution", "Begin a combat execution before committing.", CombatExecutionResultCode.MissingExecution);
                return false;
            }

            request = new CombatExecutionCommitRequest(
                ResolveCombatExecutionCommitTransactionId(reuseTransaction),
                lastCombatExecutionInstanceId,
                actor,
                ResolveActorId(actor),
                combatExecutionClockSeconds,
                authorityValidated: true);
            return true;
        }

        private bool TryBuildCombatExecutionCancelRequest(CombatExecutionCancellationReason reason, out CombatExecutionCancelRequest request, out PrototypeTestLabOperation failure)
        {
            request = default;
            failure = default;
            GameObject actor = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            if (actor == null)
            {
                failure = RecordFailure("6.7 Execution", "Prototype player actor is missing.", CombatExecutionResultCode.MissingActor);
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastCombatExecutionInstanceId))
            {
                failure = RecordFailure("6.7 Execution", "Begin a combat execution before cancelling or interrupting.", CombatExecutionResultCode.MissingExecution);
                return false;
            }

            request = new CombatExecutionCancelRequest(
                $"development.combat-execution.cancel.{Guid.NewGuid():N}",
                lastCombatExecutionInstanceId,
                actor,
                ResolveActorId(actor),
                reason,
                combatExecutionClockSeconds);
            return true;
        }

        private string ResolveCombatExecutionBeginTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastCombatExecutionBeginTransactionId))
            {
                return lastCombatExecutionBeginTransactionId;
            }

            lastCombatExecutionBeginTransactionId = $"development.combat-execution.begin.{Guid.NewGuid():N}";
            return lastCombatExecutionBeginTransactionId;
        }

        private string ResolveCombatExecutionCommitTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastCombatExecutionCommitTransactionId))
            {
                return lastCombatExecutionCommitTransactionId;
            }

            lastCombatExecutionCommitTransactionId = $"development.combat-execution.commit.{Guid.NewGuid():N}";
            return lastCombatExecutionCommitTransactionId;
        }

        private string FormatCombatExecutionCooldowns(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return "Cooldowns: None";
            }

            List<string> lines = new List<string> { "Cooldowns:" };
            IReadOnlyList<CombatExecutionDefinition> definitions = GetDefinitions<CombatExecutionDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                CombatExecutionDefinition definition = definitions[i];
                CombatExecutionCooldownSnapshot snapshot = combatExecutionService.GetCooldownState(actorId, definition.ResolveCooldownKey());
                if (snapshot == null)
                {
                    lines.Add($"- {definition.DisplayName}: Ready");
                }
                else
                {
                    lines.Add($"- {definition.DisplayName}: Charges {snapshot.CurrentCharges}/{snapshot.MaximumCharges} ReadyAt {snapshot.CooldownReadyAt:0.###}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatCombatExecutionResult(CombatExecutionResult result)
        {
            if (result == null)
            {
                return "No combat execution result.";
            }

            string state = result.State == null
                ? "State=None"
                : $"State={result.State.Phase} Instance={result.State.ExecutionInstanceId} Ready={result.State.ReadyAt:0.###} RecoveryEnd={result.State.RecoveryEndsAt:0.###}";
            string costs = result.Costs == null || result.Costs.Count == 0
                ? "Costs=None"
                : $"Costs={string.Join(", ", result.Costs.Select(cost => $"{cost.DefinitionId}:{cost.Amount:0.###}:{cost.Code}"))}";
            string cooldown = result.Cooldown == null
                ? "Cooldown=None"
                : $"Cooldown={result.Cooldown.CooldownKey} Charges={result.Cooldown.CurrentCharges}/{result.Cooldown.MaximumCharges} Ready={result.Cooldown.CooldownReadyAt:0.###}";
            return $"{result.Code} Success={result.Succeeded} Preview={result.Preview} Duplicate={result.Duplicate} Definition={result.DefinitionId}\n{state}\n{costs}\n{cooldown}\n{result.Message}";
        }

        private OngoingEffectService EnsureOngoingEffectRuntime(bool targetEnemy)
        {
            PrototypeTestLabContext labContext = context;
            GameObject actor = targetEnemy ? labContext?.EnemyTransform?.gameObject : labContext?.PlayerTransform?.gameObject;
            if (actor == null)
            {
                return null;
            }

            if (targetEnemy)
            {
                EnsureLifecycleRuntime(actor, ref labContext.EnemyLifecycle, needsResource: true);
            }
            else
            {
                EnsureLifecycleRuntime(actor, ref labContext.PlayerLifecycle, needsResource: true);
            }

            OngoingEffectService service = targetEnemy ? labContext.EnemyOngoingEffects : labContext.PlayerOngoingEffects;
            if (service == null)
            {
                service = actor.GetComponentInParent<OngoingEffectService>();
            }

            if (service == null)
            {
                service = actor.AddComponent<OngoingEffectService>();
            }

            if (service == null)
            {
                return null;
            }

            CharacterSystemCoordinator coordinator = actor.GetComponentInParent<CharacterSystemCoordinator>();
            if (coordinator == null && !targetEnemy)
            {
                return null;
            }

            service.Configure(coordinator);
            service.ConfigureDamageHealing(damageHealingService);
            service.SetClock(ongoingEffectClockSeconds);
            if (targetEnemy)
            {
                labContext.EnemyOngoingEffects = service;
            }
            else
            {
                labContext.PlayerOngoingEffects = service;
            }

            return service;
        }

        private CombatReactionService EnsureCombatReactionRuntime()
        {
            GameObject host = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            if (host == null)
            {
                return combatReactionService;
            }

            if (combatReactionService == null)
            {
                combatReactionService = host.GetComponentInParent<CombatReactionService>();
            }

            if (combatReactionService == null)
            {
                combatReactionService = host.AddComponent<CombatReactionService>();
            }

            OngoingEffectService ongoing = EnsureOngoingEffectRuntime(targetEnemy: true) ?? EnsureOngoingEffectRuntime(targetEnemy: false);
            if (ongoing != null)
            {
                combatReactionService.Configure(ongoing, damageHealingService);
            }

            return combatReactionService;
        }

        private CombatContributionService EnsureCombatContributionRuntime()
        {
            GameObject host = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            if (host == null)
            {
                return combatContributionService;
            }

            if (combatContributionService == null)
            {
                combatContributionService = host.GetComponentInParent<CombatContributionService>();
            }

            if (combatContributionService == null)
            {
                combatContributionService = host.AddComponent<CombatContributionService>();
            }

            CombatContributionPolicyDefinition policy = GetDefinitions<CombatContributionPolicyDefinition>().FirstOrDefault();
            if (policy != null)
            {
                combatContributionService.Configure(policy);
            }

            return combatContributionService;
        }

        private bool TryBuildOngoingEffectRequest(
            OngoingEffectDefinition definition,
            bool targetEnemy,
            float amount,
            float interval,
            float duration,
            int tickCount,
            int stacks,
            bool reuseTransaction,
            out OngoingEffectService service,
            out OngoingEffectApplicationRequest request,
            out PrototypeTestLabOperation failure)
        {
            service = EnsureOngoingEffectRuntime(targetEnemy);
            request = default;
            failure = default;
            if (definition == null)
            {
                failure = RecordFailure("Ongoing Effect", "Ongoing effect definition is missing.", "MissingDefinition");
                return false;
            }

            GameObject target = targetEnemy ? context?.EnemyTransform?.gameObject : context?.PlayerTransform?.gameObject;
            GameObject source = targetEnemy ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (service == null || target == null)
            {
                failure = RecordFailure("Ongoing Effect", "Ongoing effect target service or target object is missing.", "MissingTarget");
                return false;
            }

            request = new OngoingEffectApplicationRequest(
                ResolveOngoingEffectTransactionId(reuseTransaction),
                definition,
                ResolveActorId(source),
                source,
                ResolveActorId(target),
                target,
                "development.test-lab",
                Mathf.Max(0f, amount),
                Mathf.Max(0f, interval),
                Mathf.Max(0f, duration),
                Mathf.Max(0, tickCount),
                Mathf.Max(1, stacks),
                authorityValidated: true);
            return true;
        }

        private bool TryResolveLifecycleTarget(bool targetEnemy, out ActorLifecycleController lifecycle, out GameObject target, out string actorId, out PrototypeTestLabOperation failure)
        {
            target = targetEnemy ? context?.EnemyTransform?.gameObject : context?.PlayerTransform?.gameObject;
            lifecycle = targetEnemy ? context?.EnemyLifecycle : context?.PlayerLifecycle;
            failure = default;
            actorId = string.Empty;

            if (context == null)
            {
                failure = RecordFailure("Lifecycle Target", "Test Lab context is missing.", "MissingContext");
                return false;
            }

            if (targetEnemy)
            {
                EnsureLifecycleRuntime(target, ref context.EnemyLifecycle, needsResource: true);
                lifecycle = context?.EnemyLifecycle;
            }
            else
            {
                EnsureLifecycleRuntime(target, ref context.PlayerLifecycle, needsResource: true);
                lifecycle = context?.PlayerLifecycle;
            }

            if (target == null || lifecycle == null)
            {
                failure = RecordFailure("Lifecycle Target", "Lifecycle target is missing.", "MissingTarget");
                return false;
            }

            actorId = ResolveActorId(target);
            if (string.IsNullOrWhiteSpace(actorId))
            {
                actorId = lifecycle.ActorId;
            }

            return true;
        }

        private string ResolveAttackTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastAttackTransactionId))
            {
                return lastAttackTransactionId;
            }

            lastAttackTransactionId = AttackDeterministicRoll.NewTransactionId("development.attack-resolution");
            return lastAttackTransactionId;
        }

        private string ResolveDefenseActivationTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastDefenseActivationTransactionId))
            {
                return lastDefenseActivationTransactionId;
            }

            lastDefenseActivationTransactionId = $"development.defense-action.activate.{Guid.NewGuid():N}";
            return lastDefenseActivationTransactionId;
        }

        private string ResolveCombatStateTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastCombatStateTransactionId))
            {
                return lastCombatStateTransactionId;
            }

            lastCombatStateTransactionId = $"development.combat-state.{Guid.NewGuid():N}";
            return lastCombatStateTransactionId;
        }

        private string ResolveCombatStateSplitTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastCombatStateSplitTransactionId))
            {
                return lastCombatStateSplitTransactionId;
            }

            lastCombatStateSplitTransactionId = $"development.combat-state.split.{Guid.NewGuid():N}";
            return lastCombatStateSplitTransactionId;
        }

        private string ResolveLifecycleTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastLifecycleTransactionId))
            {
                return lastLifecycleTransactionId;
            }

            lastLifecycleTransactionId = $"development.lifecycle.{Guid.NewGuid():N}";
            return lastLifecycleTransactionId;
        }

        private string ResolveOngoingEffectTransactionId(bool reuse)
        {
            if (reuse && !string.IsNullOrWhiteSpace(lastOngoingEffectTransactionId))
            {
                return lastOngoingEffectTransactionId;
            }

            lastOngoingEffectTransactionId = $"development.ongoing-effect.{Guid.NewGuid():N}";
            return lastOngoingEffectTransactionId;
        }

        private static string FormatAttackResolution(AttackResolutionResult result)
        {
            if (result == null)
            {
                return "Attack result is missing.";
            }

            string damage = result.DamageResult == null
                ? "Damage=None"
                : $"Damage={result.DamageResult.Code} final={result.DamageResult.FinalDamageAmount:0.###} Health={result.DamageResult.OldHealth:0.###}->{result.DamageResult.NewHealth:0.###}";
            string defense = result.DefenseResult == null
                ? "Defense=None"
                : $"Defense={result.DefenseResult.Outcome} action={result.DefenseResult.DefensiveActionId} chance={result.DefenseResult.FinalDefenseChance:0.###} roll={result.DefenseResult.Request.Roll:0.###} prevented={result.DefenseResult.PreventedDamage:0.###} remaining={result.DefenseResult.RemainingDamage:0.###} consumed={result.DefenseResult.Consumed}";
            return $"{result.Outcome} hitChance={result.FinalHitChance:0.###} roll={result.HitRoll:0.###} crit={result.Critical} critRoll={result.CriticalRoll:0.###} dmgAfterCrit={result.DamageAfterCritical:0.###} duplicate={result.Duplicate} {defense} {damage}. {result.Message}";
        }

        private bool TryBuildCombatEngagementRequest(
            CombatActivityClassification classification,
            bool reuseTransaction,
            out CombatStateService service,
            out CombatEngagementRequest request,
            out PrototypeTestLabOperation failure)
        {
            service = EnsureCombatStateRuntime();
            request = default;
            failure = default;
            GameObject source = context?.PlayerTransform == null ? null : context.PlayerTransform.gameObject;
            GameObject target = context?.EnemyTransform == null ? null : context.EnemyTransform.gameObject;
            if (service == null || source == null || target == null)
            {
                failure = RecordFailure("6.5 Combat Engagement", "Combat State service, player, or enemy is missing.", "MissingReference");
                return false;
            }

            request = new CombatEngagementRequest(
                ResolveCombatStateTransactionId(reuseTransaction),
                ResolveCombatStateActorId(source),
                source,
                ResolveCombatStateActorId(target),
                target,
                classification,
                "development.test-lab",
                hostile: true,
                authorityValidated: true);
            return true;
        }

        private static string FormatCombatStateSnapshot(string label, ActorCombatStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return $"{label}: Missing";
            }

            float remaining = Mathf.Max(0f, snapshot.DisengageEligibleAt - snapshot.LastActivityAt);
            return $"{label}: {snapshot.State} Actor={snapshot.ActorId} Encounter={(string.IsNullOrWhiteSpace(snapshot.EncounterId) ? "None" : snapshot.EncounterId)} Participants={snapshot.ParticipantCount} Engagements={snapshot.ActiveEngagementCount} Entered={snapshot.EnteredAt:0.###} Last={snapshot.LastActivityAt:0.###} TimeoutWindow={remaining:0.###} Rev={snapshot.Revision} Reason={snapshot.TransitionReason}";
        }

        private static string FormatCombatEntryResult(CombatEntryResult result)
        {
            if (result == null)
            {
                return "Combat entry result is missing.";
            }

            return $"{result.SourceActorId}->{result.TargetActorId} Encounter={result.EncounterId} Engagement={result.EngagementId} Created={result.EncounterCreated} Added={result.SourceParticipantAdded}/{result.TargetParticipantAdded} Merged={result.EncounterMerged} Preview={result.Preview} Duplicate={result.Duplicate}. {result.Message}";
        }

        private static string FormatCombatExitResult(CombatExitResult result)
        {
            if (result == null)
            {
                return "Combat exit result is missing.";
            }

            return $"{result.ActorId} left Encounter={result.EncounterId} Reason={result.Reason} EndedEngagements={result.EngagementsEnded.Count} Preview={result.Preview} Duplicate={result.Duplicate}. {result.Message}";
        }

        private static string FormatCombatProcessResult(CombatStateProcessResult result)
        {
            if (result == null)
            {
                return "Combat process result is missing.";
            }

            return $"Delta={result.DeltaSeconds:0.###} Exits={result.ProcessedExits} Capped={result.Capped} Splits={result.SplitResults.Count} EndedEncounters={result.EndedEncounters.Count}";
        }

        private static string FormatCombatSplitResult(CombatEncounterSplitResult result)
        {
            if (result == null)
            {
                return "Combat split result is missing.";
            }

            string created = result.CreatedEncounterIds.Count == 0 ? "None" : string.Join(", ", result.CreatedEncounterIds);
            string left = result.ParticipantsLeftCombat.Count == 0 ? "None" : string.Join(", ", result.ParticipantsLeftCombat);
            string components = result.Components.Count == 0
                ? "None"
                : string.Join(" | ", result.Components.Select(component => $"{component.EncounterId}{(component.RetainedOriginalEncounterId ? "*" : string.Empty)} P=[{string.Join(",", component.ParticipantIds)}] E=[{string.Join(",", component.EngagementIds)}]"));
            string ended = result.EndedEngagementIds.Count == 0 ? "None" : string.Join(", ", result.EndedEngagementIds);
            return $"Original={result.OriginalEncounterId} Survivor={result.SurvivingEncounterId} Created={created} EndedEdges={ended} Left={left} Components={components} Duplicate={result.Duplicate}. {result.Message}";
        }

        private static string FormatCombatEncounter(CombatEncounterSnapshot encounter)
        {
            if (encounter == null)
            {
                return "Encounter: None";
            }

            string participants = encounter.ParticipantIds.Count == 0 ? "None" : string.Join(", ", encounter.ParticipantIds);
            string engagements = encounter.Engagements.Count == 0 ? "None" : string.Join(" | ", encounter.Engagements.Select(engagement => $"{engagement.EngagementId}:{engagement.SourceActorId}<->{engagement.TargetActorId}:{(engagement.Active ? "Active" : engagement.EndReason.ToString())}"));
            return $"Encounter: {encounter.EncounterId} Active={encounter.Active} Created={encounter.CreatedAt:0.###} Last={encounter.LastActivityAt:0.###} Participants=[{participants}] Engagements=[{engagements}] Completion={encounter.CompletionReason}";
        }

        private static string FormatLifecycleResult(ActorLifecycleResult result)
        {
            if (result == null)
            {
                return "Lifecycle result is missing.";
            }

            string requirement = string.IsNullOrWhiteSpace(result.RequirementSummary) ? string.Empty : $" Requirements={result.RequirementSummary}.";
            return $"{result.PreviousState}->{result.ResultingState} Transition={result.Transition} Trigger={result.Trigger} Health={result.OldHealth:0.###}->{result.NewHealth:0.###} Restore={result.AppliedHealthRestore:0.###}/{result.RequestedHealthRestore:0.###} Duplicate={result.Duplicate}. {result.Message}{requirement}";
        }

        private static string FormatLifecycleSummary(string label, ActorLifecycleController lifecycle, CharacterResourceCollection resources)
        {
            string state = lifecycle == null ? "Missing" : lifecycle.State.ToString();
            string actorId = lifecycle == null ? "None" : lifecycle.ActorId;
            string policy = lifecycle == null || lifecycle.DefeatPolicy == null ? "Default local living-being policy" : $"{lifecycle.DefeatPolicy.DisplayName} ({lifecycle.DefeatPolicy.Id})";
            string health = resources != null && resources.TryGetResource(ResourceIds.Health, out ResourceSnapshot snapshot)
                ? $"{snapshot.Current:0.###}/{snapshot.Maximum:0.###}"
                : "Missing";
            return $"{label}: State={state} Actor={actorId} Health={health} Policy={policy}";
        }

        private static string FormatOngoingEffectTarget(string label, OngoingEffectService service, GameObject target, CharacterResourceCollection resources, ActorLifecycleController lifecycle)
        {
            string health = FormatResourceSnapshot(resources, ResourceIds.Health);
            string mana = FormatResourceSnapshot(resources, ResourceIds.Mana);
            string stamina = FormatResourceSnapshot(resources, ResourceIds.Stamina);
            string state = lifecycle == null ? "Active" : lifecycle.State.ToString();
            IReadOnlyList<RuntimeOngoingEffectInstance> instances = service == null ? Array.Empty<RuntimeOngoingEffectInstance>() : service.QueryActiveEffects(target);
            string active = instances.Count == 0
                ? "None"
                : string.Join(" | ", instances.Select(instance => $"{instance.Definition.DisplayName} x{instance.StackCount} next={instance.NextTickElapsedSeconds:0.###}s rem={instance.RemainingDuration:0.###}s ticks={instance.CompletedTicks}/{(instance.FiniteTickCount > 0 ? instance.FiniteTickCount.ToString() : "duration")} [{instance.InstanceId}]"));
            return $"{label}: Lifecycle={state} H={health} M={mana} S={stamina} Active={active}";
        }

        private static string FormatResourceSnapshot(CharacterResourceCollection resources, string resourceId)
        {
            return resources != null && resources.TryGetResource(resourceId, out ResourceSnapshot snapshot)
                ? $"{snapshot.Current:0.###}/{snapshot.Maximum:0.###}"
                : "Missing";
        }

        private static string FormatOngoingApplicationResult(OngoingEffectApplicationResult result)
        {
            if (result == null)
            {
                return "Ongoing effect application result is missing.";
            }

            string ticks = result.ImmediateTicks == null || result.ImmediateTicks.Count == 0
                ? "ImmediateTicks=0"
                : $"ImmediateTicks={result.ImmediateTicks.Count} Last={FormatOngoingTickResult(result.ImmediateTicks[result.ImmediateTicks.Count - 1])}";
            return $"{result.Outcome} {result.DefinitionId} Instance={result.InstanceId} Stacks={result.PreviousStackCount}->{result.ResultingStackCount} Duration={result.PreviousRemainingDuration:0.###}->{result.ResultingRemainingDuration:0.###} Preview={result.Preview} Duplicate={result.Duplicate}. {ticks}. {result.Message}";
        }

        private static string FormatOngoingProcessResult(OngoingEffectProcessResult result)
        {
            if (result == null)
            {
                return "Missing";
            }

            string last = result.TickResults.Count == 0 ? "NoTicks" : FormatOngoingTickResult(result.TickResults[result.TickResults.Count - 1]);
            return $"Ticks={result.ProcessedTicks} Capped={result.Capped} {last}";
        }

        private static string FormatOngoingTickResult(OngoingEffectTickResult result)
        {
            if (result == null)
            {
                return "Tick=None";
            }

            string nested = result.DamageResult != null
                ? $"Damage={result.DamageResult.FinalDamageAmount:0.###} Health={result.DamageResult.OldHealth:0.###}->{result.DamageResult.NewHealth:0.###} Immune={result.DamageResult.Immune}"
                : result.HealingResult != null
                    ? $"Healing={result.HealingResult.FinalHealingAmount:0.###} Overheal={result.HealingResult.OverhealAmount:0.###} Health={result.HealingResult.OldHealth:0.###}->{result.HealingResult.NewHealth:0.###}"
                    : result.ResourceResult != null
                        ? $"Resource={result.ResourceResult.Request.ResourceId} {result.ResourceResult.OldCurrent:0.###}->{result.ResourceResult.NewCurrent:0.###} Applied={result.ResourceResult.AppliedAmount:0.###}"
                        : "Nested=None";
            return $"Tick#{result.TickIndex} {result.Outcome} Tx={result.TickTransactionId} Amt={result.RequestedAmount:0.###} {nested}";
        }

        private static string FormatOngoingCancellationResult(OngoingEffectCancellationResult result)
        {
            if (result == null)
            {
                return "Ongoing effect cancellation result is missing.";
            }

            return $"Instance={result.InstanceId} Definition={result.DefinitionId} Preview={result.Preview} Duplicate={result.Duplicate}. {result.Message}";
        }

        private static string FormatCombatReactionChain(CombatReactionChainResult result)
        {
            if (result == null)
            {
                return "Combat reaction result is missing.";
            }

            string reactions = result.Reactions.Count == 0
                ? "None"
                : string.Join(" | ", result.Reactions.Select(reaction => $"{reaction.DefinitionId}:{reaction.Code}:Succeeded={reaction.Succeeded}:Preview={reaction.Preview}:Duplicate={reaction.Duplicate}:Amount={reaction.FinalAmount:0.###}:Tx={reaction.TransactionId}"));
            return $"Trigger={result.RootContext?.TriggerType} Preview={result.Preview} Depth={result.Depth} Reactions={result.Reactions.Count}. {result.Message} {reactions}";
        }

        private static string FormatContributionRecordResult(CombatContributionRecordResult result)
        {
            if (result == null)
            {
                return "Contribution result is missing.";
            }

            CombatContributionRecord record = result.Record;
            string recordText = record == null
                ? "Record=None"
                : $"Record={record.RecordId} Type={record.ContributionType} Contributor={record.ContributorActorId} Target={record.TargetActorId} Beneficiary={record.BeneficiaryActorId} Actual={record.ActualAmount:0.###} Prevented={record.PreventedAmount:0.###} Weight={record.ContributionWeight:0.###}";
            return $"Contribution Success={result.Succeeded} Preview={result.Preview} Duplicate={result.Duplicate} Code={result.Code} Rev={result.RevisionBefore}->{result.RevisionAfter}. {recordText}. {result.Message}";
        }

        private static string FormatContributionLedger(CombatContributionLedgerSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "Contribution ledger is missing.";
            }

            string summaries = snapshot.Summaries.Count == 0
                ? "No summaries"
                : string.Join(" | ", snapshot.Summaries.Select(summary => $"{summary.ContributorActorId}:Dmg={summary.TotalActualDamage:0.###} Heal={summary.TotalEffectiveHealing:0.###} Def={summary.TotalDamagePrevented:0.###} Elig={string.Join(",", summary.Eligibility)}"));
            return $"{snapshot.LedgerId} Encounter={(string.IsNullOrWhiteSpace(snapshot.EncounterId) ? "None" : snapshot.EncounterId)} Target={(string.IsNullOrWhiteSpace(snapshot.TargetActorId) ? "Mixed" : snapshot.TargetActorId)} Records={snapshot.Records.Count} Finalized={snapshot.Finalized} Rev={snapshot.Revision}. {summaries}";
        }

        private static string FormatCreditResult(CombatCreditResolutionResult result)
        {
            if (result == null)
            {
                return "Credit result is missing.";
            }

            string assists = result.Assists.Count == 0 ? "None" : string.Join(",", result.Assists.Select(summary => summary.ContributorActorId));
            return $"Credit={result.CreditType} Success={result.Succeeded} Duplicate={result.Duplicate} Primary={(string.IsNullOrWhiteSpace(result.PrimaryContributorActorId) ? "Unassigned" : result.PrimaryContributorActorId)} Assists={assists} Contributors={result.Contributors.Count} GrantsRewards={result.GrantsConcreteRewards}. {result.Message}";
        }

        private static string FormatDamageApplication(DamageApplicationResult result)
        {
            if (result == null)
            {
                return "Damage result missing.";
            }

            return $"Damage Success={result.Succeeded} Preview={result.Preview} Duplicate={result.Duplicate} Actual={result.FinalDamageAmount:0.###} Health={result.OldHealth:0.###}->{result.NewHealth:0.###}.";
        }

        private static string FormatHealingApplication(HealingApplicationResult result)
        {
            if (result == null)
            {
                return "Healing result missing.";
            }

            return $"Healing Success={result.Succeeded} Preview={result.Preview} Duplicate={result.Duplicate} Effective={result.FinalHealingAmount:0.###} Overheal={result.OverhealAmount:0.###} Health={result.OldHealth:0.###}->{result.NewHealth:0.###}.";
        }

        private static string FormatCombatTransactionTrace(CombatTransactionTraceSnapshot trace)
        {
            if (trace == null)
            {
                return "Transaction Trace: None";
            }

            return $"Transaction Trace: Root={EmptyAs(trace.RootTransactionId, "None")} Execution={EmptyAs(trace.ExecutionTransactionId, "None")} Attack={EmptyAs(trace.AttackTransactionId, "None")} Defense={EmptyAs(trace.DefenseTransactionId, "None")} Damage={EmptyAs(trace.DamageTransactionId, "None")} Reaction={EmptyAs(trace.ReactionTransactionId, "None")} Contribution={EmptyAs(trace.ContributionTransactionId, "None")} Coherent={trace.IsCoherent}.";
        }

        private DefensiveActionDefinition FindDefensiveAction(string idOrNameFragment)
        {
            if (string.IsNullOrWhiteSpace(idOrNameFragment))
            {
                return null;
            }

            return GetDefinitions<DefensiveActionDefinition>()
                .FirstOrDefault(definition =>
                    definition != null
                    && ((definition.Id != null && definition.Id.IndexOf(idOrNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (definition.DisplayName != null && definition.DisplayName.IndexOf(idOrNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        private bool EnsureCompatibleDefenseEquipment(DefensiveActionDefinition definition, out string failureReason)
        {
            failureReason = string.Empty;
            if (definition == null)
            {
                failureReason = "Defensive action definition is missing.";
                return false;
            }

            bool requiresEquipment = definition.RequiresEquipmentSource
                || !string.IsNullOrWhiteSpace(definition.RequiredEquipmentCategoryId)
                || !string.IsNullOrWhiteSpace(definition.RequiredEquipmentTagId);
            if (!requiresEquipment)
            {
                return true;
            }

            if (IsCompatibleDefenseItemEquipped(definition))
            {
                return true;
            }

            if (context?.Inventory == null || context.Equipment == null)
            {
                failureReason = "Inventory or equipment is missing.";
                return false;
            }

            ItemDefinition item = GetDefinitions<ItemDefinition>().FirstOrDefault(candidate => IsCompatibleDefenseItem(candidate, definition));
            if (item == null)
            {
                failureReason = $"No item definition satisfies defensive action '{definition.Id}'.";
                return false;
            }

            int slotIndex = FindInventorySlot(item);
            if (slotIndex < 0)
            {
                InventoryAddResult add = context.Inventory.AddItem(item, 1);
                if (add.AddedQuantity <= 0)
                {
                    failureReason = $"Could not grant required item '{item.DisplayName}' for defensive action '{definition.Id}'.";
                    return false;
                }

                slotIndex = FindInventorySlot(item);
            }

            if (slotIndex < 0)
            {
                failureReason = $"Required item '{item.DisplayName}' was not found in inventory after grant.";
                return false;
            }

            EquipmentOperationResult equip = context.Equipment.EquipFromInventorySlot(slotIndex);
            if (!equip.Succeeded)
            {
                failureReason = equip.Message;
                return false;
            }

            return IsCompatibleDefenseItemEquipped(definition);
        }

        private bool IsCompatibleDefenseItemEquipped(DefensiveActionDefinition definition)
        {
            if (context?.Equipment == null)
            {
                return false;
            }

            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty && IsCompatibleDefenseItem(slot.Item, definition))
                {
                    return true;
                }
            }

            return false;
        }

        private int FindInventorySlot(ItemDefinition item)
        {
            if (context?.Inventory == null || item == null)
            {
                return -1;
            }

            for (int i = 0; i < context.Inventory.Slots.Count; i++)
            {
                InventorySlot slot = context.Inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty && slot.Item == item)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsCompatibleDefenseItem(ItemDefinition item, DefensiveActionDefinition definition)
        {
            if (item == null || definition == null || !item.IsEquippable)
            {
                return false;
            }

            if (definition.ActionType == DefensiveActionType.Parry && item.Equipment?.MeleeWeapon?.IsWeapon != true)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.RequiredEquipmentCategoryId)
                && !string.Equals(item.PrimaryCategory?.Id, definition.RequiredEquipmentCategoryId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.RequiredEquipmentTagId) && !ItemHasTag(item, definition.RequiredEquipmentTagId))
            {
                return false;
            }

            return true;
        }

        private static bool ItemHasTag(ItemDefinition item, string tagId)
        {
            IReadOnlyList<TagDefinition> tags = item == null ? Array.Empty<TagDefinition>() : item.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null && string.Equals(tags[i].Id, tagId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountDiagnostics(IReadOnlyList<CombatRuntimeDiagnostic> diagnostics, CombatIntegritySeverity severity)
        {
            return diagnostics == null ? 0 : diagnostics.Count(diagnostic => diagnostic != null && diagnostic.Severity == severity);
        }

        private static string EmptyAs(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private HealingApplicationRequest CreatePipelineHealingRequest(float amount, bool targetPlayer, string transactionId)
        {
            GameObject source = context?.PlayerTransform?.gameObject;
            GameObject target = targetPlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            EnsureAttackResolutionRuntime(source, needsResource: false);
            EnsureAttackResolutionRuntime(target, needsResource: true);
            return new HealingApplicationRequest(
                transactionId,
                ResolveActorId(source),
                source,
                ResolveActorId(target),
                target,
                Mathf.Max(0f, amount),
                "Prototype Test Lab");
        }

        private static string ResolveActorId(GameObject actor)
        {
            if (actor == null)
            {
                return string.Empty;
            }

            CharacterSystemCoordinator character = actor.GetComponentInParent<CharacterSystemCoordinator>();
            if (character != null && !string.IsNullOrWhiteSpace(character.ActorId))
            {
                return character.ActorId;
            }

            WorldEntityIdentity identity = actor.GetComponentInParent<WorldEntityIdentity>();
            return identity == null ? string.Empty : identity.EntityId;
        }

        private static string ResolveCombatStateActorId(GameObject actor)
        {
            if (actor == null)
            {
                return string.Empty;
            }

            WorldEntityIdentity identity = actor.GetComponentInParent<WorldEntityIdentity>();
            return identity == null ? string.Empty : identity.EntityId;
        }

        public PrototypeTestLabOperation DefeatEnemy(DamageTypeDefinition damageType)
        {
            float amount = context?.EnemyHealth == null ? 9999f : context.EnemyHealth.MaximumHealth + 9999f;
            return ApplyTypedDamage(damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault(), amount, targetEnemy: true, sourcePlayer: true);
        }

        public PrototypeTestLabOperation StartQuest(QuestDefinition quest)
        {
            if (context?.QuestLog == null || quest == null)
            {
                return RecordFailure("Start Quest", "Quest log or quest definition is missing.", "MissingReference");
            }

            QuestOperationResult result = context.QuestLog.StartQuest(quest);
            return Record(result.Succeeded, "Start Quest", result.Succeeded ? "Started" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ReportTalk(PersonDefinition person)
        {
            if (person == null)
            {
                return RecordFailure("Report Talk", "No person definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportTalk(person.Id);
            return RecordSuccess("Report Talk", $"Reported talk with {FormatDefinition(person)}.");
        }

        public PrototypeTestLabOperation ReportReach(PlaceDefinition place)
        {
            if (place == null)
            {
                return RecordFailure("Report Reach Location", "No place definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportReachLocation(place);
            return RecordSuccess("Report Reach Location", $"Reported reach location {FormatDefinition(place)}.");
        }

        public PrototypeTestLabOperation ReportDefeat(string targetCategory)
        {
            if (string.IsNullOrWhiteSpace(targetCategory))
            {
                targetCategory = "prototype_enemy";
            }

            GameObject temporary = new GameObject("Development Contract Objective Target");
            try
            {
                ContractObjectiveTarget target = temporary.AddComponent<ContractObjectiveTarget>();
                target.DevelopmentSetTargetCategory(targetCategory);
                context?.QuestLog?.RecordDefeat(target);
                context?.ContractJournal?.RecordDefeat(target);
                return RecordSuccess("Report Defeat", $"Reported defeat target '{targetCategory}'.");
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(temporary);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }
        }

        public PrototypeTestLabOperation ClearQuestLog(bool confirmed)
        {
            if (!RequireConfirmation("ClearQuestLog", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.QuestLog?.DevelopmentClearQuestLog();
            return RecordSuccess("Clear Quest Log", "Quest log cleared.");
        }

        public PrototypeTestLabOperation AcceptContract(ContractDefinition contract)
        {
            if (context?.ContractJournal == null || contract == null)
            {
                return RecordFailure("Accept Contract", "Contract journal or contract definition is missing.", "MissingReference");
            }

            ContractOperationResult result = context.ContractJournal.AcceptContract(contract);
            return Record(result.Succeeded, "Accept Contract", result.Succeeded ? "Accepted" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ClearContractJournal(bool confirmed)
        {
            if (!RequireConfirmation("ClearContractJournal", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.ContractJournal?.DevelopmentClearContractJournal();
            return RecordSuccess("Clear Contract Journal", "Contract journal cleared.");
        }

        public PrototypeTestLabOperation Save()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SavePrototypeSlot();
            return Record(result.Succeeded, "Save Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation Load()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadPrototypeSlot();
            return Record(result.Succeeded, "Load Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateSave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidatePrototypeSlot();
            return Record(result.Succeeded, "Validate Prototype Slot", result.Status.ToString(), $"{result.Message} BackupAvailable={result.BackupAvailable}");
        }

        public PrototypeTestLabOperation DeleteSave(bool confirmed)
        {
            if (!RequireConfirmation("DeleteSave", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Delete Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.DeletePrototypeSlot();
            return Record(result.Succeeded, "Delete Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ForceAutosave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Force Autosave", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.ForceAutosave("TestLab");
            return Record(result.Succeeded, "Force Autosave", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation SetShortAutosaveInterval()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Set Autosave Interval", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.SetAutosaveIntervalForTesting(15f);
            return RecordSuccess("Set Autosave Interval", "Autosave interval set to 15 seconds for local testing.");
        }

        public PrototypeTestLabOperation MarkSaveDirty()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Dirty", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(true, "Test Lab marked save dirty.");
            return RecordSuccess("Mark Save Dirty", "Save dirty state set for confirmation and autosave testing.");
        }

        public PrototypeTestLabOperation MarkSaveClean()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Clean", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(false, "Test Lab marked save clean.");
            return RecordSuccess("Mark Save Clean", "Save dirty state cleared.");
        }

        public PrototypeTestLabOperation SaveManualSlotOne()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SaveManualSlot(0);
            return Record(result.Succeeded, "Save Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation LoadManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), loadBackup: true);
            return Record(result.Succeeded, "Load Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidateSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), validateBackup: true);
            return Record(result.Succeeded, "Validate Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RunRecoveryScan()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Run Recovery Scan", "Persistence service is missing.", "MissingPersistence");
            }

            SaveRecoveryScanReport report = persistence.RunRecoveryScan();
            return RecordSuccess("Run Recovery Scan", $"{report.candidates.Length} candidate(s). {report.recommendation}");
        }

        public PrototypeTestLabOperation PromoteManualSlotOneBackup(bool confirmed)
        {
            if (!RequireConfirmation("PromoteManualSlotOneBackup", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Promote Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.PromoteBackup(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Promote Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation QuarantineManualSlotOnePrimary(bool confirmed)
        {
            if (!RequireConfirmation("QuarantineManualSlotOnePrimary", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Quarantine Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.QuarantinePrimary(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Quarantine Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation CleanupTemporarySaves(bool confirmed)
        {
            if (!RequireConfirmation("CleanupTemporarySaves", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Cleanup Temporary Saves", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.CleanupStaleTemporaryFiles();
            return Record(result.Succeeded, "Cleanup Temporary Saves", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation InjectPrepareFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Prepare Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadPrepare);
            return RecordSuccess("Inject Prepare Failure", "Next load prepare phase will fail once.");
        }

        public PrototypeTestLabOperation InjectCommitFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Commit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadCommit);
            return RecordSuccess("Inject Commit Failure", "Next load commit phase will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation InjectAuditFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Audit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.ConsistencyAudit);
            return RecordSuccess("Inject Audit Failure", "Next consistency audit will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation RecordFingerprint()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Record Fingerprint", "Persistence service is missing.", "MissingPersistence");
            }

            return RecordSuccess("Record Fingerprint", persistence.BuildRuntimeStateFingerprint());
        }

        public PrototypeTestLabOperation Teleport(PrototypeTestPoint point)
        {
            if (context?.PlayerTransform == null || point == null)
            {
                return RecordFailure("Teleport", "Player transform or test point is missing.", "MissingReference");
            }

            CharacterController characterController = context.PlayerTransform.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            context.PlayerTransform.SetPositionAndRotation(point.transform.position, point.transform.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            return RecordSuccess("Teleport", $"Teleported to {point.DisplayName} ({point.TestPointId}).");
        }

        public PrototypeTestLabOperation ValidateCurrentLocation()
        {
            if (context?.Persistence == null)
            {
                return RecordFailure("Validate Current Location", "Persistence service is missing.", "MissingPersistence");
            }

            string summary = context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return RecordSuccess("Validate Current Location", summary.Replace(Environment.NewLine, " | "));
        }

        public PrototypeTestLabOperation ValidateIdentityProgression()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Validate Identity", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            bool valid = progression.ValidateIdentity(out string failureReason);
            return Record(valid, "Validate Identity", valid ? "Valid" : "Invalid", valid ? "Identity IDs are distinct and well-formed." : failureReason);
        }

        public PrototypeTestLabOperation GenerateOrigin(int seed)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Generate Origin", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            if (registry == null)
            {
                return RecordFailure("Generate Origin", "Definition registry is missing.", "MissingRegistry");
            }

            int effectiveSeed = seed == 0 ? Environment.TickCount : seed;
            ProgressionOperationResult result = progression.AssignRandomOrigin(registry, effectiveSeed);
            return Record(result.Succeeded, "Generate Origin", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ProveOriginAssignmentIsOnceOnly()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Duplicate Origin Proof", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            if (registry == null)
            {
                return RecordFailure("Duplicate Origin Proof", "Definition registry is missing.", "MissingRegistry");
            }

            ProgressionOperationResult result = progression.AssignRandomOrigin(registry, Environment.TickCount);
            bool expectedFailure = !result.Succeeded && string.Equals(result.Code, "OriginAlreadyAssigned", StringComparison.Ordinal);
            return Record(expectedFailure, "Duplicate Origin Proof", expectedFailure ? "Rejected" : result.Code, expectedFailure ? "Second origin assignment was correctly rejected." : result.Message);
        }

        public PrototypeTestLabOperation ResetIdentityProgression(bool confirmed)
        {
            if (!RequireConfirmation("ResetIdentityProgression", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Reset Identity", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.ResetIdentityProgressionForDevelopment();
            return Record(result.Succeeded, "Reset Identity", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AdvanceBirthGiftProgress(float seconds)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Advance Birth Gift", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AdvanceBirthGiftProgressForTesting(Mathf.Max(0f, seconds), registry);
            return Record(result.Succeeded, "Advance Birth Gift", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ForceBirthGiftAwakening()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Awaken Birth Gift", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.ForceBirthGiftAwakening(registry);
            return Record(result.Succeeded, "Awaken Birth Gift", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddRole(RoleDefinition role, bool acceptConflicts)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Add Role", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            RoleAcquisitionResult result = progression.AddRole(role, "test-lab", "manual-test-lab", primary: false, acceptConflicts: acceptConflicts);
            string message = result.Conflict != null && result.Conflict.HasConflict
                ? $"{result.Message} Blockers={string.Join(", ", result.Conflict.Blockers.Select(blocker => blocker.roleDefinitionId))}"
                : result.Message;
            return Record(result.Succeeded, acceptConflicts ? "Add Role Accepting Conflicts" : "Add Role", result.Code, message);
        }

        public PrototypeTestLabOperation SuspendFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.SuspendRole(role.recordId);
            return Record(result.Succeeded, "Suspend Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RevokeFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.RevokeRole(role.recordId);
            return Record(result.Succeeded, "Revoke Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AbandonFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.AbandonRole(role.recordId);
            return Record(result.Succeeded, "Abandon Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddGlobalSocialStatus(SocialStatusDefinition status)
        {
            return AddSocialStatus(status, SocialStatusContextKind.Global, string.Empty, "Add Global Status");
        }

        public PrototypeTestLabOperation AddPlaceSocialStatus(SocialStatusDefinition status, PlaceDefinition place)
        {
            string placeId = place == null ? string.Empty : place.Id;
            return AddSocialStatus(status, SocialStatusContextKind.Place, placeId, "Add Place Status");
        }

        public PrototypeTestLabOperation ResolveFirstActiveSocialStatus()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Resolve Social Status", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            RuntimeSocialStatusRecord status = progression.SocialStatuses.FirstOrDefault(record => record.lifecycleState == SocialStatusLifecycleState.Active);
            if (status == null)
            {
                return RecordFailure("Resolve Social Status", "No active social status exists.", "MissingActiveStatus");
            }

            ProgressionOperationResult result = progression.ResolveSocialStatus(status.recordId, "test-lab-resolved");
            return Record(result.Succeeded, "Resolve Social Status", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddCurrency(CurrencyDefinition currency, long amount)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Add Currency", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AddCurrency(currency, Math.Max(0L, amount));
            return Record(result.Succeeded, "Add Currency", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SpendCurrency(CurrencyDefinition currency, long amount)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Spend Currency", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.SpendCurrency(currency, Math.Max(0L, amount));
            return Record(result.Succeeded, "Spend Currency", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RecordSuccessfulActivity(float difficulty)
        {
            return RecordActivity(ActivityOutcome.Success, difficulty, "Record Success Activity");
        }

        public PrototypeTestLabOperation RecordFailedActivity(float difficulty)
        {
            return RecordActivity(ActivityOutcome.Failure, difficulty, "Record Failure Activity");
        }

        public PrototypeTestLabOperation RecordParticipation()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Record Participation", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.RecordParticipation($"participation.test-lab.{Guid.NewGuid():N}", "test-lab", "PrototypeTestLab");
            return Record(result.Succeeded, "Record Participation", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RefreshWorldEntityDiagnostics()
        {
            return RecordSuccess("World Entity Diagnostics", $"Registered {WorldEntityRegistry.Count} world entity identity object(s).");
        }

        public PrototypeTestLabOperation SpawnPersistentWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Persistent World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Persistent Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.CreateRuntimeIdentity(pickup, "scene.prototype", PersistenceService.LocalWorldId, item.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Spawn Persistent World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = item;
            return RecordWorldEntityResult("Spawn Persistent World Loot", $"Spawned {item.DisplayName} as {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation SpawnTransientWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Transient World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.right * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = $"Transient Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntityIdentity identity = pickup.AddComponent<WorldEntityIdentity>();
            identity.TryMarkTransient(out _);
            return RecordWorldEntityResult("Spawn Transient World Loot", $"Spawned transient {item.DisplayName}; it is intentionally not persistently registered.");
        }

        public PrototypeTestLabOperation DestroyLastSpawnedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) || !WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out WorldEntityIdentity identity))
            {
                return RecordFailure("Destroy Spawned World Loot", "No spawned world entity is currently registered.", "MissingEntity");
            }

            WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
            lastDestroyedWorldEntityId = identity.EntityId;
            lastDestroyedWorldEntityItem = pickup == null ? null : pickup.Item;
            WorldEntityRegistry.Unregister(identity);
            DestroyTestObject(identity.gameObject);
            return RecordWorldEntityResult("Destroy Spawned World Loot", $"Destroyed {lastDestroyedWorldEntityId}.");
        }

        public PrototypeTestLabOperation RecreateDestroyedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) || lastDestroyedWorldEntityItem == null)
            {
                return RecordFailure("Recreate World Loot", "No destroyed persistent world loot is available to recreate.", "MissingSnapshot");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Restored Test Loot - {lastDestroyedWorldEntityItem.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(lastDestroyedWorldEntityItem, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(pickup, lastDestroyedWorldEntityId, "scene.prototype", PersistenceService.LocalWorldId, lastDestroyedWorldEntityItem.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Recreate World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = lastDestroyedWorldEntityItem;
            return RecordWorldEntityResult("Recreate World Loot", $"Recreated {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation AttemptDuplicateWorldEntityRegistration()
        {
            if (!TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity existingIdentity, out ItemDefinition item, out string failureReason))
            {
                return RecordWorldEntityFailure("Duplicate World Entity Proof", failureReason, "MissingEntity");
            }

            GameObject duplicate = new GameObject("Duplicate World Entity Proof");
            duplicate.name = "Duplicate World Entity Proof";
            duplicate.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(duplicate, lastSpawnedWorldEntityId, existingIdentity.SceneKey, existingIdentity.WorldId, item.Id);
            if (result.Succeeded)
            {
                WorldEntityRegistry.Unregister(result.Identity);
                DestroyTestObject(duplicate);
                return RecordWorldEntityFailure("Duplicate World Entity Proof", "Duplicate registration unexpectedly succeeded.", "UnexpectedSuccess");
            }

            DestroyTestObject(duplicate);
            return RecordWorldEntityResult("Duplicate World Entity Proof", $"Duplicate rejected: {result.Code}.");
        }

        private bool TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity identity, out ItemDefinition item, out string failureReason)
        {
            identity = null;
            item = null;
            failureReason = string.Empty;

            if (!string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                && WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out identity))
            {
                WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
                item = pickup == null ? lastSpawnedWorldEntityItem : pickup.Item;
                if (item != null)
                {
                    return true;
                }

                failureReason = "The spawned world entity has no item definition to duplicate.";
                return false;
            }

            foreach (WorldEntityIdentity candidate in WorldEntityRegistry.RegisteredEntities)
            {
                if (candidate == null
                    || candidate.IdentityKind == WorldEntityIdentityKind.Transient
                    || (!candidate.name.StartsWith("Persistent Test Loot", StringComparison.Ordinal)
                        && !candidate.name.StartsWith("Restored Test Loot", StringComparison.Ordinal)))
                {
                    continue;
                }

                WorldItemPickup pickup = candidate.GetComponent<WorldItemPickup>();
                if (pickup == null || pickup.Item == null)
                {
                    continue;
                }

                identity = candidate;
                item = pickup.Item;
                lastSpawnedWorldEntityId = identity.EntityId;
                lastSpawnedWorldEntityItem = item;
                return true;
            }

            failureReason = string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                ? "Spawn persistent world loot first."
                : $"World entity '{lastSpawnedWorldEntityId}' is no longer registered. Spawn persistent world loot again.";
            return false;
        }

        private PrototypeTestLabOperation RecordWorldEntityResult(string operationName, string message)
        {
            lastWorldEntityOperationMessage = message;
            Debug.Log($"{operationName}: {message}");
            return RecordSuccess(operationName, message);
        }

        private PrototypeTestLabOperation RecordWorldEntityFailure(string operationName, string message, string code)
        {
            lastWorldEntityOperationMessage = message;
            return RecordFailure(operationName, message, code);
        }

        private static void DestroyTestObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        public PrototypeTestLabOperation RunScenario(string scenarioId, ItemDefinition item, QuestDefinition quest, ContractDefinition contract, DamageTypeDefinition damageType)
        {
            switch (scenarioId)
            {
                case "clean":
                    context?.TestController?.ResetPrototypeState();
                    return RecordSuccess("Scenario: Clean Baseline", "Prototype reset executed; persistent player collections preserved.");
                case "combat":
                    context?.TestController?.ResetPrototypeState();
                    if (item != null)
                    {
                        GrantStatefulItem(item);
                        EquipFirstCompatible(item);
                    }

                    return RecordSuccess("Scenario: Combat Ready", "Reset vitals/enemy and attempted to grant/equip selected item.");
                case "full-inventory":
                    return FillInventory(item);
                case "quest":
                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    return RecordSuccess("Scenario: Quest Midpoint", "Started selected quest. Use Talk/Reach/Defeat actions to progress through normal events.");
                case "contract":
                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Contract Testing", "Accepted selected contract if available.");
                case "persistence":
                    RestoreVitals();
                    if (item != null)
                    {
                        GrantItem(item, 2);
                    }

                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Persistence Round Trip", "Prepared representative player state for save/load testing.");
                default:
                    return RecordFailure("Scenario", $"Unknown scenario '{scenarioId}'.", "UnknownScenario");
            }
        }

        public string RunDiagnostics()
        {
            List<string> lines = new List<string>
            {
                "Diagnostics",
                $"Definitions loaded: {(registry == null ? 0 : registry.Count)}"
            };

            AddDuplicateInstanceDiagnostics(lines);
            AddDuplicateStatusDiagnostics(lines, "Player", context?.PlayerStatuses);
            AddDuplicateStatusDiagnostics(lines, "Enemy", context?.EnemyStatuses);
            AddCharacterSystemDiagnostics(lines);
            AddReferenceDiagnostic(lines, "Inventory", context?.Inventory);
            AddReferenceDiagnostic(lines, "Equipment", context?.Equipment);
            AddReferenceDiagnostic(lines, "Quest Log", context?.QuestLog);
            AddReferenceDiagnostic(lines, "Contract Journal", context?.ContractJournal);
            AddReferenceDiagnostic(lines, "Persistence", context?.Persistence);
            AddReferenceDiagnostic(lines, "Enemy Health", context?.EnemyHealth);

            string result = string.Join(Environment.NewLine, lines);
            RecordSuccess("Refresh Diagnostics", "Diagnostics refreshed.");
            return result;
        }

        public void ClearConfirmation(string confirmationKey)
        {
            if (!string.IsNullOrWhiteSpace(confirmationKey))
            {
                pendingConfirmations.Remove(confirmationKey);
            }
        }

        private void EnsureAutomation()
        {
            if (automationRegistry.Suites.Count == 0)
            {
                PrototypeStep3AutomationSuites.RegisterDefaults(automationRegistry);
                PrototypeStep4AutomationSuites.RegisterDefaults(automationRegistry);
                PrototypeStep5AutomationSuites.RegisterDefaults(automationRegistry);
                PrototypeStep6AutomationSuites.RegisterDefaults(automationRegistry);
                PrototypeStep7AutomationSuites.RegisterDefaults(automationRegistry);
            }

            if (automationRunner == null)
            {
                automationRunner = new TestLabAutomationRunner(this, automationRegistry, new PrototypeTestLabAutomationResetCoordinator());
            }
        }

        private static TestLabAutomationOptions CreateAutomationOptions(bool stopOnFirstFailure)
        {
            return new TestLabAutomationOptions
            {
                StopOnFirstFailure = stopOnFirstFailure,
                IncludeExtended = true,
                MaximumFrameWait = 120
            };
        }

        private static string FormatAutomationRun(TestLabAutomationResult result)
        {
            if (result == null)
            {
                return "No automation result.";
            }

            return $"Run {result.RunId}: {result.PassedScenarios} passed, {result.FailedScenarios} failed, {result.ErrorScenarios} error, {result.SkippedScenarios} skipped, {result.CancelledScenarios} cancelled, {result.TotalSteps} steps.";
        }

        private void UpdateAutomationBatchResult()
        {
            if (string.IsNullOrWhiteSpace(automationBatchRunId))
            {
                return;
            }

            lastAutomationResult = new TestLabAutomationResult(
                automationBatchRunId,
                automationBatchMode,
                automationBatchStartedAtUtc,
                DateTime.UtcNow,
                automationBatchCancelled,
                automationBatchScenarios);
        }

        private bool EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence)
        {
            persistence = context?.Persistence;
            if (persistence == null)
            {
                return false;
            }

            persistence.EnsureInitialized();
            return true;
        }

        private bool EnsureIdentityProgression(out PlayerIdentityProgression progression)
        {
            progression = context?.IdentityProgression;
            if (progression == null)
            {
                return false;
            }

            progression.RegisterDefinitionCache(registry);
            return true;
        }

        private bool EnsureSkills(out CharacterSkillCollection skills)
        {
            skills = context?.PlayerSkills;
            if (skills == null)
            {
                return false;
            }

            skills.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            return true;
        }

        private bool EnsureResources(out CharacterResourceCollection resources)
        {
            resources = context?.PlayerResources;
            if (resources == null && context?.PlayerTransform != null)
            {
                resources = context.PlayerTransform.GetComponentInParent<CharacterResourceCollection>();
            }

            if (resources == null && context?.PlayerTransform != null)
            {
                resources = context.PlayerTransform.gameObject.AddComponent<CharacterResourceCollection>();
            }

            if (resources == null)
            {
                return false;
            }

            context.PlayerResources = resources;
            resources.Configure(registry, context.PlayerCalculatedStats, PersistenceService.LocalPlayerId);
            return true;
        }

        private bool EnsureTraits(out CharacterTraitCollection traits)
        {
            traits = context?.PlayerTraits;
            if (traits == null && context?.PlayerTransform != null)
            {
                traits = context.PlayerTransform.GetComponentInParent<CharacterTraitCollection>();
            }

            if (traits == null && context?.PlayerTransform != null)
            {
                traits = context.PlayerTransform.gameObject.AddComponent<CharacterTraitCollection>();
            }

            if (traits == null)
            {
                return false;
            }

            context.PlayerTraits = traits;
            traits.Configure(registry, context.PlayerCalculatedStats, context.PlayerSkills, PersistenceService.LocalPlayerId);
            return true;
        }

        private bool EnsureCharacterSystem(out CharacterSystemCoordinator character, bool initialize = true)
        {
            character = context?.CharacterSystem;
            if (character == null && context?.PlayerTransform != null)
            {
                character = context.PlayerTransform.GetComponentInParent<CharacterSystemCoordinator>();
            }

            if (character == null && context?.PlayerTransform != null)
            {
                character = context.PlayerTransform.gameObject.AddComponent<CharacterSystemCoordinator>();
            }

            if (character == null)
            {
                return false;
            }

            context.CharacterSystem = character;
            if (initialize && !character.IsReady)
            {
                character.InitializeFromRegistry(registry, restoring: false, addMissingCore: true);
            }

            return true;
        }

        private bool EnsureBodyRuntime(out ActorBodyRuntime body)
        {
            body = null;
            if (context?.PlayerTransform == null)
            {
                return false;
            }

            EnsureResources(out _);
            EnsureTraits(out _);
            EnsureCharacterSystem(out CharacterSystemCoordinator character, initialize: false);

            body = character == null ? null : character.Body;
            if (body == null)
            {
                body = context.PlayerTransform.GetComponentInParent<ActorBodyRuntime>();
            }

            if (body == null)
            {
                body = context.PlayerTransform.gameObject.AddComponent<ActorBodyRuntime>();
            }

            string actorId = character == null ? ResolveActorId(context.PlayerTransform.gameObject) : character.ActorId;
            string personId = character == null || string.IsNullOrWhiteSpace(character.PersonId)
                ? context.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId
                : character.PersonId;
            body.Configure(registry, actorId, personId, context.PlayerTraits, context.PlayerCalculatedStats);
            if (!body.IsReady)
            {
                body.AssignSpecies("species.human", restoring: false, "Test Lab body bootstrap");
            }

            return true;
        }

        private PrototypeTestLabOperation RecordBodyResult(string operationName, BodyOperationResult result)
        {
            if (result == null)
            {
                return RecordFailure(operationName, "Body operation returned no result.", BodyOperationResultCode.InvalidRequest.ToString());
            }

            BodySnapshot snapshot = result.Snapshot;
            string message = $"{result.Message} Actor={snapshot?.ActorBodyId ?? string.Empty} Person={snapshot?.PersonId ?? string.Empty} Species={snapshot?.SpeciesId ?? string.Empty} Classification={snapshot?.BiologicalClassificationId ?? string.Empty} Form={snapshot?.BodyFormId ?? string.Empty} Revision={snapshot?.BodyRevision ?? 0}.";
            if (result.Diagnostics.Count > 0)
            {
                message += " " + string.Join(" ", result.Diagnostics);
            }

            return result.Succeeded
                ? RecordSuccess(operationName, message)
                : RecordFailure(operationName, message, result.Code.ToString());
        }

        private PrototypeTestLabOperation ChangeTrait(TraitDefinition trait, string operationName, Func<CharacterTraitCollection, TraitOperationResult> action)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure(operationName, "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure(operationName, "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = action(traits);
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private RequirementEvaluationContext BuildRequirementContext(bool testLab)
        {
            EnsureTraits(out CharacterTraitCollection traits);
            EnsureResources(out CharacterResourceCollection resources);
            EnsureSkills(out CharacterSkillCollection skills);
            return new RequirementEvaluationContext
            {
                Attributes = context?.PlayerAttributes,
                CalculatedStats = context?.PlayerCalculatedStats,
                Resources = resources,
                Skills = skills,
                Traits = traits,
                Identity = context?.IdentityProgression,
                Inventory = context?.Inventory,
                Equipment = context?.Equipment,
                Statuses = context?.PlayerStatuses,
                TestLabDiagnostics = testLab
            };
        }

        private bool TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure)
        {
            role = null;
            if (!EnsureIdentityProgression(out progression))
            {
                failure = RecordFailure("Role Operation", "Player identity/progression component is missing.", "MissingIdentityProgression");
                return false;
            }

            role = progression.Roles.FirstOrDefault(record => record.lifecycleState == RoleLifecycleState.Active);
            if (role != null)
            {
                failure = default;
                return true;
            }

            failure = RecordFailure("Role Operation", "No active role exists.", "MissingActiveRole");
            return false;
        }

        private PrototypeTestLabOperation AddSocialStatus(SocialStatusDefinition status, SocialStatusContextKind contextKind, string contextTargetId, string operationName)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure(operationName, "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AddSocialStatus(status, contextKind, contextTargetId, "test-lab", "manual-test-lab");
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private PrototypeTestLabOperation RecordActivity(ActivityOutcome outcome, float difficulty, string operationName)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure(operationName, "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.RecordActivityOutcome(
                $"activity.test-lab.{Guid.NewGuid():N}",
                ActivityType.DevelopmentTest,
                outcome,
                Mathf.Clamp01(difficulty),
                "test-lab",
                "PrototypeTestLab");
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private bool RequireConfirmation(string key, bool confirmed, out PrototypeTestLabOperation result)
        {
            result = default;
            if (confirmed || pendingConfirmations.Remove(key))
            {
                return true;
            }

            pendingConfirmations.Add(key);
            result = RecordFailure("Confirmation Required", $"Press the same destructive action again to confirm '{key}'.", "ConfirmationRequired");
            return false;
        }

        private PrototypeTestLabOperation RecordSuccess(string operationName, string message)
        {
            return Record(true, operationName, "Success", message);
        }

        private PrototypeTestLabOperation RecordFailure(string operationName, string message, string code)
        {
            return Record(false, operationName, code, message);
        }

        private PrototypeTestLabOperation Record(bool succeeded, string operationName, string code, string message)
        {
            PrototypeTestLabOperation operation = new PrototypeTestLabOperation(DateTime.Now, operationName, succeeded, code, message);
            history.Insert(0, operation);
            while (history.Count > historyLimit)
            {
                history.RemoveAt(history.Count - 1);
            }

            bool automationRecord = operationName.IndexOf("Automation", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!succeeded && !suppressExpectedAutomationWarnings && !automationBatchRunning && !automationRecord && !string.Equals(code, "ConfirmationRequired", StringComparison.Ordinal))
            {
                Debug.LogWarning($"{operationName}: {message}");
            }

            HistoryChanged?.Invoke();
            return operation;
        }

        private static void LogAutomationScenarioFailures(TestLabAutomationResult result)
        {
            if (result == null || !result.HasFailures)
            {
                return;
            }

            foreach (TestLabScenarioResult scenario in result.Scenarios)
            {
                if (scenario.Status != TestLabAutomationStatus.Failed && scenario.Status != TestLabAutomationStatus.Error)
                {
                    continue;
                }

                TestLabAutomationStepResult failedStep = scenario.Steps.FirstOrDefault(step => step.Status == TestLabAutomationStatus.Failed || step.Status == TestLabAutomationStatus.Error);
                string message = failedStep == null
                    ? $"Automation failed: {scenario.SuiteId}/{scenario.ScenarioId} - {scenario.DisplayName}. Status={scenario.Status}."
                    : $"Automation failed: {scenario.SuiteId}/{scenario.ScenarioId} - {scenario.DisplayName}. Step={failedStep.StepId}. Expected='{failedStep.Expected}' Actual='{failedStep.Actual}'. Assertion={failedStep.AssertionType}. Tx='{failedStep.TransactionId}'. Diagnostics: {failedStep.Diagnostics}";
                Debug.LogWarning(message);
            }
        }

        private DefinitionRegistry CreateRegistry(DefinitionCatalog catalog)
        {
            if (catalog != null)
            {
                return catalog.CreateRegistry();
            }

#if UNITY_EDITOR
            DefinitionCatalog loaded = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(PrototypeCatalogPath);
            return loaded == null ? null : loaded.CreateRegistry();
#else
            return null;
#endif
        }

        private string FormatHealth()
        {
            return context?.PlayerHealth == null
                ? "Missing"
                : $"{context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth} Defeated={context.PlayerHealth.IsDefeated}";
        }

        private static string FormatResource(float current, float maximum)
        {
            return $"{current:0.#}/{maximum:0.#}";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        public static string FormatDefinition(IGameDefinition definition)
        {
            return definition == null ? "None" : $"{definition.DisplayName} ({definition.Id})";
        }

        private string FormatStatuses(StatusEffectController controller)
        {
            if (controller == null || controller.ActiveStatuses.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", controller.ActiveStatuses.Select(status => $"{status.Definition.DisplayName} x{status.StackCount} [{status.ApplicationId}]"));
        }

        private string FormatInventory()
        {
            if (context?.Inventory == null)
            {
                return "Missing";
            }

            return $"{context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity} slots";
        }

        private int CountEquipped()
        {
            if (context?.Equipment == null)
            {
                return 0;
            }

            int count = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private string FormatEnemy()
        {
            return context?.EnemyHealth == null
                ? "Missing"
                : $"{context.EnemyHealth.CurrentHealth:0.#}/{context.EnemyHealth.MaximumHealth:0.#} Defeated={context.EnemyHealth.IsDefeated}";
        }

        private string FormatIdentityOneLine()
        {
            if (context?.IdentityProgression == null)
            {
                return "Missing";
            }

            RuntimeOriginAssignmentRecord origin = context.IdentityProgression.Origin;
            RuntimeBirthGiftRecord gift = context.IdentityProgression.BirthGift;
            OverallLevelBreakdown level = context.IdentityProgression.CalculateOverallLevel();
            string originId = origin != null && origin.assigned ? origin.originId : "Unassigned";
            string giftId = string.IsNullOrWhiteSpace(gift?.giftDefinitionId) ? "None" : $"{gift.giftDefinitionId}:{gift.state}";
            return $"{originId} | Gift={giftId} | Level={level.OverallLevel}";
        }

        private string FormatCharacterReadinessOneLine()
        {
            return context?.CharacterSystem == null
                ? "Missing"
                : $"{context.CharacterSystem.Readiness} rev {context.CharacterSystem.Revision}";
        }

        private string FormatLocationOneLine()
        {
            if (context?.Persistence == null)
            {
                return "Missing";
            }

            return context.Persistence.BuildPlayerLocationDiagnosticSummary().Replace(Environment.NewLine, " | ");
        }

        private void AddDuplicateInstanceDiagnostics(List<string> lines)
        {
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            if (context?.Inventory != null)
            {
                foreach (InventorySlot slot in context.Inventory.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            if (context?.Equipment != null)
            {
                foreach (EquipmentSlotState slot in context.Equipment.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            lines.Add(duplicates.Count == 0 ? "Duplicate item instance IDs: none" : $"Duplicate item instance IDs: {string.Join(", ", duplicates)}");
        }

        private static void AddDuplicateStatusDiagnostics(List<string> lines, string label, StatusEffectController controller)
        {
            if (controller == null)
            {
                lines.Add($"{label} statuses: missing controller");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            foreach (RuntimeStatusEffect status in controller.ActiveStatuses)
            {
                if (!ids.Add(status.ApplicationId))
                {
                    duplicates.Add(status.ApplicationId);
                }
            }

            lines.Add(duplicates.Count == 0 ? $"{label} duplicate status IDs: none" : $"{label} duplicate status IDs: {string.Join(", ", duplicates)}");
        }

        private void AddCharacterSystemDiagnostics(List<string> lines)
        {
            if (!EnsureCharacterSystem(out CharacterSystemCoordinator character))
            {
                lines.Add("Character System: missing coordinator");
                return;
            }

            CharacterIntegrityReport report = character.ValidateIntegrity();
            lines.Add($"Character System: {character.Readiness}, revision {character.Revision}, integrity {(report.Passed ? "passed" : "failed")}");
            lines.Add($"Duplicate CharacterSystemCoordinator components: {(character.GetComponents<CharacterSystemCoordinator>().Length > 1 ? "found" : "none")}");
        }

        private static void AddReferenceDiagnostic(List<string> lines, string label, UnityEngine.Object value)
        {
            lines.Add($"{label}: {(value == null ? "Missing" : "OK")}");
        }
    }
}
#endif
