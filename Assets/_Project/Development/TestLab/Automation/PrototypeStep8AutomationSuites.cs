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
