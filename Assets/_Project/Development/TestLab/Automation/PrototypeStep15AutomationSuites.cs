#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Dialogue;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Narrative;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(15, "Quests", 1500)]
    public static class PrototypeStep15AutomationSuites
    {
        private static readonly string[] RequiredQuestDefinitionIds = PrototypeQuestDefinitionFactory.PrototypeDefinitionIds
            .Concat(PrototypeQuestSourceDefinitionFactory.PrototypeDefinitionIds)
            .Concat(PrototypeConversationDefinitionFactory.PrototypeDefinitionIds)
            .Concat(PrototypeDialogueGraphDefinitionFactory.PrototypeDefinitionIds)
            .Concat(PrototypeNarrativeEventDefinitionFactory.PrototypeDefinitionIds)
            .ToArray();

        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.1.quest-identity-definitions-runtime-records",
                "Quest Identity, Definitions, and Runtime Records",
                "15.1",
                "Definition-authored quest identity and runtime-owned quest records with issuers, recipients, origins, subject links, visibility-safe queries, persistence, and automation coverage.",
                15010,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "QuestRuntime", "QuestDefinition", "QuestRuntimePersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("90.1-readiness-and-definitions", "Quest definitions register and validate", 10, Step("step15-quest-readiness", "Resolve prototype quest definitions", ReadinessAndDefinitions)),
                    Scenario("90.2-unique-and-repeatable", "Unique quests and reusable dynamic quests have distinct identities", 20, Step("step15-quest-identity", "Create unique and dynamic quest records", UniqueAndRepeatable)),
                    Scenario("90.3-issuer-recipient-origin-subjects", "Issuer, recipient, origin, and subject links stay reference-only", 30, Step("step15-quest-references", "Create linked quest references", ReferenceBoundaries)),
                    Scenario("90.4-visibility-safe-query", "Hidden quests do not leak through ordinary queries", 40, Step("step15-quest-visibility", "Query hidden and public quests", VisibilitySafeQuery)),
                    Scenario("90.5-lifecycle-revision-idempotence", "Lifecycle, revisions, and duplicate transactions are deterministic", 50, Step("step15-quest-lifecycle", "Transition lifecycle with revision checks", LifecycleRevisionIdempotence)),
                    Scenario("90.6-persistence-world-isolation", "Quest save/restore validates world scope without replaying events", 60, Step("step15-quest-persistence", "Save, restore, and reject corrupt quest payload", PersistenceWorldIsolation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.2.quest-availability-eligibility-offering-acceptance-assignment",
                "Quest Availability, Eligibility, Offering, Acceptance, Assignment, and Abandonment",
                "15.2",
                "Participant-owned quest availability, eligibility, offer, acceptance, assignment, visibility, persistence, and abandonment boundaries.",
                15020,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "QuestRuntime", "QuestParticipationRuntime", "QuestParticipationRuntimePersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("91.1-participation-policy-readiness", "Participation policies register and validate", 10, Step("step15-participation-readiness", "Resolve quest participation policies", ParticipationPolicyReadiness)),
                    Scenario("91.2-availability-eligibility", "Availability and eligibility remain distinct", 20, Step("step15-participation-eligibility", "Evaluate availability and eligibility", AvailabilityEligibility)),
                    Scenario("91.3-offer-and-acceptance", "Offer preview, creation, consent, and acceptance are atomic", 30, Step("step15-participation-offer", "Create and accept an offer", OfferAndAcceptance)),
                    Scenario("91.4-exclusive-capacity", "Exclusive assignment prevents stale acceptance", 40, Step("step15-participation-exclusive", "Revalidate capacity on acceptance", ExclusiveCapacity)),
                    Scenario("91.5-abandonment-release", "Abandonment releases capacity when configured", 50, Step("step15-participation-abandonment", "Abandon an active assignment", AbandonmentRelease)),
                    Scenario("91.6-visibility-and-persistence", "Hidden participation and persistence preserve boundaries", 60, Step("step15-participation-persistence", "Query and restore participation state", VisibilityAndPersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.3.quest-objectives-conditions-progress-tracking",
                "Quest Objectives, Conditions, and Progress Tracking",
                "15.3",
                "Assignment-owned objective progress with stable objective identities, state reconciliation, event-driven progress, visibility, idempotence, and persistence boundaries.",
                15030,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "QuestRuntime", "QuestParticipationRuntime", "QuestObjectiveProgressRuntime", "QuestObjectiveProgressPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("92.1-objective-readiness", "Objective definitions register and validate", 10, Step("step15-objective-readiness", "Resolve objective definitions", ObjectiveReadiness)),
                    Scenario("92.2-assignment-instantiation", "Accepted assignments instantiate objective records", 20, Step("step15-objective-instantiate", "Create assignment objectives", ObjectiveInstantiation)),
                    Scenario("92.3-event-sequence-idempotence", "Event progress unlocks sequence and deduplicates source events", 30, Step("step15-objective-events", "Apply committed objective signals", ObjectiveEventSequence)),
                    Scenario("92.4-current-state-reconciliation", "Current quantity objectives reconcile without fake events", 40, Step("step15-objective-state", "Reconcile current item state", ObjectiveCurrentState)),
                    Scenario("92.5-hidden-progress-visibility", "Hidden objectives progress without public leakage", 50, Step("step15-objective-hidden", "Apply hidden objective progress", ObjectiveHiddenVisibility)),
                    Scenario("92.6-persistence-and-rejection", "Objective progress persists and rejects invalid payloads safely", 60, Step("step15-objective-persistence", "Save, restore, and reject objective progress", ObjectivePersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.4.quest-completion-failure-deadlines-rewards-consequences",
                "Quest Completion, Failure, Deadlines, Rewards, and Consequences",
                "15.4",
                "Assignment-owned terminal outcomes with deadline evaluation, completion policy enforcement, reward entitlements, owner-runtime reward delegation, visibility boundaries, and persistence-safe restore.",
                15040,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "QuestRuntime", "QuestParticipationRuntime", "QuestObjectiveProgressRuntime", "QuestOutcomeRuntime", "QuestOutcomePersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("93.1-outcome-readiness", "Outcome policies, deadlines, rewards, and consequences register", 10, Step("step15-outcome-readiness", "Resolve terminal outcome definitions", OutcomeReadiness)),
                    Scenario("93.2-turn-in-completion", "Turn-in completion creates one terminal outcome and claimable rewards", 20, Step("step15-outcome-completion", "Complete an assignment through turn-in", OutcomeTurnInCompletion)),
                    Scenario("93.3-deadline-failure", "Deadline expiration fails exactly once", 30, Step("step15-outcome-deadline", "Evaluate deadline failure", OutcomeDeadlineFailure)),
                    Scenario("93.4-reward-claim", "Reward claims delegate to owner runtimes and remain idempotent", 40, Step("step15-outcome-reward", "Claim a reward entitlement", OutcomeRewardClaim)),
                    Scenario("93.5-persistence-and-redaction", "Outcomes and hidden rewards persist with redacted ordinary projections", 50, Step("step15-outcome-persistence", "Save, restore, and query redacted outcome state", OutcomePersistenceAndRedaction))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.5.quest-sources-boards-discovery-availability-presentation",
                "Quest Sources, Quest Boards, Discovery, and Availability Presentation",
                "15.5",
                "Runtime-owned quest sources and listings with publication authority, source filtering, discovery-safe browse and inspect projections, delegated acceptance, expiration, and persistence boundaries.",
                15050,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "QuestRuntime", "QuestParticipationRuntime", "QuestSourceRuntime", "QuestSourcePersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("94.1-source-readiness", "Quest source definitions register and validate", 10, Step("step15-source-readiness", "Resolve quest source definitions", QuestSourceReadiness)),
                    Scenario("94.2-empty-source", "Quest source can exist without listings", 20, Step("step15-source-empty", "Create an empty source with scene binding", QuestSourceEmpty)),
                    Scenario("94.3-publish-browse-discovery", "Publication and browse produce visibility-safe discovery", 30, Step("step15-source-publish-browse", "Publish and browse a source listing", QuestSourcePublishBrowseDiscovery)),
                    Scenario("94.4-acceptance-claims-listing", "Source acceptance delegates to participation and marks listing taken", 40, Step("step15-source-acceptance", "Accept a quest through its listing", QuestSourceAcceptanceClaimsListing)),
                    Scenario("94.5-expiration-persistence", "Expiration and persistence preserve source graph deterministically", 50, Step("step15-source-persistence", "Expire, save, restore, and reject corrupt source payload", QuestSourceExpirationPersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.6.dialogue-conversation-identity-foundation",
                "Dialogue and Conversation Identity Foundation",
                "15.6",
                "Runtime-owned conversation identity and records with participant roles, quest/source/location context, provider boundaries, visibility-safe projections, idempotence, and persistence.",
                15060,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ConversationRuntime", "ConversationDefinition", "ConversationPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("95.1-conversation-readiness", "Conversation definitions register and validate", 10, Step("step15-conversation-readiness", "Resolve conversation definitions", ConversationReadiness)),
                    Scenario("95.2-guild-counter-context", "Guild counter conversation records quest source, listing, location, and provider context", 20, Step("step15-conversation-context", "Start guild counter conversation", ConversationGuildCounterContext)),
                    Scenario("95.3-private-projection", "Private conversations redact hidden participant and subject details", 30, Step("step15-conversation-private", "Query private conversation projections", ConversationPrivateProjection)),
                    Scenario("95.4-provider-and-location", "Provider and co-location requirements reject invalid starts without mutation", 40, Step("step15-conversation-provider-location", "Reject missing provider and wrong location", ConversationProviderLocationValidation)),
                    Scenario("95.5-idempotence-lifecycle", "Conversation transactions and lifecycle transitions are deterministic", 50, Step("step15-conversation-lifecycle", "Deduplicate start and transition lifecycle", ConversationIdempotenceLifecycle)),
                    Scenario("95.6-persistence", "Conversation save and restore preserve records without replaying events", 60, Step("step15-conversation-persistence", "Save, restore, and reject corrupt conversation payload", ConversationPersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.7.dialogue-nodes-conditions-choices-conversation-flow",
                "Dialogue Nodes, Conditions, Choices, and Conversation Flow",
                "15.7",
                "Authored dialogue graphs and runtime-owned conversation flow with stable node and choice identities, condition-gated choices, delegated effects, deterministic transitions, and persistence.",
                15070,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ConversationRuntime", "DialogueGraphDefinition", "DialogueFlowRuntime", "DialogueFlowPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("96.1-graph-readiness", "Dialogue graphs register and validate", 10, Step("step15-dialogue-flow-readiness", "Resolve prototype dialogue graphs", DialogueFlowReadiness)),
                    Scenario("96.2-start-and-visible-choices", "Starting a graph enters the canonical node with deterministic choices", 20, Step("step15-dialogue-flow-start", "Start guild counter dialogue flow", DialogueFlowStartAndChoices)),
                    Scenario("96.3-conditions-and-hidden-choices", "Hidden and unavailable choices respect condition context", 30, Step("step15-dialogue-flow-conditions", "Evaluate visible and hidden choices", DialogueFlowConditions)),
                    Scenario("96.4-choice-history-and-idempotence", "Choice selection records history and idempotence without owner mutation", 40, Step("step15-dialogue-flow-choice", "Select a dialogue choice", DialogueFlowChoiceHistory)),
                    Scenario("96.5-required-effect-failure", "Required owner-runtime effects fail atomically when no executor is available", 50, Step("step15-dialogue-flow-effect-failure", "Reject required dialogue effect without executor", DialogueFlowRequiredEffectFailure)),
                    Scenario("96.6-persistence", "Dialogue flow save and restore preserve current node without replay", 60, Step("step15-dialogue-flow-persistence", "Save, restore, and reject corrupt flow payload", DialogueFlowPersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.15.8.narrative-world-events-triggers-conditions-actions",
                "Narrative and World Events, Triggers, Conditions, and Actions",
                "15.8",
                "Runtime-owned narrative event orchestration with stable authored triggers, condition gates, typed owner-runtime actions, cascade limits, redacted projections, and persistence-safe restore.",
                15080,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "NarrativeEventRuntime", "NarrativeEventDefinition", "NarrativeEventPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("97.1-readiness-and-validation", "Narrative event definitions register and validate", 10, Step("step15-narrative-readiness", "Resolve prototype narrative event definitions", NarrativeReadiness)),
                    Scenario("97.2-location-trigger-quest-action", "Location triggers create delegated quest actions once per scoped actor", 20, Step("step15-narrative-location", "Trigger dungeon entry narrative event", NarrativeLocationQuestAction)),
                    Scenario("97.3-cross-runtime-signals", "Dialogue and knowledge signals route through explicit narrative signals", 30, Step("step15-narrative-signals", "Route dialogue and knowledge narrative signals", NarrativeCrossRuntimeSignals)),
                    Scenario("97.4-hidden-projection-boundaries", "Hidden narrative events redact ordinary projections", 40, Step("step15-narrative-hidden", "Query hidden narrative projections", NarrativeHiddenProjectionBoundaries)),
                    Scenario("97.5-required-action-failure", "Required owner action failures stop execution without fake owner mutation", 50, Step("step15-narrative-required-action", "Reject missing required owner runtime action", NarrativeRequiredActionFailure)),
                    Scenario("97.6-cascade-and-persistence", "Cascades and persistence remain deterministic and restore-safe", 60, Step("step15-narrative-persistence", "Cascade, save, restore, and reject corrupt narrative payload", NarrativeCascadePersistence))
                }), out _);
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
                requiredRuntimeAreas: TestLabRuntimeArea.Quests | TestLabRuntimeArea.WorldLocations | TestLabRuntimeArea.KnowledgeHistory,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: RequiredQuestDefinitionIds);
        }

        private static ITestLabScenarioStep Step(string id, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(id, displayName, action);
        }

        private static TestLabAutomationStepResult ReadinessAndDefinitions(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool hasAll = PrototypeQuestDefinitionFactory.PrototypeDefinitionIds.All(id => registry.TryGet(id, out QuestDefinition _));
            bool metadata = registry.TryGet(PrototypeQuestDefinitionFactory.DynamicBountyDefinitionId, out QuestDefinition bounty)
                && bounty.AllowDynamicInstances
                && bounty.Category == QuestCategory.BountyPlaceholder
                && bounty.DefaultTagIds.Contains("dynamic");
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (QuestDefinition definition in PrototypeQuestDefinitionFactory.CreateMissingQuestDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = hasAll && metadata && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-quest-readiness", "Quest definitions register and validate", valid, $"Definitions={hasAll} Metadata={metadata} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult UniqueAndRepeatable(TestLabAutomationContext context)
        {
            QuestRuntime runtime = Runtime(context);
            QuestRuntimeOperationResult first = CreateGuildPosting(runtime, "unique-a", "quest.prototype.guild.unique");
            QuestRuntimeOperationResult duplicateUnique = CreateGuildPosting(runtime, "unique-b", "quest.prototype.guild.unique-b");
            QuestRuntimeOperationResult dynamicA = CreateDynamicBounty(runtime, "dynamic-a", "wolf");
            QuestRuntimeOperationResult dynamicB = CreateDynamicBounty(runtime, "dynamic-b", "slime");
            bool valid = first.Succeeded
                && duplicateUnique.Status == QuestRuntimeOperationStatus.UniqueQuestAlreadyExists
                && dynamicA.Succeeded
                && dynamicB.Succeeded
                && dynamicA.Snapshot.QuestId != dynamicB.Snapshot.QuestId
                && runtime.Query(new QuestQuery { access = QuestVisibilityAccess.PrivilegedDiagnostic }).Count == 3;
            return TestLabAssertions.True("step15-quest-identity", "Unique and dynamic quest identity rules are enforced", valid, $"First={first.Status} UniqueDuplicate={duplicateUnique.Status} Dynamic={dynamicA.Status}/{dynamicB.Status} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult ReferenceBoundaries(TestLabAutomationContext context)
        {
            QuestRuntime runtime = Runtime(context);
            QuestRuntimeOperationResult create = runtime.CreateQuest(new QuestCreateRequest
            {
                transactionId = "tx.quest.references",
                questId = "quest.prototype.civic-investigation.001",
                questDefinitionId = PrototypeQuestDefinitionFactory.CivicInvestigationDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Government, issuerId = "government.prototype.civic", actingPersonId = "person.prototype.mayor" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = "person.prototype.player" },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.Government, locationId = "location.prototype.civic-office", interactionPointId = "interaction-point.prototype.civic-desk" },
                subjectLinks = new[]
                {
                    Subject("person.prototype.witness", QuestSubjectRole.Person, InformationSubjectType.PersonIdentity),
                    Subject("location.prototype.basement-prison", QuestSubjectRole.Location, InformationSubjectType.Location),
                    Subject("knowledge.prototype.quest-incident", QuestSubjectRole.Incident, InformationSubjectType.KnowledgeRecord)
                },
                tagIds = new[] { "civic", "investigation" },
                createdWorldTime = 15d
            });
            QuestSnapshot snapshot = create.Snapshot;
            bool subject = snapshot != null
                && snapshot.SubjectLinks.Count == 3
                && snapshot.CreateInformationSubject().tags.Contains(QuestInformationSubject.QuestTag)
                && snapshot.Issuer.issuerId == "government.prototype.civic"
                && snapshot.IntendedRecipient.recipientId == "person.prototype.player"
                && snapshot.Origin.interactionPointId == "interaction-point.prototype.civic-desk";
            bool valid = create.Succeeded && subject && runtime.Events.Count == 1;
            return TestLabAssertions.True("step15-quest-references", "Quest issuer, recipient, origin, and subjects are explicit references", valid, $"Create={create.Status} Links={snapshot?.SubjectLinks.Count} Events={runtime.Events.Count}");
        }

        private static TestLabAutomationStepResult VisibilitySafeQuery(TestLabAutomationContext context)
        {
            QuestRuntime runtime = Runtime(context);
            QuestRuntimeOperationResult publicQuest = CreateGuildPosting(runtime, "visible", "quest.prototype.guild.visible");
            QuestRuntimeOperationResult hiddenQuest = runtime.CreateQuest(new QuestCreateRequest
            {
                transactionId = "tx.quest.hidden",
                questId = "quest.prototype.hidden.dungeon",
                questDefinitionId = PrototypeQuestDefinitionFactory.HiddenDungeonRumorDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Anonymous },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Open },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.Discovery },
                subjectLinks = new[] { Subject("location.prototype.dungeon-entry", QuestSubjectRole.Location, InformationSubjectType.Location) },
                createdWorldTime = 20d
            });
            int publicCount = runtime.Query(new QuestQuery { access = QuestVisibilityAccess.PublicOnly }).Count;
            int privilegedCount = runtime.Query(new QuestQuery { access = QuestVisibilityAccess.PrivilegedDiagnostic }).Count;
            bool valid = publicQuest.Succeeded && hiddenQuest.Succeeded && publicCount == 1 && privilegedCount == 2;
            return TestLabAssertions.True("step15-quest-visibility", "Hidden quests do not leak into ordinary query results", valid, $"Public={publicCount} Privileged={privilegedCount} HiddenStatus={hiddenQuest.Status}");
        }

        private static TestLabAutomationStepResult LifecycleRevisionIdempotence(TestLabAutomationContext context)
        {
            QuestRuntime runtime = Runtime(context);
            QuestRuntimeOperationResult create = CreateDynamicBounty(runtime, "lifecycle-create", "boar");
            long createdRevision = runtime.Revision;
            QuestRuntimeOperationResult stale = runtime.TransitionLifecycle(new QuestLifecycleTransitionRequest { transactionId = "tx.quest.lifecycle.stale", questId = create.Snapshot?.QuestId, targetState = QuestRuntimeLifecycleState.Suspended, expectedRevision = createdRevision - 1L });
            QuestRuntimeOperationResult retire = runtime.TransitionLifecycle(new QuestLifecycleTransitionRequest { transactionId = "tx.quest.lifecycle.retire", questId = create.Snapshot?.QuestId, targetState = QuestRuntimeLifecycleState.Retired, expectedRevision = createdRevision, worldTime = 30d });
            QuestRuntimeOperationResult duplicate = runtime.TransitionLifecycle(new QuestLifecycleTransitionRequest { transactionId = "tx.quest.lifecycle.retire", questId = create.Snapshot?.QuestId, targetState = QuestRuntimeLifecycleState.Retired, worldTime = 30d });
            bool hiddenFromActive = runtime.Query(new QuestQuery { access = QuestVisibilityAccess.PrivilegedDiagnostic }).Count == 0;
            bool visibleWithRetired = runtime.Query(new QuestQuery { access = QuestVisibilityAccess.PrivilegedDiagnostic, includeRetired = true }).Count == 1;
            bool valid = create.Succeeded
                && stale.Status == QuestRuntimeOperationStatus.RevisionConflict
                && retire.Succeeded
                && duplicate.Duplicate
                && hiddenFromActive
                && visibleWithRetired
                && runtime.Events.Count == 2;
            return TestLabAssertions.True("step15-quest-lifecycle", "Lifecycle transitions are revision-safe and idempotent", valid, $"Create={create.Status} Stale={stale.Status} Retire={retire.Status} Duplicate={duplicate.Status} Events={runtime.Events.Count}");
        }

        private static TestLabAutomationStepResult PersistenceWorldIsolation(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime runtime = Runtime(context);
            QuestRuntimeOperationResult create = CreateDynamicBounty(runtime, "persist-create", "goblin");
            QuestRuntimePersistenceParticipant participant = new QuestRuntimePersistenceParticipant(runtime, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            QuestRuntime restored = Runtime(context);
            QuestRuntimePersistenceParticipant restoredParticipant = new QuestRuntimePersistenceParticipant(restored, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, QuestRuntimePersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            int restoredEvents = restored.Events.Count;
            QuestRuntimeSaveData corrupt = restored.CreateSaveData();
            corrupt.worldId = "world.other";
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), QuestRuntimePersistenceParticipant.CurrentParticipantSchemaVersion);
            bool unchanged = restored.Count == 1 && restored.Events.Count == restoredEvents;
            bool valid = create.Succeeded && save.Succeeded && prepare.Succeeded && commit.Succeeded && rejected.Succeeded == false && unchanged;
            return TestLabAssertions.True("step15-quest-persistence", "Quest persistence round-trips and rejects wrong-world payloads without mutation", valid, $"Create={create.Status} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Count={restored.Count} Events={restored.Events.Count}");
        }

        private static TestLabAutomationStepResult ParticipationPolicyReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool guild = registry.TryGet(PrototypeQuestDefinitionFactory.GuildPostingDefinitionId, out QuestDefinition guildDefinition)
                && guildDefinition.AssignmentPolicy == QuestAssignmentPolicy.Exclusive
                && guildDefinition.ConsentPolicy == QuestConsentPolicy.ExplicitRecipientConsentRequired
                && guildDefinition.EligibilityRequirementGroups.Count == 1;
            bool bounty = registry.TryGet(PrototypeQuestDefinitionFactory.DynamicBountyDefinitionId, out QuestDefinition bountyDefinition)
                && bountyDefinition.AssignmentPolicy == QuestAssignmentPolicy.CapacityLimited
                && bountyDefinition.AssignmentCapacity == 4;
            return TestLabAssertions.True("step15-participation-readiness", "Quest participation policies register and validate", guild && bounty, $"Guild={guild} Bounty={bounty}");
        }

        private static TestLabAutomationStepResult AvailabilityEligibility(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestRuntimeOperationResult create = CreateGuildPosting(quests, "participation-eligibility", "quest.prototype.guild.participation-eligibility");

            QuestAvailabilityResult availability = participation.EvaluateAvailability(create.Snapshot?.QuestId, EligibleContext("person.prototype.player"));
            QuestEligibilityResult missing = participation.EvaluateEligibility(create.Snapshot?.QuestId, new QuestEligibilityContext { personId = "person.prototype.player", privilegedDiagnostics = true, worldTime = 1d });
            QuestEligibilityResult eligible = participation.EvaluateEligibility(create.Snapshot?.QuestId, EligibleContext("person.prototype.player"));

            bool valid = create.Succeeded
                && availability.Available
                && missing.Eligible == false
                && missing.VisibleFailureReasons.Count > 0
                && eligible.Eligible;
            return TestLabAssertions.True("step15-participation-eligibility", "Availability and eligibility remain separate and deterministic", valid, $"Create={create.Status} Available={availability.State} Missing={missing.Eligible} Eligible={eligible.Eligible}");
        }

        private static TestLabAutomationStepResult OfferAndAcceptance(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestRuntimeOperationResult create = CreateGuildPosting(quests, "participation-offer", "quest.prototype.guild.participation-offer");

            QuestParticipationOperationResult preview = participation.CreateOffer(OfferRequest(create.Snapshot?.QuestId, "person.prototype.player", EligibleContext("person.prototype.player"), "tx.quest.participation.preview", preview: true));
            QuestParticipationOperationResult offer = participation.CreateOffer(OfferRequest(create.Snapshot?.QuestId, "person.prototype.player", EligibleContext("person.prototype.player"), "tx.quest.participation.offer"));
            QuestParticipationOperationResult missingConsent = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.no-consent", offerId = offer.Offer?.OfferId, personId = "person.prototype.player", eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 2d });
            QuestParticipationOperationResult accept = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.accept", offerId = offer.Offer?.OfferId, personId = "person.prototype.player", explicitConsent = true, consentRecordId = "consent.prototype.player.guild", eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 2d });
            QuestParticipationOperationResult duplicate = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.accept", offerId = offer.Offer?.OfferId, personId = "person.prototype.player", explicitConsent = true, eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 2d });

            bool valid = create.Succeeded
                && preview.Status == QuestParticipationOperationStatus.Preview
                && offer.Succeeded
                && missingConsent.Status == QuestParticipationOperationStatus.ConsentRequired
                && accept.Succeeded
                && duplicate.Duplicate
                && participation.OfferCount == 1
                && participation.AssignmentCount == 1;
            return TestLabAssertions.True("step15-participation-offer", "Offer creation and acceptance are consent-gated and idempotent", valid, $"Preview={preview.Status} Offer={offer.Status} Consent={missingConsent.Status} Accept={accept.Status} Duplicate={duplicate.Status} Offers={participation.OfferCount} Assignments={participation.AssignmentCount}");
        }

        private static TestLabAutomationStepResult ExclusiveCapacity(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestRuntimeOperationResult create = CreateGuildPosting(quests, "participation-exclusive", "quest.prototype.guild.participation-exclusive");
            QuestParticipationOperationResult firstOffer = participation.CreateOffer(OfferRequest(create.Snapshot?.QuestId, "person.prototype.first", EligibleContext("person.prototype.first"), "tx.quest.participation.exclusive.first-offer"));
            QuestParticipationOperationResult secondOffer = participation.CreateOffer(OfferRequest(create.Snapshot?.QuestId, "person.prototype.second", EligibleContext("person.prototype.second"), "tx.quest.participation.exclusive.second-offer"));
            QuestParticipationOperationResult first = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.exclusive.first", offerId = firstOffer.Offer?.OfferId, personId = "person.prototype.first", explicitConsent = true, eligibilityContext = EligibleContext("person.prototype.first"), worldTime = 3d });
            QuestParticipationOperationResult second = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.exclusive.second", offerId = secondOffer.Offer?.OfferId, personId = "person.prototype.second", explicitConsent = true, eligibilityContext = EligibleContext("person.prototype.second"), worldTime = 3d });

            bool valid = firstOffer.Succeeded
                && secondOffer.Succeeded
                && first.Succeeded
                && second.Status == QuestParticipationOperationStatus.Unavailable
                && participation.AssignmentCount == 1;
            return TestLabAssertions.True("step15-participation-exclusive", "Exclusive assignment capacity is revalidated at acceptance", valid, $"First={first.Status} Second={second.Status} Assignments={participation.AssignmentCount}");
        }

        private static TestLabAutomationStepResult AbandonmentRelease(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestRuntimeOperationResult create = CreateGuildPosting(quests, "participation-abandon", "quest.prototype.guild.participation-abandon");
            QuestParticipationOperationResult offer = participation.CreateOffer(OfferRequest(create.Snapshot?.QuestId, "person.prototype.player", EligibleContext("person.prototype.player"), "tx.quest.participation.abandon.offer"));
            QuestParticipationOperationResult accept = participation.AcceptOffer(new QuestAcceptOfferRequest { transactionId = "tx.quest.participation.abandon.accept", offerId = offer.Offer?.OfferId, personId = "person.prototype.player", explicitConsent = true, eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 4d });
            QuestAvailabilityResult claimed = participation.EvaluateAvailability(create.Snapshot?.QuestId, EligibleContext("person.prototype.other"));
            QuestParticipationOperationResult abandon = participation.AbandonAssignment(new QuestAssignmentLifecycleRequest { transactionId = "tx.quest.participation.abandon", assignmentId = accept.Assignment?.AssignmentId, actingPersonId = "person.prototype.player", explicitConsent = true, worldTime = 5d });
            QuestAvailabilityResult after = participation.EvaluateAvailability(create.Snapshot?.QuestId, EligibleContext("person.prototype.other"));

            bool valid = accept.Succeeded
                && claimed.State == QuestAvailabilityState.ExclusivelyAssigned
                && abandon.Succeeded
                && after.Available;
            return TestLabAssertions.True("step15-participation-abandonment", "Abandonment releases configured exclusive capacity", valid, $"Accept={accept.Status} Claimed={claimed.State} Abandon={abandon.Status} After={after.State}");
        }

        private static TestLabAutomationStepResult VisibilityAndPersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestRuntimeOperationResult create = quests.CreateQuest(new QuestCreateRequest
            {
                transactionId = "tx.quest.participation.hidden.create",
                questId = "quest.prototype.hidden.participation",
                questDefinitionId = PrototypeQuestDefinitionFactory.HiddenDungeonRumorDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Anonymous },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Open },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.Discovery },
                subjectLinks = new[] { Subject("location.prototype.secret-dungeon-entry", QuestSubjectRole.Location, InformationSubjectType.Location) },
                createdWorldTime = 5d
            });
            QuestParticipationOperationResult offer = participation.CreateOffer(new QuestOfferRequest
            {
                transactionId = "tx.quest.participation.hidden.offer",
                questId = create.Snapshot?.QuestId,
                recipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = "person.prototype.scout" },
                offeringProvider = new QuestIssuerReferenceData { issuerType = QuestIssuerType.System, issuerId = "system.quest" },
                eligibilityContext = new QuestEligibilityContext { personId = "person.prototype.scout", privilegedDiagnostics = true, worldTime = 5d },
                channel = QuestOfferChannel.NarrativeEventPlaceholder,
                worldTime = 5d
            });
            int publicOffers = participation.QueryOffers(new QuestOfferQuery { access = QuestVisibilityAccess.PublicOnly }).Count;
            int privilegedOffers = participation.QueryOffers(new QuestOfferQuery { access = QuestVisibilityAccess.PrivilegedDiagnostic }).Count;
            QuestParticipationRuntimePersistenceParticipant participant = new QuestParticipationRuntimePersistenceParticipant(participation, () => quests, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            QuestParticipationRuntime restored = Participation(quests, registry);
            QuestParticipationRuntimePersistenceParticipant restoredParticipant = new QuestParticipationRuntimePersistenceParticipant(restored, () => quests, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, QuestParticipationRuntimePersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            QuestParticipationRuntimeSaveData corrupt = restored.CreateSaveData();
            corrupt.worldId = "world.other";
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), QuestParticipationRuntimePersistenceParticipant.CurrentParticipantSchemaVersion);

            bool valid = create.Succeeded
                && offer.Succeeded
                && publicOffers == 0
                && privilegedOffers == 1
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && rejected.Succeeded == false
                && restored.OfferCount == 1;
            return TestLabAssertions.True("step15-participation-persistence", "Hidden offers do not leak and persistence validates before restore", valid, $"Offer={offer.Status} Public={publicOffers} Privileged={privilegedOffers} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Restored={restored.OfferCount}");
        }

        private static TestLabAutomationStepResult ObjectiveReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool guild = registry.TryGet(PrototypeQuestDefinitionFactory.GuildPostingDefinitionId, out QuestDefinition guildDefinition)
                && guildDefinition.ObjectiveDefinitions.Count == 4
                && guildDefinition.ObjectiveDefinitions.Any(objective => objective.progressModel == QuestObjectiveProgressModel.Counter);
            bool delivery = registry.TryGet(PrototypeQuestDefinitionFactory.MerchantDeliveryDefinitionId, out QuestDefinition deliveryDefinition)
                && deliveryDefinition.ObjectiveDefinitions.Any(objective => objective.progressModel == QuestObjectiveProgressModel.QuantityCurrent)
                && deliveryDefinition.ObjectiveGroups.Count == 1;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (QuestDefinition definition in PrototypeQuestDefinitionFactory.CreateMissingQuestDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = guild && delivery && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-objective-readiness", "Quest objective definitions register and validate", valid, $"Guild={guild} Delivery={delivery} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult ObjectiveInstantiation(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "objective-instantiate");

            QuestObjectiveOperationResult preview = objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.preview", preview: true);
            QuestObjectiveOperationResult instantiate = objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.instantiate");
            QuestObjectiveOperationResult duplicate = objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.instantiate-again");
            QuestAssignmentObjectiveSummary summary = objectives.SummarizeAssignment(assignment.AssignmentId, QuestVisibilityAccess.PrivilegedDiagnostic);

            bool valid = preview.Status == QuestObjectiveOperationStatus.Preview
                && instantiate.Succeeded
                && duplicate.Duplicate
                && instantiate.Objectives.Count == 4
                && summary.CompletionCandidate == false
                && objectives.ObjectiveCount == 4;
            return TestLabAssertions.True("step15-objective-instantiate", "Accepted assignments instantiate stable objective runtime records without completing quests", valid, $"Preview={preview.Status} Instantiate={instantiate.Status} Duplicate={duplicate.Status} Count={objectives.ObjectiveCount} Candidate={summary.CompletionCandidate}");
        }

        private static TestLabAutomationStepResult ObjectiveEventSequence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "objective-events");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.events.instantiate");

            QuestObjectiveOperationResult early = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", "source.quest.objective.early"));
            QuestObjectiveOperationResult counter = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.UseInteractionPoint, "interaction-point.prototype.guild-counter", "source.quest.objective.counter"));
            QuestObjectiveOperationResult duplicate = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.UseInteractionPoint, "interaction-point.prototype.guild-counter", "source.quest.objective.counter"));
            QuestObjectiveOperationResult dungeon = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.VisitLocation, "location.prototype.dungeon-entry", "source.quest.objective.dungeon", InformationSubjectType.Location));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", "source.quest.objective.defeat1"));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", "source.quest.objective.defeat2"));
            QuestObjectiveOperationResult defeat3 = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", "source.quest.objective.defeat3"));
            QuestObjectiveSnapshot defeat = objectives.QueryObjectives(new QuestObjectiveQuery { assignmentId = assignment.AssignmentId, category = QuestObjectiveCategory.DefeatCount, access = QuestVisibilityAccess.PrivilegedDiagnostic }).Single();

            bool valid = early.Succeeded == false
                && counter.Succeeded
                && duplicate.Status == QuestObjectiveOperationStatus.AlreadyCounted
                && dungeon.Succeeded
                && defeat3.Succeeded
                && defeat.CurrentValue == 3
                && defeat.Satisfied;
            return TestLabAssertions.True("step15-objective-events", "Committed objective signals unlock prerequisites and deduplicate source events", valid, $"Early={early.Status} Counter={counter.Status} Duplicate={duplicate.Status} Dungeon={dungeon.Status} Defeat={defeat.CurrentValue}/{defeat.TargetValue} Satisfied={defeat.Satisfied}");
        }

        private static TestLabAutomationStepResult ObjectiveCurrentState(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestAssignmentSnapshot assignment = AcceptedDeliveryAssignment(quests, participation, "objective-state");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.state.instantiate");

            QuestObjectiveOperationResult state = objectives.ReconcileState(new QuestObjectiveStateContext
            {
                assignmentId = assignment.AssignmentId,
                personId = assignment.AssigneePersonId,
                worldTime = 3d,
                facts = new QuestObjectiveStateFactSet(new[] { ObjectiveFact(QuestObjectiveCategory.PossessItem, "item.prototype.merchant-parcel", 1) })
            });
            QuestObjectiveOperationResult collect = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.ObtainItem, "item.prototype.merchant-parcel", "source.quest.objective.collect"));
            QuestObjectiveSnapshot possess = objectives.QueryObjectives(new QuestObjectiveQuery { assignmentId = assignment.AssignmentId, objectiveDefinitionId = "quest-objective-definition.prototype.delivery.possess-parcel", access = QuestVisibilityAccess.PrivilegedDiagnostic }).Single();
            QuestObjectiveSnapshot collected = objectives.QueryObjectives(new QuestObjectiveQuery { assignmentId = assignment.AssignmentId, objectiveDefinitionId = "quest-objective-definition.prototype.delivery.collect-parcel", access = QuestVisibilityAccess.PrivilegedDiagnostic }).Single();

            bool valid = state.Succeeded
                && collect.Succeeded
                && possess.Satisfied
                && collected.Satisfied
                && collected.CountedSourceEventIds.Contains("source.quest.objective.collect");
            return TestLabAssertions.True("step15-objective-state", "Current-state and cumulative event objectives remain distinct", valid, $"State={state.Status} Collect={collect.Status} Possess={possess.CurrentValue}/{possess.TargetValue} Collected={collected.Satisfied}");
        }

        private static TestLabAutomationStepResult ObjectiveHiddenVisibility(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestAssignmentSnapshot assignment = AcceptedHiddenAssignment(quests, participation, "objective-hidden");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.hidden.instantiate");

            QuestObjectiveOperationResult discover = objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DiscoverLocation, "location.prototype.secret-dungeon-entry", "source.quest.objective.hidden", InformationSubjectType.Location));
            int publicCount = objectives.QueryObjectives(new QuestObjectiveQuery { assignmentId = assignment.AssignmentId, access = QuestVisibilityAccess.PublicOnly }).Count;
            int privilegedCount = objectives.QueryObjectives(new QuestObjectiveQuery { assignmentId = assignment.AssignmentId, access = QuestVisibilityAccess.PrivilegedDiagnostic }).Count;
            QuestAssignmentObjectiveSummary summary = objectives.SummarizeAssignment(assignment.AssignmentId, QuestVisibilityAccess.PublicOnly);

            bool valid = discover.Succeeded
                && publicCount == 0
                && privilegedCount == 2
                && summary.HiddenCountsRedacted
                && summary.RequiredRemaining == -1;
            return TestLabAssertions.True("step15-objective-hidden", "Hidden objective progress does not leak through ordinary views", valid, $"Discover={discover.Status} Public={publicCount} Privileged={privilegedCount} Redacted={summary.HiddenCountsRedacted}");
        }

        private static TestLabAutomationStepResult ObjectivePersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "objective-persist");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.objective.persist.instantiate");
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.UseInteractionPoint, "interaction-point.prototype.guild-counter", "source.quest.objective.persist"));
            QuestObjectiveProgressPersistenceParticipant participant = new QuestObjectiveProgressPersistenceParticipant(objectives, () => quests, () => participation, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            QuestObjectiveProgressRuntime restored = Objectives(quests, participation, registry);
            QuestObjectiveProgressPersistenceParticipant restoredParticipant = new QuestObjectiveProgressPersistenceParticipant(restored, () => quests, () => participation, () => registry, PersistenceService.LocalWorldId);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, QuestObjectiveProgressPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            QuestObjectiveProgressRuntimeSaveData corrupt = restored.CreateSaveData();
            corrupt.objectives[0].assignmentId = "quest-assignment.missing";
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), QuestObjectiveProgressPersistenceParticipant.CurrentParticipantSchemaVersion);

            bool valid = save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && rejected.Succeeded == false
                && restored.ObjectiveCount == 4;
            return TestLabAssertions.True("step15-objective-persistence", "Quest objective progress persists and rejects invalid payloads before mutation", valid, $"Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Count={restored.ObjectiveCount}");
        }

        private static TestLabAutomationStepResult OutcomeReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool guild = registry.TryGet(PrototypeQuestDefinitionFactory.GuildPostingDefinitionId, out QuestDefinition guildDefinition)
                && guildDefinition.CompletionPolicy.policy == QuestCompletionPolicy.RequireTurnIn
                && guildDefinition.DeadlineDefinitions.Count == 1
                && guildDefinition.RewardPackages.SelectMany(package => package.rewards).Count() == 2;
            bool hidden = registry.TryGet(PrototypeQuestDefinitionFactory.HiddenDungeonRumorDefinitionId, out QuestDefinition hiddenDefinition)
                && hiddenDefinition.CompletionPolicy.policy == QuestCompletionPolicy.AutoCompleteWhenRequiredObjectivesSatisfied
                && hiddenDefinition.RewardPackages.Any(package => package.deliveryPolicy == QuestRewardDeliveryPolicy.GrantOnCompletion);
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (QuestDefinition definition in PrototypeQuestDefinitionFactory.CreateMissingQuestDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = guild && hidden && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-outcome-readiness", "Quest outcome definitions register and validate", valid, $"Guild={guild} Hidden={hidden} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult OutcomeTurnInCompletion(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestOutcomeRuntime outcomes = Outcomes(quests, participation, objectives, registry, new AutomationRewardExecutor());
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "outcome-complete");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.outcome.complete.objectives");
            outcomes.TrackAssignment(assignment, "tx.quest.outcome.complete.track");
            CompleteGuildObjectives(objectives, assignment, "outcome.complete");

            QuestCompletionEvaluationResult wrongCounter = outcomes.EvaluateCompletion(new QuestCompletionEvaluationRequest { assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, interactionPointId = "interaction-point.prototype.other", worldTime = 4d });
            QuestOutcomeOperationResult complete = outcomes.Complete(new QuestCompletionRequest { transactionId = "tx.quest.outcome.complete", assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, interactionPointId = "interaction-point.prototype.guild-counter", locationId = "location.prototype.adventurers-guild", issuerId = "organization.prototype.guild", worldTime = assignment.AssignedWorldTime + 2d });
            QuestOutcomeOperationResult duplicate = outcomes.Complete(new QuestCompletionRequest { transactionId = "tx.quest.outcome.complete", assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, interactionPointId = "interaction-point.prototype.guild-counter", worldTime = assignment.AssignedWorldTime + 2.1d });

            bool valid = wrongCounter.Status == QuestOutcomeOperationStatus.TurnInRequired
                && complete.Succeeded
                && complete.Outcome?.OutcomeKind == QuestTerminalOutcomeKind.Completed
                && complete.Rewards.Count == 2
                && complete.Rewards.All(reward => reward.State == QuestRewardEntitlementState.Claimable)
                && duplicate.Status == QuestOutcomeOperationStatus.Duplicate
                && outcomes.TerminalOutcomeCount == 1;
            return TestLabAssertions.True("step15-outcome-completion", "Turn-in completion records one terminal outcome and claimable rewards", valid, $"Wrong={wrongCounter.Status} Complete={complete.Status} Duplicate={duplicate.Status} Outcomes={outcomes.TerminalOutcomeCount} Rewards={complete.Rewards.Count}");
        }

        private static TestLabAutomationStepResult OutcomeDeadlineFailure(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            QuestOutcomeRuntime outcomes = Outcomes(quests, participation, objectives, registry, new AutomationRewardExecutor());
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "outcome-deadline");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.outcome.deadline.objectives");
            QuestOutcomeOperationResult track = outcomes.TrackAssignment(assignment, "tx.quest.outcome.deadline.track");
            QuestOutcomeOperationResult expired = outcomes.EvaluateDeadlines(assignment.AssignedWorldTime + 3d, "tx.quest.outcome.deadline");
            QuestOutcomeOperationResult duplicate = outcomes.EvaluateDeadlines(assignment.AssignedWorldTime + 3d, "tx.quest.outcome.deadline");
            QuestCompletionEvaluationResult completeAfter = outcomes.EvaluateCompletion(new QuestCompletionEvaluationRequest { assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, interactionPointId = "interaction-point.prototype.guild-counter", worldTime = assignment.AssignedWorldTime + 3.1d });

            bool valid = track.Succeeded
                && expired.Succeeded
                && expired.Outcome?.OutcomeKind == QuestTerminalOutcomeKind.Expired
                && duplicate.Status == QuestOutcomeOperationStatus.Duplicate
                && completeAfter.Status == QuestOutcomeOperationStatus.AlreadyTerminal
                && outcomes.QueryDeadlines(new QuestOutcomeQuery { assignmentId = assignment.AssignmentId, access = QuestVisibilityAccess.PrivilegedDiagnostic }).Single().Expired;
            return TestLabAssertions.True("step15-outcome-deadline", "Deadline expiration fails the assignment exactly once", valid, $"Track={track.Status} Expired={expired.Status} Duplicate={duplicate.Status} CompleteAfter={completeAfter.Status}");
        }

        private static TestLabAutomationStepResult OutcomeRewardClaim(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            AutomationRewardExecutor executor = new AutomationRewardExecutor();
            QuestOutcomeRuntime outcomes = Outcomes(quests, participation, objectives, registry, executor);
            QuestAssignmentSnapshot assignment = AcceptedGuildAssignment(quests, participation, "outcome-reward");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.outcome.reward.objectives");
            CompleteGuildObjectives(objectives, assignment, "outcome.reward");
            QuestOutcomeOperationResult complete = outcomes.Complete(new QuestCompletionRequest { transactionId = "tx.quest.outcome.reward.complete", assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, interactionPointId = "interaction-point.prototype.guild-counter", worldTime = 4d });
            QuestRewardEntitlementSnapshot reward = complete.Rewards.FirstOrDefault(value => value.Category == QuestRewardCategory.Currency);
            QuestOutcomeOperationResult claim = outcomes.ClaimReward(new QuestRewardClaimRequest { transactionId = "tx.quest.outcome.reward.claim", entitlementId = reward?.EntitlementId, claimantPersonId = assignment.AssigneePersonId, worldTime = 5d });
            QuestOutcomeOperationResult duplicate = outcomes.ClaimReward(new QuestRewardClaimRequest { transactionId = "tx.quest.outcome.reward.claim", entitlementId = reward?.EntitlementId, claimantPersonId = assignment.AssigneePersonId, worldTime = 6d });

            bool valid = complete.Succeeded
                && reward != null
                && claim.Succeeded
                && claim.Reward?.State == QuestRewardEntitlementState.Granted
                && duplicate.Status == QuestOutcomeOperationStatus.Duplicate
                && executor.Requests.Count == 1
                && executor.Requests[0].category == QuestRewardCategory.Currency;
            return TestLabAssertions.True("step15-outcome-reward", "Reward claims delegate once to owner runtimes", valid, $"Complete={complete.Status} Claim={claim.Status} Duplicate={duplicate.Status} Calls={executor.Requests.Count}");
        }

        private static TestLabAutomationStepResult OutcomePersistenceAndRedaction(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestObjectiveProgressRuntime objectives = Objectives(quests, participation, registry);
            AutomationRewardExecutor executor = new AutomationRewardExecutor();
            QuestOutcomeRuntime outcomes = Outcomes(quests, participation, objectives, registry, executor);
            QuestAssignmentSnapshot assignment = AcceptedHiddenAssignment(quests, participation, "outcome-hidden");
            objectives.InstantiateForAssignment(assignment, transactionId: "tx.quest.outcome.hidden.objectives");
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DiscoverLocation, "location.prototype.secret-dungeon-entry", "source.quest.outcome.hidden.discover", InformationSubjectType.Location));
            QuestOutcomeOperationResult complete = outcomes.Complete(new QuestCompletionRequest { transactionId = "tx.quest.outcome.hidden.complete", assignmentId = assignment.AssignmentId, requesterPersonId = assignment.AssigneePersonId, worldTime = 4d });
            QuestRewardEntitlementSnapshot publicReward = outcomes.QueryRewards(new QuestRewardQuery { assignmentId = assignment.AssignmentId, access = QuestVisibilityAccess.PublicOnly, includeHidden = true, includeTerminal = true }).SingleOrDefault();
            QuestRewardEntitlementSnapshot privilegedReward = outcomes.QueryRewards(new QuestRewardQuery { assignmentId = assignment.AssignmentId, access = QuestVisibilityAccess.PrivilegedDiagnostic, includeTerminal = true }).SingleOrDefault();
            QuestOutcomePersistenceParticipant participant = new QuestOutcomePersistenceParticipant(outcomes, () => quests, () => participation, () => objectives, () => registry, () => executor);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            QuestOutcomeRuntime restored = Outcomes(quests, participation, objectives, registry, executor);
            QuestOutcomePersistenceParticipant restoredParticipant = new QuestOutcomePersistenceParticipant(restored, () => quests, () => participation, () => objectives, () => registry, () => executor);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, QuestOutcomePersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            QuestOutcomeRuntimeSaveData corrupt = restored.CreateSaveData();
            if (corrupt.terminalOutcomes.Count > 0)
            {
                corrupt.terminalOutcomes[0].assignmentId = "quest-assignment.prototype.missing";
            }
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), QuestOutcomePersistenceParticipant.CurrentParticipantSchemaVersion);

            bool valid = complete.Succeeded
                && publicReward != null
                && publicReward.Redacted
                && string.IsNullOrEmpty(publicReward.TargetDefinitionId)
                && privilegedReward != null
                && privilegedReward.Redacted == false
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && rejected.Succeeded == false
                && restored.TerminalOutcomeCount == 1
                && restored.RewardEntitlementCount == 1;
            return TestLabAssertions.True("step15-outcome-persistence", "Hidden outcome rewards redact and persistence rejects invalid graphs before mutation", valid, $"Complete={complete.Status} PublicRedacted={publicReward?.Redacted} PrivilegedRedacted={privilegedReward?.Redacted} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Restored={restored.TerminalOutcomeCount}/{restored.RewardEntitlementCount}");
        }

        private static TestLabAutomationStepResult QuestSourceReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool hasAll = PrototypeQuestSourceDefinitionFactory.PrototypeDefinitionIds.All(id => registry.TryGet(id, out QuestSourceDefinition _));
            bool metadata = registry.TryGet(PrototypeQuestSourceDefinitionFactory.AdventurerGuildCounterDefinitionId, out QuestSourceDefinition counter)
                && counter.Category == QuestSourceCategory.GuildCounter
                && counter.PublicationPolicy.maxActiveListings == 6
                && counter.SourceRoleIds.Contains("quest-source-role.acceptance");
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (QuestSourceDefinition definition in PrototypeQuestSourceDefinitionFactory.CreateMissingQuestSourceDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = hasAll && metadata && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-source-readiness", "Quest source definitions register and validate", valid, $"Definitions={hasAll} Metadata={metadata} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult QuestSourceEmpty(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            QuestSourceOperationResult create = sources.CreateSource(new QuestSourceCreateRequest
            {
                transactionId = "tx.quest-source.empty.create",
                questSourceId = "quest-source.prototype.empty-archive.automation",
                questSourceDefinitionId = PrototypeQuestSourceDefinitionFactory.EmptyArchiveDefinitionId,
                hostLocationId = "location.prototype.guild-archive",
                interactionPointId = "interaction-point.prototype.archive",
                sceneBindingKey = "scene.prototype.guild.archive",
                visibility = QuestSourceVisibility.Restricted,
                worldTime = 1d
            });
            QuestSourceBrowseResult publicBrowse = sources.BrowseSource(new QuestSourceBrowseRequest { questSourceId = create.Source?.QuestSourceId, access = QuestVisibilityAccess.PublicOnly });
            QuestSourceBrowseResult privilegedBrowse = sources.BrowseSource(new QuestSourceBrowseRequest { questSourceId = create.Source?.QuestSourceId, access = QuestVisibilityAccess.PrivilegedDiagnostic });
            bool valid = create.Succeeded
                && sources.SourceCount == 1
                && sources.ListingCount == 0
                && create.Source.SceneBindingKey == "scene.prototype.guild.archive"
                && publicBrowse.Status == QuestSourceOperationStatus.VisibilityDenied
                && privilegedBrowse.Succeeded
                && privilegedBrowse.VisibleCount == 0;
            return TestLabAssertions.True("step15-source-empty", "Quest sources can exist without listings and preserve scene binding", valid, $"Create={create.Status} Public={publicBrowse.Status} Privileged={privilegedBrowse.Status} Sources={sources.SourceCount} Listings={sources.ListingCount}");
        }

        private static TestLabAutomationStepResult QuestSourcePublishBrowseDiscovery(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            QuestRuntimeOperationResult quest = CreateGuildPosting(quests, "source-publish", "quest.prototype.guild.source-publish");
            QuestSourceOperationResult source = CreateGuildCounter(sources, "quest-source.prototype.guild-counter.source-publish");
            QuestSourceOperationResult unauthorized = sources.PublishListing(new QuestListingPublishRequest { transactionId = "tx.quest-source.publish.unauthorized", questSourceId = source.Source?.QuestSourceId, questId = quest.Snapshot?.QuestId, worldTime = 2d });
            QuestSourceOperationResult publish = sources.PublishListing(new QuestListingPublishRequest { transactionId = "tx.quest-source.publish", questSourceId = source.Source?.QuestSourceId, questId = quest.Snapshot?.QuestId, publisherAuthorityId = "authority.prototype.guild.quest-offer", publisherPersonId = "person.prototype.guild-clerk", worldTime = 2d });
            QuestSourceBrowseResult browse = sources.BrowseSource(new QuestSourceBrowseRequest { transactionId = "tx.quest-source.browse", questSourceId = source.Source?.QuestSourceId, requesterPersonId = "person.prototype.player", access = QuestVisibilityAccess.LocalKnowledge, eligibilityContext = EligibleContext("person.prototype.player"), recordDiscovery = true, worldTime = 3d });
            QuestListingInspectionResult inspect = sources.InspectListing(new QuestListingInspectRequest { transactionId = "tx.quest-source.inspect", questSourceId = source.Source?.QuestSourceId, questListingId = publish.Listing?.QuestListingId, requesterPersonId = "person.prototype.player", access = QuestVisibilityAccess.LocalKnowledge, eligibilityContext = EligibleContext("person.prototype.player"), recordDiscovery = true, worldTime = 4d });
            bool valid = quest.Succeeded
                && source.Succeeded
                && unauthorized.Status == QuestSourceOperationStatus.UnauthorizedPublisher
                && publish.Succeeded
                && browse.Succeeded
                && browse.VisibleCount == 1
                && inspect.Succeeded
                && sources.DiscoveryCount == 2
                && participation.OfferCount == 0
                && participation.AssignmentCount == 0;
            return TestLabAssertions.True("step15-source-publish-browse", "Publication, browse, inspect, and discovery stay separate from offer creation", valid, $"Quest={quest.Status} Source={source.Status} Unauthorized={unauthorized.Status} Publish={publish.Status} Browse={browse.Status}/{browse.VisibleCount} Inspect={inspect.Status} Discoveries={sources.DiscoveryCount} Offers={participation.OfferCount} Assignments={participation.AssignmentCount}");
        }

        private static TestLabAutomationStepResult QuestSourceAcceptanceClaimsListing(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            QuestRuntimeOperationResult quest = CreateGuildPosting(quests, "source-accept", "quest.prototype.guild.source-accept");
            QuestSourceOperationResult source = CreateGuildCounter(sources, "quest-source.prototype.guild-counter.source-accept");
            QuestSourceOperationResult publish = sources.PublishListing(new QuestListingPublishRequest { transactionId = "tx.quest-source.accept.publish", questSourceId = source.Source?.QuestSourceId, questId = quest.Snapshot?.QuestId, publisherAuthorityId = "authority.prototype.guild.quest-offer", publisherPersonId = "person.prototype.guild-clerk", worldTime = 2d });
            QuestSourceOperationResult preview = sources.AcceptFromSource(new QuestSourceAcceptRequest { transactionId = "tx.quest-source.accept.preview", questListingId = publish.Listing?.QuestListingId, personId = "person.prototype.player", authorityBasisId = "authority.prototype.guild.quest-offer", eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 3d, preview = true });
            QuestSourceOperationResult accept = sources.AcceptFromSource(new QuestSourceAcceptRequest { transactionId = "tx.quest-source.accept", questListingId = publish.Listing?.QuestListingId, personId = "person.prototype.player", authorityBasisId = "authority.prototype.guild.quest-offer", eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 3d });
            QuestSourceBrowseResult after = sources.BrowseSource(new QuestSourceBrowseRequest { questSourceId = source.Source?.QuestSourceId, access = QuestVisibilityAccess.LocalKnowledge, eligibilityContext = EligibleContext("person.prototype.player"), worldTime = 4d });
            QuestVisibleListingSnapshot visible = after.Listings.FirstOrDefault();
            bool valid = quest.Succeeded
                && source.Succeeded
                && publish.Succeeded
                && preview.Status == QuestSourceOperationStatus.Preview
                && accept.Succeeded
                && accept.Assignment != null
                && accept.Listing.LifecycleState == QuestListingLifecycleState.Claimed
                && participation.OfferCount == 1
                && participation.AssignmentCount == 1
                && after.VisibleCount == 1
                && visible != null
                && visible.Taken;
            return TestLabAssertions.True("step15-source-acceptance", "Quest source acceptance delegates to participation and marks listings taken", valid, $"Quest={quest.Status} Source={source.Status} Publish={publish.Status} Preview={preview.Status} Accept={accept.Status} Listing={accept.Listing?.LifecycleState} Browse={after.VisibleCount} Taken={visible?.Taken} Offers={participation.OfferCount} Assignments={participation.AssignmentCount}");
        }

        private static TestLabAutomationStepResult QuestSourceExpirationPersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            QuestRuntimeOperationResult quest = quests.CreateQuest(new QuestCreateRequest
            {
                transactionId = "tx.quest-source.persistence.quest",
                questId = "quest.prototype.delivery.source-persist",
                questDefinitionId = PrototypeQuestDefinitionFactory.MerchantDeliveryDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Organization, issuerId = "organization.prototype.merchant-guild" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = "person.prototype.player" },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.Contract, locationId = "location.prototype.market-stall", interactionPointId = "interaction-point.prototype.merchant-counter" },
                subjectLinks = new[] { Subject("item.prototype.merchant-parcel", QuestSubjectRole.Item, InformationSubjectType.Custom) },
                createdWorldTime = 1d
            });
            QuestSourceOperationResult source = CreateMerchantCounter(sources, "quest-source.prototype.merchant-counter.source-persist");
            QuestSourceOperationResult publish = sources.PublishListing(new QuestListingPublishRequest { transactionId = "tx.quest-source.persistence.publish", questSourceId = source.Source?.QuestSourceId, questId = quest.Snapshot?.QuestId, publisherAuthorityId = "authority.prototype.merchant.quest-offer", expirationWorldTime = 5d, worldTime = 2d });
            QuestSourceOperationResult firstExpire = sources.EvaluateExpirations(5d, "tx.quest-source.persistence.expire").FirstOrDefault();
            QuestSourceOperationResult secondExpire = sources.EvaluateExpirations(5d, "tx.quest-source.persistence.expire").FirstOrDefault();
            QuestSourcePersistenceParticipant participant = new QuestSourcePersistenceParticipant(sources, () => quests, () => participation, () => registry);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            QuestSourceRuntime restored = Sources(quests, participation, registry);
            QuestSourcePersistenceParticipant restoredParticipant = new QuestSourcePersistenceParticipant(restored, () => quests, () => participation, () => registry);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, QuestSourcePersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            int restoredListings = restored.ListingCount;
            QuestSourceRuntimeSaveData corrupt = restored.CreateSaveData();
            if (corrupt.listings.Count > 0)
            {
                corrupt.listings[0].questId = "quest.prototype.missing";
            }
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), QuestSourcePersistenceParticipant.CurrentParticipantSchemaVersion);
            bool valid = quest.Succeeded
                && source.Succeeded
                && publish.Succeeded
                && firstExpire != null
                && firstExpire.Succeeded
                && firstExpire.Listing.LifecycleState == QuestListingLifecycleState.Expired
                && secondExpire == null
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && rejected.Succeeded == false
                && restored.ListingCount == restoredListings
                && restoredListings == 1;
            return TestLabAssertions.True("step15-source-persistence", "Quest source expiration and persistence are deterministic and reject invalid graphs", valid, $"Quest={quest.Status} Source={source.Status} Publish={publish.Status} Expire={firstExpire?.Status}/{firstExpire?.Listing?.LifecycleState} DuplicateExpire={secondExpire?.Status.ToString() ?? "None"} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Restored={restored.ListingCount}");
        }

        private static TestLabAutomationStepResult ConversationReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool hasAll = PrototypeConversationDefinitionFactory.PrototypeDefinitionIds.All(id => registry.TryGet(id, out ConversationDefinition _));
            bool metadata = registry.TryGet(PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId, out ConversationDefinition counter)
                && counter.Category == ConversationCategory.QuestOffer
                && counter.CoLocationPolicy == ConversationCoLocationPolicy.SameInteractionPoint
                && counter.RequiredRoles.Contains(ConversationParticipantRole.Provider);
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (ConversationDefinition definition in PrototypeConversationDefinitionFactory.CreateMissingConversationDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = hasAll && metadata && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-conversation-readiness", "Conversation definitions register and validate", valid, $"Definitions={hasAll} Metadata={metadata} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult ConversationGuildCounterContext(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            QuestRuntimeOperationResult quest = CreateGuildPosting(quests, "conversation-context", "quest.prototype.guild.conversation-context");
            QuestSourceOperationResult source = CreateGuildCounter(sources, "quest-source.prototype.guild-counter.conversation-context");
            QuestSourceOperationResult publish = sources.PublishListing(new QuestListingPublishRequest { transactionId = "tx.conversation.context.publish", questSourceId = source.Source?.QuestSourceId, questId = quest.Snapshot?.QuestId, publisherAuthorityId = "authority.prototype.guild.quest-offer", worldTime = 2d });
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult start = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.context.start",
                conversationId = "conversation.prototype.guild-counter.context",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId,
                participants = GuildCounterParticipants("interaction-point.prototype.guild-counter"),
                hostLocationId = "location.prototype.adventurers-guild",
                hostInteractionPointId = "interaction-point.prototype.guild-counter",
                questSourceId = source.Source?.QuestSourceId,
                questListingId = publish.Listing?.QuestListingId,
                questId = quest.Snapshot?.QuestId,
                operatingOrganizationId = "organization.prototype.adventurers-guild",
                sceneBindingKey = "scene.prototype.guild.counter",
                worldTime = 3d
            });

            ConversationSnapshot snapshot = start.Snapshot;
            bool valid = quest.Succeeded
                && source.Succeeded
                && publish.Succeeded
                && start.Succeeded
                && snapshot != null
                && snapshot.Participants.Count == 3
                && snapshot.SubjectLinks.Any(link => link.role == ConversationSubjectRole.Quest)
                && snapshot.SubjectLinks.Any(link => link.role == ConversationSubjectRole.QuestSource)
                && snapshot.SubjectLinks.Any(link => link.role == ConversationSubjectRole.QuestListing)
                && snapshot.CreateInformationSubject().tags.Contains(ConversationInformationSubject.ConversationTag)
                && conversations.Query(new ConversationQuery { questId = quest.Snapshot?.QuestId, access = ConversationAccessLevel.PrivilegedDiagnostic }).Count == 1;
            return TestLabAssertions.True("step15-conversation-context", "Conversation stores quest, source, location, and provider references without owning them", valid, $"Quest={quest.Status} Source={source.Status} Publish={publish.Status} Conversation={start.Status} Participants={snapshot?.Participants.Count} Subjects={snapshot?.SubjectLinks.Count}");
        }

        private static TestLabAutomationStepResult ConversationPrivateProjection(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult start = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.private.start",
                conversationId = "conversation.prototype.private-audience",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.PrivateAudienceDefinitionId,
                participants = new[]
                {
                    Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.guild-head-office", "interaction-point.prototype.guild-head-desk"),
                    Participant("person.prototype.guild-head", ConversationParticipantRole.Addressee, "location.prototype.guild-head-office", "interaction-point.prototype.guild-head-desk", hidden: true)
                },
                subjectLinks = new[] { HiddenSubject("knowledge.prototype.private-recommendation") },
                hostLocationId = "location.prototype.guild-head-office",
                hostInteractionPointId = "interaction-point.prototype.guild-head-desk",
                worldTime = 4d
            });
            ConversationProjection publicProjection = conversations.Query(new ConversationQuery { conversationId = start.Snapshot?.ConversationId, access = ConversationAccessLevel.Public, requesterPersonId = "person.prototype.visitor" }).SingleOrDefault();
            ConversationProjection participantProjection = conversations.Query(new ConversationQuery { conversationId = start.Snapshot?.ConversationId, access = ConversationAccessLevel.Participant, requesterPersonId = "person.prototype.player" }).SingleOrDefault();
            ConversationProjection privilegedProjection = conversations.Query(new ConversationQuery { conversationId = start.Snapshot?.ConversationId, access = ConversationAccessLevel.PrivilegedDiagnostic }).SingleOrDefault();
            bool valid = start.Succeeded
                && publicProjection == null
                && participantProjection != null
                && participantProjection.Redacted
                && participantProjection.Snapshot.Participants.All(value => !value.hidden)
                && participantProjection.Snapshot.SubjectLinks.All(value => !value.hidden)
                && privilegedProjection != null
                && privilegedProjection.Redacted == false
                && privilegedProjection.Snapshot.Participants.Any(value => value.hidden);
            return TestLabAssertions.True("step15-conversation-private", "Private conversation projection redacts hidden participants and subjects", valid, $"Start={start.Status} Public={(publicProjection == null ? "Denied" : "Visible")} ParticipantRedacted={participantProjection?.Redacted} Privileged={privilegedProjection != null}");
        }

        private static TestLabAutomationStepResult ConversationProviderLocationValidation(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult missingProvider = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.provider.missing",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId,
                participants = new[] { Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.adventurers-guild", "interaction-point.prototype.guild-counter") },
                hostLocationId = "location.prototype.adventurers-guild",
                hostInteractionPointId = "interaction-point.prototype.guild-counter"
            });
            ConversationOperationResult wrongLocation = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.location.wrong",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.PrisonerInterviewDefinitionId,
                participants = new[]
                {
                    Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.guild-hall", string.Empty),
                    Participant("person.prototype.prisoner", ConversationParticipantRole.Prisoner, "location.prototype.basement-prison", string.Empty),
                    Participant("person.prototype.guard", ConversationParticipantRole.Guard, "location.prototype.basement-prison", string.Empty, provenanceId: "authority.prototype.prison.interview")
                },
                hostLocationId = "location.prototype.basement-prison",
                tagIds = new[] { "authority.prototype.prison.interview" }
            });
            bool valid = missingProvider.Status == ConversationOperationStatus.MissingParticipant
                && wrongLocation.Status == ConversationOperationStatus.CoLocationRejected
                && conversations.Count == 0
                && conversations.Revision == 0;
            return TestLabAssertions.True("step15-conversation-provider-location", "Invalid provider and location requests do not mutate conversation runtime", valid, $"Provider={missingProvider.Status} Location={wrongLocation.Status} Count={conversations.Count} Revision={conversations.Revision}");
        }

        private static TestLabAutomationStepResult ConversationIdempotenceLifecycle(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult start = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.lifecycle.start",
                conversationId = "conversation.prototype.group-briefing.lifecycle",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.GroupBriefingDefinitionId,
                participants = new[]
                {
                    Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.guild-hall", string.Empty),
                    Participant("person.prototype.guild-head", ConversationParticipantRole.Speaker, "location.prototype.guild-hall", string.Empty),
                    Participant("person.prototype.adventurer", ConversationParticipantRole.Listener, "location.prototype.guild-hall", string.Empty),
                    Participant("person.prototype.scribe", ConversationParticipantRole.Witness, "location.prototype.guild-hall", string.Empty)
                },
                hostLocationId = "location.prototype.guild-hall",
                worldTime = 1d
            });
            ConversationOperationResult duplicate = conversations.StartConversation(new ConversationStartRequest { transactionId = "tx.conversation.lifecycle.start" });
            long createdRevision = conversations.Revision;
            ConversationOperationResult stale = conversations.TransitionLifecycle(new ConversationLifecycleRequest { transactionId = "tx.conversation.lifecycle.stale", conversationId = start.Snapshot?.ConversationId, targetState = ConversationLifecycleState.Completed, expectedRevision = createdRevision - 1L });
            ConversationOperationResult complete = conversations.TransitionLifecycle(new ConversationLifecycleRequest { transactionId = "tx.conversation.lifecycle.complete", conversationId = start.Snapshot?.ConversationId, targetState = ConversationLifecycleState.Completed, expectedRevision = createdRevision, worldTime = 6d });
            ConversationOperationResult duplicateComplete = conversations.TransitionLifecycle(new ConversationLifecycleRequest { transactionId = "tx.conversation.lifecycle.complete", conversationId = start.Snapshot?.ConversationId, targetState = ConversationLifecycleState.Completed, worldTime = 6d });
            bool valid = start.Succeeded
                && duplicate.Duplicate
                && stale.Status == ConversationOperationStatus.RevisionConflict
                && complete.Succeeded
                && complete.Snapshot.LifecycleState == ConversationLifecycleState.Completed
                && duplicateComplete.Duplicate
                && conversations.Events.Count == 2;
            return TestLabAssertions.True("step15-conversation-lifecycle", "Conversation start and lifecycle transitions are idempotent and revision guarded", valid, $"Start={start.Status} Duplicate={duplicate.Status} Stale={stale.Status} Complete={complete.Status}/{complete.Snapshot?.LifecycleState} Events={conversations.Events.Count}");
        }

        private static TestLabAutomationStepResult ConversationPersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult start = conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = "tx.conversation.persistence.start",
                conversationId = "conversation.prototype.records.persist",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.RecordsDeskDefinitionId,
                participants = new[]
                {
                    Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.records-room", "interaction-point.prototype.records-desk"),
                    Participant("person.prototype.records-clerk", ConversationParticipantRole.Provider, "location.prototype.records-room", "interaction-point.prototype.records-desk", provenanceId: "authority.prototype.records.read"),
                    Participant("person.prototype.player", ConversationParticipantRole.Listener, "location.prototype.records-room", "interaction-point.prototype.records-desk")
                },
                hostLocationId = "location.prototype.records-room",
                hostInteractionPointId = "interaction-point.prototype.records-desk",
                operatingOfficeId = "office.prototype.records",
                tagIds = new[] { "authority.prototype.records.read" },
                worldTime = 2d
            });
            ConversationPersistenceParticipant participant = new ConversationPersistenceParticipant(conversations, () => registry);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            ConversationRuntime restored = Conversations(registry);
            ConversationPersistenceParticipant restoredParticipant = new ConversationPersistenceParticipant(restored, () => registry);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, ConversationPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            ConversationRuntimeSaveData corrupt = restored.CreateSaveData();
            if (corrupt.conversations.Count > 0)
            {
                corrupt.conversations[0].conversationDefinitionId = "conversation-definition.prototype.missing";
            }
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), ConversationPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool valid = start.Succeeded
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && restored.Count == 1
                && restored.Events.Count == 1
                && rejected.Succeeded == false
                && restored.Count == 1;
            return TestLabAssertions.True("step15-conversation-persistence", "Conversation persistence restores records and rejects corrupt payloads safely", valid, $"Start={start.Status} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Count={restored.Count} Events={restored.Events.Count}");
        }

        private static TestLabAutomationStepResult DialogueFlowReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool hasAll = PrototypeDialogueGraphDefinitionFactory.PrototypeDefinitionIds.All(id => registry.TryGet(id, out DialogueGraphDefinition _));
            bool metadata = registry.TryGet(PrototypeDialogueGraphDefinitionFactory.AdventurerGuildCounterGraphId, out DialogueGraphDefinition graph)
                && graph.ConversationDefinitionId == PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId
                && graph.Nodes.Any(node => node.choices.Any(choice => choice.choiceId == "guild.choice.accept-posting"));
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (DialogueGraphDefinition definition in PrototypeDialogueGraphDefinitionFactory.CreateMissingDialogueGraphDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
                UnityEngine.Object.DestroyImmediate(definition);
            }

            bool valid = hasAll && metadata && report.ErrorCount == 0;
            return TestLabAssertions.True("step15-dialogue-flow-readiness", "Dialogue graph definitions register and validate", valid, $"Definitions={hasAll} Metadata={metadata} Errors={report.ErrorCount} Warnings={report.WarningCount}");
        }

        private static TestLabAutomationStepResult DialogueFlowStartAndChoices(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult conversation = StartGuildConversation(conversations, "flow-start");
            DialogueFlowRuntime flows = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowOperationResult start = flows.StartFlow(new DialogueFlowStartRequest
            {
                transactionId = "tx.dialogue.flow.start",
                conversationId = conversation.Snapshot?.ConversationId,
                conditionContext = GuildDialogueContext(),
                worldTime = 10d
            });

            bool valid = conversation.Succeeded
                && start.Succeeded
                && start.Snapshot.CurrentNodeId == "guild.entry"
                && start.Snapshot.State == DialogueFlowState.AwaitingChoice
                && start.Snapshot.VisibleChoices.Select(choice => choice.ChoiceId).SequenceEqual(new[] { "guild.choice.accept-posting", "guild.choice.ask-work", "guild.choice.leave" });
            return TestLabAssertions.True("step15-dialogue-flow-start", "Dialogue flow starts at the canonical node with deterministic visible choices", valid, $"Conversation={conversation.Status} Flow={start.Status} Node={start.Snapshot?.CurrentNodeId} Choices={string.Join(",", start.Snapshot?.VisibleChoices.Select(choice => choice.ChoiceId) ?? Array.Empty<string>())}");
        }

        private static TestLabAutomationStepResult DialogueFlowConditions(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult conversation = StartGuildConversation(conversations, "flow-conditions");
            DialogueFlowRuntime flows = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowOperationResult start = flows.StartFlow(new DialogueFlowStartRequest { transactionId = "tx.dialogue.conditions.start", conversationId = conversation.Snapshot?.ConversationId, conditionContext = GuildDialogueContext(), worldTime = 1d });
            DialogueFlowSnapshot ordinary = start.Snapshot;
            DialogueFlowSnapshot ranked;
            bool rankedSnapshot = flows.TryGetSnapshot(start.Snapshot?.FlowId, GuildDialogueContext(rank: true), out ranked);

            bool valid = start.Succeeded
                && ordinary.VisibleChoices.All(choice => choice.ChoiceId != "guild.choice.silver-rank")
                && rankedSnapshot
                && ranked.VisibleChoices.Any(choice => choice.ChoiceId == "guild.choice.silver-rank")
                && ranked.VisibleChoices.First(choice => choice.ChoiceId == "guild.choice.silver-rank").Evaluation.Selectable;
            return TestLabAssertions.True("step15-dialogue-flow-conditions", "Dialogue flow conditions hide restricted choices until context satisfies them", valid, $"Start={start.Status} Ordinary={string.Join(",", ordinary?.VisibleChoices.Select(choice => choice.ChoiceId) ?? Array.Empty<string>())} Ranked={rankedSnapshot}:{string.Join(",", ranked?.VisibleChoices.Select(choice => choice.ChoiceId) ?? Array.Empty<string>())}");
        }

        private static TestLabAutomationStepResult DialogueFlowChoiceHistory(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult conversation = StartGuildConversation(conversations, "flow-choice");
            DialogueFlowRuntime flows = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowOperationResult start = flows.StartFlow(new DialogueFlowStartRequest { transactionId = "tx.dialogue.choice.start", conversationId = conversation.Snapshot?.ConversationId, conditionContext = GuildDialogueContext(), worldTime = 1d });
            long beforeConversationRevision = conversations.Revision;
            DialogueFlowOperationResult select = flows.SelectChoice(new DialogueChoiceSelectionRequest { transactionId = "tx.dialogue.choice.select", flowId = start.Snapshot?.FlowId, choiceId = "guild.choice.ask-work", actorPersonId = "person.prototype.player", conditionContext = GuildDialogueContext(), worldTime = 2d });
            DialogueFlowOperationResult duplicate = flows.SelectChoice(new DialogueChoiceSelectionRequest { transactionId = "tx.dialogue.choice.select", flowId = start.Snapshot?.FlowId, choiceId = "guild.choice.ask-work", actorPersonId = "person.prototype.player", conditionContext = GuildDialogueContext(), worldTime = 2d });

            bool valid = start.Succeeded
                && select.Succeeded
                && duplicate.Duplicate
                && select.Snapshot.CurrentNodeId == "guild.entry"
                && select.Snapshot.Visits.Count == 3
                && select.Snapshot.Selections.Count == 1
                && select.Snapshot.LocalVariables.Any(value => value.variableId == "flag.guild.asked-work" && value.boolValue)
                && conversations.Revision == beforeConversationRevision;
            return TestLabAssertions.True("step15-dialogue-flow-choice", "Dialogue choice selection records deterministic history without mutating conversation ownership", valid, $"Start={start.Status} Select={select.Status} Duplicate={duplicate.Status} Node={select.Snapshot?.CurrentNodeId} Visits={select.Snapshot?.Visits.Count} Selections={select.Snapshot?.Selections.Count} ConversationRevision={conversations.Revision}->{beforeConversationRevision}");
        }

        private static TestLabAutomationStepResult DialogueFlowRequiredEffectFailure(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = RegistryWithRequiredEffectGraph(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult conversation = StartGuildConversation(conversations, "flow-effect-failure");
            DialogueFlowRuntime flows = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowOperationResult start = flows.StartFlow(new DialogueFlowStartRequest { transactionId = "tx.dialogue.effect.start", graphId = "dialogue-graph.prototype.required-effect-test", conversationId = conversation.Snapshot?.ConversationId, conditionContext = GuildDialogueContext(), worldTime = 1d });
            DialogueFlowOperationResult select = flows.SelectChoice(new DialogueChoiceSelectionRequest { transactionId = "tx.dialogue.effect.select", flowId = start.Snapshot?.FlowId, choiceId = "required.choice", actorPersonId = "person.prototype.player", conditionContext = GuildDialogueContext(), worldTime = 2d });
            DialogueFlowSnapshot after;
            bool snapshot = flows.TryGetSnapshot(start.Snapshot?.FlowId, GuildDialogueContext(), out after);

            bool valid = start.Succeeded
                && select.Status == DialogueFlowOperationStatus.EffectFailed
                && snapshot
                && after.CurrentNodeId == "required.entry"
                && after.Selections.Count == 0
                && flows.Revision == 1;
            return TestLabAssertions.True("step15-dialogue-flow-effect-failure", "Required delegated dialogue effects fail atomically without an executor", valid, $"Start={start.Status} Select={select.Status} Snapshot={snapshot} Node={after?.CurrentNodeId} Selections={after?.Selections.Count} Revision={flows.Revision}");
        }

        private static TestLabAutomationStepResult DialogueFlowPersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            ConversationOperationResult conversation = StartGuildConversation(conversations, "flow-persistence");
            DialogueFlowRuntime flows = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowOperationResult start = flows.StartFlow(new DialogueFlowStartRequest { transactionId = "tx.dialogue.persistence.start", conversationId = conversation.Snapshot?.ConversationId, conditionContext = GuildDialogueContext(), worldTime = 1d });
            DialogueFlowOperationResult select = flows.SelectChoice(new DialogueChoiceSelectionRequest { transactionId = "tx.dialogue.persistence.select", flowId = start.Snapshot?.FlowId, choiceId = "guild.choice.ask-work", actorPersonId = "person.prototype.player", conditionContext = GuildDialogueContext(), worldTime = 2d });
            DialogueFlowPersistenceParticipant participant = new DialogueFlowPersistenceParticipant(flows, () => registry, () => conversations);
            PersistenceParticipantSaveResult save = participant.CapturePayload();

            DialogueFlowRuntime restored = new DialogueFlowRuntime(registry, conversations, null, PersistenceService.LocalWorldId);
            DialogueFlowPersistenceParticipant restoredParticipant = new DialogueFlowPersistenceParticipant(restored, () => registry, () => conversations);
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, DialogueFlowPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            DialogueFlowRuntimeSaveData corrupt = restored.CreateSaveData();
            if (corrupt.flows.Count > 0)
            {
                corrupt.flows[0].graphId = "dialogue-graph.prototype.missing";
            }
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), DialogueFlowPersistenceParticipant.CurrentParticipantSchemaVersion);

            bool valid = conversation.Succeeded
                && start.Succeeded
                && select.Succeeded
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && restored.Count == 1
                && restored.Events.Count == flows.Events.Count
                && rejected.Succeeded == false
                && restored.Count == 1;
            return TestLabAssertions.True("step15-dialogue-flow-persistence", "Dialogue flow persistence restores current state and rejects corrupt graph references safely", valid, $"Conversation={conversation.Status} Start={start.Status} Select={select.Status} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Restored={restored.Count} Events={restored.Events.Count}");
        }

        private static TestLabAutomationStepResult NarrativeReadiness(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            bool hasAll = PrototypeNarrativeEventDefinitionFactory.PrototypeDefinitionIds.All(id => registry.TryGet(id, out NarrativeEventDefinition _));
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (NarrativeEventDefinition definition in PrototypeNarrativeEventDefinitionFactory.CreateMissingNarrativeEventDefinitions(Array.Empty<string>()))
            {
                definition.ValidateCatalogDefinition(registry.DefinitionsById, report);
            }

            NarrativeEventRuntime runtime = NarrativeRuntime(registry);
            bool valid = hasAll && report.HasErrors == false && report.WarningCount == 0 && runtime.Count == 0;
            return TestLabAssertions.True("step15-narrative-readiness", "Narrative event definitions register and validate without runtime mutation", valid, $"Definitions={hasAll} Errors={report.ErrorCount} Warnings={report.WarningCount} Runtime={runtime.Count}");
        }

        private static TestLabAutomationStepResult NarrativeLocationQuestAction(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            QuestRuntime quests = Runtime(context);
            QuestParticipationRuntime participation = Participation(quests, registry);
            QuestSourceRuntime sources = Sources(quests, participation, registry);
            CreateGuildBoard(sources, "quest-source.prototype.guild-board");
            NarrativeEventRuntime runtime = NarrativeRuntime(registry, quests, sources, Conversations(registry));

            NarrativeTriggerRequest request = new NarrativeTriggerRequest
            {
                transactionId = Tx(context, "narrative-dungeon-entry"),
                source = Source(NarrativeTriggerCategory.LocationEntered, PrototypeNarrativeEventDefinitionFactory.DungeonEntrySignalId, "location.prototype.dungeon-entry", "person.prototype.player", 10d),
                conditionContext = NarrativeContext("person.prototype.player", locationId: "location.prototype.dungeon-entry", organizationIds: new[] { "organization.prototype.adventurers-guild" })
            };

            NarrativeEventOperationResult preview = runtime.RouteTrigger(new NarrativeTriggerRequest { transactionId = request.transactionId + ".preview", source = request.source.Clone(), conditionContext = request.conditionContext.Clone(), preview = true });
            NarrativeEventOperationResult execute = runtime.RouteTrigger(request);
            NarrativeEventOperationResult duplicate = runtime.RouteTrigger(new NarrativeTriggerRequest { transactionId = request.transactionId + ".duplicate", source = request.source.Clone(), conditionContext = request.conditionContext.Clone() });
            NarrativeEventSnapshot snapshot = execute.Snapshots.FirstOrDefault();

            bool valid = preview.Preview
                && execute.Succeeded
                && duplicate.Succeeded
                && runtime.Count == 1
                && quests.Count == 1
                && snapshot != null
                && snapshot.Lifecycle == NarrativeEventLifecycle.Resolved
                && snapshot.ActionExecutions.Any(action => action.category == NarrativeActionCategory.InstantiateQuest && action.lifecycle == NarrativeActionLifecycle.Committed)
                && snapshot.ActionExecutions.Any(action => action.category == NarrativeActionCategory.PublishQuestListing);
            return TestLabAssertions.True("step15-narrative-location", "Committed location trigger creates one scoped narrative event and delegates quest creation once", valid, $"Preview={preview.Status} Execute={execute.Status} Duplicate={duplicate.Status} Events={runtime.Count} Quests={quests.Count} Actions={snapshot?.ActionExecutions.Count ?? 0}");
        }

        private static TestLabAutomationStepResult NarrativeCrossRuntimeSignals(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            ConversationRuntime conversations = Conversations(registry);
            NarrativeEventRuntime runtime = NarrativeRuntime(registry, Runtime(context), null, conversations);

            NarrativeEventOperationResult dialogue = runtime.RouteTrigger(new NarrativeTriggerRequest
            {
                transactionId = Tx(context, "narrative-dialogue-choice"),
                source = Source(NarrativeTriggerCategory.DialogueChoice, PrototypeNarrativeEventDefinitionFactory.DialogueChoiceSignalId, "guild.choice.ask-work", "person.prototype.player", 11d),
                conditionContext = NarrativeContext("person.prototype.player", conversationId: "conversation.prototype.guild.counter", dialogueIds: new[] { "dialogue-choice.guild.choice.ask-work" })
            });
            NarrativeEventOperationResult knowledge = runtime.EmitSignal(new NarrativeSignalRequest
            {
                transactionId = Tx(context, "narrative-knowledge"),
                signalDefinitionId = PrototypeNarrativeEventDefinitionFactory.KnowledgeLearnedSignalId,
                actorPersonId = "person.prototype.player",
                subjectIds = new[] { "subject.prototype.hidden-dungeon" },
                conditionContext = NarrativeContext("person.prototype.player", locationId: "location.prototype.dungeon-entry", knownIds: new[] { "subject.prototype.hidden-dungeon" }),
                worldTime = 12d
            });

            bool dialogueSignal = runtime.Signals.Any(signal => signal.signalDefinitionId == PrototypeNarrativeEventDefinitionFactory.CascadeStartSignalId);
            bool valid = dialogue.Succeeded
                && knowledge.Succeeded
                && dialogueSignal
                && conversations.Count == 1
                && runtime.Query(new NarrativeEventQuery { definitionId = PrototypeNarrativeEventDefinitionFactory.DialogueChoiceWorldEventDefinitionId }).Count == 1
                && runtime.Query(new NarrativeEventQuery { definitionId = PrototypeNarrativeEventDefinitionFactory.KnowledgeUnlockConversationDefinitionId }).Count == 1;
            return TestLabAssertions.True("step15-narrative-signals", "Dialogue choices and knowledge signals route through typed narrative definitions", valid, $"Dialogue={dialogue.Status} Knowledge={knowledge.Status} Events={runtime.Count} Signals={runtime.Signals.Count} Conversations={conversations.Count}");
        }

        private static TestLabAutomationStepResult NarrativeHiddenProjectionBoundaries(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            NarrativeEventRuntime runtime = NarrativeRuntime(registry, Runtime(context));
            NarrativeEventOperationResult result = runtime.RouteTrigger(new NarrativeTriggerRequest
            {
                transactionId = Tx(context, "narrative-hidden"),
                source = Source(NarrativeTriggerCategory.SocialState, PrototypeNarrativeEventDefinitionFactory.HiddenFactionSignalId, "faction.prototype.hidden", "person.prototype.player", 13d),
                conditionContext = NarrativeContext("person.prototype.player", socialIds: new[] { "faction.prototype.hidden" })
            });

            NarrativeEventSnapshot development = runtime.Query(new NarrativeEventQuery { definitionId = PrototypeNarrativeEventDefinitionFactory.HiddenFactionOfferDefinitionId, developmentView = true }).FirstOrDefault();
            NarrativeEventSnapshot redacted = runtime.Query(new NarrativeEventQuery { definitionId = PrototypeNarrativeEventDefinitionFactory.HiddenFactionOfferDefinitionId, developmentView = false }).FirstOrDefault();
            bool valid = result.Succeeded
                && development != null
                && redacted != null
                && development.IsHidden
                && development.ActionExecutions.Count > 0
                && redacted.ActionExecutions.Count == 0
                && redacted.MatchedConditions.Count == 0;
            return TestLabAssertions.True("step15-narrative-hidden", "Hidden narrative event projections redact action and condition details outside development views", valid, $"Result={result.Status} Hidden={development?.IsHidden ?? false} DevActions={development?.ActionExecutions.Count ?? 0} PublicActions={redacted?.ActionExecutions.Count ?? 0}");
        }

        private static TestLabAutomationStepResult NarrativeRequiredActionFailure(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            NarrativeEventRuntime runtime = NarrativeRuntime(registry);
            NarrativeEventOperationResult result = runtime.RouteTrigger(new NarrativeTriggerRequest
            {
                transactionId = Tx(context, "narrative-required-failure"),
                source = Source(NarrativeTriggerCategory.LocationEntered, PrototypeNarrativeEventDefinitionFactory.DungeonEntrySignalId, "location.prototype.dungeon-entry", "person.prototype.player", 14d),
                conditionContext = NarrativeContext("person.prototype.player", locationId: "location.prototype.dungeon-entry", organizationIds: new[] { "organization.prototype.adventurers-guild" })
            });

            NarrativeEventSnapshot failed = runtime.Query(new NarrativeEventQuery { definitionId = PrototypeNarrativeEventDefinitionFactory.DungeonEntryQuestDefinitionId }).FirstOrDefault();
            bool valid = result.Status == NarrativeOperationStatus.ActionFailed
                && failed != null
                && failed.Lifecycle == NarrativeEventLifecycle.Failed
                && failed.ActionExecutions.Any(action => action.category == NarrativeActionCategory.InstantiateQuest && action.lifecycle == NarrativeActionLifecycle.Failed);
            return TestLabAssertions.True("step15-narrative-required-action", "Missing required owner runtime integration fails the narrative event without faking owner mutation", valid, $"Result={result.Status} Lifecycle={failed?.Lifecycle.ToString() ?? "None"} Actions={failed?.ActionExecutions.Count ?? 0}");
        }

        private static TestLabAutomationStepResult NarrativeCascadePersistence(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            NarrativeEventRuntime runtime = NarrativeRuntime(registry);
            NarrativeEventOperationResult cascade = runtime.EmitSignal(new NarrativeSignalRequest
            {
                transactionId = Tx(context, "narrative-cascade"),
                signalDefinitionId = PrototypeNarrativeEventDefinitionFactory.CascadeStartSignalId,
                actorPersonId = "person.prototype.player",
                subjectIds = new[] { "subject.prototype.cascade" },
                conditionContext = NarrativeContext("person.prototype.player", subjectId: "subject.prototype.cascade"),
                worldTime = 20d
            });

            NarrativeEventPersistenceParticipant participant = new NarrativeEventPersistenceParticipant(runtime, () => registry, () => NarrativeIntegrations());
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            NarrativeEventRuntime restored = NarrativeRuntime(registry);
            NarrativeEventPersistenceParticipant restoredParticipant = new NarrativeEventPersistenceParticipant(restored, () => registry, () => NarrativeIntegrations());
            PersistenceParticipantPrepareResult prepare = restoredParticipant.PreparePayload(save.PayloadJson, NarrativeEventPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantCommitResult commit = restoredParticipant.CommitPreparedPayload(prepare.PreparedPayload);
            NarrativeEventRuntimeSaveData corrupt = restored.CreateSaveData();
            if (corrupt.events.Count > 0) corrupt.events[0].eventDefinitionId = "narrative-event-definition.prototype.missing";
            int beforeReject = restored.Count;
            PersistenceParticipantPrepareResult rejected = restoredParticipant.PreparePayload(JsonUtility.ToJson(corrupt), NarrativeEventPersistenceParticipant.CurrentParticipantSchemaVersion);

            bool valid = cascade.Succeeded
                && runtime.Signals.Count >= 2
                && save.Succeeded
                && prepare.Succeeded
                && commit.Succeeded
                && restored.Count == runtime.Count
                && rejected.Succeeded == false
                && restored.Count == beforeReject;
            return TestLabAssertions.True("step15-narrative-persistence", "Narrative cascades persist and corrupt restore payloads are rejected before live mutation", valid, $"Cascade={cascade.Status} Signals={runtime.Signals.Count} Save={save.Succeeded} Prepare={prepare.Succeeded} Commit={commit.Succeeded} Reject={rejected.Succeeded} Restored={restored.Count}");
        }

        private static DefinitionRegistry Registry(TestLabAutomationContext context)
        {
            DefinitionRegistry baseRegistry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            return PrototypeNarrativeEventDefinitionFactory.AddMissingPrototypeNarrativeEventDefinitions(PrototypeDialogueGraphDefinitionFactory.AddMissingPrototypeDialogueGraphDefinitions(PrototypeConversationDefinitionFactory.AddMissingPrototypeConversationDefinitions(PrototypeQuestSourceDefinitionFactory.AddMissingPrototypeQuestSourceDefinitions(PrototypeQuestDefinitionFactory.AddMissingPrototypeQuestDefinitions(baseRegistry)))));
        }

        private static QuestRuntime Runtime(TestLabAutomationContext context)
        {
            return new QuestRuntime(Registry(context), PersistenceService.LocalWorldId);
        }

        private static QuestParticipationRuntime Participation(QuestRuntime quests, DefinitionRegistry registry)
        {
            return new QuestParticipationRuntime(quests, registry, PersistenceService.LocalWorldId);
        }

        private static QuestObjectiveProgressRuntime Objectives(QuestRuntime quests, QuestParticipationRuntime participation, DefinitionRegistry registry)
        {
            return new QuestObjectiveProgressRuntime(quests, participation, registry, PersistenceService.LocalWorldId);
        }

        private static QuestOutcomeRuntime Outcomes(QuestRuntime quests, QuestParticipationRuntime participation, QuestObjectiveProgressRuntime objectives, DefinitionRegistry registry, IQuestRewardEffectExecutor executor)
        {
            return new QuestOutcomeRuntime(quests, participation, objectives, registry, executor, PersistenceService.LocalWorldId);
        }

        private static QuestSourceRuntime Sources(QuestRuntime quests, QuestParticipationRuntime participation, DefinitionRegistry registry)
        {
            return new QuestSourceRuntime(quests, participation, registry, PersistenceService.LocalWorldId);
        }

        private static ConversationRuntime Conversations(DefinitionRegistry registry)
        {
            return new ConversationRuntime(registry, PersistenceService.LocalWorldId);
        }

        private static NarrativeEventRuntime NarrativeRuntime(DefinitionRegistry registry, QuestRuntime quests = null, QuestSourceRuntime sources = null, ConversationRuntime conversations = null, NarrativeEventRuntimeIntegrations integrations = null)
        {
            return new NarrativeEventRuntime(registry, integrations ?? NarrativeIntegrations(quests, sources, conversations), PersistenceService.LocalWorldId);
        }

        private static NarrativeEventRuntimeIntegrations NarrativeIntegrations(QuestRuntime quests = null, QuestSourceRuntime sources = null, ConversationRuntime conversations = null)
        {
            return new NarrativeEventRuntimeIntegrations
            {
                QuestRuntime = quests,
                QuestSourceRuntime = sources,
                ConversationRuntime = conversations,
                InformationGrantExecutor = target => !string.IsNullOrWhiteSpace(target),
                TravelConditionExecutor = target => !string.IsNullOrWhiteSpace(target),
                ConnectionChangeExecutor = target => !string.IsNullOrWhiteSpace(target),
                SocialActionExecutor = target => !string.IsNullOrWhiteSpace(target),
                OrganizationActionExecutor = target => !string.IsNullOrWhiteSpace(target),
                LegalActionExecutor = target => !string.IsNullOrWhiteSpace(target)
            };
        }

        private static NarrativeTriggerSourceData Source(NarrativeTriggerCategory category, string sourceId, string subjectId, string actorId, double worldTime)
        {
            return new NarrativeTriggerSourceData
            {
                category = category,
                sourceId = sourceId,
                sourceTransactionId = $"source.{NarrativeModelUtility.SanitizeForId(sourceId)}.{worldTime:0}",
                actorPersonId = actorId,
                targetId = subjectId,
                subjectId = subjectId,
                ownerRuntime = "TestLabAutomation",
                worldTime = worldTime,
                committed = true
            };
        }

        private static NarrativeConditionContextData NarrativeContext(
            string actorId,
            string locationId = "",
            string conversationId = "",
            string subjectId = "",
            string[] knownIds = null,
            string[] dialogueIds = null,
            string[] organizationIds = null,
            string[] socialIds = null)
        {
            return new NarrativeConditionContextData
            {
                actorPersonId = actorId,
                locationId = locationId,
                conversationId = conversationId,
                subjectId = subjectId,
                worldTime = 10d,
                knownSubjectIds = knownIds ?? Array.Empty<string>(),
                dialogueStateIds = dialogueIds ?? Array.Empty<string>(),
                organizationStateIds = organizationIds ?? Array.Empty<string>(),
                socialStateIds = socialIds ?? Array.Empty<string>()
            };
        }

        private static string Tx(TestLabAutomationContext context, string operation)
        {
            return context?.TransactionIds?.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, operation)
                ?? $"testlab.step15.{operation}";
        }

        private static ConversationOperationResult StartGuildConversation(ConversationRuntime conversations, string key)
        {
            return conversations.StartConversation(new ConversationStartRequest
            {
                transactionId = $"tx.dialogue.{key}.conversation",
                conversationId = $"conversation.prototype.dialogue.{key}",
                conversationDefinitionId = PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId,
                participants = GuildCounterParticipants("interaction-point.prototype.guild-counter"),
                hostLocationId = "location.prototype.adventurers-guild",
                hostInteractionPointId = "interaction-point.prototype.guild-counter",
                questId = "quest.prototype.guild.counter",
                questSourceId = "quest-source.prototype.guild-counter",
                questListingId = "quest-listing.prototype.guild-counter",
                operatingOrganizationId = "organization.prototype.adventurers-guild",
                sceneBindingKey = "scene.prototype.guild.counter",
                worldTime = 1d
            });
        }

        private static DialogueConditionContext GuildDialogueContext(bool rank = false)
        {
            return new DialogueConditionContext
            {
                actorPersonId = "person.prototype.player",
                listenerPersonId = "person.prototype.player",
                locationId = "location.prototype.adventurers-guild",
                interactionPointId = "interaction-point.prototype.guild-counter",
                worldTime = 1d,
                facts = new QuestEligibilityFactSet(
                    organizationMemberships: new[] { "organization.prototype.adventurers-guild" },
                    organizationRanks: rank ? new[] { "rank.prototype.adventurers.silver" } : Array.Empty<string>(),
                    authorityGrants: new[] { "authority.prototype.guild.quest-offer", "authority.prototype.records.read", "authority.prototype.city.quest-assign" },
                    knownSubjects: new[] { "subject.prototype.hidden-dungeon" }),
                activeQuestIds = new[] { "quest.prototype.guild.counter" },
                activeOfferIds = new[] { "offer.prototype.guild.counter" },
                activeAssignmentQuestIds = Array.Empty<string>(),
                completedQuestIds = Array.Empty<string>(),
                claimableRewardIds = Array.Empty<string>()
            };
        }

        private static DefinitionRegistry RegistryWithRequiredEffectGraph(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = Registry(context);
            List<IGameDefinition> definitions = registry.DefinitionsById.Values.Where(definition => definition != null).ToList();
            DialogueGraphDefinition graph = ScriptableObject.CreateInstance<DialogueGraphDefinition>();
            graph.name = "Required Effect Test Dialogue";
            graph.DevelopmentConfigure(
                "dialogue-graph.prototype.required-effect-test",
                "Required Effect Test Dialogue",
                PrototypeConversationDefinitionFactory.AdventurerGuildCounterDefinitionId,
                "required.entry",
                "required.end",
                new[]
                {
                    new DialogueNodeDefinitionData
                    {
                        nodeId = "required.entry",
                        category = DialogueNodeCategory.ChoicePrompt,
                        authoredText = "Required effect test.",
                        speaker = new DialogueSpeakerSelectorData { kind = DialogueSpeakerSelectorKind.Provider },
                        listener = new DialogueListenerSelectorData { kind = DialogueListenerSelectorKind.AllParticipants },
                        choices = new[]
                        {
                            new DialogueChoiceDefinitionData
                            {
                                choiceId = "required.choice",
                                displayText = "Run required owner effect",
                                category = DialogueChoiceCategory.ServiceRequest,
                                effects = new[]
                                {
                                    new DialogueEffectData
                                    {
                                        effectId = "effect.required.owner",
                                        kind = DialogueEffectKind.CreateQuestOffer,
                                        requirement = DialogueEffectRequirement.Required,
                                        targetId = "quest.prototype.guild.counter"
                                    }
                                }
                            }
                        }
                    },
                    new DialogueNodeDefinitionData { nodeId = "required.end", category = DialogueNodeCategory.End, authoredText = "End.", speaker = new DialogueSpeakerSelectorData { kind = DialogueSpeakerSelectorKind.None } }
                },
                tags: new[] { "prototype", "test", "required-effect" });
            definitions.Add(graph);
            return new DefinitionRegistry(definitions);
        }

        private static ConversationParticipantRecordData[] GuildCounterParticipants(string interactionPointId)
        {
            return new[]
            {
                Participant("person.prototype.player", ConversationParticipantRole.Initiator, "location.prototype.adventurers-guild", interactionPointId),
                Participant("person.prototype.guild-clerk", ConversationParticipantRole.Provider, "location.prototype.adventurers-guild", interactionPointId, organizationId: "organization.prototype.adventurers-guild"),
                Participant("person.prototype.player", ConversationParticipantRole.QuestRecipient, "location.prototype.adventurers-guild", interactionPointId)
            };
        }

        private static ConversationParticipantRecordData Participant(string personId, ConversationParticipantRole role, string locationId, string interactionPointId, string organizationId = "", string officeId = "", bool hidden = false, string provenanceId = "")
        {
            return new ConversationParticipantRecordData
            {
                personId = personId,
                role = role,
                currentLocationId = locationId,
                currentInteractionPointId = interactionPointId,
                representedOrganizationId = organizationId,
                representedOfficeId = officeId,
                hidden = hidden,
                provenanceId = provenanceId
            };
        }

        private static ConversationSubjectLinkData HiddenSubject(string subjectId)
        {
            return new ConversationSubjectLinkData
            {
                role = ConversationSubjectRole.Information,
                subject = new InformationSubjectReferenceData { subjectType = InformationSubjectType.KnowledgeRecord, subjectId = subjectId, tags = new[] { "private" } },
                hidden = true
            };
        }

        private static QuestSourceOperationResult CreateGuildCounter(QuestSourceRuntime sources, string sourceId)
        {
            return sources.CreateSource(new QuestSourceCreateRequest
            {
                transactionId = $"tx.{sourceId}.create",
                questSourceId = sourceId,
                questSourceDefinitionId = PrototypeQuestSourceDefinitionFactory.AdventurerGuildCounterDefinitionId,
                hostLocationId = "location.prototype.adventurers-guild",
                interactionPointId = "interaction-point.prototype.guild-counter",
                operatingOrganizationId = "organization.prototype.guild",
                sceneBindingKey = "scene.prototype.guild.counter",
                worldTime = 1d
            });
        }

        private static QuestSourceOperationResult CreateGuildBoard(QuestSourceRuntime sources, string sourceId)
        {
            return sources.CreateSource(new QuestSourceCreateRequest
            {
                transactionId = $"tx.{sourceId}.create",
                questSourceId = sourceId,
                questSourceDefinitionId = PrototypeQuestSourceDefinitionFactory.AdventurerGuildBoardDefinitionId,
                hostLocationId = "location.prototype.adventurers-guild",
                interactionPointId = "interaction-point.prototype.guild-board",
                operatingOrganizationId = "organization.prototype.guild",
                sceneBindingKey = "scene.prototype.guild.board",
                worldTime = 1d
            });
        }

        private static QuestSourceOperationResult CreateMerchantCounter(QuestSourceRuntime sources, string sourceId)
        {
            return sources.CreateSource(new QuestSourceCreateRequest
            {
                transactionId = $"tx.{sourceId}.create",
                questSourceId = sourceId,
                questSourceDefinitionId = PrototypeQuestSourceDefinitionFactory.MerchantGuildCounterDefinitionId,
                hostLocationId = "location.prototype.market-stall",
                interactionPointId = "interaction-point.prototype.merchant-counter",
                operatingOrganizationId = "organization.prototype.merchant-guild",
                sceneBindingKey = "scene.prototype.guild.merchant-counter",
                worldTime = 1d
            });
        }

        private static QuestAssignmentSnapshot AcceptedGuildAssignment(QuestRuntime quests, QuestParticipationRuntime participation, string key)
        {
            string questId = $"quest.prototype.guild.{key}";
            string personId = $"person.prototype.{key}";
            CreateGuildPosting(quests, key, questId);
            QuestParticipationOperationResult offer = participation.CreateOffer(OfferRequest(questId, personId, EligibleContext(personId), $"tx.quest.offer.{key}"));
            QuestParticipationOperationResult accept = participation.AcceptOffer(new QuestAcceptOfferRequest
            {
                transactionId = $"tx.quest.accept.{key}",
                offerId = offer.Offer?.OfferId,
                personId = personId,
                explicitConsent = true,
                consentRecordId = $"consent.prototype.{key}",
                eligibilityContext = EligibleContext(personId),
                worldTime = 2d
            });
            return accept.Assignment;
        }

        private static QuestAssignmentSnapshot AcceptedDeliveryAssignment(QuestRuntime quests, QuestParticipationRuntime participation, string key)
        {
            string questId = $"quest.prototype.delivery.{key}";
            string personId = $"person.prototype.{key}";
            quests.CreateQuest(new QuestCreateRequest
            {
                transactionId = $"tx.quest.delivery.create.{key}",
                questId = questId,
                questDefinitionId = PrototypeQuestDefinitionFactory.MerchantDeliveryDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Business, issuerId = "business.prototype.merchant" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = personId },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.Contract, locationId = "location.prototype.market", interactionPointId = "interaction-point.prototype.merchant-counter" },
                subjectLinks = new[] { Subject("item.prototype.merchant-parcel", QuestSubjectRole.Item, InformationSubjectType.Custom) },
                createdWorldTime = 1d
            });
            QuestEligibilityContext context = new QuestEligibilityContext
            {
                personId = personId,
                interactionPointId = "interaction-point.prototype.merchant-counter",
                privilegedDiagnostics = true,
                worldTime = 1d,
                facts = new QuestEligibilityFactSet(authorityGrants: new[] { "authority.prototype.merchant.quest-offer" })
            };
            QuestParticipationOperationResult offer = participation.CreateOffer(new QuestOfferRequest
            {
                transactionId = $"tx.quest.delivery.offer.{key}",
                questId = questId,
                recipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = personId },
                institutionalIssuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Business, issuerId = "business.prototype.merchant" },
                offeringProvider = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Business, issuerId = "business.prototype.merchant", actingPersonId = "person.prototype.merchant" },
                channel = QuestOfferChannel.InteractionPoint,
                sourceInteractionPointId = "interaction-point.prototype.merchant-counter",
                sourceLocationId = "location.prototype.market",
                authorityBasisId = "authority.prototype.merchant.quest-offer",
                eligibilityContext = context,
                worldTime = 1d
            });
            QuestParticipationOperationResult accept = participation.AcceptOffer(new QuestAcceptOfferRequest
            {
                transactionId = $"tx.quest.delivery.accept.{key}",
                offerId = offer.Offer?.OfferId,
                personId = personId,
                explicitConsent = true,
                consentRecordId = $"consent.prototype.{key}",
                eligibilityContext = context,
                worldTime = 2d
            });
            return accept.Assignment;
        }

        private static QuestAssignmentSnapshot AcceptedHiddenAssignment(QuestRuntime quests, QuestParticipationRuntime participation, string key)
        {
            string questId = $"quest.prototype.hidden.{key}";
            string personId = $"person.prototype.{key}";
            quests.CreateQuest(new QuestCreateRequest
            {
                transactionId = $"tx.quest.hidden.create.{key}",
                questId = questId,
                questDefinitionId = PrototypeQuestDefinitionFactory.HiddenDungeonRumorDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.System, issuerId = "system.quest" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Open },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.WorldEvent, locationId = "location.prototype.tavern" },
                subjectLinks = new[] { Subject("location.prototype.secret-dungeon-entry", QuestSubjectRole.Location, InformationSubjectType.Location) },
                visibility = QuestVisibility.Hidden,
                createdWorldTime = 1d
            });
            QuestParticipationOperationResult assignment = participation.DirectAssign(new QuestDirectAssignmentRequest
            {
                transactionId = $"tx.quest.hidden.assign.{key}",
                questId = questId,
                assigneePersonId = personId,
                assignedBy = new QuestIssuerReferenceData { issuerType = QuestIssuerType.System, issuerId = "system.quest" },
                explicitConsent = true,
                consentRecordId = $"consent.prototype.{key}",
                eligibilityContext = new QuestEligibilityContext { personId = personId, privilegedDiagnostics = true, worldTime = 1d, facts = new QuestEligibilityFactSet(knownSubjects: new[] { "subject.prototype.hidden-dungeon" }) },
                worldTime = 2d,
                visibility = QuestVisibility.Hidden
            });
            return assignment.Assignment;
        }

        private static QuestObjectiveSignal ObjectiveSignal(QuestAssignmentSnapshot assignment, QuestObjectiveCategory category, string targetId, string sourceEventId, InformationSubjectType targetType = InformationSubjectType.Custom)
        {
            return new QuestObjectiveSignal
            {
                transactionId = $"tx.{sourceEventId}",
                assignmentId = assignment.AssignmentId,
                questId = assignment.QuestId,
                participantPersonId = assignment.AssigneePersonId,
                actorPersonId = assignment.AssigneePersonId,
                category = category,
                target = new InformationSubjectReferenceData { subjectType = targetType, subjectId = targetId },
                amount = 1,
                sourceEventId = sourceEventId,
                sourceRuntimeId = "testlab.objective-signal",
                worldTime = 5d,
                committed = true
            };
        }

        private static void CompleteGuildObjectives(QuestObjectiveProgressRuntime objectives, QuestAssignmentSnapshot assignment, string key)
        {
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.UseInteractionPoint, "interaction-point.prototype.guild-counter", $"source.quest.{key}.counter"));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.VisitLocation, "location.prototype.dungeon-entry", $"source.quest.{key}.dungeon", InformationSubjectType.Location));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", $"source.quest.{key}.defeat1"));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", $"source.quest.{key}.defeat2"));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.DefeatCount, "enemy-family.prototype.monster", $"source.quest.{key}.defeat3"));
            objectives.ApplySignal(ObjectiveSignal(assignment, QuestObjectiveCategory.UseInteractionPoint, "interaction-point.prototype.guild-counter", $"source.quest.{key}.report"));
        }

        private static QuestObjectiveStateFactData ObjectiveFact(QuestObjectiveCategory category, string targetId, int value)
        {
            return new QuestObjectiveStateFactData
            {
                category = category,
                target = new InformationSubjectReferenceData { subjectType = InformationSubjectType.Custom, subjectId = targetId },
                value = value
            };
        }

        private static QuestOfferRequest OfferRequest(string questId, string personId, QuestEligibilityContext context, string transactionId, bool preview = false)
        {
            return new QuestOfferRequest
            {
                transactionId = transactionId,
                questId = questId,
                recipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Person, recipientId = personId },
                institutionalIssuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Organization, issuerId = "organization.prototype.guild" },
                offeringProvider = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Organization, issuerId = "organization.prototype.guild", actingPersonId = "person.prototype.guild-clerk" },
                channel = QuestOfferChannel.GuildCounter,
                sourceInteractionPointId = "interaction-point.prototype.guild-counter",
                sourceLocationId = "location.prototype.adventurers-guild",
                authorityBasisId = "authority.prototype.guild.quest-offer",
                eligibilityContext = context,
                worldTime = 1d,
                preview = preview
            };
        }

        private static QuestEligibilityContext EligibleContext(string personId)
        {
            return new QuestEligibilityContext
            {
                personId = personId,
                locationId = "location.prototype.adventurers-guild",
                interactionPointId = "interaction-point.prototype.guild-counter",
                privilegedDiagnostics = true,
                worldTime = 1d,
                facts = new QuestEligibilityFactSet(
                    organizationMemberships: new[] { "organization.prototype.adventurers-guild" },
                    authorityGrants: new[] { "authority.prototype.guild.quest-offer" })
            };
        }

        private static QuestRuntimeOperationResult CreateGuildPosting(QuestRuntime runtime, string transaction, string questId)
        {
            return runtime.CreateQuest(new QuestCreateRequest
            {
                transactionId = $"tx.quest.{transaction}",
                questId = questId,
                questDefinitionId = PrototypeQuestDefinitionFactory.GuildPostingDefinitionId,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Organization, issuerId = "organization.prototype.guild" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Open },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.QuestBoard, locationId = "location.prototype.adventurers-guild", interactionPointId = "interaction-point.prototype.guild-counter" },
                subjectLinks = new[] { Subject("location.prototype.dungeon-entry", QuestSubjectRole.Location, InformationSubjectType.Location) },
                createdWorldTime = 10d
            });
        }

        private static QuestRuntimeOperationResult CreateDynamicBounty(QuestRuntime runtime, string transaction, string key)
        {
            return runtime.CreateQuest(new QuestCreateRequest
            {
                transactionId = $"tx.quest.{transaction}",
                questDefinitionId = PrototypeQuestDefinitionFactory.DynamicBountyDefinitionId,
                repeatInstanceKey = key,
                issuer = new QuestIssuerReferenceData { issuerType = QuestIssuerType.Organization, issuerId = "organization.prototype.guild" },
                intendedRecipient = new QuestRecipientReferenceData { recipientScope = QuestRecipientScope.Open },
                origin = new QuestOriginReferenceData { sourceChannel = QuestSourceChannel.QuestBoard, locationId = "location.prototype.adventurers-guild", interactionPointId = "interaction-point.prototype.guild-board" },
                subjectLinks = new[] { Subject($"encounter.prototype.{key}", QuestSubjectRole.Encounter, InformationSubjectType.Custom) },
                createdWorldTime = 12d,
                tagIds = new[] { "bounty", key }
            });
        }

        private static QuestSubjectLinkData Subject(string id, QuestSubjectRole role, InformationSubjectType type)
        {
            return new QuestSubjectLinkData
            {
                role = role,
                subject = new InformationSubjectReferenceData { subjectType = type, subjectId = id, tags = new[] { role.ToString().ToLowerInvariant() } },
                provenanceId = "prototype.quest.automation"
            };
        }

        private sealed class AutomationRewardExecutor : IQuestRewardEffectExecutor
        {
            public readonly List<QuestRewardEffectRequest> Requests = new List<QuestRewardEffectRequest>();

            public QuestRewardEffectResult Execute(QuestRewardEffectRequest request)
            {
                Requests.Add(request);
                return QuestRewardEffectResult.Success($"owner.{request.category.ToString().ToLowerInvariant()}", $"{request.category}.{request.targetDefinitionId}.{request.quantity}");
            }
        }
    }
}
#endif
