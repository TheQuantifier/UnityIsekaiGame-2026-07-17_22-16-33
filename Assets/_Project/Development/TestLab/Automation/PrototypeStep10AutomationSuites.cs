#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Professions;

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
