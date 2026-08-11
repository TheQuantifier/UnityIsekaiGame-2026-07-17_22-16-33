#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(15, "Quests", 1500)]
    public static class PrototypeStep15AutomationSuites
    {
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
                requiredDefinitionIds: PrototypeQuestDefinitionFactory.PrototypeDefinitionIds);
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

        private static DefinitionRegistry Registry(TestLabAutomationContext context)
        {
            DefinitionRegistry baseRegistry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            return PrototypeQuestDefinitionFactory.AddMissingPrototypeQuestDefinitions(baseRegistry);
        }

        private static QuestRuntime Runtime(TestLabAutomationContext context)
        {
            return new QuestRuntime(Registry(context), PersistenceService.LocalWorldId);
        }

        private static QuestParticipationRuntime Participation(QuestRuntime quests, DefinitionRegistry registry)
        {
            return new QuestParticipationRuntime(quests, registry, PersistenceService.LocalWorldId);
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
    }
}
#endif
