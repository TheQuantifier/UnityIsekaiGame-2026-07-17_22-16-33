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
    }
}
#endif
