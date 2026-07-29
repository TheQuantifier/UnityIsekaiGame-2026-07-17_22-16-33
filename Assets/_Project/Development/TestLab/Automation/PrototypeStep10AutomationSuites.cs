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
            registry.TryRegister(BuildQualificationsCredentialsCertificationSuite(), out _);
            registry.TryRegister(BuildProfessionalRanksMasterySpecializationsSuite(), out _);
            registry.TryRegister(BuildPositionsDutiesEmploymentFoundationsSuite(), out _);
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

        private static ITestLabAutomationSuite BuildQualificationsCredentialsCertificationSuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.5.qualifications-credentials-certification",
                "Feature 10.5 Qualifications, Credentials, and Certification",
                "10.5",
                "Credential qualification, application, examination, issuing authority, lifecycle, access, and persistence automation.",
                1050,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "CredentialRuntime", "CredentialDefinition", "CredentialExaminationDefinition", "PersonProfessionRuntime", "TrainingRuntime", "ProfessionalActivityRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    CredentialScenario("definitions-and-qualification", "Credential definitions resolve and qualification evaluates without mutation", 10, Step("step10-credential-qualification", CredentialDefinitionsAndQualification)),
                    CredentialScenario("application-examination-issuance", "Applications, examinations, and issuance produce authoritative credentials", 20, Step("step10-credential-issue", CredentialApplicationExaminationIssuance)),
                    CredentialScenario("stale-unauthorized-forgery", "Unauthorized issuers, stale qualifications, and forged claims are rejected safely", 30, Step("step10-credential-boundaries", CredentialStaleUnauthorizedForgery)),
                    CredentialScenario("lifecycle-permissions", "Expiration, suspension, reinstatement, revocation, and renewal affect permissions", 40, Step("step10-credential-lifecycle", CredentialLifecyclePermissions)),
                    CredentialScenario("access-persistence", "Credential projections redact and persistence restores atomically", 50, Step("step10-credential-persistence", CredentialAccessPersistence))
                });
        }

        private static ITestLabAutomationScenario CredentialScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId,
                    PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId,
                    PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId,
                    PrototypeProfessionDefinitionFactory.BlacksmithWrittenExaminationId,
                    PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId,
                    PrototypeProfessionDefinitionFactory.BlacksmithSupervisedPracticeActivityDefinitionId
                });
        }

        private static ITestLabAutomationSuite BuildProfessionalRanksMasterySpecializationsSuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.6.professional-ranks-mastery-specializations",
                "Feature 10.6 Professional Ranks, Mastery, and Specializations",
                "10.6",
                "Professional rank ladders, advancement, specialization progression, mastery, permissions, lifecycle, access, and persistence automation.",
                1060,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ProfessionalRankRuntime", "ProfessionalRankDefinition", "ProfessionalRankLadderDefinition", "ProfessionalMasteryDefinition", "CredentialRuntime", "TrainingRuntime", "ProfessionalActivityRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    RankScenario("definitions-ladders", "Rank definitions, ladders, and mastery definitions validate", 10, Step("step10-rank-definitions", RankDefinitionsLadders)),
                    RankScenario("advancement-application-promotion", "Advancement application and promotion revalidate requirements", 20, Step("step10-rank-promotion", RankAdvancementApplicationPromotion)),
                    RankScenario("specialization-mastery", "Specialization ranks and mastery use explicit evidence", 30, Step("step10-rank-mastery", RankSpecializationMastery)),
                    RankScenario("lifecycle-permissions-boundaries", "Rank lifecycle controls permissions without granting competencies", 40, Step("step10-rank-lifecycle", RankLifecyclePermissionsBoundaries)),
                    RankScenario("access-persistence", "Rank projections redact and persistence restores atomically", 50, Step("step10-rank-persistence", RankAccessPersistence))
                });
        }

        private static ITestLabAutomationScenario RankScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId,
                    PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId,
                    PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithRankMasterId,
                    PrototypeProfessionDefinitionFactory.BlacksmithRankLadderId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithRankLadderId,
                    PrototypeProfessionDefinitionFactory.WeaponsmithMasteryId
                });
        }

        private static ITestLabAutomationSuite BuildPositionsDutiesEmploymentFoundationsSuite()
        {
            return new TestLabAutomationSuite(
                "feature.10.7.positions-duties-employment-foundations",
                "Feature 10.7 Positions, Duties, and Employment Foundations",
                "10.7",
                "Position definitions, exact organization positions, employment records, duties, authority, lifecycle, access, and persistence automation.",
                1070,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "PositionEmploymentRuntime", "PositionDefinition", "DutyDefinition", "ProfessionalRankRuntime", "CredentialRuntime", "ProfessionalActivityRuntime" },
                scenarios: new ITestLabAutomationScenario[]
                {
                    PositionScenario("definitions-and-vacancies", "Position and duty definitions validate and vacant fixtures are created", 10, Step("step10-position-definitions", PositionDefinitionsAndVacancies)),
                    PositionScenario("eligibility-applications-appointments", "Eligibility, application, offer, acceptance, and direct appointment are integrated", 20, Step("step10-position-appointment", PositionEligibilityApplicationsAppointments)),
                    PositionScenario("capacity-conflicts-duties-authority", "Capacity, conflicts, duties, activity evidence, and authority are enforced", 30, Step("step10-position-duties", PositionCapacityConflictsDutiesAuthority)),
                    PositionScenario("reporting-lifecycle-persistence", "Reporting, lifecycle, projections, and persistence are safe", 40, Step("step10-position-persistence", PositionReportingLifecyclePersistence))
                });
        }

        private static ITestLabAutomationScenario PositionScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId,
                    PrototypeProfessionDefinitionFactory.GuildClerkPositionId,
                    PrototypeProfessionDefinitionFactory.ApprenticeSupervisorPositionId,
                    PrototypeProfessionDefinitionFactory.SeniorSmithCraftDutyId,
                    PrototypeProfessionDefinitionFactory.GuildClerkRecordDutyId
                });
        }

        private static TestLabAutomationStepResult PositionDefinitionsAndVacancies(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in PrototypeProfessionDefinitionFactory.CreateDefinitions().OfType<IGameDefinition>())
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(runtimes.DefinitionRegistry.DefinitionsById, report);
                }
            }

            bool senior = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, out PositionDefinition seniorDefinition);
            bool clerk = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.GuildClerkPositionId, out PositionDefinition clerkDefinition);
            bool duty = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.SeniorSmithCraftDutyId, out DutyDefinition craftDuty);
            PositionEmploymentOperationResult vacant = CreatePosition(context, "vacant-senior", PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, 1);
            PositionEmploymentOperationResult shared = CreatePosition(context, "vacant-clerk", PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, 2);

            bool valid = report.ErrorCount == 0
                && report.WarningCount == 0
                && senior
                && clerk
                && duty
                && seniorDefinition != null
                && seniorDefinition.RequiredRankDefinitionIds.Contains(PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId)
                && seniorDefinition.RequiredCredentialDefinitionIds.Contains(PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId)
                && seniorDefinition.RequiredTrainingProgramIds.Contains(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId)
                && seniorDefinition.ExperienceRequirement.minimumValidatedActivities >= 2
                && clerkDefinition != null
                && clerkDefinition.SharedPositionAllowed
                && craftDuty != null
                && craftDuty.PositionDefinitionId == PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId
                && vacant.Succeeded
                && vacant.Position?.state == PositionInstanceState.Vacant
                && shared.Succeeded
                && shared.Position?.maximumHolders == 2;
            return TestLabAssertions.True("step10-position-definitions", "Position and duty definitions validate and vacant position fixtures are created", valid, $"Errors={report.ErrorCount} Warnings={report.WarningCount} Senior={senior} Clerk={clerk} Duty={duty} Vacant={vacant.Status} Shared={shared.Status}");
        }

        private static TestLabAutomationStepResult PositionEligibilityApplicationsAppointments(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            PositionEmploymentRuntime runtime = runtimes.PositionEmployment;
            PositionEmploymentOperationResult position = CreatePosition(context, "appointment-senior", PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, 1);
            long beforeEligibilityRevision = runtime.Revision;
            PositionEligibilityResult before = runtime.EvaluateEligibility(runtimes.PersonId, position.Position?.positionInstanceId, perceived: true, privilegedDiagnostics: false);
            bool noEligibilityMutation = beforeEligibilityRevision == runtime.Revision;
            EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, "position-appointment");
            EnsureGuildLicense(context, "position-appointment");
            PositionEligibilityResult authoritative = runtime.EvaluateEligibility(runtimes.PersonId, position.Position?.positionInstanceId, privilegedDiagnostics: true);
            PositionEligibilityResult perceived = runtime.EvaluateEligibility(runtimes.PersonId, position.Position?.positionInstanceId, perceived: true, privilegedDiagnostics: false);
            PositionEmploymentOperationResult apply = runtime.SubmitApplication(context.ScenarioContext.ScopedId("position-application", "senior"), runtimes.PersonId, position.Position?.positionInstanceId, authoritative.Snapshot, "100", context.ScenarioContext.ScopedId("position-tx", "apply"));
            PositionEmploymentOperationResult offer = runtime.OfferPosition(apply.Application?.requestId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, "101", context.ScenarioContext.ScopedId("position-tx", "offer"));
            PositionEmploymentOperationResult accept = runtime.AcceptOffer(apply.Application?.requestId, runtimes.PersonId, "102", context.ScenarioContext.ScopedId("position-tx", "accept"));
            PositionEmploymentOperationResult staleAppointment = runtime.AppointPerson(context.ScenarioContext.ScopedId("employment", "stale"), apply.Application?.requestId, runtimes.PersonId, position.Position?.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, authoritative.Snapshot, "103", context.ScenarioContext.ScopedId("position-tx", "stale"));
            PositionEligibilityResult currentEligibility = runtime.EvaluateEligibility(runtimes.PersonId, position.Position?.positionInstanceId, privilegedDiagnostics: true);
            PositionEligibilitySnapshotData tampered = currentEligibility.Snapshot.Clone();
            tampered.evaluationHash = "stale";
            PositionEmploymentOperationResult tamperedAppointment = runtime.AppointPerson(context.ScenarioContext.ScopedId("employment", "tampered"), apply.Application?.requestId, runtimes.PersonId, position.Position?.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, tampered, "104", context.ScenarioContext.ScopedId("position-tx", "tampered"));
            PositionEmploymentOperationResult unauthorized = runtime.AppointPerson(context.ScenarioContext.ScopedId("employment", "unauthorized"), apply.Application?.requestId, runtimes.PersonId, position.Position?.positionInstanceId, "authority.bad", currentEligibility.Snapshot, "105", context.ScenarioContext.ScopedId("position-tx", "unauthorized"));
            PositionEmploymentOperationResult appoint = runtime.AppointPerson(context.ScenarioContext.ScopedId("employment", "senior"), apply.Application?.requestId, runtimes.PersonId, position.Position?.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, currentEligibility.Snapshot, "106", context.ScenarioContext.ScopedId("position-tx", "appoint"));

            bool boundaries = runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).Count == 1
                && runtimes.Credentials.QueryByRecipient(runtimes.PersonId, activeOnly: true).Any()
                && runtimes.ProfessionalRanks.QueryByPerson(runtimes.PersonId, currentOnly: true).Any()
                && runtimes.Knowledge.KnowledgeRevision == 0;
            bool valid = position.Succeeded
                && !before.AuthoritativeEligible
                && noEligibilityMutation
                && authoritative.AuthoritativeEligible
                && perceived.PerceivedEligible
                && apply.Succeeded
                && offer.Succeeded
                && accept.Succeeded
                && !staleAppointment.Succeeded
                && staleAppointment.Status == PositionEmploymentOperationStatus.StaleEvaluation
                && !tamperedAppointment.Succeeded
                && tamperedAppointment.Status == PositionEmploymentOperationStatus.StaleEvaluation
                && !unauthorized.Succeeded
                && unauthorized.Status == PositionEmploymentOperationStatus.UnauthorizedAuthority
                && appoint.Succeeded
                && appoint.Employment?.state == EmploymentState.Active
                && boundaries;
            return TestLabAssertions.True("step10-position-appointment", "Eligibility, applications, offers, acceptance, stale checks, authority, and employment boundaries are integrated", valid, $"Before={before.AuthoritativeEligible} Auth={authoritative.AuthoritativeEligible} Perceived={perceived.PerceivedEligible} Apply={apply.Status} Offer={offer.Status} Accept={accept.Status} Stale={staleAppointment.Status} Tampered={tamperedAppointment.Status} Unauthorized={unauthorized.Status} Appoint={appoint.Status} Rev={beforeEligibilityRevision}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult PositionCapacityConflictsDutiesAuthority(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            PositionEmploymentRuntime runtime = runtimes.PositionEmployment;
            EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, "position-duty");
            EnsureGuildLicense(context, "position-duty");
            PositionEmploymentOperationResult senior = AppointEligiblePerson(context, "duty-senior", PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId);
            PositionEmploymentOperationResult overflow = AppointEligiblePerson(context, "duty-overflow", PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId);
            PositionEmploymentOperationResult clerk = AppointEligiblePerson(context, "duty-clerk", PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult duplicateSamePosition = runtime.AppointPerson(context.ScenarioContext.ScopedId("employment", "duplicate-holder"), string.Empty, runtimes.PersonId, senior.Position?.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, runtime.EvaluateEligibility(runtimes.PersonId, senior.Position?.positionInstanceId, privilegedDiagnostics: true).Snapshot, "130", context.ScenarioContext.ScopedId("position-tx", "duplicate-holder"));
            PositionEmploymentOperationResult duty = runtime.AssignDuty(context.ScenarioContext.ScopedId("position-duty", "craft"), senior.Employment?.employmentId, PrototypeProfessionDefinitionFactory.SeniorSmithCraftDutyId, "131", context.ScenarioContext.ScopedId("position-tx", "assign-duty"));
            ProfessionalActivityOperationResult activity = runtimes.ProfessionalActivities.RegisterAndValidateActivity(
                ActivityRequest(context, "position-duty-craft", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "position-duty-craft", "production.activity.forging"), ProfessionalResponsibilityLevel.IndependentPractitioner),
                context.ScenarioContext.ScopedId("professional-evidence", "position-duty-craft"),
                "authority.guild.prototype",
                context.ScenarioContext.ScopedId("professional-tx", "position-duty-craft"));
            PositionEmploymentOperationResult complete = runtime.CompleteDutyWithEvidence(duty.Duty?.assignmentId, new[] { activity.Evidence?.evidenceId }, "132", context.ScenarioContext.ScopedId("position-tx", "complete-duty"));
            PositionEmploymentOperationResult superviseDuty = runtime.AssignDuty(context.ScenarioContext.ScopedId("position-duty", "supervise"), senior.Employment?.employmentId, PrototypeProfessionDefinitionFactory.SeniorSmithSuperviseDutyId, "133", context.ScenarioContext.ScopedId("position-tx", "assign-supervise"));
            PositionEmploymentOperationResult delegated = runtime.DelegateDuty(superviseDuty.Duty?.assignmentId, runtimes.PersonId, runtimes.PersonId, "134", context.ScenarioContext.ScopedId("position-tx", "delegate-duty"));
            bool authorityActive = runtime.HasActiveAuthority(runtimes.PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);
            PositionEmploymentOperationResult suspended = runtime.SuspendEmployment(senior.Employment?.employmentId, "135", context.ScenarioContext.ScopedId("position-tx", "suspend"));
            bool authoritySuspended = runtime.HasActiveAuthority(runtimes.PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);
            PositionEmploymentOperationResult reinstated = runtime.ReinstateEmployment(senior.Employment?.employmentId, "136", context.ScenarioContext.ScopedId("position-tx", "reinstate"));
            bool authorityReinstated = runtime.HasActiveAuthority(runtimes.PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);

            bool valid = senior.Succeeded
                && !overflow.Succeeded
                && (overflow.Status == PositionEmploymentOperationStatus.CapacityExceeded || overflow.Status == PositionEmploymentOperationStatus.EmploymentConflict || overflow.Status == PositionEmploymentOperationStatus.MissingRequirement || overflow.Status == PositionEmploymentOperationStatus.StaleEvaluation)
                && clerk.Succeeded
                && !duplicateSamePosition.Succeeded
                && duty.Succeeded
                && activity.Succeeded
                && complete.Succeeded
                && complete.Duty?.state == DutyAssignmentState.Completed
                && delegated.Succeeded
                && authorityActive
                && suspended.Succeeded
                && !authoritySuspended
                && reinstated.Succeeded
                && authorityReinstated;
            return TestLabAssertions.True("step10-position-duties", "Capacity, conflicts, duties, real activity evidence, delegation, and active-state authority are enforced", valid, $"Senior={senior.Status} Overflow={overflow.Status} Clerk={clerk.Status} Duplicate={duplicateSamePosition.Status} Duty={duty.Status} Activity={activity.Status} Complete={complete.Status} Delegate={delegated.Status} Authority={authorityActive}/{authoritySuspended}/{authorityReinstated}");
        }

        private static TestLabAutomationStepResult PositionReportingLifecyclePersistence(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            PositionEmploymentRuntime runtime = runtimes.PositionEmployment;
            EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId, "position-persistence");
            EnsureGuildLicense(context, "position-persistence");
            PositionEmploymentOperationResult supervisor = AppointEligiblePerson(context, "persistence-supervisor", PrototypeProfessionDefinitionFactory.ApprenticeSupervisorPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId);
            PositionEmploymentOperationResult clerk = AppointEligiblePerson(context, "persistence-clerk", PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult assignSupervisor = runtime.AssignSupervisor(clerk.Position?.positionInstanceId, supervisor.Position?.positionInstanceId, context.ScenarioContext.ScopedId("position-tx", "supervisor"));
            PositionEmploymentOperationResult cycle = runtime.AssignSupervisor(supervisor.Position?.positionInstanceId, clerk.Position?.positionInstanceId, context.ScenarioContext.ScopedId("position-tx", "cycle"));
            PositionEmploymentOperationResult secretDuty = runtime.AssignDuty(context.ScenarioContext.ScopedId("position-duty", "secret-records"), clerk.Employment?.employmentId, PrototypeProfessionDefinitionFactory.GuildClerkRecordDutyId, "149", context.ScenarioContext.ScopedId("position-tx", "secret-duty"));
            PositionEmploymentProjection<DutyAssignmentData> publicProjection = runtime.ProjectDuty(secretDuty.Duty?.assignmentId, PositionEmploymentProjectionAudience.Public, null);
            PositionEmploymentOperationResult resign = runtime.Resign(clerk.Employment?.employmentId, "150", context.ScenarioContext.ScopedId("position-tx", "resign"));
            bool authorityEnded = runtime.HasActiveAuthority(runtimes.PersonId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult close = runtime.ClosePosition(clerk.Position?.positionInstanceId, "151", context.ScenarioContext.ScopedId("position-tx", "close"), forceEndActiveEmployment: true);
            PositionEmploymentRuntimeSaveData save = runtime.CreateSaveData();
            PositionEmploymentRuntime restored = new PositionEmploymentRuntime();
            PositionEmploymentOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.Credentials, runtimes.ProfessionalRanks, runtimes.KnownPersonIds, new[] { "organization.prototype.guild", "organization.prototype.royal-forge" }, new[] { "authority.guild.prototype", PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId }, restoring: true);
            PositionEmploymentRuntimeSaveData corrupt = save.Clone();
            corrupt.employments[0].personId = "person.missing";
            int beforeCount = restored.EmploymentCount;
            long beforeRevision = restored.Revision;
            PositionEmploymentOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.Credentials, runtimes.ProfessionalRanks, runtimes.KnownPersonIds, new[] { "organization.prototype.guild", "organization.prototype.royal-forge" }, new[] { "authority.guild.prototype", PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId }, restoring: true);
            PositionEmploymentOperationResult legacyEmpty = new PositionEmploymentRuntime().RestoreFromSaveData(null, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.Credentials, runtimes.ProfessionalRanks, runtimes.KnownPersonIds, Array.Empty<string>(), Array.Empty<string>(), restoring: true);

            bool valid = supervisor.Succeeded
                && clerk.Succeeded
                && assignSupervisor.Succeeded
                && !cycle.Succeeded
                && cycle.Status == PositionEmploymentOperationStatus.ReportingCycle
                && secretDuty.Succeeded
                && publicProjection.Record != null
                && publicProjection.Redacted
                && resign.Succeeded
                && !authorityEnded
                && close.Succeeded
                && close.Position?.state == PositionInstanceState.Closed
                && restore.Succeeded
                && restored.EmploymentCount == save.employments.Count
                && restored.PositionCount == save.positions.Count
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.EmploymentCount == beforeCount
                && restored.Revision == beforeRevision
                && legacyEmpty.Succeeded;
            return TestLabAssertions.True("step10-position-persistence", "Reporting, lifecycle transitions, redacted projections, and persistence restore safely", valid, $"Supervisor={supervisor.Status} Clerk={clerk.Status} Assign={assignSupervisor.Status} Cycle={cycle.Status} ProjectionRedacted={publicProjection.Redacted} Resign={resign.Status} Close={close.Status} Restore={restore.Status} Reject={rejected.Status} Legacy={legacyEmpty.Status}");
        }

        private static TestLabAutomationStepResult RankDefinitionsLadders(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in PrototypeProfessionDefinitionFactory.CreateDefinitions().OfType<IGameDefinition>())
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(runtimes.DefinitionRegistry.DefinitionsById, report);
                }
            }

            bool hasApprentice = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, out ProfessionalRankDefinition apprentice);
            bool hasLadder = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.BlacksmithRankLadderId, out ProfessionalRankLadderDefinition ladder);
            bool hasMastery = runtimes.DefinitionRegistry.TryGet(PrototypeProfessionDefinitionFactory.WeaponsmithMasteryId, out ProfessionalMasteryDefinition mastery);
            bool hasDefinitions = hasApprentice && hasLadder && hasMastery;
            long revision = runtimes.ProfessionalRanks.Revision;
            ProfessionalRankAdvancementResult evaluation = runtimes.ProfessionalRanks.EvaluateAdvancement(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, "authority.guild.prototype", privilegedDiagnostics: true);
            bool nonMutating = runtimes.ProfessionalRanks.Revision == revision;
            bool valid = report.ErrorCount == 0
                && report.WarningCount == 0
                && hasDefinitions
                && apprentice != null
                && apprentice.RankOrder == 10
                && ladder != null
                && ladder.OrderedRankDefinitionIds.SequenceEqual(new[] { PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId })
                && mastery != null
                && mastery.RequiredRankDefinitionId == PrototypeProfessionDefinitionFactory.WeaponsmithRankMasterId
                && !evaluation.AuthoritativeEligible
                && nonMutating;
            return TestLabAssertions.True("step10-rank-definitions", "Rank definitions, ladders, mastery, and non-mutating evaluation validate", valid, $"Errors={report.ErrorCount} Warnings={report.WarningCount} Has={hasDefinitions} Eligible={evaluation.AuthoritativeEligible} Rev={revision}->{runtimes.ProfessionalRanks.Revision}");
        }

        private static TestLabAutomationStepResult RankAdvancementApplicationPromotion(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureApprenticeRankFoundation(context, "promotion");
            ProfessionalRankAdvancementResult evaluation = runtimes.ProfessionalRanks.EvaluateAdvancement(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, "authority.guild.prototype", privilegedDiagnostics: true);
            ProfessionalRankOperationResult submit = runtimes.ProfessionalRanks.SubmitApplication(context.ScenarioContext.ScopedId("rank-application", "apprentice"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, "authority.guild.prototype", evaluation.Snapshot, "40", context.ScenarioContext.ScopedId("rank-tx", "submit"));
            ProfessionalRankAdvancementSnapshotData staleSnapshot = evaluation.Snapshot?.Clone() ?? new ProfessionalRankAdvancementSnapshotData();
            staleSnapshot.evaluationHash = "stale";
            ProfessionalRankOperationResult stale = runtimes.ProfessionalRanks.ApprovePromotion(submit.Application?.applicationId, runtimes.PersonId, staleSnapshot, "41", context.ScenarioContext.ScopedId("rank-tx", "stale"));
            ProfessionalRankOperationResult approve = runtimes.ProfessionalRanks.ApprovePromotion(submit.Application?.applicationId, runtimes.PersonId, evaluation.Snapshot, "42", context.ScenarioContext.ScopedId("rank-tx", "approve"));
            ProfessionalRankOperationResult promote = runtimes.ProfessionalRanks.PromotePerson(context.ScenarioContext.ScopedId("rank-record", "apprentice"), submit.Application?.applicationId, evaluation.Snapshot, "43", context.ScenarioContext.ScopedId("rank-tx", "promote"));
            bool historicalPreserved = runtimes.ProfessionalRanks.QueryByPerson(runtimes.PersonId).Any(item => item.rankDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId);
            bool valid = evaluation.AuthoritativeEligible
                && submit.Succeeded
                && !stale.Succeeded
                && stale.Status == ProfessionalRankOperationStatus.StaleEvaluation
                && approve.Succeeded
                && promote.Succeeded
                && historicalPreserved
                && !runtimes.Credentials.QueryByRecipient(runtimes.PersonId, activeOnly: true).Any(item => item.credentialDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId);
            return TestLabAssertions.True("step10-rank-promotion", "Rank application and promotion revalidate current state", valid, $"Eval={evaluation.AuthoritativeEligible} Submit={submit.Status} Stale={stale.Status} Approve={approve.Status} Promote={promote.Status}");
        }

        private static TestLabAutomationStepResult RankSpecializationMastery(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionalRankOperationResult apprentice = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, "mastery-apprentice");
            ProfessionalRankOperationResult journeyman = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, "mastery-journey");
            EnsureGuildLicense(context, "mastery-license");
            ProfessionalRankOperationResult weaponsmithApprentice = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.WeaponsmithRankApprenticeId, "weaponsmith-apprentice");
            ProfessionalRankOperationResult weaponsmithMaster = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.WeaponsmithRankMasterId, "weaponsmith-master");
            ProfessionalRankOperationResult achievement = runtimes.ProfessionalRanks.RecordQualifyingAchievement(new ProfessionalQualifyingAchievementData
            {
                achievementId = PrototypeProfessionDefinitionFactory.BlacksmithMasterworkAchievementId,
                personId = runtimes.PersonId,
                professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                specializationId = PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                sourceActivityId = context.ScenarioContext.ScopedId("professional-activity", "masterwork"),
                activityDefinitionId = PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId,
                description = "Prototype masterwork blade",
                quality = 850,
                difficulty = ProfessionalActivityDifficulty.Skilled,
                validatingAuthorityId = "authority.guild.prototype",
                worldTime = "70",
                accessPolicyId = PrototypeProfessionDefinitionFactory.AccessPublicId
            }, context.ScenarioContext.ScopedId("rank-tx", "achievement"));
            ProfessionalRankAdvancementResult masteryEval = runtimes.ProfessionalRanks.EvaluateMastery(runtimes.PersonId, PrototypeProfessionDefinitionFactory.WeaponsmithMasteryId, "authority.guild.prototype", privilegedDiagnostics: true);
            ProfessionalRankOperationResult mastery = runtimes.ProfessionalRanks.GrantMastery(context.ScenarioContext.ScopedId("mastery-record", "weaponsmith"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.WeaponsmithMasteryId, "authority.guild.prototype", masteryEval.Snapshot, "71", context.ScenarioContext.ScopedId("rank-tx", "mastery"));
            bool distinctGeneralAndSpecialization = runtimes.ProfessionalRanks.QueryByPerson(runtimes.PersonId, currentOnly: true).Any(item => item.rankDefinitionId == PrototypeProfessionDefinitionFactory.WeaponsmithRankMasterId)
                && runtimes.ProfessionalRanks.QueryByPerson(runtimes.PersonId).Any(item => item.rankDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId);
            bool valid = apprentice.Succeeded
                && journeyman.Succeeded
                && weaponsmithApprentice.Succeeded
                && weaponsmithMaster.Succeeded
                && achievement.Succeeded
                && masteryEval.AuthoritativeEligible
                && mastery.Succeeded
                && distinctGeneralAndSpecialization;
            return TestLabAssertions.True("step10-rank-mastery", "Specialization rank and mastery use explicit evidence", valid, $"Apprentice={apprentice.Status} Journey={journeyman.Status} WeaponMaster={weaponsmithMaster.Status} Achievement={achievement.Status} Mastery={mastery.Status}");
        }

        private static TestLabAutomationStepResult RankLifecyclePermissionsBoundaries(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionalRankOperationResult promote = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId, "lifecycle-master");
            string rankId = promote.Rank?.rankRecordId ?? string.Empty;
            bool teachActive = runtimes.ProfessionalRanks.CanTeach(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);
            ProfessionalRankOperationResult suspend = runtimes.ProfessionalRanks.SuspendRank(rankId, "80", context.ScenarioContext.ScopedId("rank-tx", "suspend"));
            bool teachSuspended = runtimes.ProfessionalRanks.CanTeach(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);
            ProfessionalRankOperationResult reinstate = runtimes.ProfessionalRanks.ReinstateRank(rankId, "81", context.ScenarioContext.ScopedId("rank-tx", "reinstate"));
            bool teachReinstated = runtimes.ProfessionalRanks.CanTeach(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);
            ProfessionalRankOperationResult demote = runtimes.ProfessionalRanks.DemotePerson(rankId, context.ScenarioContext.ScopedId("rank-record", "demoted-journeyman"), PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, "82", context.ScenarioContext.ScopedId("rank-tx", "demote"));
            bool teachDemoted = runtimes.ProfessionalRanks.CanTeach(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId);
            ProfessionalRankOperationResult revoke = runtimes.ProfessionalRanks.RevokeRank(demote.Rank?.rankRecordId, "83", context.ScenarioContext.ScopedId("rank-tx", "revoke"));
            bool skillsUnaffected = true;
            bool credentialsUnaffected = runtimes.Credentials.CredentialCount > 0;
            bool valid = promote.Succeeded
                && teachActive
                && suspend.Succeeded
                && !teachSuspended
                && reinstate.Succeeded
                && teachReinstated
                && demote.Succeeded
                && !teachDemoted
                && revoke.Succeeded
                && skillsUnaffected
                && credentialsUnaffected;
            return TestLabAssertions.True("step10-rank-lifecycle", "Rank lifecycle affects rank permissions without granting competencies", valid, $"Promote={promote.Status} Active={teachActive} Suspended={teachSuspended} Reinstated={teachReinstated} Demote={demote.Status} Revoked={revoke.Status}");
        }

        private static TestLabAutomationStepResult RankAccessPersistence(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionalRankOperationResult promote = EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, "persistence-rank");
            ProfessionalRankProjection<ProfessionalRankRecordData> projection = runtimes.ProfessionalRanks.ProjectRank(promote.Rank?.rankRecordId, ProfessionalRankProjectionAudience.Public, null);
            ProfessionalRankRuntimeSaveData save = runtimes.ProfessionalRanks.CreateSaveData();
            ProfessionalRankRuntime restored = new ProfessionalRankRuntime();
            ProfessionalRankOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.Credentials, runtimes.KnownPersonIds, new[] { "authority.guild.prototype" }, restoring: true);
            ProfessionalRankRuntimeSaveData corrupt = save.Clone();
            corrupt.ranks[0].rankDefinitionId = "profession-rank.missing";
            ProfessionalRankOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.Credentials, runtimes.KnownPersonIds, new[] { "authority.guild.prototype" }, restoring: true);
            bool valid = promote.Succeeded
                && projection.Record != null
                && restore.Succeeded
                && restored.QueryByPerson(runtimes.PersonId).Count == 1
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.QueryByPerson(runtimes.PersonId).Count == 1;
            return TestLabAssertions.True("step10-rank-persistence", "Rank projections and persistence restore safely without replay", valid, $"Promote={promote.Status} Restore={restore.Status} Rejected={rejected.Status} Count={restored.QueryByPerson(runtimes.PersonId).Count}");
        }

        private static TestLabAutomationStepResult CredentialDefinitionsAndQualification(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in PrototypeProfessionDefinitionFactory.CreateDefinitions().OfType<IGameDefinition>())
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(runtimes.DefinitionRegistry.DefinitionsById, report);
                }
            }

            long professionRevision = runtimes.Professions.Revision;
            long trainingRevision = runtimes.Training.Revision;
            long activityRevision = runtimes.ProfessionalActivities.Revision;
            long credentialRevision = runtimes.Credentials.Revision;
            CredentialQualificationResult before = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, privilegedDiagnostics: true);
            bool nonMutatingInitial = runtimes.Professions.Revision == professionRevision
                && runtimes.Training.Revision == trainingRevision
                && runtimes.ProfessionalActivities.Revision == activityRevision
                && runtimes.Credentials.Revision == credentialRevision;

            EnsureBlacksmithCredentialFoundation(context, "qualification");
            CredentialQualificationResult afterFoundation = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, privilegedDiagnostics: true);
            RecordCredentialExam(context, "qualification", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 820);
            CredentialQualificationResult qualified = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, privilegedDiagnostics: true);

            bool valid = report.ErrorCount == 0
                && report.WarningCount == 0
                && !before.AuthoritativeQualified
                && !afterFoundation.AuthoritativeQualified
                && afterFoundation.BlockingFailures.Contains($"examination:{PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId}")
                && qualified.AuthoritativeQualified
                && nonMutatingInitial;
            return TestLabAssertions.True("step10-credential-qualification", "Credential definitions resolve and qualification evaluates without mutation", valid, $"Errors={report.ErrorCount} Warnings={report.WarningCount} Before={before.AuthoritativeQualified} AfterFoundation={afterFoundation.AuthoritativeQualified} Qualified={qualified.AuthoritativeQualified} NonMutating={nonMutatingInitial}");
        }

        private static TestLabAutomationStepResult CredentialApplicationExaminationIssuance(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithCredentialFoundation(context, "issue");
            CredentialOperationResult exam = RecordCredentialExam(context, "issue", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
            CredentialQualificationResult qualification = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId);
            CredentialOperationResult apply = runtimes.Credentials.SubmitApplication(context.ScenarioContext.ScopedId("credential-application", "issue"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, GuildIssuer(), qualification.Snapshot, "30", context.ScenarioContext.ScopedId("credential-tx", "apply"));
            CredentialOperationResult approve = runtimes.Credentials.ApproveApplication(apply.Application?.applicationId, "authority.guild.prototype", qualification.Snapshot, "31", context.ScenarioContext.ScopedId("credential-tx", "approve"));
            CredentialOperationResult issue = runtimes.Credentials.IssueCredential(context.ScenarioContext.ScopedId("credential-record", "issue"), PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, runtimes.PersonId, GuildIssuer(), apply.Application?.applicationId, exam.ExaminationAttempt?.attemptId, context.ScenarioContext.ScopedId("registration", "issue"), qualification.Snapshot, "32", context.ScenarioContext.ScopedId("credential-tx", "issue"));
            bool permission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId);

            bool valid = exam.Succeeded
                && qualification.AuthoritativeQualified
                && apply.Succeeded
                && approve.Succeeded
                && issue.Succeeded
                && issue.Credential?.authenticityState == CredentialAuthenticityState.Authoritative
                && permission
                && runtimes.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).Count == 1;
            return TestLabAssertions.True("step10-credential-issue", "Applications, examinations, and issuance produce authoritative credentials", valid, $"Exam={exam.Status} Qualified={qualification.AuthoritativeQualified} Apply={apply.Status} Approve={approve.Status} Issue={issue.Status} Permission={permission}");
        }

        private static TestLabAutomationStepResult CredentialStaleUnauthorizedForgery(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithCredentialFoundation(context, "boundary");
            CredentialOperationResult exam = RecordCredentialExam(context, "boundary", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
            CredentialQualificationResult qualification = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId);
            CredentialOperationResult badIssuer = runtimes.Credentials.SubmitApplication(context.ScenarioContext.ScopedId("credential-application", "bad-issuer"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, new CredentialIssuerReferenceData { issuerId = "authority.medical.prototype", issuerKind = CredentialIssuerAuthorityKind.ProfessionalOrganization }, qualification.Snapshot, "33", context.ScenarioContext.ScopedId("credential-tx", "bad-issuer"));
            CredentialOperationResult apply = runtimes.Credentials.SubmitApplication(context.ScenarioContext.ScopedId("credential-application", "stale"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, GuildIssuer(), qualification.Snapshot, "34", context.ScenarioContext.ScopedId("credential-tx", "stale-apply"));
            runtimes.ProfessionalActivities.RegisterAndValidateActivity(ActivityRequest(context, "stale-extra", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, "stale-extra", "production.activity.forging")), context.ScenarioContext.ScopedId("professional-evidence", "stale-extra"), "authority.guild.prototype", context.ScenarioContext.ScopedId("professional-tx", "stale-extra"));
            CredentialQualificationResult current = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId);
            CredentialOperationResult staleApprove = runtimes.Credentials.ApproveApplication(apply.Application?.applicationId, "authority.guild.prototype", current.Snapshot, "35", context.ScenarioContext.ScopedId("credential-tx", "stale-approve"));
            CredentialOperationResult forged = runtimes.Credentials.RecordForgedClaim(context.ScenarioContext.ScopedId("credential-forged", "claim"), PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, runtimes.PersonId, "authority.guild.prototype", "36", context.ScenarioContext.ScopedId("credential-tx", "forged"));
            bool forgedPermission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId, CredentialPermissionStatePolicy.AnyNonRevoked);

            bool valid = exam.Succeeded
                && !badIssuer.Succeeded
                && badIssuer.Status == CredentialOperationStatus.UnauthorizedIssuer
                && apply.Succeeded
                && !staleApprove.Succeeded
                && staleApprove.Status == CredentialOperationStatus.StaleQualification
                && forged.Succeeded
                && forged.Credential?.authenticityState == CredentialAuthenticityState.ForgedClaim
                && !forgedPermission;
            return TestLabAssertions.True("step10-credential-boundaries", "Unauthorized issuers, stale qualifications, and forged claims are rejected safely", valid, $"Exam={exam.Status} BadIssuer={badIssuer.Status} Apply={apply.Status} Stale={staleApprove.Status} Forged={forged.Status} ForgedPermission={forgedPermission}");
        }

        private static TestLabAutomationStepResult CredentialLifecyclePermissions(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            CredentialOperationResult issued = IssueApprenticeshipCredential(context, "lifecycle", out _);
            string credentialId = issued.Credential?.credentialId;
            bool activePermission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId);
            CredentialOperationResult suspend = runtimes.Credentials.SuspendCredential(credentialId, "40", context.ScenarioContext.ScopedId("credential-tx", "suspend"));
            bool suspendedPermission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId);
            CredentialOperationResult reinstate = runtimes.Credentials.ReinstateCredential(credentialId, "41", context.ScenarioContext.ScopedId("credential-tx", "reinstate"));
            bool reinstatedPermission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId);
            CredentialOperationResult renew = runtimes.Credentials.RenewCredential(credentialId, null, "42", context.ScenarioContext.ScopedId("credential-tx", "renew"));
            CredentialOperationResult expire = runtimes.Credentials.ExpireCredential(credentialId, "43", context.ScenarioContext.ScopedId("credential-tx", "expire"));
            bool expiredPermission = runtimes.Credentials.HasActivePermission(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithPracticePermissionId);
            CredentialOperationResult revoke = runtimes.Credentials.RevokeCredential(credentialId, "44", context.ScenarioContext.ScopedId("credential-tx", "revoke"));
            CredentialOperationResult revokedReinstate = runtimes.Credentials.ReinstateCredential(credentialId, "45", context.ScenarioContext.ScopedId("credential-tx", "revoked-reinstate"));

            bool valid = issued.Succeeded
                && activePermission
                && suspend.Succeeded
                && !suspendedPermission
                && reinstate.Succeeded
                && reinstatedPermission
                && !renew.Succeeded
                && renew.Status == CredentialOperationStatus.InvalidTransition
                && expire.Succeeded
                && !expiredPermission
                && revoke.Succeeded
                && !revokedReinstate.Succeeded
                && revokedReinstate.Status == CredentialOperationStatus.InvalidTransition;
            return TestLabAssertions.True("step10-credential-lifecycle", "Expiration, suspension, reinstatement, revocation, and renewal affect permissions", valid, $"Issued={issued.Status} Active={activePermission} Suspend={suspend.Status} SuspendedPermission={suspendedPermission} Reinstate={reinstate.Status} Renew={renew.Status} Expire={expire.Status} Revoke={revoke.Status} RevokedReinstate={revokedReinstate.Status}");
        }

        private static TestLabAutomationStepResult CredentialAccessPersistence(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            CredentialOperationResult issued = IssueApprenticeshipCredential(context, "persist", out string applicationId);
            InformationAccessDecision decision = new InformationAccessDecision(
                "person.observer",
                CredentialInformationSubject.Create(CredentialInformationSubject.CredentialTag, issued.Credential?.credentialId, runtimes.PersonId),
                InformationAccessMode.Inspect,
                InformationAccessDecisionKind.RedactedAccess,
                InformationAccessDenialCode.DetailRestriction,
                false,
                InformationResharingPolicy.NoResharing,
                new[] { "credential-definition-id", "state" },
                CredentialInformationSubject.ProtectedFields,
                Array.Empty<string>(),
                new[] { PrototypeProfessionDefinitionFactory.AccessPublicId },
                50d,
                "Credential redacted.",
                "Credential supporting evidence hidden.",
                true);
            CredentialProjection<CredentialRecordData> projection = runtimes.Credentials.ProjectCredential(issued.Credential?.credentialId, CredentialProjectionAudience.PublicInspection, decision);
            CredentialRuntimeSaveData save = runtimes.Credentials.CreateSaveData();
            string[] authorities = { "authority.guild.prototype", "authority.medical.prototype", "organization.prototype.guild" };
            CredentialRuntime restored = new CredentialRuntime();
            CredentialOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.KnownPersonIds, authorities, restoring: true);
            CredentialRuntimeSaveData corrupt = save.Clone();
            corrupt.credentials[0].supportingApplicationId = string.Empty;
            CredentialOperationResult rejected = restored.RestoreFromSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Professions, runtimes.Training, runtimes.ProfessionalActivities, runtimes.KnownPersonIds, authorities, restoring: true);

            bool valid = issued.Succeeded
                && !string.IsNullOrWhiteSpace(applicationId)
                && projection.Redacted
                && projection.Record != null
                && string.IsNullOrWhiteSpace(projection.Record.registrationNumber)
                && restore.Succeeded
                && restored.CredentialCount == 1
                && restored.HistoryHooks.Count == 0
                && !rejected.Succeeded
                && restored.CredentialCount == 1;
            return TestLabAssertions.True("step10-credential-persistence", "Credential projections redact and persistence restores atomically", valid, $"Issued={issued.Status} Redacted={projection.Redacted} Restore={restore.Status} Rejected={rejected.Status} Count={restored.CredentialCount}");
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

        private static PositionEmploymentOperationResult CreatePosition(TestLabAutomationContext context, string slug, string positionDefinitionId, string organizationId, string organizationTypeId, int maxHolders)
        {
            return context.ScenarioContext.Runtimes.PositionEmployment.CreatePosition(new PositionInstanceData
            {
                positionInstanceId = context.ScenarioContext.ScopedId("position-instance", slug),
                positionDefinitionId = positionDefinitionId,
                organizationId = organizationId,
                organizationTypeId = organizationTypeId,
                state = PositionInstanceState.Vacant,
                maximumHolders = Math.Max(1, maxHolders),
                vacancyAllowed = true,
                createdWorldTime = context.ScenarioContext.ScopedId("position-time", $"{slug}-created"),
                accessPolicyId = PrototypeProfessionDefinitionFactory.AccessPublicId,
                provenance = "test-lab"
            }, context.ScenarioContext.ScopedId("position-tx", $"{slug}-create"));
        }

        private static PositionEmploymentOperationResult AppointEligiblePerson(TestLabAutomationContext context, string slug, string positionDefinitionId, string organizationId, string organizationTypeId, string authorityId, EmploymentClassification? classification = null)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            PositionEmploymentOperationResult position = CreatePosition(context, slug, positionDefinitionId, organizationId, organizationTypeId, positionDefinitionId == PrototypeProfessionDefinitionFactory.GuildClerkPositionId ? 2 : 1);
            if (!position.Succeeded && !position.Duplicate)
            {
                return position;
            }

            string positionInstanceId = position.Position?.positionInstanceId ?? context.ScenarioContext.ScopedId("position-instance", slug);
            PositionEligibilityResult eligibility = runtimes.PositionEmployment.EvaluateEligibility(runtimes.PersonId, positionInstanceId, privilegedDiagnostics: true);
            return runtimes.PositionEmployment.AppointPerson(context.ScenarioContext.ScopedId("employment", slug), string.Empty, runtimes.PersonId, positionInstanceId, authorityId, eligibility.Snapshot, context.ScenarioContext.ScopedId("position-time", $"{slug}-appoint"), context.ScenarioContext.ScopedId("position-tx", $"{slug}-appoint"), classification);
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
                formalPractice = true,
                informalPractice = true,
                selfDeclared = true,
                recognized = true,
                recognizingAuthorityId = "authority.guild.prototype",
                recognitionReferenceId = context.ScenarioContext.ScopedId("profession-recognition", "activity-blacksmith"),
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

        private static void EnsureBlacksmithCredentialFoundation(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithActivityProfession(context);

            bool completedTraining = runtimes.Training.QueryByProgram(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId)
                .Any(item => string.Equals(item.PersonId, runtimes.PersonId, StringComparison.Ordinal) && item.State == TrainingEnrollmentState.Completed);
            if (!completedTraining)
            {
                string enrollmentId = BeginTrainingApprenticeship(context, $"credential-{slug}");
                CompleteTrainingVisibleRequirements(context, enrollmentId, completePractice: true);
                runtimes.Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithHiddenAssessmentModuleId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-hidden"));
                TrainingProgressResult progress = runtimes.Training.EvaluateProgress(enrollmentId, perceived: false);
                runtimes.Training.CompleteProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"{slug}-complete"), progress.RuntimeToken, worldTime: 24d);
            }

            if (runtimes.ProfessionalActivities.BuildExperienceSummary(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).TotalValidatedActivities == 0)
            {
                runtimes.ProfessionalActivities.RegisterAndValidateActivity(
                    ActivityRequest(
                        context,
                        $"credential-{slug}-supervised",
                        PrototypeProfessionDefinitionFactory.BlacksmithSupervisedPracticeActivityDefinitionId,
                        ActivityCustomSource(context, ProfessionalActivitySourceType.TrainingPracticalAssignment, $"credential-{slug}-practice", "training.activity.practical"),
                        ProfessionalResponsibilityLevel.SupervisedWorker,
                        TrainingSupervisionLevel.CloselySupervised),
                    context.ScenarioContext.ScopedId("professional-evidence", $"credential-{slug}"),
                    "authority.guild.prototype",
                    context.ScenarioContext.ScopedId("professional-tx", $"credential-{slug}"));
            }
        }

        private static CredentialOperationResult RecordCredentialExam(TestLabAutomationContext context, string slug, string examinationDefinitionId, int score)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            return runtimes.Credentials.RecordExaminationAttempt(new CredentialExaminationAttemptData
            {
                attemptId = context.ScenarioContext.ScopedId("credential-exam", slug),
                examinationDefinitionId = examinationDefinitionId,
                applicantPersonId = runtimes.PersonId,
                evaluatorPersonId = runtimes.PersonId,
                evaluatorAuthorityId = "authority.guild.prototype",
                startWorldTime = "25",
                completionWorldTime = "26",
                score = score,
                sectionResults = new[]
                {
                    new CredentialExaminationSectionResultData
                    {
                        sectionId = context.ScenarioContext.ScopedId("credential-exam-section", slug),
                        displayName = "Prototype assessment",
                        score = score,
                        passed = score >= 700
                    }
                },
                provenance = context.ScenarioContext.ScopedId("credential-exam-provenance", slug)
            }, context.ScenarioContext.ScopedId("credential-tx", $"exam-{slug}"));
        }

        private static CredentialOperationResult IssueApprenticeshipCredential(TestLabAutomationContext context, string slug, out string applicationId)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            EnsureBlacksmithCredentialFoundation(context, slug);
            CredentialOperationResult exam = RecordCredentialExam(context, slug, PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
            CredentialQualificationResult qualification = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId);
            CredentialOperationResult apply = runtimes.Credentials.SubmitApplication(context.ScenarioContext.ScopedId("credential-application", slug), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, GuildIssuer(), qualification.Snapshot, "30", context.ScenarioContext.ScopedId("credential-tx", $"{slug}-apply"));
            CredentialOperationResult approve = runtimes.Credentials.ApproveApplication(apply.Application?.applicationId, "authority.guild.prototype", qualification.Snapshot, "31", context.ScenarioContext.ScopedId("credential-tx", $"{slug}-approve"));
            applicationId = apply.Application?.applicationId ?? string.Empty;
            if (!exam.Succeeded || !qualification.AuthoritativeQualified || !apply.Succeeded || !approve.Succeeded)
            {
                return CredentialOperationResult.Failure(CredentialOperationStatus.InvalidRequest, $"Credential fixture setup failed. Exam={exam.Status} Qualified={qualification.AuthoritativeQualified} Apply={apply.Status} Approve={approve.Status}", runtimes.Credentials.Revision, qualification);
            }

            return runtimes.Credentials.IssueCredential(context.ScenarioContext.ScopedId("credential-record", slug), PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, runtimes.PersonId, GuildIssuer(), applicationId, exam.ExaminationAttempt?.attemptId, context.ScenarioContext.ScopedId("registration", slug), qualification.Snapshot, "32", context.ScenarioContext.ScopedId("credential-tx", $"{slug}-issue"));
        }

        private static CredentialIssuerReferenceData GuildIssuer()
        {
            return new CredentialIssuerReferenceData
            {
                issuerId = "authority.guild.prototype",
                issuerKind = CredentialIssuerAuthorityKind.Guild
            };
        }

        private static void EnsureApprenticeRankFoundation(TestLabAutomationContext context, string slug)
        {
            EnsureBlacksmithCredentialFoundation(context, $"rank-{slug}");
        }

        private static ProfessionalRankOperationResult EnsurePromotedRank(TestLabAutomationContext context, string rankDefinitionId, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            ProfessionalRankRecordData existing = runtimes.ProfessionalRanks.QueryByPerson(runtimes.PersonId, currentOnly: true)
                .FirstOrDefault(item => string.Equals(item.rankDefinitionId, rankDefinitionId, StringComparison.Ordinal));
            if (existing != null)
            {
                return ProfessionalRankOperationResult.Success("Rank already active.", runtimes.ProfessionalRanks.Revision, runtimes.ProfessionalRanks.Revision, rank: existing, duplicate: true);
            }

            if (rankDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId)
            {
                EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, $"{slug}-prior");
                if (!runtimes.Credentials.QueryByRecipient(runtimes.PersonId, activeOnly: true).Any(item => item.credentialDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId))
                {
                    IssueApprenticeshipCredential(context, $"{slug}-apprenticeship", out _);
                }
            }
            else if (rankDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId)
            {
                EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, $"{slug}-prior");
                EnsureGuildLicense(context, $"{slug}-license");
            }
            else if (rankDefinitionId == PrototypeProfessionDefinitionFactory.WeaponsmithRankApprenticeId)
            {
                EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, $"{slug}-prior");
                if (!runtimes.Credentials.QueryByRecipient(runtimes.PersonId, activeOnly: true).Any(item => item.credentialDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId))
                {
                    IssueApprenticeshipCredential(context, $"{slug}-apprenticeship", out _);
                }
            }
            else if (rankDefinitionId == PrototypeProfessionDefinitionFactory.WeaponsmithRankMasterId)
            {
                EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, $"{slug}-general-prior");
                EnsurePromotedRank(context, PrototypeProfessionDefinitionFactory.WeaponsmithRankApprenticeId, $"{slug}-special-prior");
                EnsureGuildLicense(context, $"{slug}-license");
            }
            else
            {
                EnsureApprenticeRankFoundation(context, slug);
            }

            ProfessionalRankAdvancementResult evaluation = runtimes.ProfessionalRanks.EvaluateAdvancement(runtimes.PersonId, rankDefinitionId, "authority.guild.prototype", privilegedDiagnostics: true);
            ProfessionalRankOperationResult submit = runtimes.ProfessionalRanks.SubmitApplication(context.ScenarioContext.ScopedId("rank-application", slug), runtimes.PersonId, rankDefinitionId, "authority.guild.prototype", evaluation.Snapshot, "50", context.ScenarioContext.ScopedId("rank-tx", $"{slug}-submit"));
            ProfessionalRankOperationResult approve = runtimes.ProfessionalRanks.ApprovePromotion(submit.Application?.applicationId, runtimes.PersonId, evaluation.Snapshot, "51", context.ScenarioContext.ScopedId("rank-tx", $"{slug}-approve"));
            if (!evaluation.AuthoritativeEligible || !submit.Succeeded || !approve.Succeeded)
            {
                return ProfessionalRankOperationResult.Failure(ProfessionalRankOperationStatus.MissingQualification, $"Rank fixture setup failed. Eligible={evaluation.AuthoritativeEligible} Submit={submit.Status} Approve={approve.Status}", runtimes.ProfessionalRanks.Revision, evaluation);
            }

            return runtimes.ProfessionalRanks.PromotePerson(context.ScenarioContext.ScopedId("rank-record", slug), submit.Application?.applicationId, evaluation.Snapshot, "52", context.ScenarioContext.ScopedId("rank-tx", $"{slug}-promote"));
        }

        private static CredentialOperationResult EnsureGuildLicense(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            CredentialRecordData existing = runtimes.Credentials.QueryByRecipient(runtimes.PersonId, activeOnly: true)
                .FirstOrDefault(item => string.Equals(item.credentialDefinitionId, PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, StringComparison.Ordinal));
            if (existing != null)
            {
                return CredentialOperationResult.Success("Guild license already active.", runtimes.Credentials.Revision, runtimes.Credentials.Revision, credential: existing, duplicate: true);
            }

            EnsureBlacksmithCredentialFoundation(context, $"guild-{slug}");
            EnsureBlacksmithSafetyTraining(context, slug);
            RegisterIndependentBlacksmithActivity(context, $"{slug}-independent");
            RecordCredentialExam(context, $"{slug}-practical", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
            RecordCredentialExam(context, $"{slug}-written", PrototypeProfessionDefinitionFactory.BlacksmithWrittenExaminationId, 840);
            CredentialQualificationResult qualification = runtimes.Credentials.EvaluateQualification(runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, privilegedDiagnostics: true);
            CredentialOperationResult apply = runtimes.Credentials.SubmitApplication(context.ScenarioContext.ScopedId("credential-application", $"guild-{slug}"), runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, GuildIssuer(), qualification.Snapshot, "60", context.ScenarioContext.ScopedId("credential-tx", $"guild-{slug}-apply"));
            CredentialOperationResult approve = runtimes.Credentials.ApproveApplication(apply.Application?.applicationId, "authority.guild.prototype", qualification.Snapshot, "61", context.ScenarioContext.ScopedId("credential-tx", $"guild-{slug}-approve"));
            if (!qualification.AuthoritativeQualified || !apply.Succeeded || !approve.Succeeded)
            {
                return CredentialOperationResult.Failure(CredentialOperationStatus.MissingQualification, $"Guild license fixture failed. Qualified={qualification.AuthoritativeQualified} Apply={apply.Status} Approve={approve.Status}", runtimes.Credentials.Revision, qualification);
            }

            return runtimes.Credentials.IssueCredential(context.ScenarioContext.ScopedId("credential-record", $"guild-{slug}"), PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, runtimes.PersonId, GuildIssuer(), apply.Application?.applicationId, context.ScenarioContext.ScopedId("credential-exam", $"{slug}-practical"), context.ScenarioContext.ScopedId("registration", $"guild-{slug}"), qualification.Snapshot, "62", context.ScenarioContext.ScopedId("credential-tx", $"guild-{slug}-issue"));
        }

        private static void EnsureBlacksmithSafetyTraining(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            bool completed = runtimes.Training.QueryByProgram(PrototypeProfessionDefinitionFactory.BlacksmithSafetyProgramId)
                .Any(item => string.Equals(item.PersonId, runtimes.PersonId, StringComparison.Ordinal) && item.State == TrainingEnrollmentState.Completed);
            if (completed)
            {
                return;
            }

            string enrollmentId = context.ScenarioContext.ScopedId("training-enrollment", $"safety-{slug}");
            runtimes.Training.ApplyToProgram(enrollmentId, runtimes.PersonId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyProgramId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-apply"), worldTime: 33d);
            runtimes.Training.AcceptEnrollment(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-accept"));
            runtimes.Training.BeginProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-begin"));
            runtimes.Training.RunLearningSession(context.ScenarioContext.ScopedId("training-session", $"safety-{slug}"), enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyLessonId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-lesson"));
            runtimes.Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-module"));
            TrainingProgressResult progress = runtimes.Training.EvaluateProgress(enrollmentId, perceived: false);
            runtimes.Training.CompleteProgram(enrollmentId, context.ScenarioContext.ScopedId("training-tx", $"safety-{slug}-complete"), progress.RuntimeToken, worldTime: 36d);
        }

        private static void RegisterIndependentBlacksmithActivity(TestLabAutomationContext context, string slug)
        {
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            runtimes.ProfessionalActivities.RegisterAndValidateActivity(
                ActivityRequest(
                    context,
                    slug,
                    PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId,
                    ActivityCustomSource(context, ProfessionalActivitySourceType.CraftingOperation, slug, "production.activity.forging", ProfessionalActivityDifficulty.Skilled),
                    ProfessionalResponsibilityLevel.IndependentPractitioner,
                    TrainingSupervisionLevel.IndependentWithReview),
                context.ScenarioContext.ScopedId("professional-evidence", slug),
                "authority.guild.prototype",
                context.ScenarioContext.ScopedId("professional-tx", slug));
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
