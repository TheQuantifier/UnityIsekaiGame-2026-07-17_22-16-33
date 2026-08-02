#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityIsekaiGame.Crimes;
using UnityIsekaiGame.Development.Automation;
using UnityIsekaiGame.Diplomacy;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Governments;
using UnityIsekaiGame.Justice;
using UnityIsekaiGame.Laws;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Organizations.Integration;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Tests
{
    public sealed class OrganizationsGovernmentsLawIntegrationFinalizationTests
    {
        private const string CatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";
        private static readonly string[] KnownPersons =
        {
            PersistenceService.LocalPlayerId,
            "person.prototype.friend",
            "person.prototype.guard",
            "person.prototype.magistrate"
        };

        [Test]
        public void ReadinessOwnershipAndPersistenceGraphAreClean()
        {
            using TestLabRuntimeBundle bundle = CreateBundle();
            Step13InstitutionalIntegrationFacade facade = CreateFacade(bundle);

            Step13IntegrationValidationReport report = facade.ValidateComplete();
            Step13ReadinessSnapshot readiness = facade.CreateReadinessSnapshot();

            Assert.That(report.Succeeded, Is.True, string.Join(Environment.NewLine, report.Diagnostics));
            Assert.That(readiness.Status, Is.EqualTo(Step13IntegrationHealthStatus.Ready), string.Join(Environment.NewLine, readiness.Diagnostics));
            Assert.That(facade.OwnershipMap.Select(item => item.DomainId), Is.Unique);
            Assert.That(facade.OwnershipMap.Single(item => item.DomainId == "organization-identity").AuthoritativeRuntime, Is.EqualTo(nameof(OrganizationRuntime)));
            Assert.That(facade.OwnershipMap.Single(item => item.DomainId == "warrant").AuthoritativeRuntime, Is.EqualTo(nameof(CrimeRuntime)));
            Assert.That(facade.PersistenceDependencies.Single(item => item.ParticipantKey == JusticePersistenceParticipant.Key).DependsOn, Does.Contain(CrimePersistenceParticipant.Key));
            Assert.That(readiness.Runtimes.Count, Is.EqualTo(11));
        }

        [Test]
        public void InstitutionalActionPipelineSeparatesIdentityAuthorityJurisdictionLawAndDomain()
        {
            using TestLabRuntimeBundle bundle = CreateBundle();
            Step13InstitutionalIntegrationFacade facade = CreateFacade(bundle);
            Step13InstitutionalActionContext valid = new Step13InstitutionalActionContext(
                PersistenceService.LocalPlayerId,
                "organization.prototype.guild",
                "government.prototype.village",
                "office-assignment.prototype.magistrate",
                "authority-grant.prototype.magistrate",
                new Step13InstitutionalSubjectReference(Step13InstitutionalSubjectType.Warrant, "warrant.prototype.arrest", PersistenceService.LocalWorldId, nameof(CrimeRuntime)),
                "institutional-action.prototype.issue-warrant",
                "place.prototype.village",
                "territory.prototype.village",
                "jurisdiction.prototype.village",
                "legal-subject.prototype.public-order",
                string.Empty,
                string.Empty,
                "incident.prototype.public-order",
                string.Empty,
                42d,
                "test.provenance");
            Step13InstitutionalActionContext missingAuthority = new Step13InstitutionalActionContext(
                PersistenceService.LocalPlayerId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new Step13InstitutionalSubjectReference(Step13InstitutionalSubjectType.Warrant, "warrant.prototype.arrest", PersistenceService.LocalWorldId, nameof(CrimeRuntime)),
                "institutional-action.prototype.issue-warrant",
                "place.prototype.village",
                "territory.prototype.village",
                "jurisdiction.prototype.village",
                "legal-subject.prototype.public-order",
                string.Empty,
                string.Empty,
                "incident.prototype.public-order",
                string.Empty,
                42d);

            Step13ActionEvaluationResult allowed = facade.EvaluateProtectedAction(valid);
            Step13ActionEvaluationResult denied = facade.EvaluateProtectedAction(missingAuthority);

            Assert.That(allowed.Executable, Is.True, string.Join(Environment.NewLine, allowed.FailedGates.Select(item => item.Message)));
            Assert.That(allowed.Gates.Select(item => item.Gate).ToArray(), Is.EqualTo(allowed.Gates.Select(item => item.Gate).OrderBy(item => item).ToArray()));
            Assert.That(denied.Executable, Is.False);
            Assert.That(denied.FailedGates.Any(item => item.Gate == Step13ActionGate.Authority), Is.True);
            Assert.That(denied.FailedGates.Any(item => item.Gate == Step13ActionGate.Prepared), Is.True);
        }

        [Test]
        public void ContextSnapshotsAreBoundedImmutableAndDeterministic()
        {
            using TestLabRuntimeBundle bundle = CreateBundle();
            string organizationId = "organization.test.integration-snapshot";
            CreateProjectionOrganization(bundle, organizationId);
            Assert.That(bundle.Organizations.CreateSaveData().records.Any(item => item.organizationId == organizationId), Is.True);
            Step13InstitutionalIntegrationFacade facade = CreateFacade(bundle);
            Step13InstitutionalContextOptions options = new Step13InstitutionalContextOptions { MaxOrganizations = 1 };

            Step13InstitutionalContextSnapshot first = facade.CreateInstitutionalContextSnapshot(PersistenceService.LocalPlayerId, PersistenceService.LocalPlayerId, organizationId, string.Empty, string.Empty, 10d, options);
            Step13InstitutionalContextSnapshot second = facade.CreateInstitutionalContextSnapshot(PersistenceService.LocalPlayerId, PersistenceService.LocalPlayerId, organizationId, string.Empty, string.Empty, 10d, options);
            IReadOnlyList<Step13ContextRecordReference> returnedRecords = first.Records;
            IReadOnlyList<Step13ContextRecordReference> secondRead = first.Records;

            Assert.That(first.Fingerprint, Is.EqualTo(second.Fingerprint));
            Assert.That(first.Records.Count, Is.GreaterThan(0));
            Assert.That(ReferenceEquals(returnedRecords, secondRead), Is.False);
            Assert.That(first.Records.Any(item => item.RuntimeName == "mutated"), Is.False);
            Assert.That(first.SourceRuntimes.Count, Is.EqualTo(11));
        }

        [Test]
        public void ValidatorRejectsCyclesAndUnsafeSchedulerConfiguration()
        {
            Step13IntegrationValidationReport report = new Step13IntegrationValidationReport();

            Step13InstitutionalIntegrationValidator.ValidatePersistenceDependencies(new[]
            {
                new Step13PersistenceDependencyEntry("a", "b"),
                new Step13PersistenceDependencyEntry("b", "a")
            }, report);
            Step13InstitutionalIntegrationValidator.ValidateSchedulerBudget(new Step13SchedulerBudget
            {
                MaximumEvaluationsPerTick = 0,
                MaximumQueuedInstitutionalConsequences = 10,
                MaximumTraversalDepth = 99,
                UseSystemTime = true,
                AllowImmediateRecursiveDispatch = true
            }, report);

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Diagnostics.Any(item => item.Code == "dependency-cycle"), Is.True);
            Assert.That(report.Diagnostics.Any(item => item.Code == "system-time"), Is.True);
            Assert.That(report.Diagnostics.Any(item => item.Code == "immediate-recursion"), Is.True);
        }

        [Test]
        public void TransactionCoordinatorPreviewsRollsBackAndDeduplicatesWithoutPartialCommit()
        {
            Step13InstitutionalTransactionCoordinator coordinator = new Step13InstitutionalTransactionCoordinator();
            bool previewed = false;
            bool committed = false;
            bool rolledBack = false;

            Step13TransactionParticipantPlan[] plans =
            {
                new Step13TransactionParticipantPlan("crime", Step13TransactionFailurePolicy.Required, () => previewed = true, () => true, () => committed = true, () => rolledBack = true),
                new Step13TransactionParticipantPlan("justice", Step13TransactionFailurePolicy.Required, () => true, () => true, () => false, () => rolledBack = true)
            };

            Step13TransactionResult preview = coordinator.Execute("tx.step13.integration", plans, preview: true);
            Step13TransactionResult failed = coordinator.Execute("tx.step13.integration", plans);
            Step13TransactionResult success = coordinator.Execute("tx.step13.integration.success", new[]
            {
                new Step13TransactionParticipantPlan("justice", Step13TransactionFailurePolicy.Required, () => true, () => true, () => true, () => true)
            });
            Step13TransactionResult duplicate = coordinator.Execute("tx.step13.integration.success", plans);

            Assert.That(preview.Succeeded, Is.True);
            Assert.That(preview.Preview, Is.True);
            Assert.That(previewed, Is.True);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(committed, Is.True);
            Assert.That(rolledBack, Is.True);
            Assert.That(success.Succeeded, Is.True);
            Assert.That(duplicate.Succeeded, Is.True);
            Assert.That(duplicate.Duplicate, Is.True);
        }

        private static TestLabRuntimeBundle CreateBundle()
        {
            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            DefinitionRegistry registry = PrototypeJusticeDefinitionFactory.AddMissingPrototypeJusticeDefinitions(
                PrototypeCrimeDefinitionFactory.AddMissingPrototypeCrimeDefinitions(
                    PrototypeLegalDefinitionFactory.AddMissingPrototypeLegalDefinitions(
                        PrototypeGovernmentDefinitionFactory.AddMissingPrototypeGovernmentDefinitions(
                            PrototypeDiplomacyDefinitionFactory.AddMissingPrototypeDiplomacyDefinitions(
                                PrototypeFactionDefinitionFactory.AddMissingPrototypeFactionDefinitions(
                                    PrototypeOrganizationDecisionDefinitionFactory.AddMissingPrototypeOrganizationDecisionDefinitions(
                                        PrototypeOrganizationResourceDefinitionFactory.AddMissingPrototypeOrganizationResourceDefinitions(
                                            PrototypeOrganizationAuthorityDefinitionFactory.AddMissingPrototypeOrganizationAuthorityDefinitions(
                                                PrototypeOrganizationMembershipDefinitionFactory.AddMissingPrototypeOrganizationMembershipDefinitions(
                                                    PrototypeOrganizationDefinitionFactory.AddMissingPrototypeOrganizationDefinitions(catalog.CreateRegistry())))))))))));
            Assert.That(registry.TryGet(PrototypeOrganizationDefinitionFactory.GuildDefinitionId, out OrganizationDefinition _), Is.True);
            return TestLabRuntimeBundle.CreateFresh(registry, PersistenceService.LocalPlayerId, PersistenceService.LocalWorldId, KnownPersons, Array.Empty<string>(), "Step 13 Integration Tests");
        }

        private static void CreateProjectionOrganization(TestLabRuntimeBundle bundle, string organizationId)
        {
            OrganizationOperationResult result = bundle.Organizations.CreateOrganization(new OrganizationCreateRequest
            {
                organizationId = organizationId,
                organizationDefinitionId = PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                officialName = "Integration Projection Guild",
                shortName = "Integration",
                initialLifecycleState = OrganizationLifecycleState.Active,
                visibility = OrganizationVisibility.Public,
                transactionId = $"tx.{organizationId}"
            });
            Assert.That(result.Succeeded || result.Duplicate, Is.True, result.Message);
        }

        private static Step13InstitutionalIntegrationFacade CreateFacade(TestLabRuntimeBundle bundle)
        {
            return new Step13InstitutionalIntegrationFacade(
                bundle.DefinitionRegistry,
                PersistenceService.LocalWorldId,
                KnownPersons,
                Array.Empty<string>(),
                bundle.Organizations,
                bundle.OrganizationMemberships,
                bundle.OrganizationAuthority,
                bundle.OrganizationResources,
                bundle.OrganizationDecisions,
                bundle.Factions,
                bundle.Diplomacy,
                bundle.Governments,
                bundle.Laws,
                bundle.Crimes,
                bundle.Justice,
                bundle.Economy,
                bundle.Properties,
                bundle.Businesses,
                bundle.ItemInstances);
        }
    }
}
#endif
