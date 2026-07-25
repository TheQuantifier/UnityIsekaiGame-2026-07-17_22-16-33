#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;

namespace UnityIsekaiGame.Development.Automation
{
    public static class PrototypeStep8AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            TryRegister(registry, BuildKnowledgeSuite());
            TryRegister(registry, BuildObservationSuite());
            TryRegister(registry, BuildHistorySuite());
            TryRegister(registry, BuildMemorySuite());
            TryRegister(registry, BuildLifeEventsSuite());
            TryRegister(registry, BuildInformationSourcesSuite());
            TryRegister(registry, BuildInformationSharingSuite());
            TryRegister(registry, BuildInformationAccessSuite());
            TryRegister(registry, BuildKnowledgeRecordsSuite());
            TryRegister(registry, BuildKnowledgeHistoryIntegrationSuite());
            TryRegister(registry, BuildStep8MasterIntegrationSuite());
        }

        private static ITestLabAutomationSuite BuildKnowledgeSuite()
        {
            return Suite("feature.8.1.knowledge-facts-beliefs", "Feature 8.1 Knowledge, Facts, and Beliefs", "8.1", 810,
                Required("PersonKnowledgeRuntime", "KnowledgeFactDefinition", "KnowledgeObservationProjection"),
                Scenario("person-knowledge-ready", "Person Knowledge runtime is ready", 10,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-reset")),
                    Step("validate", "Validate Knowledge", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-ready"))),
                Scenario("person-starts-with-authored-baseline", "Only authored baseline Knowledge is present", 20,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-baseline-reset")),
                    Step("validate", "Validate baseline", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-baseline"))),
                Scenario("observation-preview-mutates-nothing", "Observation preview does not mutate", 30,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-preview-reset")),
                    Step("preview", "Preview visible injury", context => Operation(context.Service.PreviewKnowledgeVisibleInjury(), context, "step8-preview"))),
                Scenario("observation-creates-belief", "Observation creates evidence and belief", 40,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-reset")),
                    Step("record", "Record visible injury", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-observation-record"))),
                Scenario("duplicate-observation-idempotent", "Duplicate observation is idempotent", 50,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-duplicate-reset")),
                    Step("duplicate", "Duplicate observation", context => Operation(context.Service.ProveKnowledgeDuplicateObservation(), context, "step8-duplicate"))),
                Scenario("subthreshold-evidence-creates-suspicion", "Weak evidence creates suspicion", 60,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-weak-reset")),
                    Step("weak", "Add weak evidence", context => Operation(context.Service.AddWeakKnowledgeEvidence(), context, "step8-weak"))),
                Scenario("repeated-evidence-increases-confidence", "Distinct evidence increases confidence deterministically", 70,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-repeat-reset")),
                    Step("weak", "Add weak evidence", context => Operation(context.Service.AddWeakKnowledgeEvidence(), context, "step8-repeat-weak")),
                    Step("strong", "Add strong evidence", context => Operation(context.Service.AddStrongKnowledgeEvidence(), context, "step8-repeat-strong"))),
                Scenario("conflicting-evidence-creates-dispute", "Opposing evidence creates dispute", 80,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-conflict-reset")),
                    Step("support", "Add strong evidence", context => Operation(context.Service.AddStrongKnowledgeEvidence(), context, "step8-conflict-support")),
                    Step("oppose", "Add opposing evidence", context => Operation(context.Service.AddOpposingKnowledgeEvidence(), context, "step8-conflict-oppose"))),
                Scenario("high-confidence-belief-can-be-wrong", "High-confidence misconception does not alter truth", 90,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-misconception-reset")),
                    Step("misconception", "Create misconception", context => Operation(context.Service.CreateKnowledgeMisconception(), context, "step8-misconception"))),
                Scenario("authoritative-correction-revises-belief", "Authorized correction revises belief", 100,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-correction-reset")),
                    Step("misconception", "Create misconception", context => Operation(context.Service.CreateKnowledgeMisconception(), context, "step8-correction-misconception")),
                    Step("correct", "Correct belief", context => Operation(context.Service.CorrectKnowledgeMisconception(), context, "step8-correction"))),
                Scenario("testimony-is-not-direct-observation", "Shared belief becomes testimony evidence", 110,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-testimony-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-testimony-record")),
                    Step("share", "Share belief", context => Operation(context.Service.ShareFirstKnowledgeBelief(), context, "step8-testimony-share"))),
                Scenario("source-credibility-affects-confidence", "Credibility changes confidence", 120,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-credibility-reset")),
                    Step("weak", "Weak source", context => Operation(context.Service.AddWeakKnowledgeEvidence(), context, "step8-credibility-weak")),
                    Step("strong", "Strong source", context => Operation(context.Service.AddStrongKnowledgeEvidence(), context, "step8-credibility-strong"))),
                Scenario("visible-injury-observation-limited", "Visible injury does not expose hidden internals", 130,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-visible-reset")),
                    Step("record", "Record visible injury", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-visible"))),
                Scenario("symptom-does-not-equal-diagnosis", "Symptom and diagnosis remain separate", 140,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-symptom-reset")),
                    Step("weak", "Add symptom-like weak evidence", context => Operation(context.Service.AddWeakKnowledgeEvidence(), context, "step8-symptom"))),
                Scenario("species-capability-discovery", "Species capability discovery updates belief", 150,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-species-reset")),
                    Step("strong", "Add species capability evidence", context => Operation(context.Service.AddStrongKnowledgeEvidence(), context, "step8-species"))),
                Scenario("false-species-rumor", "False species rumor can create misconception", 160,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-rumor-reset")),
                    Step("rumor", "Create false species rumor", context => Operation(context.Service.CreateKnowledgeMisconception(), context, "step8-rumor"))),
                Scenario("body-replacement-preserves-person-knowledge", "Body replacement preserves Person Knowledge", 170,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-body-preserve-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-body-preserve-record")),
                    Step("construct", "Assign Construct body", context => Operation(context.Service.AssignBodySpecies("species.basic-construct"), context, "step8-body-preserve-construct")),
                    Step("validate", "Validate Knowledge", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-body-preserve-validate"))),
                Scenario("body-specific-belief-becomes-stale", "Body-specific belief can become stale", 180,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-stale-body-reset")),
                    Step("record", "Record body belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-stale-body-record")),
                    Step("stale", "Mark stale", context => Operation(context.Service.MarkFirstKnowledgeStale(), context, "step8-stale-body"))),
                Scenario("previous-body-history-retained", "Previous body history can be retained", 190,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-history-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-history-record")),
                    Step("validate", "Validate history-capable Knowledge", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-history"))),
                Scenario("knowledge-does-not-transfer-between-persons", "Knowledge does not transfer automatically", 200,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-isolation-reset")),
                    Step("record", "Record player belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-isolation-record")),
                    Step("validate", "Validate player-only Knowledge", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-isolation"))),
                Scenario("share-belief-creates-listener-evidence", "Sharing creates listener evidence", 210,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-share-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-share-record")),
                    Step("share", "Share belief", context => Operation(context.Service.ShareFirstKnowledgeBelief(), context, "step8-share"))),
                Scenario("private-fact-blocked", "Diagnostic-only fact is blocked", 220,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-private-reset")),
                    Step("blocked", "Attempt private diagnostic observation", context => Operation(context.Service.AttemptPrivateDiagnosticKnowledgeObservation(), context, "step8-private"))),
                Scenario("development-truth-comparison-separate", "Development truth comparison remains separate", 230,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-truth-reset")),
                    Step("misconception", "Use authorized development fixture", context => Operation(context.Service.CreateKnowledgeMisconception(), context, "step8-truth"))),
                Scenario("forgetting-reduces-or-removes-active-belief", "Forgetting changes active belief", 240,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-forget-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-forget-record")),
                    Step("forget", "Forget belief", context => Operation(context.Service.ForgetFirstKnowledgeBelief(), context, "step8-forget"))),
                Scenario("stale-belief-not-deleted", "Stale belief remains queryable", 250,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-stale-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-stale-record")),
                    Step("stale", "Mark stale", context => Operation(context.Service.MarkFirstKnowledgeStale(), context, "step8-stale"))),
                Scenario("snapshot-read-only", "Snapshot creation is read-only", 260,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-snapshot-reset")),
                    Step("validate", "Validate snapshot-ready Knowledge", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-snapshot"))),
                Scenario("snapshot-immutable", "Snapshot collections are immutable", 270,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-immutable-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-immutable-record")),
                    Step("validate", "Validate immutable snapshot boundary", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-immutable"))),
                Scenario("save-restore-preserves-beliefs", "Save and restore preserves beliefs", 280,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-save-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-save-record")),
                    Step("save-restore", "Save restore Knowledge", context => Operation(context.Service.ValidateKnowledgeSaveRestore(), context, "step8-save"))),
                Scenario("restore-no-discovery-replay", "Restore emits no discovery replay", 290,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-restore-events-reset")),
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-restore-events-record")),
                    Step("save-restore", "Save restore without events", context => Operation(context.Service.ValidateKnowledgeSaveRestore(), context, "step8-restore-events"))),
                Scenario("replacement-body-isolation", "Body-bound observations stay with owning Person", 300,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-replacement-reset")),
                    Step("record", "Record body observation", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-replacement-record")),
                    Step("validate", "Validate Person isolation", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-replacement"))),
                Scenario("automation-reset-knowledge", "Automation reset restores canonical Knowledge", 310,
                    Step("record", "Record belief", context => Operation(context.Service.RecordKnowledgeVisibleInjury(), context, "step8-auto-reset-record")),
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-auto-reset")),
                    Step("validate", "Validate reset", context => Operation(context.Service.ValidateKnowledgeRuntime(), context, "step8-auto-reset-validate"))));
        }

        private static ITestLabAutomationSuite BuildObservationSuite()
        {
            return Suite("feature.8.2.observation-examination-identification-diagnosis", "Feature 8.2 Observation, Examination, Identification, and Diagnosis", "8.2", 820,
                Required("ObservationService", "ObservationMethodDefinition", "ExaminationMethodDefinition", "IdentificationMethodDefinition", "DiagnosticMethodDefinition"),
                Scenario("foundation-validates", "Observation definitions and service are ready", 10,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-foundation-reset")),
                    Step("validate", "Validate Observation", context => Operation(context.Service.ValidateObservationFoundation(), context, "step8-observation-foundation"))),
                Scenario("preview-visual-no-mutation", "Visual observation preview does not mutate Knowledge", 20,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-preview-reset")),
                    Step("preview", "Preview visual observation", context => Operation(context.Service.PreviewOrdinaryVisualObservation(), context, "step8-observation-preview"))),
                Scenario("commit-visual-records-evidence", "Visual observation records evidence", 30,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-commit-reset")),
                    Step("record", "Record visual observation", context => Operation(context.Service.CommitOrdinaryVisualObservation(), context, "step8-observation-commit"))),
                Scenario("duplicate-observation-idempotent", "Duplicate observation transaction is idempotent", 40,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-duplicate-reset")),
                    Step("duplicate", "Duplicate observation", context => Operation(context.Service.ProveObservationDuplicateProtection(), context, "step8-observation-duplicate"))),
                Scenario("medical-examination-stronger-evidence", "Medical examination records higher-quality evidence", 50,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-medical-reset")),
                    Step("medical", "Medical examination", context => Operation(context.Service.CommitMedicalExaminationObservation(), context, "step8-observation-medical"))),
                Scenario("diagnosis-produces-differential", "Diagnosis produces a differential hypothesis", 60,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-diagnosis-reset")),
                    Step("diagnose", "Diagnose biological condition", context => Operation(context.Service.DiagnoseBiologicalConditionFoundation(), context, "step8-observation-diagnosis"))),
                Scenario("player-irrelevant-not-tracked", "Player-irrelevant observation is not tracked", 70,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-filter-reset")),
                    Step("filter", "Player irrelevant observation", context => Operation(context.Service.ProvePlayerIrrelevantObservationNotTracked(), context, "step8-observation-filter"))),
                Scenario("npc-full-tracking-records", "NPC full tracking records relevant observations", 80,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-npc-reset")),
                    Step("npc", "NPC full observation", context => Operation(context.Service.ProveNpcFullObservationTracks(), context, "step8-observation-npc"))),
                Scenario("remote-player-irrelevant-not-tracked", "Remote player irrelevant observation is not tracked", 90,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-remote-reset")),
                    Step("remote", "Remote player irrelevant observation", context => Operation(context.Service.ProveRemotePlayerIrrelevantObservationNotTracked(), context, "step8-observation-remote"))),
                Scenario("development-observer-no-mutation", "Development observer does not mutate Knowledge", 100,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-dev-reset")),
                    Step("dev", "Development observer", context => Operation(context.Service.ProveDevelopmentObserverDoesNotMutate(), context, "step8-observation-dev"))),
                Scenario("repeated-observation-bounded", "Repeated identical observations are bounded", 110,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-repeat-reset")),
                    Step("repeat", "Repeated observation bound", context => Operation(context.Service.ProveRepeatedObservationIsBounded(), context, "step8-observation-repeat"))),
                Scenario("stale-projection-rejected", "Stale observation projection is rejected", 120,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-stale-reset")),
                    Step("stale", "Reject stale projection", context => Operation(context.Service.RejectStaleObservationProjection(), context, "step8-observation-stale"))),
                Scenario("inactive-foundation-method-rejected", "Inactive foundation methods do not execute", 130,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-inactive-reset")),
                    Step("inactive", "Reject inactive foundation", context => Operation(context.Service.RejectInactiveFoundationObservationMethod(), context, "step8-observation-inactive"))),
                Scenario("concealment-lowers-quality", "Concealment reduces observation quality", 140,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-conceal-reset")),
                    Step("conceal", "Concealment quality", context => Operation(context.Service.ProveConcealmentLowersObservationQuality(), context, "step8-observation-conceal"))),
                Scenario("private-medical-without-access-rejected", "Private medical observation requires access", 150,
                    Step("reset", "Reset Knowledge", context => Operation(context.Service.ResetKnowledgeFixture(), context, "step8-observation-private-reset")),
                    Step("reject", "Reject private observation", context => Operation(context.Service.RejectPrivateMedicalObservationWithoutAccess(), context, "step8-observation-private"))));
        }

        private static ITestLabAutomationSuite BuildHistorySuite()
        {
            return Suite("feature.8.3.character-history-memory-timelines", "Feature 8.3 Character History, Memory, and Timelines", "8.3", 830,
                Required("AuthoritativeHistoryRuntime", "PersonMemoryRuntime", "HistoricalEventDefinition", "PersonKnowledgeRuntime"),
                Scenario("foundation-validates", "History definitions and runtimes are ready", 10,
                    Step("validate", "Validate History", context => Operation(context.Service.ValidateHistoryFoundation(), context, "step8-history-foundation"))),
                Scenario("authoritative-event-records", "Authoritative history records a Person event", 20,
                    Step("record", "Record authoritative event", context => Operation(context.Service.RecordAuthoritativeHistoryEvent(), context, "step8-history-record"))),
                Scenario("hidden-history-privacy", "Hidden history does not leak to uninformed Person", 30,
                    Step("hidden", "Record hidden event", context => Operation(context.Service.RecordHiddenHistoryEvent(), context, "step8-history-hidden")),
                    Step("privacy", "Verify privacy", context => Operation(context.Service.ProveUninformedPersonCannotQueryHiddenHistory(), context, "step8-history-privacy"))),
                Scenario("memory-and-testimony", "Witness memory and testimony create Person-owned records", 40,
                    Step("memory", "Form witness memory", context => Operation(context.Service.FormWitnessHistoryMemory(), context, "step8-history-memory")),
                    Step("testimony", "Share testimony", context => Operation(context.Service.ShareHistoricalTestimony(), context, "step8-history-testimony"))),
                Scenario("correction-belief-revision", "Correction preserves authoritative history and revises belief by evidence", 50,
                    Step("false-belief", "Create false belief", context => Operation(context.Service.CreateIncorrectHistoricalBelief(), context, "step8-history-false-belief")),
                    Step("correct-event", "Correct authoritative event", context => Operation(context.Service.CorrectAuthoritativeHistoryEvent(), context, "step8-history-correct-event")),
                    Step("revise-belief", "Revise historical belief", context => Operation(context.Service.ReviseHistoricalBeliefWithEvidence(), context, "step8-history-revise-belief"))),
                Scenario("memory-forgetting-preserves-history", "Forgetting memory preserves authoritative history", 60,
                    Step("memory", "Form witness memory", context => Operation(context.Service.FormWitnessHistoryMemory(), context, "step8-history-forget-memory")),
                    Step("forget", "Forget memory", context => Operation(context.Service.ForgetFirstHistoryMemory(), context, "step8-history-forget"))),
                Scenario("body-continuity", "Persistent Person history spans bodies", 70,
                    Step("transition", "Record body transition", context => Operation(context.Service.RecordBodyTransitionHistory(), context, "step8-history-body-transition"))),
                Scenario("compare-authoritative-known-remembered", "Compare authoritative, known, and remembered views", 80,
                    Step("compare", "Compare views", context => Operation(context.Service.CompareHistoryKnowledgeMemoryViews(), context, "step8-history-compare"))),
                Scenario("save-restore-round-trip", "History and memory save/restore preserves state silently", 90,
                    Step("save-restore", "Validate save restore", context => Operation(context.Service.ValidateHistorySaveRestore(), context, "step8-history-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildMemorySuite()
        {
            return Suite("feature.8.4.memory-recall-forgetting-alteration", "Feature 8.4 Memory Recall, Forgetting, and Alteration", "8.4", 840,
                Required("PersonMemoryRuntime", "AuthoritativeHistoryRuntime", "PersonKnowledgeRuntime"),
                Scenario("foundation-validates", "Memory 8.4 runtime data validates", 10,
                    Step("validate", "Validate Memory", context => Operation(context.Service.ValidateMemory84(), context, "step8-memory-validate"))),
                Scenario("recall-accessible-memory", "Accessible event-linked memory can be recalled", 20,
                    Step("recall", "Recall memory", context => Operation(context.Service.RecallPrototypeMemory(), context, "step8-memory-recall"))),
                Scenario("recall-by-subject-and-cue", "Subject and cue recall are deterministic", 30,
                    Step("subject", "Recall by subject", context => Operation(context.Service.RecallPrototypeMemoryBySubject(), context, "step8-memory-subject")),
                    Step("cue", "Recall with cue", context => Operation(context.Service.RecallPrototypeMemoryWithCue(), context, "step8-memory-cue"))),
                Scenario("reinforcement-is-not-truth", "False memory can be reinforced without changing history", 40,
                    Step("false-reinforce", "Reinforce false memory", context => Operation(context.Service.ReinforceFalsePrototypeMemory(), context, "step8-memory-false-reinforce"))),
                Scenario("degradation-and-difficulty", "Clarity/confidence reduction changes accessibility", 50,
                    Step("idempotence", "Prove degradation idempotence", context => Operation(context.Service.ProveMemoryDegradationIdempotence(), context, "step8-memory-degrade-idempotence")),
                    Step("clarity", "Reduce clarity", context => Operation(context.Service.ReduceMemoryClarity(), context, "step8-memory-clarity")),
                    Step("difficulty", "Make difficult", context => Operation(context.Service.MakeMemoryDifficult(), context, "step8-memory-difficult"))),
                Scenario("partial-forgetting", "Participant, time, and location details can be unavailable", 60,
                    Step("participant", "Forget participant", context => Operation(context.Service.ForgetMemoryParticipant(), context, "step8-memory-forget-participant")),
                    Step("time-location", "Forget time or location", context => Operation(context.Service.ForgetMemoryTimeOrLocation(), context, "step8-memory-forget-time-location"))),
                Scenario("suppression-stacking-and-removal", "Suppression blocks recall until removed or expired", 70,
                    Step("stacking", "Prove suppression stacking", context => Operation(context.Service.ProveMemorySuppressionStacking(), context, "step8-memory-suppression-stacking")),
                    Step("suppress", "Add suppression", context => Operation(context.Service.AddMemorySuppression(), context, "step8-memory-suppress")),
                    Step("remove", "Remove suppression", context => Operation(context.Service.RemoveMemorySuppression(), context, "step8-memory-remove-suppression")),
                    Step("expire", "Expire suppression", context => Operation(context.Service.ExpireMemorySuppression(), context, "step8-memory-expire-suppression"))),
                Scenario("recovery-and-alteration", "Recovery, alteration, and correction preserve revisions", 80,
                    Step("recover", "Recover memory", context => Operation(context.Service.RecoverPrototypeMemory(), context, "step8-memory-recover")),
                    Step("alter", "Alter memory", context => Operation(context.Service.AlterPrototypeMemory(), context, "step8-memory-alter")),
                    Step("correct", "Correct altered memory", context => Operation(context.Service.CorrectAlteredMemory(), context, "step8-memory-correct")),
                    Step("revisions", "Show revisions", context => Operation(context.Service.ShowMemoryRevisionHistory(), context, "step8-memory-revisions"))),
                Scenario("conflicting-memories", "Multiple conflicting memories remain separate", 90,
                    Step("conflicts", "Create conflicts", context => Operation(context.Service.CreateConflictingMemories(), context, "step8-memory-conflicts"))),
                Scenario("previous-body-accessibility", "Previous-body association can be suppressed and recovered", 100,
                    Step("suppress-body", "Suppress previous body", context => Operation(context.Service.SuppressPreviousBodyAssociation(), context, "step8-memory-suppress-body")),
                    Step("recover-body", "Recover previous body", context => Operation(context.Service.RecoverPreviousBodyAssociation(), context, "step8-memory-recover-body"))),
                Scenario("compare-and-save-restore", "Memory, belief, history, and persistence stay separated", 110,
                    Step("compare", "Compare views", context => Operation(context.Service.CompareMemoryBeliefHistory(), context, "step8-memory-compare")),
                    Step("save-restore", "Save restore", context => Operation(context.Service.ValidateMemory84SaveRestore(), context, "step8-memory-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildLifeEventsSuite()
        {
            return Suite("feature.8.5.character-history-life-events", "Feature 8.5 Character History and Life Events", "8.5", 850,
                Required("AuthoritativeHistoryRuntime", "PersonMemoryRuntime", "HistoricalEventDefinition", "PersonKnowledgeRuntime"),
                Scenario("foundation-validates", "Life Event definitions and runtimes are ready", 10,
                    Step("validate", "Validate Life Events", context => Operation(context.Service.ValidateLifeEventDefinitions(), context, "step8-life-events-foundation"))),
                Scenario("birth-discovery-title", "Birth, discovery, role, and title events record as canonical history", 20,
                    Step("birth", "Record birth", context => Operation(context.Service.RecordLifeEventBirthOrCreation(), context, "step8-life-birth")),
                    Step("discovery", "Record discovery", context => Operation(context.Service.RecordLifeEventDiscovery(), context, "step8-life-discovery")),
                    Step("role", "Record role", context => Operation(context.Service.RecordLifeEventRoleAppointment(), context, "step8-life-role")),
                    Step("title", "Record title", context => Operation(context.Service.RecordLifeEventTitleGrant(), context, "step8-life-title"))),
                Scenario("affiliation-battle-injury-sequence", "Affiliation, battle, injury, recovery, and sequence links are queryable", 30,
                    Step("affiliation", "Record affiliation", context => Operation(context.Service.RecordLifeEventAffiliationChange(), context, "step8-life-affiliation")),
                    Step("battle", "Record battle", context => Operation(context.Service.RecordLifeEventBattleParticipation(), context, "step8-life-battle")),
                    Step("injury", "Record injury", context => Operation(context.Service.RecordLifeEventMajorInjury(), context, "step8-life-injury")),
                    Step("recovery", "Record recovery", context => Operation(context.Service.RecordLifeEventRecovery(), context, "step8-life-recovery")),
                    Step("sequence", "Create sequence", context => Operation(context.Service.CreateLifeEventSequence(), context, "step8-life-sequence")),
                    Step("link", "Link cause and consequence", context => Operation(context.Service.LinkLifeEventCauseAndConsequence(), context, "step8-life-link"))),
                Scenario("privacy-biography-views", "Public, authoritative, known, and remembered biography views remain distinct", 40,
                    Step("crime", "Record hidden crime", context => Operation(context.Service.RecordLifeEventCrimeOrAccusation(), context, "step8-life-crime")),
                    Step("public", "Show public biography", context => Operation(context.Service.ShowLifeEventPublicBiography(), context, "step8-life-public")),
                    Step("authoritative", "Show authoritative biography", context => Operation(context.Service.ShowLifeEventAuthoritativeBiography(), context, "step8-life-authoritative")),
                    Step("known", "Show known biography", context => Operation(context.Service.ShowLifeEventPersonKnownBiography(), context, "step8-life-known")),
                    Step("remembered", "Show remembered biography", context => Operation(context.Service.ShowLifeEventPersonRememberedBiography(), context, "step8-life-remembered"))),
                Scenario("death-return-correction-body-transition", "Death, presumed death, return, correction, and body transition stay linked", 50,
                    Step("death", "Record death", context => Operation(context.Service.RecordLifeEventDeath(), context, "step8-life-death")),
                    Step("presumed", "Record presumed death", context => Operation(context.Service.RecordLifeEventPresumedDeath(), context, "step8-life-presumed")),
                    Step("return", "Record return", context => Operation(context.Service.RecordLifeEventReturn(), context, "step8-life-return")),
                    Step("correct", "Correct presumed death", context => Operation(context.Service.CorrectLifeEventPresumedDeath(), context, "step8-life-correct")),
                    Step("body", "Record body transition", context => Operation(context.Service.RecordLifeEventBodyTransition(), context, "step8-life-body"))),
                Scenario("save-restore-round-trip", "Life events persist and restore without replaying current state", 60,
                    Step("save-restore", "Validate save restore", context => Operation(context.Service.ValidateLifeEventSaveRestore(), context, "step8-life-save-restore"))),
                Scenario("timeline-and-milestones", "Timeline and major milestone queries are available", 70,
                    Step("timeline", "Show timeline", context => Operation(context.Service.ShowLifeEventPersonTimeline(), context, "step8-life-timeline")),
                    Step("milestones", "Show milestones", context => Operation(context.Service.ShowLifeEventMajorMilestones(), context, "step8-life-milestones"))));
        }

        private static ITestLabAutomationSuite BuildInformationSourcesSuite()
        {
            return Suite("feature.8.6.information-sources-reliability", "Feature 8.6 Information Sources and Reliability", "8.6", 860,
                Required("InformationSourceRuntime", "InformationSourceDefinition", "PersonKnowledgeRuntime"),
                Scenario("foundation-validates", "Information Source definitions validate", 10,
                    Step("validate", "Validate source definitions", context => Operation(context.Service.ValidateInformationSourceDefinitions(), context, "step8-sources-validate"))),
                Scenario("register-source-categories", "Representative source categories register", 20,
                    Step("direct", "Register direct observation", context => Operation(context.Service.RegisterDirectObservationSource(), context, "step8-sources-direct")),
                    Step("expert", "Register expert testimony", context => Operation(context.Service.RegisterExpertSource(), context, "step8-sources-expert")),
                    Step("testimony", "Register testimony", context => Operation(context.Service.RegisterTestimonySource(), context, "step8-sources-testimony")),
                    Step("anonymous", "Register anonymous testimony", context => Operation(context.Service.RegisterAnonymousSource(), context, "step8-sources-anonymous")),
                    Step("official", "Register official record", context => Operation(context.Service.RegisterOfficialRecordSource(), context, "step8-sources-official"))),
                Scenario("person-relative-assessments", "Two Persons can assess the same source differently", 30,
                    Step("compare", "Compare person assessments", context => Operation(context.Service.CompareTwoPersonsSourceAssessments(), context, "step8-sources-person-relative"))),
                Scenario("trust-authority-bias-risk", "Trust, authority, bias, error, and deception are distinct", 40,
                    Step("trusted", "Mark trusted", context => Operation(context.Service.MarkSourceTrusted(), context, "step8-sources-trusted")),
                    Step("untrusted", "Mark untrusted", context => Operation(context.Service.MarkSourceUntrusted(), context, "step8-sources-untrusted")),
                    Step("authority", "Add authority", context => Operation(context.Service.AddSourceDomainAuthority(), context, "step8-sources-authority")),
                    Step("bias", "Add bias", context => Operation(context.Service.AddSourceBias(), context, "step8-sources-bias")),
                    Step("error", "Add error risk", context => Operation(context.Service.AddSourceErrorRisk(), context, "step8-sources-error")),
                    Step("deception", "Add deception risk", context => Operation(context.Service.AddSourceDeceptionRisk(), context, "step8-sources-deception"))),
                Scenario("transformations-and-chain", "Copies, translations, and summaries retain source chains", 50,
                    Step("copy", "Copy source", context => Operation(context.Service.CopySource(), context, "step8-sources-copy")),
                    Step("translate", "Translate source", context => Operation(context.Service.TranslateSource(), context, "step8-sources-translate")),
                    Step("summarize", "Summarize source", context => Operation(context.Service.SummarizeSource(), context, "step8-sources-summarize")),
                    Step("trace", "Trace chain", context => Operation(context.Service.TraceSourceChain(), context, "step8-sources-trace")),
                    Step("compare", "Compare immediate and original", context => Operation(context.Service.CompareImmediateAndOriginalSource(), context, "step8-sources-immediate-original"))),
                Scenario("age-staleness-reliability", "Age and staleness influence reliability deterministically", 60,
                    Step("age", "Age source", context => Operation(context.Service.AgeSource(), context, "step8-sources-age")),
                    Step("stale", "Evaluate staleness", context => Operation(context.Service.EvaluateSourceStaleness(), context, "step8-sources-stale")),
                    Step("evaluate", "Evaluate reliability", context => Operation(context.Service.EvaluateReliability(), context, "step8-sources-evaluate"))),
                Scenario("independence-and-corroboration", "Dependent reports and independent corroboration stay distinct", 70,
                    Step("dependent", "Dependent reports", context => Operation(context.Service.TestDependentReports(), context, "step8-sources-dependent")),
                    Step("independent", "Independent corroboration", context => Operation(context.Service.TestIndependentCorroboration(), context, "step8-sources-independent"))),
                Scenario("privacy-correction-evidence", "Privacy, corrections, and effective evidence are represented", 80,
                    Step("hide", "Hide original source", context => Operation(context.Service.HideOriginalSource(), context, "step8-sources-hide")),
                    Step("correct", "Correct assessment", context => Operation(context.Service.CorrectSourceAssessment(), context, "step8-sources-correct")),
                    Step("raw-effective", "Compare raw and effective evidence", context => Operation(context.Service.CompareRawAndEffectiveEvidenceStrength(), context, "step8-sources-raw-effective"))),
                Scenario("save-restore-round-trip", "Information Sources save and restore silently", 90,
                    Step("save-restore", "Validate save restore", context => Operation(context.Service.ValidateInformationSourceSaveRestore(), context, "step8-sources-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildInformationSharingSuite()
        {
            return Suite("feature.8.7.information-sharing-teaching", "Feature 8.7 Information Sharing and Teaching", "8.7", 870,
                Required("InformationTransferRuntime", "InformationTransferDefinition", "PersonKnowledgeRuntime", "PersonMemoryRuntime", "InformationSourceRuntime"),
                Scenario("foundation-validates", "Information Transfer definitions validate", 10,
                    Step("validate", "Validate transfer definitions", context => Operation(context.Service.ValidateInformationTransferDefinitions(), context, "step8-sharing-validate"))),
                Scenario("true-fact-transfer", "Known true fact transfers as testimony evidence", 20,
                    Step("share", "Share true fact", context => Operation(context.Service.ShareKnownTrueFact(), context, "step8-sharing-true"))),
                Scenario("false-belief-transfer", "False belief transfer requires explicit authorization", 30,
                    Step("share", "Share sincere false belief", context => Operation(context.Service.ShareSincereFalseBelief(), context, "step8-sharing-false"))),
                Scenario("recall-boundaries", "Recall-required sharing respects memory accessibility", 40,
                    Step("partial", "Share partially recalled event", context => Operation(context.Service.SharePartiallyRecalledEvent(), context, "step8-sharing-recall")),
                    Step("suppressed", "Reject suppressed memory", context => Operation(context.Service.AttemptSuppressedMemoryTransfer(), context, "step8-sharing-suppressed"))),
                Scenario("source-lineage-confidence", "Source lineage and inherited confidence affect recipient evidence", 50,
                    Step("direct", "Share direct observation", context => Operation(context.Service.ShareDirectObservation(), context, "step8-sharing-direct")),
                    Step("expert", "Share expert diagnosis", context => Operation(context.Service.ShareExpertDiagnosis(), context, "step8-sharing-expert")),
                    Step("confidence", "Compare inherited confidence", context => Operation(context.Service.CompareInheritedConfidenceByDomain(), context, "step8-sharing-confidence")),
                    Step("lineage", "Trace transfer lineage", context => Operation(context.Service.TraceTransferSourceLineage(), context, "step8-sharing-lineage"))),
                Scenario("teaching-and-demonstration", "Teaching creates knowledge and memory without granting skills", 60,
                    Step("concept", "Teach concept", context => Operation(context.Service.TeachSemanticConcept(), context, "step8-sharing-teach-concept")),
                    Step("procedure", "Teach procedure", context => Operation(context.Service.TeachProcedureReference(), context, "step8-sharing-teach-procedure")),
                    Step("demo", "Demonstrate procedure", context => Operation(context.Service.DemonstrateProcedure(), context, "step8-sharing-demo")),
                    Step("no-prereq", "Teach without prerequisites", context => Operation(context.Service.TeachWithoutPrerequisites(), context, "step8-sharing-no-prereq"))),
                Scenario("clarification-reshare-correction", "Clarification, resharing, distortion, omission, and correction stay linked", 70,
                    Step("clarify", "Clarify transfer", context => Operation(context.Service.ClarifyTransfer(), context, "step8-sharing-clarify")),
                    Step("reshare", "Reshare transfer", context => Operation(context.Service.ReshareTransfer(), context, "step8-sharing-reshare")),
                    Step("distort", "Reshare distorted version", context => Operation(context.Service.ReshareDistortedVersion(), context, "step8-sharing-distort")),
                    Step("omit", "Deliberately omit detail", context => Operation(context.Service.DeliberatelyOmitDetail(), context, "step8-sharing-omit")),
                    Step("correct", "Correct prior transfer", context => Operation(context.Service.CorrectPriorTransfer(), context, "step8-sharing-correct"))),
                Scenario("privacy-and-records", "Public, private, anonymous, and official transfers are represented", 80,
                    Step("anonymous", "Share anonymous information", context => Operation(context.Service.ShareAnonymousInformation(), context, "step8-sharing-anonymous")),
                    Step("official", "Read official record", context => Operation(context.Service.ReadOfficialRecord(), context, "step8-sharing-official")),
                    Step("summarize", "Copy and summarize", context => Operation(context.Service.CopyAndSummarizeTransferSource(), context, "step8-sharing-summary")),
                    Step("privacy", "Create scoped transfers", context => Operation(context.Service.CreatePublicPrivateRestrictedTransfers(), context, "step8-sharing-privacy"))),
                Scenario("save-restore-round-trip", "Information Transfer audit state saves and restores silently", 90,
                    Step("save-restore", "Validate save restore", context => Operation(context.Service.ValidateInformationTransferSaveRestore(), context, "step8-sharing-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildInformationAccessSuite()
        {
            return Suite("feature.8.8.secrets-visibility-information-access", "Feature 8.8 Secrets, Visibility, and Information Access", "8.8", 880,
                Required("InformationAccessRuntime", "InformationAccessPolicyDefinition", "PersonKnowledgeRuntime", "InformationSourceRuntime", "InformationTransferRuntime"),
                Scenario("foundation-validates", "Information Access definitions validate", 10,
                    Step("validate", "Validate access definitions", context => Operation(context.Service.ValidateInformationAccessDefinitions(), context, "step8-access-validate"))),
                Scenario("public-private-boundaries", "Public information is visible while private secrets require access", 20,
                    Step("public", "Public access", context => Operation(context.Service.CreatePublicInformationAccess(), context, "step8-access-public")),
                    Step("secret-deny", "Unauthorized secret denied", context => Operation(context.Service.CreatePrivateInformationAccess(), context, "step8-access-secret-deny"))),
                Scenario("grants-and-resharing", "Explicit grants control inspection, sharing, and resharing", 30,
                    Step("inspect", "Grant inspect access", context => Operation(context.Service.GrantInspectInformationAccess(), context, "step8-access-grant-inspect")),
                    Step("share", "Grant share access", context => Operation(context.Service.GrantShareInformationAccess(), context, "step8-access-grant-share")),
                    Step("no-reshare", "Limit resharing", context => Operation(context.Service.AttemptNoReshareInformationAccess(), context, "step8-access-no-reshare"))),
                Scenario("source-protection-and-existence", "Source identity and secret existence are protected separately", 40,
                    Step("hide-source", "Hide source identity", context => Operation(context.Service.ProtectInformationSourceIdentity(), context, "step8-access-hide-source")),
                    Step("reveal-source", "Reveal source identity", context => Operation(context.Service.RevealInformationSourceIdentity(), context, "step8-access-reveal-source")),
                    Step("hide-existence", "Hide existence", context => Operation(context.Service.HideSecretExistence(), context, "step8-access-hide-existence")),
                    Step("reveal-existence", "Reveal existence boundary", context => Operation(context.Service.RevealSecretExistence(), context, "step8-access-reveal-existence"))),
                Scenario("discovery-classification-audit", "Discovery, classification changes, and audit records are deterministic", 50,
                    Step("discover", "Discover hidden information", context => Operation(context.Service.DiscoverHiddenInformationAccess(), context, "step8-access-discover")),
                    Step("declassify", "Declassify information", context => Operation(context.Service.DeclassifyInformationAccess(), context, "step8-access-declassify")),
                    Step("audit", "Audit unauthorized access", context => Operation(context.Service.AttemptUnauthorizedInformationAccess(), context, "step8-access-audit"))),
                Scenario("projection-and-save-restore", "Redacted projections and persistence preserve access state", 60,
                    Step("projection", "Compare projections", context => Operation(context.Service.CompareInformationAccessProjections(), context, "step8-access-projection")),
                    Step("adapters", "Validate non-transfer projection adapters", context => Operation(context.Service.ValidateInformationAccessProjectionAdapters(), context, "step8-access-adapters")),
                    Step("save-restore", "Validate save restore", context => Operation(context.Service.ValidateInformationAccessSaveRestore(), context, "step8-access-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildKnowledgeRecordsSuite()
        {
            return Suite("feature.8.9.historical-records-journals-codex", "Feature 8.9 Historical Records, Journals, and Codex", "8.9", 890,
                Required("KnowledgeRecordRuntime", "KnowledgeRecordDefinition", "InformationAccessRuntime", "InformationSourceRuntime", "AuthoritativeHistoryRuntime", "PersonMemoryRuntime"),
                Scenario("foundation-validates", "Knowledge Record definitions validate", 10,
                    Step("validate", "Validate record definitions", context => Operation(context.Service.ValidateKnowledgeRecordDefinitions(), context, "step8-records-validate"))),
                Scenario("explicit-record-categories", "Representative explicit record categories can be created", 20,
                    Step("journal", "Create journal", context => Operation(context.Service.CreatePersonalJournalRecord(), context, "step8-records-journal")),
                    Step("history", "Create history", context => Operation(context.Service.CreateHistoricalArchiveRecord(), context, "step8-records-history")),
                    Step("biography", "Create biography", context => Operation(context.Service.CreateBiographyProjectionRecord(), context, "step8-records-biography")),
                    Step("bestiary", "Create bestiary", context => Operation(context.Service.CreateBestiaryRecord(), context, "step8-records-bestiary")),
                    Step("location", "Create location", context => Operation(context.Service.CreateLocationRecord(), context, "step8-records-location"))),
                Scenario("specialized-records", "Medical and investigation records remain explicit and typed", 30,
                    Step("medical", "Create medical", context => Operation(context.Service.CreateMedicalRecord(), context, "step8-records-medical")),
                    Step("investigation", "Create investigation", context => Operation(context.Service.CreateInvestigationRecord(), context, "step8-records-investigation"))),
                Scenario("projection-boundaries", "Live projections preview without mutating owning systems", 40,
                    Step("projection", "Validate projection boundaries", context => Operation(context.Service.ValidateKnowledgeRecordLiveProjectionBoundaries(), context, "step8-records-projection"))),
                Scenario("access-aware-read", "Record reading applies authorized source, evidence, and memory effects", 50,
                    Step("owner", "Owner read effects", context => Operation(context.Service.ReadKnowledgeRecordAsOwner(), context, "step8-records-owner-read")),
                    Step("deny", "Unauthorized denied", context => Operation(context.Service.AttemptUnauthorizedKnowledgeRecordRead(), context, "step8-records-deny-read"))),
                Scenario("correction-and-collection", "Corrections and collections preserve original records", 60,
                    Step("correct", "Correct record", context => Operation(context.Service.CreateCorrectedKnowledgeRecord(), context, "step8-records-correct")),
                    Step("collection", "Create collection", context => Operation(context.Service.CreateKnowledgeRecordCollection(), context, "step8-records-collection"))),
                Scenario("search-and-save-restore", "Search and persistence preserve records deterministically", 70,
                    Step("search", "Search records", context => Operation(context.Service.SearchKnowledgeRecords(), context, "step8-records-search")),
                    Step("save-restore", "Save restore", context => Operation(context.Service.ValidateKnowledgeRecordSaveRestore(), context, "step8-records-save-restore"))));
        }

        private static ITestLabAutomationSuite BuildKnowledgeHistoryIntegrationSuite()
        {
            return Suite("feature.8.10.knowledge-history-integration", "Feature 8.10 Knowledge and History Integration", "8.10", 900,
                Required("KnowledgeHistoryFacade", "PersonKnowledgeRuntime", "AuthoritativeHistoryRuntime", "PersonMemoryRuntime", "InformationAccessRuntime", "KnowledgeRecordRuntime"),
                Scenario("readiness-and-validation", "Integrated Step 8 readiness and validation pass", 10,
                    Step("prepare", "Prepare integration fixtures", context => Operation(context.Service.PrepareKnowledgeHistoryIntegrationFixtures(), context, "step8-integration-prepare")),
                    Step("readiness", "Validate readiness snapshot", context => Operation(context.Service.ValidateKnowledgeHistoryReadiness(), context, "step8-integration-readiness")),
                    Step("validate", "Validate integrated state", context => Operation(context.Service.ValidateKnowledgeHistoryIntegration(), context, "step8-integration-validate"))),
                Scenario("definition-fallbacks-and-save-graph", "Fallback diagnostics and persistence graph are explicit", 20,
                    Step("prepare", "Prepare integration fixtures", context => Operation(context.Service.PrepareKnowledgeHistoryIntegrationFixtures(), context, "step8-integration-prepare")),
                    Step("fallbacks", "Show fallback diagnostics", context => Operation(context.Service.ShowKnowledgeHistoryFallbackDiagnostics(), context, "step8-integration-fallbacks")),
                    Step("save-graph", "Validate save capture graph", context => Operation(context.Service.ValidateKnowledgeHistorySaveCapture(), context, "step8-integration-save-graph"))),
                Scenario("facade-workflows", "Facade routes representative workflows through owning runtimes", 30,
                    Step("prepare", "Prepare integration fixtures", context => Operation(context.Service.PrepareKnowledgeHistoryIntegrationFixtures(), context, "step8-integration-prepare")),
                    Step("discovery", "Run discovery flow", context => Operation(context.Service.RunKnowledgeHistoryDiscoveryFlow(), context, "step8-integration-discovery")),
                    Step("event-memory", "Run event and memory flow", context => Operation(context.Service.RunKnowledgeHistoryEventMemoryFlow(), context, "step8-integration-event-memory")),
                    Step("record-read", "Run record reading flow", context => Operation(context.Service.RunKnowledgeHistoryRecordReadingFlow(), context, "step8-integration-record-read"))),
                Scenario("access-and-step9-contracts", "Access projection remains enforced and Step 9 contracts are present", 40,
                    Step("prepare", "Prepare integration fixtures", context => Operation(context.Service.PrepareKnowledgeHistoryIntegrationFixtures(), context, "step8-integration-prepare")),
                    Step("access", "Run access projection", context => Operation(context.Service.RunKnowledgeHistoryAccessProjectionFlow(), context, "step8-integration-access")),
                    Step("step9", "Preview Step 9 contracts", context => Operation(context.Service.PreviewStep9KnowledgeContracts(), context, "step8-integration-step9"))));
        }

        private static ITestLabAutomationSuite BuildStep8MasterIntegrationSuite()
        {
            return new TestLabAutomationSuite(
                "step.8.knowledge-history-integration",
                "Step 8 Knowledge and History Integration Master",
                "Step 8",
                "Runs Feature 8.1 through 8.10 in dependency order, then performs final Step 8 integration hardening checks.",
                910,
                TestLabAutomationCategory.Standard,
                includeInRunAll: false,
                requiredServices: Required("PersonKnowledgeRuntime", "AuthoritativeHistoryRuntime", "PersonMemoryRuntime", "InformationSourceRuntime", "InformationTransferRuntime", "InformationAccessRuntime", "KnowledgeRecordRuntime", "KnowledgeHistoryFacade"),
                scenarios: new[]
                {
                    Scenario("feature-suites-in-order", "Run Step 8 feature suites in dependency order", 10,
                        Step("feature-8-1", "Run Feature 8.1 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.1.knowledge-facts-beliefs", stopOnFirstFailure: true), context, "step8-master-8-1")),
                        Step("feature-8-2", "Run Feature 8.2 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.2.observation-examination-identification-diagnosis", stopOnFirstFailure: true), context, "step8-master-8-2")),
                        Step("feature-8-3", "Run Feature 8.3 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.3.character-history-memory-timelines", stopOnFirstFailure: true), context, "step8-master-8-3")),
                        Step("feature-8-4", "Run Feature 8.4 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.4.memory-recall-forgetting-alteration", stopOnFirstFailure: true), context, "step8-master-8-4")),
                        Step("feature-8-5", "Run Feature 8.5 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.5.character-history-life-events", stopOnFirstFailure: true), context, "step8-master-8-5")),
                        Step("feature-8-6", "Run Feature 8.6 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.6.information-sources-reliability", stopOnFirstFailure: true), context, "step8-master-8-6")),
                        Step("feature-8-7", "Run Feature 8.7 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.7.information-sharing-teaching", stopOnFirstFailure: true), context, "step8-master-8-7")),
                        Step("feature-8-8", "Run Feature 8.8 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.8.secrets-visibility-information-access", stopOnFirstFailure: true), context, "step8-master-8-8")),
                        Step("feature-8-9", "Run Feature 8.9 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.9.historical-records-journals-codex", stopOnFirstFailure: true), context, "step8-master-8-9")),
                        Step("feature-8-10", "Run Feature 8.10 suite", context => Operation(context.Service.RunAutomationSuite("feature.8.10.knowledge-history-integration", stopOnFirstFailure: true), context, "step8-master-8-10"))),
                    Scenario("final-hardening", "Run final cross-runtime integration hardening checks", 20,
                        Step("prepare", "Prepare integration fixtures", context => Operation(context.Service.PrepareKnowledgeHistoryIntegrationFixtures(), context, "step8-master-prepare")),
                        Step("cross-runtime", "Cross-runtime workflows", context => Operation(context.Service.RunKnowledgeHistoryEventMemoryFlow(), context, "step8-master-cross-runtime")),
                        Step("save-capture", "Full save capture graph", context => Operation(context.Service.ValidateKnowledgeHistorySaveCapture(), context, "step8-master-save-capture")),
                        Step("save-restore", "Full save restore", context => Operation(context.Service.ValidateKnowledgeHistoryFullSaveRestore(), context, "step8-master-save-restore")),
                        Step("corrupt-restore", "Corrupt restore rollback", context => Operation(context.Service.ValidateKnowledgeHistoryCorruptRestoreRollback(), context, "step8-master-corrupt-restore")),
                        Step("definitions", "Definition registry consistency", context => Operation(context.Service.ShowKnowledgeHistoryFallbackDiagnostics(), context, "step8-master-definitions")),
                        Step("access", "Access and redaction safety", context => Operation(context.Service.ValidateKnowledgeHistoryAccessSafety(), context, "step8-master-access")),
                        Step("snapshot", "Snapshot immutability", context => Operation(context.Service.ValidateKnowledgeHistorySnapshotImmutability(), context, "step8-master-snapshot")),
                        Step("ordering", "Deterministic ordering", context => Operation(context.Service.ValidateKnowledgeHistoryDeterministicOrdering(), context, "step8-master-ordering")),
                        Step("dirty-events", "Dirty state and restore event behavior", context => Operation(context.Service.ValidateKnowledgeHistoryDirtyAndEventBoundaries(), context, "step8-master-dirty-events"))),
                    Scenario("step9-contracts", "Step 9 contracts remain present and non-authoritative", 30,
                        Step("contracts", "Preview Step 9 contracts", context => Operation(context.Service.PreviewStep9KnowledgeContracts(), context, "step8-master-step9")))
                });
        }

        private static ITestLabAutomationSuite Suite(string suiteId, string displayName, string feature, int order, System.Collections.Generic.IReadOnlyList<string> required, params ITestLabAutomationScenario[] scenarios)
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

        private static System.Collections.Generic.IReadOnlyList<string> Required(params string[] services)
        {
            return services.ToArray();
        }

        private static TestLabAutomationStepResult Operation(PrototypeTestLabOperation operation, TestLabAutomationContext context, string operationId)
        {
            string transactionId = context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, operationId);
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
