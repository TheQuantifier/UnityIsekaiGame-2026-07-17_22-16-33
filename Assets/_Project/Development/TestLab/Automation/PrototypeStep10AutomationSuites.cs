#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Knowledge;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Knowledge.History;
using UnityIsekaiGame.Knowledge.Sharing;
using UnityIsekaiGame.Professions;
using UnityEngine;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(10, "Professions", 1000)]
    public static class PrototypeStep10AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.TryRegister(BuildProfessionIdentitySuite(), out _);
            registry.TryRegister(BuildProfessionalEligibilityEntrySuite(), out _);
            registry.TryRegister(BuildEducationTrainingApprenticeshipSuite(), out _);
            registry.TryRegister(BuildProfessionalActivityExperienceSuite(), out _);
        }

        private static ITestLabAutomationSuite BuildProfessionIdentitySuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.1.profession-identity-definitions",
                "Feature 10.1 Profession Identity and Definitions",
                "10.1",
                "Profession definition, specialization, relationship, access, persistence, and boundary automation.",
                1010,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "PersonProfessionRuntime", "ProfessionDefinition", "ProfessionSpecializationDefinition", "InformationAccessRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    Scenario("definitions-and-specializations", "Profession definitions and specialization parentage resolve", 10, Step("step10-profession-definitions", DefinitionsAndSpecializations)),
                    Scenario("formal-informal-primary", "Formal, informal, primary, and duplicate profession rules are enforced", 20, Step("step10-profession-primary", FormalInformalPrimary)),
                    Scenario("secret-access-projection", "Secret profession relationships redact ordinary projections", 30, Step("step10-profession-secret", SecretAccessProjection)),
                    Scenario("persistence-round-trip", "Profession relationships save and restore without replay", 40, Step("step10-profession-persistence", PersistenceRoundTrip)),
                    Scenario("competency-boundary", "Profession identity does not grant skills or capabilities", 50, Step("step10-profession-boundary", CompetencyBoundary))
                });
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
                requiredRuntimeAreas: TestLabRuntimeArea.Professions | TestLabRuntimeArea.KnowledgeHistory,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[]
                {
                    PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                    PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                    PrototypeProfessionDefinitionFactory.AccessSecretId
                });
        }

        private static ITestLabAutomationSuite BuildProfessionalEligibilityEntrySuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.2.professional-eligibility-entry",
                "Feature 10.2 Professional Eligibility and Entry",
                "10.2",
                "Professional eligibility, informal entry, formal requests, specialization, reentry, access, and persistence automation.",
                1020,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ProfessionEntryRuntime", "PersonProfessionRuntime", "ProfessionEntryPathDefinition" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    EntryScenario("eligibility-preview", "Eligibility preview is non-mutating and immutable", 10, EntryStep("step10-entry-preview", EligibilityPreview)),
                    EntryScenario("informal-self-declaration", "Self-declared informal practice commits without recognition grants", 20, EntryStep("step10-entry-informal", InformalSelfDeclaration)),
                    EntryScenario("formal-request-approval", "Formal request approval revalidates eligibility and creates recognition", 30, EntryStep("step10-entry-formal", FormalRequestApproval)),
                    EntryScenario("stale-and-authority-rejection", "Invalid authority and stale eligibility are rejected before mutation", 40, EntryStep("step10-entry-stale", StaleAndAuthorityRejection)),
                    EntryScenario("specialization-and-reentry", "Specialization entry and inactive reentry preserve parent relationships", 50, EntryStep("step10-entry-specialization-reentry", SpecializationAndReentry)),
                    EntryScenario("projection-and-persistence", "Entry request projections redact and persistence restores without replay", 60, EntryStep("step10-entry-persistence", ProjectionAndPersistence))
                });
        }

        private static ITestLabAutomationScenario EntryScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                requiredRuntimeAreas: TestLabRuntimeArea.Professions | TestLabRuntimeArea.KnowledgeHistory,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[]
                {
                    PrototypeProfessionDefinitionFactory.BlacksmithSelfDeclaredEntryPathId,
                    PrototypeProfessionDefinitionFactory.FieldMedicRecognitionEntryPathId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationEntryPathId,
                    PrototypeProfessionDefinitionFactory.BlacksmithReentryPathId
                });
        }

        private static ITestLabScenarioStep EntryStep(string stepId, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, stepId, action);
        }

        private static ITestLabScenarioStep Step(string stepId, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, stepId, action);
        }

        private static ITestLabAutomationSuite BuildEducationTrainingApprenticeshipSuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.3.education-training-apprenticeship",
                "Feature 10.3 Education, Training, and Apprenticeship",
                "10.3",
                "Training programs, curricula, apprenticeship enrollment, instruction, supervised work, progress, access, and persistence automation.",
                1030,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "TrainingRuntime", "TrainingProgramDefinition", "TrainingCurriculumDefinition", "InformationTransferRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    TrainingScenario("definitions-and-curricula", "Training definitions and curricula validate", 10, Step("step10-training-definitions", TrainingDefinitionsAndCurricula)),
                    TrainingScenario("enrollment-apprenticeship", "Enrollment and apprenticeship transitions preserve boundaries", 20, Step("step10-training-enrollment", TrainingEnrollmentApprenticeship)),
                    TrainingScenario("learning-session-teaching", "Learning sessions route teaching through Step 8", 30, Step("step10-training-teaching", TrainingLearningSessionTeaching)),
                    TrainingScenario("practical-supervised-work", "Practical assignments and supervised work reference activity records", 40, Step("step10-training-practical", TrainingPracticalSupervisedWork)),
                    TrainingScenario("progress-completion-boundaries", "Progress evaluation and completion enforce hidden requirements", 50, Step("step10-training-progress", TrainingProgressCompletionBoundaries)),
                    TrainingScenario("persistence-projections", "Training persistence and projections are atomic and redacted", 60, Step("step10-training-persistence", TrainingPersistenceProjections))
                });
        }

        private static ITestLabAutomationScenario TrainingScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                requiredRuntimeAreas: TestLabRuntimeArea.Professions | TestLabRuntimeArea.KnowledgeHistory,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[]
                {
                    PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                    PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId,
                    PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCurriculumId,
                    PrototypeProfessionDefinitionFactory.BlacksmithSafetyProgramId,
                    PrototypeProfessionDefinitionFactory.BlacksmithSafetyCurriculumId,
                    PrototypeProfessionDefinitionFactory.TrainingLessonTransferDefinitionId,
                    PrototypeProfessionDefinitionFactory.TrainingDemonstrationTransferDefinitionId,
                    PrototypeProfessionDefinitionFactory.TrainingGuidedPracticeTransferDefinitionId
                });
        }

        private static ITestLabAutomationSuite BuildProfessionalActivityExperienceSuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.4.professional-activity-experience",
                "Feature 10.4 Professional Activity and Experience",
                "10.4",
                "Professional activity source adapters, validation, evidence summaries, boundaries, access, and persistence automation.",
                1040,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ProfessionalActivityRuntime", "ProfessionalActivityDefinition", "PersonProfessionRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    ActivityScenario("definitions-and-adapters", "Professional activity definitions and source adapters validate", 10, Step("step10-activity-definitions", ProfessionalActivityDefinitionsAndAdapters)),
                    ActivityScenario("record-and-validate", "Validated professional activity creates experience evidence", 20, Step("step10-activity-validate", ProfessionalActivityRecordAndValidate)),
                    ActivityScenario("duplicates-shared-credit", "Exclusive duplicates and shared role credit are deterministic", 30, Step("step10-activity-duplicates", ProfessionalActivityDuplicatesSharedCredit)),
                    ActivityScenario("summary-requirements-boundaries", "Experience summaries and requirements do not mutate owning systems", 40, Step("step10-activity-summary", ProfessionalActivitySummaryRequirementsBoundaries)),
                    ActivityScenario("access-persistence", "Activity projections redact and persistence restores atomically", 50, Step("step10-activity-persistence", ProfessionalActivityAccessPersistence))
                });
        }

        private static ITestLabAutomationScenario ActivityScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                requiredRuntimeAreas: TestLabRuntimeArea.Professions | TestLabRuntimeArea.KnowledgeHistory | TestLabRuntimeArea.Items,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[]
                {
                    PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                    PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId,
                    PrototypeProfessionDefinitionFactory.BlacksmithSupervisedPracticeActivityDefinitionId,
                    PrototypeProfessionDefinitionFactory.BlacksmithTeachingActivityDefinitionId,
                    PrototypeProfessionDefinitionFactory.BlacksmithExperimentationActivityDefinitionId
                });
        }

        private static TestLabAutomationStepResult ProfessionalActivityDefinitionsAndAdapters(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in PrototypeProfessionDefinitionFactory.CreateDefinitions().OfType<IGameDefinition>())
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(registry.DefinitionsById, report);
                }
            }

            ProfessionalActivitySourceSnapshot crafting = ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "adapter-craft", "production.activity.forging");
            ProfessionalActivitySourceSnapshot practice = ActivityCustomSource(context, ProfessionalActivitySourceType.TrainingPracticalAssignment, "adapter-practice", "training.activity.practical");
            bool definitions = registry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, out ProfessionalActivityDefinition craftDefinition)
                && craftDefinition.AcceptedSourceTypes.Contains(ProfessionalActivitySourceType.CraftingOperation)
                && registry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithExperimentationActivityDefinitionId, out ProfessionalActivityDefinition experimentDefinition)
                && experimentDefinition.FailureCreditPolicy == ProfessionalFailureCreditPolicy.CountsAsFailedAttempt;
            bool adapters = crafting.Completed
                && crafting.Reference.sourceType == ProfessionalActivitySourceType.CraftingOperation
                && practice.Tags.Contains("training.activity.practical");
            bool valid = report.ErrorCount == 0 && report.WarningCount == 0 && definitions && adapters;
            return TestLabAssertions.True("step10-activity-definitions", "Professional activity definitions and source adapters validate", valid, $"Errors={report.ErrorCount} Warnings={report.WarningCount} Definitions={definitions} Adapters={adapters}");
        }

        private static TestLabAutomationStepResult ProfessionalActivityRecordAndValidate(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithActivityProfession(context);
            long professionRevision = runtimes.Professions.Revision;
            long knowledgeRevision = runtimes.Knowledge.KnowledgeRevision;
            ProfessionalActivityOperationResult result = runtimes.ProfessionalActivities.RegisterAndValidateActivity(
                ActivityRequest(context, "validated", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "validated-source", "production.activity.forging")),
                context.ScenarioContext.ScopedId("professional-evidence", "validated"),
                "authority.guild.prototype",
                context.ScenarioContext.ScopedId("professional-tx", "validated"));
            ProfessionalExperienceSummary summary = runtimes.ProfessionalActivities.BuildExperienceSummary(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);

            bool valid = result.Succeeded
                && result.Evidence != null
                && summary.TotalValidatedActivities == 1
                && summary.SuccessfulCount == 1
                && runtimes.Professions.Revision == professionRevision
                && runtimes.Knowledge.KnowledgeRevision == knowledgeRevision;
            return TestLabAssertions.True("step10-activity-validate", "Validated professional activity creates experience evidence", valid, $"Result={result.Status} Evidence={result.Evidence?.evidenceId} Summary={summary.TotalValidatedActivities} Profession={professionRevision}->{runtimes.Professions.Revision} Knowledge={knowledgeRevision}->{runtimes.Knowledge.KnowledgeRevision}");
        }

        private static TestLabAutomationStepResult ProfessionalActivityDuplicatesSharedCredit(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithActivityProfession(context);
            ProfessionalActivitySourceSnapshot source = ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "exclusive-source", "production.activity.forging");
            ProfessionalActivityOperationResult first = runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "exclusive-a", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, source), context.ScenarioContext.ScopedId("professional-evidence", "exclusive-a"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "exclusive-a"));
            ProfessionalActivityOperationResult second = runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "exclusive-b", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, source), context.ScenarioContext.ScopedId("professional-evidence", "exclusive-b"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "exclusive-b"));
            ProfessionalActivitySourceSnapshot shared = ActivityCustomSource(context, ProfessionalActivitySourceType.TeachingSession, "shared-source", "training.activity.teaching", difficulty: ProfessionalActivityDifficulty.Skilled);
            ProfessionalActivityOperationResult instructor = runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "shared-instructor", PrototypeProfessionDefinitionFactory.BlacksmithTeachingActivityDefinitionId, shared, ProfessionalResponsibilityLevel.Instructor, TrainingSupervisionLevel.IndependentWithReview), context.ScenarioContext.ScopedId("professional-evidence", "shared-instructor"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "shared-instructor"));
            ProfessionalActivityOperationResult assistant = runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "shared-assistant", PrototypeProfessionDefinitionFactory.BlacksmithTeachingActivityDefinitionId, shared, ProfessionalResponsibilityLevel.Assistant, TrainingSupervisionLevel.ObservationOnly), context.ScenarioContext.ScopedId("professional-evidence", "shared-assistant"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "shared-assistant"));
            ProfessionalExperienceSummary summary = runtimes.ProfessionalActivities.BuildExperienceSummary(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);

            bool valid = first.Succeeded
                && !second.Succeeded
                && second.Status == ProfessionalActivityOperationStatus.DuplicateExclusiveSource
                && instructor.Succeeded
                && assistant.Succeeded
                && summary.TotalValidatedActivities == 3;
            return TestLabAssertions.True("step10-activity-duplicates", "Exclusive duplicates and shared role credit are deterministic", valid, $"First={first.Status} Second={second.Status} Instructor={instructor.Status} Assistant={assistant.Status} Summary={summary.TotalValidatedActivities}");
        }

        private static TestLabAutomationStepResult ProfessionalActivitySummaryRequirementsBoundaries(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithActivityProfession(context);
            long professionRevision = runtimes.Professions.Revision;
            long trainingRevision = runtimes.Training.Revision;
            runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "summary-independent", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "summary-independent", "production.activity.forging"), ProfessionalResponsibilityLevel.IndependentPractitioner), context.ScenarioContext.ScopedId("professional-evidence", "summary-independent"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "summary-independent"));
            runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "summary-supervised", PrototypeProfessionDefinitionFactory.BlacksmithSupervisedPracticeActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.TrainingPracticalAssignment, "summary-supervised", "training.activity.practical"), ProfessionalResponsibilityLevel.SupervisedWorker, TrainingSupervisionLevel.CloselySupervised), context.ScenarioContext.ScopedId("professional-evidence", "summary-supervised"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "summary-supervised"));

            bool requirement = runtimes.ProfessionalActivities.EvaluateExperienceRequirement(runtimes.PersonId, new ProfessionalExperienceRequirementData
            {
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                minimumValidatedActivities = 2,
                minimumIndependentActivities = 1,
                minimumSupervisedActivities = 1,
                minimumQuality = 600,
                minimumDifficulty = ProfessionalActivityDifficulty.Routine,
                requireRecentActivity = true
            }, out ProfessionalExperienceSummary summary);

            bool valid = requirement
                && summary.IndependentCount == 1
                && summary.SupervisedCount == 1
                && summary.BreadthScore >= 2
                && runtimes.Professions.Revision == professionRevision
                && runtimes.Training.Revision == trainingRevision;
            return TestLabAssertions.True("step10-activity-summary", "Experience summaries and requirements do not mutate owning systems", valid, $"Requirement={requirement} Independent={summary.IndependentCount} Supervised={summary.SupervisedCount} Breadth={summary.BreadthScore} Profession={professionRevision}->{runtimes.Professions.Revision} Training={trainingRevision}->{runtimes.Training.Revision}");
        }

        private static TestLabAutomationStepResult ProfessionalActivityAccessPersistence(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithActivityProfession(context);
            ProfessionalActivityOperationResult recorded = runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "persisted", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "persisted-source", "production.activity.forging")), context.ScenarioContext.ScopedId("professional-evidence", "persisted"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "persisted"));
            InformationAccessDecision decision = new InformationAccessDecision("person.observer", ProfessionalActivityInformationSubject.Create(ProfessionalActivityInformationSubject.ActivityTag, recorded.Activity?.activityId, runtimes.PersonId), InformationAccessMode.Inspect, InformationAccessDecisionKind.RedactedAccess, InformationAccessDenialCode.DetailRestriction, false, InformationResharingPolicy.NoResharing, new[] { "profession-id", "state" }, ProfessionalActivityInformationSubject.ProtectedFields, Array.Empty<string>(), new[] { PrototypeProfessionDefinitionFactory.AccessPublicId }, 1d, "Redacted professional activity.", "Professional source hidden.", true);
            ProfessionalActivityProjection<ProfessionalActivityRecordData> projection = runtimes.ProfessionalActivities.ProjectActivity(recorded.Activity?.activityId, ProfessionalActivityProjectionAudience.PublicInspection, decision);
            ProfessionalActivityRuntimeSaveData save = runtimes.ProfessionalActivities.CreateSaveData();
            ProfessionalActivityRuntime restored = new ProfessionalActivityRuntime();
            ProfessionalActivityOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.KnownPersonIds, restoring: true);
            ProfessionalActivityRuntimeSaveData corrupt = save.Clone();
            corrupt.activities[0].activityDefinitionId = "professional-activity.missing";
            ProfessionalActivityOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.KnownPersonIds, restoring: true);

            bool valid = recorded.Succeeded
                && projection.Redacted
                && projection.Record != null
                && string.IsNullOrWhiteSpace(projection.Record.personId)
                && restore.Succeeded
                && restored.EvidenceCount == 1
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.EvidenceCount == 1;
            return TestLabAssertions.True("step10-activity-persistence", "Activity projections redact and persistence restores atomically", valid, $"Record={recorded.Status} Redacted={projection.Redacted} Restore={restore.Status} Rejected={rejected.Status} Evidence={restored.EvidenceCount}");
        }

        private static TestLabAutomationStepResult TrainingDefinitionsAndCurricula(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in PrototypeProfessionDefinitionFactory.CreateDefinitions().OfType<IGameDefinition>())
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(registry.DefinitionsById, report);
                }
            }

            TrainingCurriculumDefinition cyclic = ScriptableObject.CreateInstance<TrainingCurriculumDefinition>();
            cyclic.DevelopmentConfigure(
                "training-curriculum.automation.cycle",
                PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId,
                "Automation Cycle",
                new[]
                {
                    TrainingModule("training-module.automation.cycle-a", true, false, dependencies: new[] { "training-module.automation.cycle-b" }),
                    TrainingModule("training-module.automation.cycle-b", true, false, dependencies: new[] { "training-module.automation.cycle-a" })
                },
                Array.Empty<TrainingLessonDefinitionData>());
            DefinitionValidationReport cycleReport = new DefinitionValidationReport();
            cyclic.ValidateCatalogDefinition(registry.DefinitionsById, cycleReport);
            UnityEngine.Object.DestroyImmediate(cyclic);

            bool program = registry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId, out TrainingProgramDefinition apprenticeship)
                && apprenticeship.Category == TrainingProgramCategory.Apprenticeship;
            bool curriculum = registry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCurriculumId, out TrainingCurriculumDefinition trainingCurriculum)
                && trainingCurriculum.Modules.Count >= 3
                && trainingCurriculum.Lessons.Any(lesson => lesson.teachingMethod == TrainingTeachingMethod.Demonstration);
            bool transfers = registry.TryGet(PrototypeProfessionDefinitionFactory.TrainingLessonTransferDefinitionId, out InformationTransferDefinition lecture)
                && lecture.Mode == InformationTransferMode.Lecture
                && registry.TryGet(PrototypeProfessionDefinitionFactory.TrainingDemonstrationTransferDefinitionId, out InformationTransferDefinition demonstration)
                && demonstration.Mode == InformationTransferMode.Demonstration;

            bool valid = report.ErrorCount == 0
                && report.WarningCount == 0
                && cycleReport.ErrorCount > 0
                && program
                && curriculum
                && transfers;
            return TestLabAssertions.True("step10-training-definitions", "Training definitions and curricula validate", valid, $"Errors={report.ErrorCount} Warnings={report.WarningCount} CycleErrors={cycleReport.ErrorCount} Program={program} Curriculum={curriculum} Transfers={transfers}");
        }

        private static TestLabAutomationStepResult TrainingEnrollmentApprenticeship(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            long professionRevision = runtimes.Professions.Revision;
            long knowledgeRevision = runtimes.Knowledge.KnowledgeRevision;
            string enrollmentId = TrainingEnrollmentId(context, "enrollment");

            TrainingOperationResult apply = runtimes.Training.ApplyToProgram(enrollmentId, runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId, context.ScenarioContext.ScopedId("training-tx", "apply"), worldTime: 1d);
            TrainingOperationResult duplicate = runtimes.Training.ApplyToProgram(context.ScenarioContext.ScopedId("training-enrollment", "duplicate"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId, context.ScenarioContext.ScopedId("training-tx", "duplicate"), worldTime: 2d);
            TrainingOperationResult accept = runtimes.Training.AcceptEnrollment(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "accept"));
            TrainingOperationResult instructor = runtimes.Training.AssignInstructor(enrollmentId, context.ScenarioContext.ScopedId("training-instructor", "master"), TrainingInstructorRoleKind.Master, runtimes.PersonId, context.ScenarioContext.ScopedId("training-tx", "master"), professionId: PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, authorityId: "authority.guild.prototype");
            TrainingOperationResult begin = runtimes.Training.BeginProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "begin"));
            TrainingOperationResult withdraw = runtimes.Training.Withdraw(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "withdraw"));
            TrainingOperationResult terminalBegin = runtimes.Training.BeginProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "terminal-begin"));

            bool valid = apply.Succeeded
                && !duplicate.Succeeded
                && duplicate.Status == TrainingOperationStatus.Duplicate
                && accept.Succeeded
                && instructor.Succeeded
                && begin.Succeeded
                && withdraw.Succeeded
                && !terminalBegin.Succeeded
                && terminalBegin.Status == TrainingOperationStatus.InvalidTransition
                && runtimes.Professions.Revision == professionRevision
                && runtimes.Knowledge.KnowledgeRevision == knowledgeRevision;
            return TestLabAssertions.True("step10-training-enrollment", "Enrollment and apprenticeship transitions preserve boundaries", valid, $"Apply={apply.Status} Duplicate={duplicate.Status} Accept={accept.Status} Instructor={instructor.Status} Begin={begin.Status} Withdraw={withdraw.Status} TerminalBegin={terminalBegin.Status} Profession={professionRevision}->{runtimes.Professions.Revision} Knowledge={knowledgeRevision}->{runtimes.Knowledge.KnowledgeRevision}");
        }

        private static TestLabAutomationStepResult TrainingLearningSessionTeaching(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            string enrollmentId = BeginTrainingApprenticeship(context, "teaching");
            TrainingOperationResult attendance = runtimes.Training.RunLearningSession(context.ScenarioContext.ScopedId("training-session", "attendance"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyLessonId, context.ScenarioContext.ScopedId("training-tx", "attendance"), startWorldTime: 10d, completionWorldTime: 11d);
            long knowledgeBeforeTeaching = runtimes.Knowledge.KnowledgeRevision;
            TrainingOperationResult taught = runtimes.Training.RunLearningSession(
                context.ScenarioContext.ScopedId("training-session", "teaching"),
                enrollmentId,
                PrototypeProfessionDefinitionFactory.BlacksmithPracticeModuleId,
                PrototypeProfessionDefinitionFactory.BlacksmithDemonstrationLessonId,
                context.ScenarioContext.ScopedId("training-tx", "teaching"),
                BuildTrainingTeachingRequest(context, "teaching-transfer"),
                startWorldTime: 12d,
                completionWorldTime: 13d);

            bool valid = attendance.Succeeded
                && taught.Succeeded
                && taught.Transfer != null
                && taught.Transfer.Succeeded
                && taught.Transfer.Record != null
                && taught.Transfer.Record.Data.teachingRequested
                && runtimes.Knowledge.KnowledgeRevision > knowledgeBeforeTeaching;
            return TestLabAssertions.True("step10-training-teaching", "Learning sessions route teaching through Step 8", valid, $"Attendance={attendance.Status} Teaching={taught.Status} Transfer={taught.Transfer?.Status} Requested={taught.Transfer?.Record?.Data.teachingRequested} Knowledge={knowledgeBeforeTeaching}->{runtimes.Knowledge.KnowledgeRevision}");
        }

        private static TestLabAutomationStepResult TrainingPracticalSupervisedWork(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            string enrollmentId = BeginTrainingApprenticeship(context, "practical");
            CompleteTrainingVisibleRequirements(context, enrollmentId, completePractice: false);
            string activityId = context.ScenarioContext.ScopedId("crafting-operation", "blacksmith-training");

            TrainingOperationResult missingSupervisor = runtimes.Training.RecordPracticalAssignment(context.ScenarioContext.ScopedId("training-practical", "missing-supervisor"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticalAssignmentId, activityId, TrainingAssignmentActivityCategory.Crafting, context.ScenarioContext.ScopedId("training-tx", "practice-missing"), supervisorPersonId: string.Empty);
            TrainingOperationResult accepted = runtimes.Training.RecordPracticalAssignment(context.ScenarioContext.ScopedId("training-practical", "accepted"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticalAssignmentId, activityId, TrainingAssignmentActivityCategory.Crafting, context.ScenarioContext.ScopedId("training-tx", "practice-accepted"), quality: 700, supervisorPersonId: runtimes.PersonId);
            TrainingOperationResult duplicate = runtimes.Training.RecordPracticalAssignment(context.ScenarioContext.ScopedId("training-practical", "duplicate"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticalAssignmentId, activityId, TrainingAssignmentActivityCategory.Crafting, context.ScenarioContext.ScopedId("training-tx", "practice-duplicate"), quality: 700, supervisorPersonId: runtimes.PersonId);
            TrainingOperationResult supervised = runtimes.Training.RecordSupervisedWork(context.ScenarioContext.ScopedId("training-supervised", "forge"), enrollmentId, runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, activityId, TrainingSupervisionLevel.CloselySupervised, TrainingWorkOutcome.Succeeded, context.ScenarioContext.ScopedId("training-tx", "supervised"), quality: 725, startWorldTime: 20d, completionWorldTime: 21d);
            TrainingRuntimeSaveData save = runtimes.Training.CreateSaveData();

            bool valid = !missingSupervisor.Succeeded
                && missingSupervisor.Status == TrainingOperationStatus.RequirementBlocked
                && accepted.Succeeded
                && !duplicate.Succeeded
                && duplicate.Status == TrainingOperationStatus.DuplicateActivity
                && supervised.Succeeded
                && save.practicalWorkRecords.Any(record => string.Equals(record.activityReferenceId, activityId, StringComparison.Ordinal))
                && save.supervisedWorkRecords.Any(record => record.supervisionLevel == TrainingSupervisionLevel.CloselySupervised && string.Equals(record.activityReferenceId, activityId, StringComparison.Ordinal));
            return TestLabAssertions.True("step10-training-practical", "Practical assignments and supervised work reference activity records", valid, $"Missing={missingSupervisor.Status} Accepted={accepted.Status} Duplicate={duplicate.Status} Supervised={supervised.Status} Practical={save.practicalWorkRecords.Count} SupervisedCount={save.supervisedWorkRecords.Count}");
        }

        private static TestLabAutomationStepResult TrainingProgressCompletionBoundaries(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            string enrollmentId = BeginTrainingApprenticeship(context, "progress");
            CompleteTrainingVisibleRequirements(context, enrollmentId, completePractice: true);

            TrainingProgressResult perceived = runtimes.Training.EvaluateProgress(enrollmentId, perceived: true);
            TrainingProgressResult authoritative = runtimes.Training.EvaluateProgress(enrollmentId, perceived: false);
            TrainingProgressTokenData staleToken = authoritative.RuntimeToken;
            TrainingOperationResult blocked = runtimes.Training.CompleteProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "complete-blocked"), authoritative.RuntimeToken);
            TrainingOperationResult hidden = runtimes.Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithHiddenAssessmentModuleId, context.ScenarioContext.ScopedId("training-tx", "hidden"));
            TrainingOperationResult stale = runtimes.Training.CompleteProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "complete-stale"), staleToken);
            TrainingProgressResult current = runtimes.Training.EvaluateProgress(enrollmentId, perceived: false);
            TrainingOperationResult complete = runtimes.Training.CompleteProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", "complete"), current.RuntimeToken, worldTime: 100d);

            bool valid = perceived.EligibleForCompletion
                && !authoritative.EligibleForCompletion
                && authoritative.RemainingRequirements.Contains(PrototypeProfessionDefinitionFactory.BlacksmithHiddenAssessmentModuleId)
                && !blocked.Succeeded
                && blocked.Status == TrainingOperationStatus.RequirementBlocked
                && hidden.Succeeded
                && !stale.Succeeded
                && stale.Status == TrainingOperationStatus.StaleProgress
                && complete.Succeeded
                && complete.Enrollment != null
                && complete.Enrollment.State == TrainingEnrollmentState.Completed
                && runtimes.Professions.Count == 0;
            return TestLabAssertions.True("step10-training-progress", "Progress evaluation and completion enforce hidden requirements", valid, $"Perceived={perceived.EligibleForCompletion}/{perceived.Percentage} Authoritative={authoritative.EligibleForCompletion}/{authoritative.Percentage} Blocked={blocked.Status} Hidden={hidden.Status} Stale={stale.Status} Complete={complete.Status} Professions={runtimes.Professions.Count}");
        }

        private static TestLabAutomationStepResult TrainingPersistenceProjections(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            string enrollmentId = BeginTrainingApprenticeship(context, "persistence");
            CompleteTrainingVisibleRequirements(context, enrollmentId, completePractice: true);
            TrainingRuntimeSaveData save = runtimes.Training.CreateSaveData();
            TrainingRuntime restored = new TrainingRuntime();
            TrainingOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Transfers, runtimes.KnownPersonIds, restoring: true);

            TrainingRuntimeSaveData corrupt = save.Clone();
            corrupt.enrollments[0].programId = "training-program.missing";
            int beforeCount = restored.EnrollmentCount;
            long beforeRevision = restored.Revision;
            TrainingOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Transfers, runtimes.KnownPersonIds, restoring: true);

            InformationAccessDecision decision = new InformationAccessDecision(
                "person.observer",
                TrainingInformationSubject.Create(TrainingInformationSubject.EnrollmentTag, enrollmentId, runtimes.PersonId),
                InformationAccessMode.Inspect,
                InformationAccessDecisionKind.RedactedAccess,
                InformationAccessDenialCode.DetailRestriction,
                false,
                InformationResharingPolicy.NoResharing,
                new[] { "program-id", "state" },
                TrainingInformationSubject.ProtectedFields,
                Array.Empty<string>(),
                new[] { PrototypeProfessionDefinitionFactory.AccessPublicId },
                50d,
                "Redacted training enrollment access.",
                "Training enrollment hides learner and progress token details.",
                true);
            TrainingProjection<TrainingEnrollmentSnapshot> projection = restored.ProjectEnrollment(enrollmentId, TrainingProjectionAudience.PublicInspection, decision);

            bool valid = restore.Succeeded
                && restored.EnrollmentCount == runtimes.Training.EnrollmentCount
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.EnrollmentCount == beforeCount
                && restored.Revision == beforeRevision
                && projection.Record != null
                && projection.Redacted
                && !projection.Denied
                && string.IsNullOrWhiteSpace(projection.Record.PersonId)
                && projection.Record.ProgramId == PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId;
            return TestLabAssertions.True("step10-training-persistence", "Training persistence and projections are atomic and redacted", valid, $"Restore={restore.Status} Rejected={rejected.Status} Count={restored.EnrollmentCount}/{runtimes.Training.EnrollmentCount} Hooks={restored.HistoryHooks.Count} Redacted={projection.Redacted} Person='{projection.Record?.PersonId}'");
        }

        private static TestLabAutomationStepResult DefinitionsAndSpecializations(TestLabAutomationContext context)
        {
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool blacksmith = registry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, out ProfessionDefinition profession);
            bool specialization = registry.TryGet(PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId, out ProfessionSpecializationDefinition weaponsmith);
            bool parentMatches = specialization && string.Equals(weaponsmith.ParentProfessionId, profession?.Id, StringComparison.Ordinal);
            bool listed = blacksmith && profession.AllowedSpecializationIds.Contains(weaponsmith.Id);
            bool noGrantPayloads = blacksmith && profession.RelatedSkillIds.Contains("skill.smithing") && profession.RelatedCapabilityIds.Count == 0;

            return TestLabAssertions.True("step10-profession-definitions", "Profession definitions and specialization parentage resolve", blacksmith && specialization && parentMatches && listed && noGrantPayloads, $"Blacksmith={blacksmith} Specialization={specialization} Parent={parentMatches} Listed={listed} CapabilityGrants={(profession == null ? 0 : profession.RelatedCapabilityIds.Count)}");
        }

        private static TestLabAutomationStepResult FormalInformalPrimary(TestLabAutomationContext context)
        {
            PersonProfessionRuntime runtime = context.ScenarioContext.Runtimes.Professions;
            string personId = context.ScenarioContext.Runtimes.PersonId;
            ProfessionOperationResult informal = runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.blacksmith",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                informalPractice = true,
                primary = true,
                startWorldTime = "10",
                transactionId = "tx.profession.blacksmith"
            });
            ProfessionOperationResult duplicate = runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.blacksmith.duplicate",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                informalPractice = true,
                startWorldTime = "11",
                transactionId = "tx.profession.blacksmith.duplicate"
            });
            ProfessionOperationResult medical = runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.medic",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                informalPractice = true,
                startWorldTime = "12",
                transactionId = "tx.profession.medic"
            });
            ProfessionOperationResult recognizeMissing = runtime.Recognize("profession-relationship.test.medic", string.Empty, transactionId: "tx.profession.medic.recognize.missing");
            ProfessionOperationResult recognize = runtime.Recognize("profession-relationship.test.medic", "authority.medical.prototype", "credential.prototype.medic", "tx.profession.medic.recognize");
            ProfessionOperationResult primary = runtime.SetPrimary("profession-relationship.test.medic", "tx.profession.medic.primary");

            bool onePrimary = runtime.QueryPrimary(personId).Count == 1 && runtime.QueryPrimary(personId).Single().ProfessionId == PrototypeProfessionDefinitionFactory.FieldMedicProfessionId;
            bool valid = informal.Succeeded
                && !duplicate.Succeeded
                && duplicate.Status == ProfessionOperationStatus.DuplicateActiveRelationship
                && medical.Succeeded
                && !recognizeMissing.Succeeded
                && recognizeMissing.Status == ProfessionOperationStatus.MissingRecognitionAuthority
                && recognize.Succeeded
                && primary.Succeeded
                && onePrimary;

            return TestLabAssertions.True("step10-profession-primary", "Formal, informal, primary, and duplicate profession rules are enforced", valid, $"Informal={informal.Status} Duplicate={duplicate.Status} Medical={medical.Status} MissingAuth={recognizeMissing.Status} Recognize={recognize.Status} Primary={primary.Status} OnePrimary={onePrimary}");
        }

        private static TestLabAutomationStepResult SecretAccessProjection(TestLabAutomationContext context)
        {
            PersonProfessionRuntime runtime = context.ScenarioContext.Runtimes.Professions;
            string personId = context.ScenarioContext.Runtimes.PersonId;
            ProfessionOperationResult created = runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.spy",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.SpyProfessionId,
                state = ProfessionRelationshipState.Secret,
                informalPractice = true,
                active = true,
                startWorldTime = "20",
                accessPolicyId = PrototypeProfessionDefinitionFactory.AccessSecretId,
                tags = new[] { "profession.secret" },
                transactionId = "tx.profession.spy"
            });

            InformationAccessDecision redactedDecision = new InformationAccessDecision(
                "person.observer",
                ProfessionInformationSubject.Relationship("profession-relationship.test.spy", personId, PrototypeProfessionDefinitionFactory.SpyProfessionId, new[] { "profession.secret" }),
                InformationAccessMode.Inspect,
                InformationAccessDecisionKind.RedactedAccess,
                InformationAccessDenialCode.DetailRestriction,
                false,
                InformationResharingPolicy.NoResharing,
                new[] { "profession-id", "state" },
                ProfessionInformationSubject.ProtectedFields,
                Array.Empty<string>(),
                new[] { PrototypeProfessionDefinitionFactory.AccessSecretId },
                20d,
                "Redacted profession access.",
                "Secret profession relationship hides identity details.",
                true);
            PersonProfessionProjection projection = runtime.Project("profession-relationship.test.spy", ProfessionProjectionAudience.PublicInspection, redactedDecision);

            bool valid = created.Succeeded
                && projection.Redacted
                && !projection.Denied
                && projection.Snapshot != null
                && string.IsNullOrWhiteSpace(projection.Snapshot.PersonId)
                && projection.Snapshot.ProfessionId == PrototypeProfessionDefinitionFactory.SpyProfessionId;
            return TestLabAssertions.True("step10-profession-secret", "Secret profession relationships redact ordinary projections", valid, $"Created={created.Status} Redacted={projection.Redacted} Denied={projection.Denied} Person='{projection.Snapshot?.PersonId}' Profession='{projection.Snapshot?.ProfessionId}'");
        }

        private static TestLabAutomationStepResult PersistenceRoundTrip(TestLabAutomationContext context)
        {
            PersonProfessionRuntime runtime = context.ScenarioContext.Runtimes.Professions;
            string personId = context.ScenarioContext.Runtimes.PersonId;
            runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.persist",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                informalPractice = true,
                primary = true,
                specializationIds = new[] { PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId },
                startWorldTime = "30",
                transactionId = "tx.profession.persist"
            });

            PersonProfessionRuntimeSaveData save = runtime.CreateSaveData();
            PersonProfessionRuntime restored = new PersonProfessionRuntime();
            ProfessionOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.KnownPersonIds, restoring: true);
            PersonProfessionRuntimeSaveData corrupt = save.Clone();
            corrupt.relationships[0].professionId = "profession.missing";
            ProfessionOperationResult rejected = restored.RestoreFromSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.KnownPersonIds, restoring: true);

            bool valid = restore.Succeeded
                && restored.Count == runtime.Count
                && restored.Revision == runtime.Revision
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.Count == runtime.Count;
            return TestLabAssertions.True("step10-profession-persistence", "Profession relationships save and restore without replay", valid, $"Restore={restore.Status} Rejected={rejected.Status} Count={restored.Count}/{runtime.Count} Hooks={restored.HistoryHooks.Count}");
        }

        private static TestLabAutomationStepResult CompetencyBoundary(TestLabAutomationContext context)
        {
            PersonProfessionRuntime runtime = context.ScenarioContext.Runtimes.Professions;
            string personId = context.ScenarioContext.Runtimes.PersonId;
            long knowledgeRevision = context.ScenarioContext.Runtimes.Knowledge.KnowledgeRevision;
            long accessRevision = context.ScenarioContext.Runtimes.Access.AccessRevision;
            runtime.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.test.boundary",
                personId = personId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                informalPractice = true,
                startWorldTime = "40",
                transactionId = "tx.profession.boundary"
            });

            bool valid = context.ScenarioContext.Runtimes.Knowledge.KnowledgeRevision == knowledgeRevision
                && context.ScenarioContext.Runtimes.Access.AccessRevision == accessRevision
                && context.ScenarioContext.Runtimes.History.HistoryRevision == 0L
                && runtime.HistoryHooks.Count >= 1;
            return TestLabAssertions.True("step10-profession-boundary", "Profession identity does not grant skills or capabilities", valid, $"Knowledge={knowledgeRevision}->{context.ScenarioContext.Runtimes.Knowledge.KnowledgeRevision} Access={accessRevision}->{context.ScenarioContext.Runtimes.Access.AccessRevision} History={context.ScenarioContext.Runtimes.History.HistoryRevision} Hooks={runtime.HistoryHooks.Count}");
        }

        private static TestLabAutomationStepResult EligibilityPreview(TestLabAutomationContext context)
        {
            ProfessionEntryRuntime entries = context.ScenarioContext.Runtimes.ProfessionEntries;
            long entryRevision = entries.Revision;
            long professionRevision = context.ScenarioContext.Runtimes.Professions.Revision;
            ProfessionEligibilityResult result = entries.Evaluate(BlacksmithContext(context, preview: true));
            bool snapshotImmutable = result.RuntimeToken != null;

            context.ScenarioContext.Runtimes.Professions.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.step10.preview-other",
                personId = context.ScenarioContext.Runtimes.PersonId,
                professionId = PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                informalPractice = true,
                startWorldTime = "1",
                transactionId = "tx.step10.preview-other"
            });

            bool valid = result.Succeeded
                && result.Preview
                && entries.Revision == entryRevision
                && professionRevision == 0L
                && snapshotImmutable
                && result.RuntimeToken.professionRevision == professionRevision;
            return TestLabAssertions.True("step10-entry-preview", "Eligibility preview is non-mutating and immutable", valid, $"Status={result.Status} EntryRev={entryRevision}->{entries.Revision} ProfessionToken={result.RuntimeToken?.professionRevision} CurrentProf={context.ScenarioContext.Runtimes.Professions.Revision}");
        }

        private static TestLabAutomationStepResult InformalSelfDeclaration(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            long knowledgeRevision = runtimes.Knowledge.KnowledgeRevision;
            long accessRevision = runtimes.Access.AccessRevision;
            ProfessionEntryOperationResult entry = runtimes.ProfessionEntries.EnterInformal(BlacksmithContext(context, preview: false), "tx.step10.informal");
            PersonProfessionSnapshot snapshot = entry.Relationship;

            bool valid = entry.Succeeded
                && snapshot != null
                && snapshot.SelfDeclared
                && snapshot.InformalPractice
                && !snapshot.FormalPractice
                && !snapshot.Recognized
                && runtimes.Knowledge.KnowledgeRevision == knowledgeRevision
                && runtimes.Access.AccessRevision == accessRevision;
            return TestLabAssertions.True("step10-entry-informal", "Self-declared informal practice commits without recognition grants", valid, $"Entry={entry.Status} Self={snapshot?.SelfDeclared} Informal={snapshot?.InformalPractice} Recognized={snapshot?.Recognized} Knowledge={knowledgeRevision}->{runtimes.Knowledge.KnowledgeRevision}");
        }

        private static TestLabAutomationStepResult FormalRequestApproval(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionEligibilityContext requestContext = MedicContext(context, preview: false);
            ProfessionEntryOperationResult submit = runtimes.ProfessionEntries.SubmitFormalRequest(requestContext, "tx.step10.formal.submit", "profession-entry-request.step10.medic");
            bool noRelationshipBeforeApproval = runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.FieldMedicProfessionId).Count == 0;
            ProfessionOperationResult unrelatedMutation = runtimes.Professions.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.step10.formal.unrelated",
                personId = runtimes.PersonId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                informalPractice = true,
                selfDeclared = true,
                startWorldTime = "2.5",
                transactionId = "tx.step10.formal.unrelated"
            });
            ProfessionEntryOperationResult approve = runtimes.ProfessionEntries.ApproveFormalRequest("profession-entry-request.step10.medic", "authority.medical.prototype", "tx.step10.formal.approve");
            PersonProfessionSnapshot relationship = approve.Relationship;

            bool valid = submit.Succeeded
                && noRelationshipBeforeApproval
                && unrelatedMutation.Succeeded
                && approve.Succeeded
                && relationship != null
                && relationship.Recognized
                && relationship.FormalPractice
                && !relationship.SelfDeclared
                && relationship.RecognizingAuthorityId == "authority.medical.prototype";
            return TestLabAssertions.True("step10-entry-formal", "Formal request approval revalidates eligibility and creates recognition", valid, $"Submit={submit.Status} Unrelated={unrelatedMutation.Status} Approve={approve.Status} Before={noRelationshipBeforeApproval} Recognized={relationship?.Recognized} Authority={relationship?.RecognizingAuthorityId}");
        }

        private static TestLabAutomationStepResult StaleAndAuthorityRejection(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionEligibilityResult evaluated = runtimes.ProfessionEntries.Evaluate(BlacksmithContext(context, preview: true));
            ProfessionEligibilityResult badAuthority = runtimes.ProfessionEntries.Evaluate(new ProfessionEligibilityContext(
                runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                PrototypeProfessionDefinitionFactory.FieldMedicRecognitionEntryPathId,
                formal: true,
                authorityId: "authority.guild.prototype",
                worldTime: 2d,
                skills: new[] { Skill("skill.healing-magic", 1) },
                knowledgeSubjects: new[] { "knowledge.subject.first-aid" },
                preview: true));
            runtimes.Professions.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = "profession-relationship.step10.stale-other",
                personId = runtimes.PersonId,
                professionId = PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                informalPractice = true,
                startWorldTime = "3",
                transactionId = "tx.step10.stale-other"
            });
            ProfessionEntryOperationResult stale = runtimes.ProfessionEntries.EnterInformal(new ProfessionEligibilityContext(
                runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                PrototypeProfessionDefinitionFactory.BlacksmithSelfDeclaredEntryPathId,
                selfDeclared: true,
                worldTime: 4d,
                preview: false,
                expectedRuntimeToken: evaluated.RuntimeToken), "tx.step10.stale");

            bool valid = evaluated.Succeeded
                && !badAuthority.Succeeded
                && badAuthority.Status == ProfessionEligibilityStatus.InvalidAuthority
                && !stale.Succeeded
                && stale.Status == ProfessionEntryOperationStatus.EligibilityFailed
                && runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).Count == 0;
            return TestLabAssertions.True("step10-entry-stale", "Invalid authority and stale eligibility are rejected before mutation", valid, $"Evaluated={evaluated.Status} BadAuthority={badAuthority.Status} Stale={stale.Status} BlacksmithCount={runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).Count}");
        }

        private static TestLabAutomationStepResult SpecializationAndReentry(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionEntryOperationResult baseEntry = runtimes.ProfessionEntries.EnterInformal(BlacksmithContext(context, preview: false), "tx.step10.spec.base", "profession-relationship.step10.spec.blacksmith");
            ProfessionEntryOperationResult specialization = runtimes.ProfessionEntries.EnterSpecialization(new ProfessionEligibilityContext(
                runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationEntryPathId,
                PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                worldTime: 5d,
                skills: new[] { Skill("skill.smithing", 1) },
                preview: false), "profession-relationship.step10.spec.blacksmith", "tx.step10.spec");
            ProfessionOperationResult inactive = runtimes.Professions.Activate("profession-relationship.step10.spec.blacksmith", false, "tx.step10.inactive");
            ProfessionEntryOperationResult resume = runtimes.ProfessionEntries.ResumeInactive(new ProfessionEligibilityContext(
                runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                PrototypeProfessionDefinitionFactory.BlacksmithReentryPathId,
                worldTime: 6d,
                preview: false), "profession-relationship.step10.spec.blacksmith", "tx.step10.resume");
            runtimes.Professions.TryGetSnapshot("profession-relationship.step10.spec.blacksmith", out PersonProfessionSnapshot final);

            bool valid = baseEntry.Succeeded
                && specialization.Succeeded
                && inactive.Succeeded
                && resume.Succeeded
                && final != null
                && final.Active
                && final.SpecializationIds.Contains(PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId);
            return TestLabAssertions.True("step10-entry-specialization-reentry", "Specialization entry and inactive reentry preserve parent relationships", valid, $"Base={baseEntry.Status} Spec={specialization.Status} Inactive={inactive.Status} Resume={resume.Status} Active={final?.Active} Specs={string.Join(",", final?.SpecializationIds ?? Array.Empty<string>())}");
        }

        private static TestLabAutomationStepResult ProjectionAndPersistence(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionEntryOperationResult submit = runtimes.ProfessionEntries.SubmitFormalRequest(MedicContext(context, preview: false), "tx.step10.persist.submit", "profession-entry-request.step10.persist");
            InformationAccessDecision redactedDecision = new InformationAccessDecision(
                "person.observer",
                ProfessionEntryInformationSubject.Request("profession-entry-request.step10.persist", runtimes.PersonId, PrototypeProfessionDefinitionFactory.FieldMedicProfessionId),
                InformationAccessMode.Inspect,
                InformationAccessDecisionKind.RedactedAccess,
                InformationAccessDenialCode.DetailRestriction,
                false,
                InformationResharingPolicy.NoResharing,
                new[] { "profession-id", "entry-path-id", "state" },
                ProfessionEntryInformationSubject.ProtectedFields,
                Array.Empty<string>(),
                new[] { PrototypeProfessionDefinitionFactory.AccessPublicId },
                7d,
                "Redacted profession entry request.",
                "Entry request hides applicant and authority details.",
                true);
            ProfessionEntryProjection<ProfessionEntryRequestSnapshot> projection = runtimes.ProfessionEntries.ProjectRequest("profession-entry-request.step10.persist", ProfessionEntryProjectionAudience.PublicInspection, redactedDecision);
            ProfessionEntryRuntimeSaveData save = runtimes.ProfessionEntries.CreateSaveData();
            ProfessionEntryRuntime restored = new ProfessionEntryRuntime();
            ProfessionEntryOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.KnownPersonIds, restoring: true);
            ProfessionEntryRuntimeSaveData corrupt = save.Clone();
            corrupt.requests[0].entryPathId = "profession-entry.missing";
            ProfessionEntryOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.KnownPersonIds, restoring: true);

            bool valid = submit.Succeeded
                && projection.Redacted
                && projection.Record != null
                && string.IsNullOrWhiteSpace(projection.Record.ApplicantPersonId)
                && restore.Succeeded
                && restored.Count == 1
                && !rejected.Succeeded
                && restored.Count == 1
                && restored.HistoryHooks.Count == 0;
            return TestLabAssertions.True("step10-entry-persistence", "Entry request projections redact and persistence restores without replay", valid, $"Submit={submit.Status} Redacted={projection.Redacted} Applicant='{projection.Record?.ApplicantPersonId}' Restore={restore.Status} Rejected={rejected.Status} Count={restored.Count}");
        }

        private static ProfessionEligibilityContext BlacksmithContext(TestLabAutomationContext context, bool preview)
        {
            return new ProfessionEligibilityContext(
                context.ScenarioContext.Runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                PrototypeProfessionDefinitionFactory.BlacksmithSelfDeclaredEntryPathId,
                selfDeclared: true,
                worldTime: 1d,
                correlationId: "step10.blacksmith",
                preview: preview);
        }

        private static string TrainingEnrollmentId(TestLabAutomationContext context, string slug)
        {
            return context.ScenarioContext.ScopedId("training-enrollment", slug);
        }

        private static string BeginTrainingApprenticeship(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            string enrollmentId = TrainingEnrollmentId(context, slug);
            TrainingOperationResult apply = runtimes.Training.ApplyToProgram(enrollmentId, runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-apply"), worldTime: 1d);
            if (apply.Succeeded)
            {
                runtimes.Training.AcceptEnrollment(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-accept"));
                runtimes.Training.AssignInstructor(enrollmentId, context.ScenarioContext.ScopedId("training-instructor", $"{slug}-master"), TrainingInstructorRoleKind.Master, runtimes.PersonId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-master"), professionId: PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, authorityId: "authority.guild.prototype");
                runtimes.Training.BeginProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-begin"));
            }

            return enrollmentId;
        }

        private static void CompleteTrainingVisibleRequirements(TestLabAutomationContext context, string enrollmentId, bool completePractice)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            runtimes.Training.RunLearningSession(context.ScenarioContext.ScopedId("training-session", "safety"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyLessonId, context.ScenarioContext.ScopedId("training-tx", "safety"));
            runtimes.Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, context.ScenarioContext.ScopedId("training-tx", "module-basics"));
            runtimes.Training.RunLearningSession(context.ScenarioContext.ScopedId("training-session", "practice"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticeModuleId, PrototypeProfessionDefinitionFactory.BlacksmithDemonstrationLessonId, context.ScenarioContext.ScopedId("training-tx", "practice-lesson"));
            if (!completePractice)
            {
                return;
            }

            string activityId = context.ScenarioContext.ScopedId("crafting-operation", "practice-complete");
            runtimes.Training.RecordPracticalAssignment(context.ScenarioContext.ScopedId("training-practical", "practice-complete"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticalAssignmentId, activityId, TrainingAssignmentActivityCategory.Crafting, context.ScenarioContext.ScopedId("training-tx", "practice-complete"), quality: 700, supervisorPersonId: runtimes.PersonId);
            runtimes.Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticeModuleId, context.ScenarioContext.ScopedId("training-tx", "module-practice"));
        }

        private static InformationTransferRequest BuildTrainingTeachingRequest(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            KnowledgePropositionData proposition = TrainingTeachingProposition();
            runtimes.Knowledge.RecordObservation(new KnowledgeObservationRequest
            {
                PersonId = runtimes.PersonId,
                TransactionId = context.ScenarioContext.ScopedId("knowledge-observation", $"{slug}-teacher"),
                Proposition = proposition,
                AcquisitionSource = KnowledgeAcquisitionSource.DirectObservation,
                Provenance = KnowledgeProvenance.DirectObservation,
                Direction = KnowledgeEvidenceDirection.Supports,
                Strength = 950,
                Credibility = 950,
                SourceId = context.ScenarioContext.ScopedId("training-source", "teacher"),
                Visibility = KnowledgeVisibility.Public,
                PrivateAccessAuthorized = true
            });

            string transferTx = context.ScenarioContext.ScopedId("training-transfer", slug);
            return new InformationTransferRequest
            {
                TransactionId = transferTx,
                TransferId = context.ScenarioContext.ScopedId("transfer", slug),
                SenderPersonId = runtimes.PersonId,
                RecipientPersonIds = new[] { runtimes.PersonId },
                ContentItems = new[]
                {
                    new TransferContentItemData
                    {
                        contentItemId = context.ScenarioContext.ScopedId("transfer-content", slug),
                        contentType = InformationTransferContentType.InstructionalConcept,
                        domain = KnowledgeDomain.Professional,
                        proposition = proposition,
                        senderConfidence = 900,
                        senderBeliefState = KnowledgeBeliefState.Known,
                        privacyClassification = KnowledgeVisibility.Public,
                        assertionType = InformationTransferAssertionType.Instruction,
                        typedPayloadId = "procedure.blacksmith.forge-safety",
                        rawEvidenceStrength = 850
                    }
                },
                WorldTimeSeconds = 12d,
                PrivacyScope = TransferPrivacyScope.RecipientOnly,
                SenderKnowledge = runtimes.Knowledge,
                SenderMemory = runtimes.Memory,
                SourceRuntime = null,
                RecipientKnowledgeRuntimes = new Dictionary<string, PersonKnowledgeRuntime> { [runtimes.PersonId] = runtimes.Knowledge },
                RecipientMemoryRuntimes = new Dictionary<string, PersonMemoryRuntime> { [runtimes.PersonId] = runtimes.Memory },
                PrivilegedAccess = true
            };
        }

        private static KnowledgePropositionData TrainingTeachingProposition()
        {
            return new KnowledgePropositionData
            {
                factDefinitionId = BuiltInKnowledgeFacts.SpeciesCapability,
                subjectType = KnowledgeSubjectType.Species,
                subjectId = "species.human",
                valueType = KnowledgeValueType.StableId,
                stableValueId = "capability.profession.blacksmith-safety"
            };
        }

        private static void EnsureBlacksmithActivityProfession(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            if (runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, activeOnly: true).Any(item => string.Equals(item.PersonId, runtimes.PersonId, StringComparison.Ordinal)))
            {
                return;
            }

            runtimes.Professions.AddRelationship(new AddProfessionRelationshipRequest
            {
                relationshipId = context.ScenarioContext.ScopedId("profession-relationship", "activity-blacksmith"),
                personId = runtimes.PersonId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                specializationIds = new[] { PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId },
                informalPractice = true,
                selfDeclared = true,
                active = true,
                startWorldTime = "1",
                transactionId = context.ScenarioContext.ScopedId("profession-tx", "activity-blacksmith")
            });
        }

        private static ProfessionalActivityRegistrationRequest ActivityRequest(
            TestLabAutomationContext context,
            string slug,
            string definitionId,
            ProfessionalActivitySourceSnapshot source,
            ProfessionalResponsibilityLevel responsibility = ProfessionalResponsibilityLevel.IndependentPractitioner,
            TrainingSupervisionLevel supervision = TrainingSupervisionLevel.IndependentWithReview)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            return new ProfessionalActivityRegistrationRequest
            {
                ActivityId = context.ScenarioContext.ScopedId("professional-activity", slug),
                PersonId = runtimes.PersonId,
                ProfessionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                SpecializationId = PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                ActivityDefinitionId = definitionId,
                Source = source,
                Responsibility = responsibility,
                SupervisionLevel = supervision,
                CompletionWorldTime = source?.WorldTime,
                QuantityOrDuration = source?.QuantityOrDuration ?? 1f,
                Quality = source?.Quality ?? 700,
                Difficulty = source?.Difficulty ?? ProfessionalActivityDifficulty.Routine,
                Outcome = source?.Outcome ?? ProfessionalActivityOutcomeState.Successful,
                AccessPolicyId = PrototypeProfessionDefinitionFactory.AccessPublicId,
                Provenance = "test-lab"
            };
        }

        private static ProfessionalActivitySourceSnapshot ActivityCustomSource(
            TestLabAutomationContext context,
            ProfessionalActivitySourceType sourceType,
            string slug,
            string tag,
            ProfessionalActivityDifficulty difficulty = ProfessionalActivityDifficulty.Routine)
        {
            return ProfessionalActivitySourceAdapters.FromCustom(
                sourceType,
                context.ScenarioContext.ScopedId("professional-source", slug),
                context.ScenarioContext.Runtimes.PersonId,
                ProfessionalActivityOutcomeState.Successful,
                quality: difficulty >= ProfessionalActivityDifficulty.Skilled ? 780 : 720,
                difficulty: difficulty,
                worldTime: context.ScenarioContext.ScopedId("professional-time", slug),
                tags: tag);
        }

        private static TrainingModuleDefinitionData TrainingModule(string id, bool required, bool hidden, string[] dependencies = null)
        {
            return new TrainingModuleDefinitionData
            {
                moduleId = id,
                displayName = id,
                required = required,
                hiddenFromLearner = hidden,
                dependencyModuleIds = dependencies ?? Array.Empty<string>()
            };
        }

        private static ProfessionEligibilityContext MedicContext(TestLabAutomationContext context, bool preview)
        {
            return new ProfessionEligibilityContext(
                context.ScenarioContext.Runtimes.PersonId,
                PrototypeProfessionDefinitionFactory.FieldMedicProfessionId,
                PrototypeProfessionDefinitionFactory.FieldMedicRecognitionEntryPathId,
                formal: true,
                authorityId: "authority.medical.prototype",
                worldTime: 2d,
                correlationId: "step10.medic",
                preview: preview,
                skills: new[] { Skill("skill.healing-magic", 1) },
                knowledgeSubjects: new[] { "knowledge.subject.first-aid" });
        }

        private static ProfessionEntrySkillStateData Skill(string skillId, int grade)
        {
            return new ProfessionEntrySkillStateData { skillId = skillId, grade = grade };
        }
    }
}
#endif
