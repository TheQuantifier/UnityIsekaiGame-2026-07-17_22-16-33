using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Knowledge.Sharing;
using UnityIsekaiGame.Professions;

namespace UnityIsekaiGame.Tests
{
    public sealed class PositionsDutiesEmploymentFoundationsTests
    {
        private const string PersonId = "person.position.test";
        private const string OtherPersonId = "person.position.other";
        private const string GuildAuthority = "authority.guild.prototype";
        private const string CatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";

        [Test]
        public void PrototypePositionAndDutyDefinitionsValidate()
        {
            DefinitionRegistry registry = Registry();
            DefinitionValidationReport report = ValidateRegistry(registry);

            Assert.That(report.ErrorCount, Is.Zero, report.GetSummary());
            Assert.That(report.WarningCount, Is.Zero, report.GetSummary());
            Assert.That(registry.TryGet(PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, out PositionDefinition senior), Is.True);
            Assert.That(registry.TryGet(PrototypeProfessionDefinitionFactory.SeniorSmithCraftDutyId, out DutyDefinition duty), Is.True);
            Assert.That(senior.RequiredRankDefinitionIds, Does.Contain(PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId));
            Assert.That(senior.RequiredCredentialDefinitionIds, Does.Contain(PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId));
            Assert.That(senior.RequiredTrainingProgramIds, Does.Contain(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId));
            Assert.That(senior.ExperienceRequirement.minimumValidatedActivities, Is.GreaterThanOrEqualTo(2));
            Assert.That(duty.PositionDefinitionId, Is.EqualTo(senior.Id));
        }

        [Test]
        public void InvalidPositionDefinitionReferencesAreRejected()
        {
            DefinitionRegistry registry = Registry();
            PositionDefinition invalid = ScriptableObject.CreateInstance<PositionDefinition>();
            invalid.DevelopmentConfigure(
                "position.test.invalid",
                "Invalid Position",
                PositionCategory.Custom,
                new[] { "profession.missing" },
                organizationTypeId: "bad-org-type",
                ranks: new[] { "profession-rank.missing" },
                credentials: new[] { "credential.missing" },
                trainingPrograms: new[] { "training-program.missing" },
                duties: new[] { "duty.missing" },
                authorities: new[] { "bad-authority" });
            DefinitionRegistry invalidRegistry = new DefinitionRegistry(registry.DefinitionsById.Values.Concat(new IGameDefinition[] { invalid }));
            DefinitionValidationReport report = ValidateRegistry(invalidRegistry);

            Assert.That(report.ErrorCount, Is.GreaterThanOrEqualTo(6), report.GetSummary());
        }

        [Test]
        public void EligibilityApplicationOfferAndAppointmentUseUpstreamQualificationsWithoutMutation()
        {
            Fixture fixture = CreateFixture();
            PositionEmploymentOperationResult position = fixture.CreatePosition("senior", PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId);
            long positionRevision = fixture.Positions.Revision;
            PositionEligibilityResult blocked = fixture.Positions.EvaluateEligibility(PersonId, position.Position.positionInstanceId, perceived: true, privilegedDiagnostics: false);
            Assert.That(blocked.AuthoritativeEligible, Is.False);
            Assert.That(fixture.Positions.Revision, Is.EqualTo(positionRevision));

            fixture.EnsureSeniorSmithQualified("appointment");
            PositionEligibilityResult eligible = fixture.Positions.EvaluateEligibility(PersonId, position.Position.positionInstanceId, privilegedDiagnostics: true);
            PositionEmploymentOperationResult application = fixture.Positions.SubmitApplication("position-application.test", PersonId, position.Position.positionInstanceId, eligible.Snapshot, "10", "tx.position.apply");
            PositionEmploymentOperationResult offer = fixture.Positions.OfferPosition(application.Application.requestId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, "11", "tx.position.offer");
            PositionEmploymentOperationResult accept = fixture.Positions.AcceptOffer(application.Application.requestId, PersonId, "12", "tx.position.accept");
            PositionEmploymentOperationResult staleAppointment = fixture.Positions.AppointPerson("employment.stale", application.Application.requestId, PersonId, position.Position.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, eligible.Snapshot, "13", "tx.position.stale");
            PositionEligibilityResult currentEligibility = fixture.Positions.EvaluateEligibility(PersonId, position.Position.positionInstanceId, privilegedDiagnostics: true);
            PositionEligibilitySnapshotData tampered = currentEligibility.Snapshot.Clone();
            tampered.evaluationHash = "stale";
            PositionEmploymentOperationResult tamperedAppointment = fixture.Positions.AppointPerson("employment.tampered", application.Application.requestId, PersonId, position.Position.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, tampered, "14", "tx.position.tampered");
            PositionEmploymentOperationResult unauthorized = fixture.Positions.AppointPerson("employment.unauthorized", application.Application.requestId, PersonId, position.Position.positionInstanceId, "authority.bad", currentEligibility.Snapshot, "15", "tx.position.bad-auth");
            PositionEmploymentOperationResult appoint = fixture.Positions.AppointPerson("employment.senior", application.Application.requestId, PersonId, position.Position.positionInstanceId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, currentEligibility.Snapshot, "16", "tx.position.appoint");

            Assert.That(eligible.AuthoritativeEligible, Is.True, string.Join(",", eligible.BlockingFailures));
            Assert.That(application.Succeeded, Is.True, application.Message);
            Assert.That(offer.Succeeded, Is.True, offer.Message);
            Assert.That(accept.Succeeded, Is.True, accept.Message);
            Assert.That(staleAppointment.Succeeded, Is.False);
            Assert.That(staleAppointment.Status, Is.EqualTo(PositionEmploymentOperationStatus.StaleEvaluation));
            Assert.That(tamperedAppointment.Succeeded, Is.False);
            Assert.That(tamperedAppointment.Status, Is.EqualTo(PositionEmploymentOperationStatus.StaleEvaluation));
            Assert.That(unauthorized.Succeeded, Is.False);
            Assert.That(unauthorized.Status, Is.EqualTo(PositionEmploymentOperationStatus.UnauthorizedAuthority));
            Assert.That(appoint.Succeeded, Is.True, appoint.Message);
            Assert.That(appoint.Employment.state, Is.EqualTo(EmploymentState.Active));
            Assert.That(fixture.Professions.QueryByProfession(PrototypeProfessionDefinitionFactory.FieldMedicProfessionId).Count, Is.Zero);
            Assert.That(fixture.Credentials.QueryByRecipient(PersonId, activeOnly: true).Count, Is.EqualTo(2));
            Assert.That(fixture.Ranks.QueryByPerson(PersonId, currentOnly: true).Count, Is.EqualTo(1));
        }

        [Test]
        public void SharedVacancyCapacityAndConcurrentEmploymentPoliciesAreEnforced()
        {
            Fixture fixture = CreateFixture();
            fixture.EnsureSeniorSmithQualified("capacity");
            PositionEmploymentOperationResult senior = fixture.Appoint("senior-capacity", PersonId, PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId);
            PositionEmploymentOperationResult secondFullTime = fixture.Appoint("senior-conflict", PersonId, PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId);
            PositionEmploymentOperationResult clerk = fixture.Appoint("guild-clerk", PersonId, PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult clerkOther = fixture.Appoint("guild-clerk-other", OtherPersonId, PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId, existingPosition: clerk.Position.positionInstanceId);
            PositionEligibilityResult full = fixture.Positions.EvaluateEligibility(PersonId, clerk.Position.positionInstanceId, privilegedDiagnostics: true);

            Assert.That(senior.Succeeded, Is.True, senior.Message);
            Assert.That(secondFullTime.Succeeded, Is.False);
            Assert.That(secondFullTime.Status, Is.EqualTo(PositionEmploymentOperationStatus.MissingRequirement));
            Assert.That(clerk.Succeeded, Is.True, clerk.Message);
            Assert.That(clerkOther.Succeeded, Is.True, clerkOther.Message);
            Assert.That(full.AuthoritativeEligible, Is.False);
            Assert.That(full.BlockingFailures, Does.Contain("position.capacity"));
            Assert.That(fixture.Positions.TryGetPosition(clerk.Position.positionInstanceId, out PositionInstanceData shared), Is.True);
            Assert.That(shared.state, Is.EqualTo(PositionInstanceState.Filled));
            Assert.That(shared.holderPersonIds, Is.EquivalentTo(new[] { PersonId, OtherPersonId }));
        }

        [Test]
        public void DutiesUseRealActivityEvidenceAndAuthorityFollowsEmploymentState()
        {
            Fixture fixture = CreateFixture();
            fixture.EnsureSeniorSmithQualified("duty");
            PositionEmploymentOperationResult appointment = fixture.Appoint("duty-senior", PersonId, PrototypeProfessionDefinitionFactory.RoyalForgeSeniorSmithPositionId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.ForgeOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId);
            PositionEmploymentOperationResult duty = fixture.Positions.AssignDuty("duty-assignment.craft", appointment.Employment.employmentId, PrototypeProfessionDefinitionFactory.SeniorSmithCraftDutyId, "20", "tx.duty.assign");
            ProfessionalActivityOperationResult activity = fixture.RecordIndependentActivity("duty-evidence");
            PositionEmploymentOperationResult missingEvidence = fixture.Positions.CompleteDutyWithEvidence(duty.Duty.assignmentId, Array.Empty<string>(), "21", "tx.duty.missing");
            PositionEmploymentOperationResult complete = fixture.Positions.CompleteDutyWithEvidence(duty.Duty.assignmentId, new[] { activity.Evidence.evidenceId, activity.Evidence.evidenceId }, "22", "tx.duty.complete");
            bool activeAuthority = fixture.Positions.HasActiveAuthority(PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);
            PositionEmploymentOperationResult suspended = fixture.Positions.SuspendEmployment(appointment.Employment.employmentId, "23", "tx.suspend");
            bool suspendedAuthority = fixture.Positions.HasActiveAuthority(PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);
            PositionEmploymentOperationResult reinstated = fixture.Positions.ReinstateEmployment(appointment.Employment.employmentId, "24", "tx.reinstate");
            bool reinstatedAuthority = fixture.Positions.HasActiveAuthority(PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);
            PositionEmploymentOperationResult resigned = fixture.Positions.Resign(appointment.Employment.employmentId, "25", "tx.resign");
            bool endedAuthority = fixture.Positions.HasActiveAuthority(PersonId, "organization.prototype.royal-forge", PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId);

            Assert.That(appointment.Succeeded, Is.True, appointment.Message);
            Assert.That(duty.Succeeded, Is.True, duty.Message);
            Assert.That(activity.Succeeded, Is.True, activity.Message);
            Assert.That(missingEvidence.Succeeded, Is.False);
            Assert.That(complete.Succeeded, Is.True, complete.Message);
            Assert.That(complete.Duty.state, Is.EqualTo(DutyAssignmentState.Completed));
            Assert.That(complete.Duty.completionEvidenceReferenceIds.Count(), Is.EqualTo(1));
            Assert.That(activeAuthority, Is.True);
            Assert.That(suspended.Succeeded, Is.True);
            Assert.That(suspendedAuthority, Is.False);
            Assert.That(reinstated.Succeeded, Is.True);
            Assert.That(reinstatedAuthority, Is.True);
            Assert.That(resigned.Succeeded, Is.True);
            Assert.That(endedAuthority, Is.False);
        }

        [Test]
        public void ReportingLifecycleProjectionAndPersistenceRestoreAtomically()
        {
            Fixture fixture = CreateFixture();
            fixture.EnsureSeniorSmithQualified("persist");
            fixture.Promote(PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId, "persist-master");
            PositionEmploymentOperationResult supervisor = fixture.Appoint("supervisor", PersonId, PrototypeProfessionDefinitionFactory.ApprenticeSupervisorPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId);
            PositionEmploymentOperationResult clerk = fixture.Appoint("clerk", PersonId, PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult reporting = fixture.Positions.AssignSupervisor(clerk.Position.positionInstanceId, supervisor.Position.positionInstanceId, "tx.reporting");
            PositionEmploymentOperationResult cycle = fixture.Positions.AssignSupervisor(supervisor.Position.positionInstanceId, clerk.Position.positionInstanceId, "tx.reporting.cycle");
            PositionEmploymentOperationResult secretDuty = fixture.Positions.AssignDuty("duty-assignment.secret", clerk.Employment.employmentId, PrototypeProfessionDefinitionFactory.GuildClerkRecordDutyId, "30", "tx.secret-duty");
            PositionEmploymentProjection<DutyAssignmentData> redacted = fixture.Positions.ProjectDuty(secretDuty.Duty.assignmentId, PositionEmploymentProjectionAudience.Public, null);
            PositionEmploymentOperationResult dismissed = fixture.Positions.Dismiss(clerk.Employment.employmentId, "31", "tx.dismiss");
            PositionEmploymentOperationResult close = fixture.Positions.ClosePosition(clerk.Position.positionInstanceId, "32", "tx.close", forceEndActiveEmployment: true);
            PositionEmploymentRuntimeSaveData save = fixture.Positions.CreateSaveData();
            PositionEmploymentRuntime restored = fixture.NewPositionsRuntime();
            PositionEmploymentOperationResult restore = restored.RestoreFromSaveData(save, fixture.Registry, fixture.Professions, fixture.Training, fixture.Activities, fixture.Credentials, fixture.Ranks, fixture.KnownPersons, fixture.KnownOrganizations, fixture.KnownAuthorities, restoring: true);
            PositionEmploymentRuntimeSaveData corrupt = save.Clone();
            corrupt.positions[0].organizationId = "organization.missing";
            int beforeEmploymentCount = restored.EmploymentCount;
            long beforeRevision = restored.Revision;
            PositionEmploymentOperationResult rejected = restored.RestoreFromSaveData(corrupt, fixture.Registry, fixture.Professions, fixture.Training, fixture.Activities, fixture.Credentials, fixture.Ranks, fixture.KnownPersons, fixture.KnownOrganizations, fixture.KnownAuthorities, restoring: true);
            PositionEmploymentOperationResult emptyLegacy = fixture.NewPositionsRuntime().RestoreFromSaveData(null, fixture.Registry, fixture.Professions, fixture.Training, fixture.Activities, fixture.Credentials, fixture.Ranks, fixture.KnownPersons, fixture.KnownOrganizations, fixture.KnownAuthorities, restoring: true);

            Assert.That(supervisor.Succeeded, Is.True, supervisor.Message);
            Assert.That(clerk.Succeeded, Is.True, clerk.Message);
            Assert.That(reporting.Succeeded, Is.True, reporting.Message);
            Assert.That(cycle.Succeeded, Is.False);
            Assert.That(cycle.Status, Is.EqualTo(PositionEmploymentOperationStatus.ReportingCycle));
            Assert.That(secretDuty.Succeeded, Is.True);
            Assert.That(redacted.Redacted, Is.True);
            Assert.That(redacted.Record.assignedPersonId, Is.Empty);
            Assert.That(dismissed.Succeeded, Is.True);
            Assert.That(close.Succeeded, Is.True);
            Assert.That(restore.Succeeded, Is.True, restore.Message);
            Assert.That(restored.HistoryHooks.Count, Is.Zero);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(restored.EmploymentCount, Is.EqualTo(beforeEmploymentCount));
            Assert.That(restored.Revision, Is.EqualTo(beforeRevision));
            Assert.That(emptyLegacy.Succeeded, Is.True);
        }

        [Test]
        public void TransferContractEndAndRetirementAreAtomicAndDoNotGrantSeparateProgression()
        {
            Fixture fixture = CreateFixture();
            PositionEmploymentOperationResult clerk = fixture.Appoint("transfer-clerk", PersonId, PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult contractorPosition = fixture.CreatePosition("transfer-contractor", PrototypeProfessionDefinitionFactory.IndependentContractorPositionId, "organization.prototype.independent", PrototypeProfessionDefinitionFactory.IndependentOrganizationTypeId, 4);
            PositionEligibilityResult contractorEligibility = fixture.Positions.EvaluateEligibility(PersonId, contractorPosition.Position.positionInstanceId, privilegedDiagnostics: true);
            int professionCount = fixture.Professions.QueryByPerson(PersonId, activeOnly: true).Count;
            int credentialCount = fixture.Credentials.QueryByRecipient(PersonId, activeOnly: true).Count;
            int rankCount = fixture.Ranks.QueryByPerson(PersonId, currentOnly: true).Count;

            PositionEmploymentOperationResult transfer = fixture.Positions.TransferPerson(clerk.Employment.employmentId, "employment.transfer.contractor", contractorPosition.Position.positionInstanceId, GuildAuthority, contractorEligibility.Snapshot, "40", "tx.position.transfer");
            fixture.Positions.TryGetEmployment(clerk.Employment.employmentId, out EmploymentRecordData formerClerk);
            fixture.Positions.TryGetEmployment("employment.transfer.contractor", out EmploymentRecordData transferredContractor);
            PositionEmploymentOperationResult contractEnd = fixture.Positions.EndContract(transferredContractor.employmentId, "41", "tx.position.contract-end");
            PositionEmploymentOperationResult retirePosition = fixture.CreatePosition("retire-contractor", PrototypeProfessionDefinitionFactory.IndependentContractorPositionId, "organization.prototype.independent", PrototypeProfessionDefinitionFactory.IndependentOrganizationTypeId, 1);
            PositionEligibilityResult retireEligibility = fixture.Positions.EvaluateEligibility(PersonId, retirePosition.Position.positionInstanceId, privilegedDiagnostics: true);
            PositionEmploymentOperationResult retirementAppointment = fixture.Positions.AppointPerson("employment.retire.contractor", string.Empty, PersonId, retirePosition.Position.positionInstanceId, GuildAuthority, retireEligibility.Snapshot, "42", "tx.position.retire-appoint", EmploymentClassification.IndependentServiceFoundation);
            PositionEmploymentOperationResult retirement = fixture.Positions.Retire(retirementAppointment.Employment.employmentId, "43", "tx.position.retire");

            Fixture atomicFixture = CreateFixture();
            PositionEmploymentOperationResult atomicClerk = atomicFixture.Appoint("atomic-clerk", PersonId, PrototypeProfessionDefinitionFactory.GuildClerkPositionId, "organization.prototype.guild", PrototypeProfessionDefinitionFactory.GuildOrganizationTypeId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId);
            PositionEmploymentOperationResult atomicTarget = atomicFixture.CreatePosition("atomic-contractor", PrototypeProfessionDefinitionFactory.IndependentContractorPositionId, "organization.prototype.independent", PrototypeProfessionDefinitionFactory.IndependentOrganizationTypeId, 1);
            PositionEligibilityResult atomicEligibility = atomicFixture.Positions.EvaluateEligibility(PersonId, atomicTarget.Position.positionInstanceId, privilegedDiagnostics: true);
            PositionEligibilitySnapshotData stale = atomicEligibility.Snapshot.Clone();
            stale.evaluationHash = "stale";
            PositionEmploymentOperationResult rejectedTransfer = atomicFixture.Positions.TransferPerson(atomicClerk.Employment.employmentId, "employment.atomic.contractor", atomicTarget.Position.positionInstanceId, GuildAuthority, stale, "44", "tx.position.atomic-transfer");
            atomicFixture.Positions.TryGetEmployment(atomicClerk.Employment.employmentId, out EmploymentRecordData stillActiveClerk);

            Assert.That(clerk.Succeeded, Is.True, clerk.Message);
            Assert.That(contractorPosition.Succeeded, Is.True, contractorPosition.Message);
            Assert.That(contractorEligibility.AuthoritativeEligible, Is.True, string.Join(",", contractorEligibility.BlockingFailures));
            Assert.That(transfer.Succeeded, Is.True, transfer.Message);
            Assert.That(formerClerk.state, Is.EqualTo(EmploymentState.Former));
            Assert.That(transferredContractor.state, Is.EqualTo(EmploymentState.Active));
            Assert.That(transferredContractor.contractTermsFoundationId, Is.Not.Empty);
            Assert.That(contractEnd.Succeeded, Is.True, contractEnd.Message);
            Assert.That(contractEnd.Employment.state, Is.EqualTo(EmploymentState.ContractEnded));
            Assert.That(retirementAppointment.Succeeded, Is.True, retirementAppointment.Message);
            Assert.That(retirement.Succeeded, Is.True, retirement.Message);
            Assert.That(retirement.Employment.state, Is.EqualTo(EmploymentState.Retired));
            Assert.That(fixture.Professions.QueryByPerson(PersonId, activeOnly: true).Count, Is.EqualTo(professionCount));
            Assert.That(fixture.Credentials.QueryByRecipient(PersonId, activeOnly: true).Count, Is.EqualTo(credentialCount));
            Assert.That(fixture.Ranks.QueryByPerson(PersonId, currentOnly: true).Count, Is.EqualTo(rankCount));
            Assert.That(rejectedTransfer.Succeeded, Is.False);
            Assert.That(rejectedTransfer.Status, Is.EqualTo(PositionEmploymentOperationStatus.StaleEvaluation));
            Assert.That(stillActiveClerk.state, Is.EqualTo(EmploymentState.Active));
            Assert.That(atomicFixture.Positions.TryGetEmployment("employment.atomic.contractor", out EmploymentRecordData _), Is.False);
        }

        private static DefinitionRegistry Registry()
        {
            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return PrototypeProfessionDefinitionFactory.AddMissingPrototypeProfessionDefinitions(catalog.CreateRegistry());
        }

        private static DefinitionValidationReport ValidateRegistry(DefinitionRegistry registry)
        {
            DefinitionValidationReport report = new DefinitionValidationReport();
            foreach (IGameDefinition definition in registry.DefinitionsById.Values)
            {
                if (definition is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(registry.DefinitionsById, report);
                }
            }

            return report;
        }

        private static Fixture CreateFixture()
        {
            return new Fixture(Registry());
        }

        private sealed class Fixture
        {
            public Fixture(DefinitionRegistry registry)
            {
                Registry = registry;
                Professions = new PersonProfessionRuntime();
                Transfers = new InformationTransferRuntime();
                Training = new TrainingRuntime();
                Activities = new ProfessionalActivityRuntime();
                Credentials = new CredentialRuntime();
                Ranks = new ProfessionalRankRuntime();
                Positions = new PositionEmploymentRuntime();
                Professions.Configure(registry, KnownPersons);
                Transfers.Configure(registry, PersonId);
                Training.Configure(registry, Professions, Transfers, KnownPersons);
                Activities.Configure(registry, Professions, KnownPersons);
                Credentials.Configure(registry, Professions, Training, Activities, KnownPersons, KnownAuthorities);
                Ranks.Configure(registry, Professions, Training, Activities, Credentials, KnownPersons, KnownAuthorities);
                Positions.Configure(registry, Professions, Training, Activities, Credentials, Ranks, KnownPersons, KnownOrganizations, KnownAuthorities);
                EnsureProfession();
            }

            public DefinitionRegistry Registry { get; }
            public PersonProfessionRuntime Professions { get; }
            public InformationTransferRuntime Transfers { get; }
            public TrainingRuntime Training { get; }
            public ProfessionalActivityRuntime Activities { get; }
            public CredentialRuntime Credentials { get; }
            public ProfessionalRankRuntime Ranks { get; }
            public PositionEmploymentRuntime Positions { get; }
            public string[] KnownPersons { get; } = { PersonId, OtherPersonId };
            public string[] KnownOrganizations { get; } = { "organization.prototype.guild", "organization.prototype.royal-forge", "organization.prototype.temple", "organization.prototype.university", "organization.prototype.government", "organization.prototype.independent" };
            public string[] KnownAuthorities { get; } = { GuildAuthority, "authority.medical.prototype", PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId, PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId, PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId, PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId, PrototypeProfessionDefinitionFactory.BlacksmithTeachPermissionId, PrototypeProfessionDefinitionFactory.ForgeRestrictedStationPermissionId, "organization.prototype.guild" };

            public PositionEmploymentRuntime NewPositionsRuntime()
            {
                PositionEmploymentRuntime runtime = new PositionEmploymentRuntime();
                runtime.Configure(Registry, Professions, Training, Activities, Credentials, Ranks, KnownPersons, KnownOrganizations, KnownAuthorities);
                return runtime;
            }

            public PositionEmploymentOperationResult CreatePosition(string slug, string definitionId, string organizationId, string organizationTypeId, int maxHolders = 1)
            {
                return Positions.CreatePosition(new PositionInstanceData
                {
                    positionInstanceId = $"position-instance.{slug}",
                    positionDefinitionId = definitionId,
                    organizationId = organizationId,
                    organizationTypeId = organizationTypeId,
                    state = PositionInstanceState.Vacant,
                    maximumHolders = maxHolders,
                    vacancyAllowed = true,
                    createdWorldTime = "1",
                    accessPolicyId = PrototypeProfessionDefinitionFactory.AccessPublicId
                }, $"tx.position.create.{slug}");
            }

            public PositionEmploymentOperationResult Appoint(string slug, string personId, string definitionId, string organizationId, string organizationTypeId, string authorityId, string existingPosition = "")
            {
                string positionId = existingPosition;
                PositionEmploymentOperationResult position = null;
                if (string.IsNullOrWhiteSpace(positionId))
                {
                    int capacity = definitionId == PrototypeProfessionDefinitionFactory.GuildClerkPositionId ? 2 : 1;
                    position = CreatePosition(slug, definitionId, organizationId, organizationTypeId, capacity);
                    positionId = position.Position.positionInstanceId;
                }

                PositionEligibilityResult eligibility = Positions.EvaluateEligibility(personId, positionId, privilegedDiagnostics: true);
                PositionEmploymentOperationResult appoint = Positions.AppointPerson($"employment.{slug}.{personId}", string.Empty, personId, positionId, authorityId, eligibility.Snapshot, "2", $"tx.position.appoint.{slug}");
                return appoint.Succeeded && position != null
                    ? PositionEmploymentOperationResult.Success(appoint.Message, appoint.PriorRevision, appoint.ResultingRevision, appoint.Eligibility, position.Position, appoint.Application, appoint.Employment, appoint.Duty)
                    : appoint;
            }

            public void EnsureProfession()
            {
                Professions.AddRelationship(new AddProfessionRelationshipRequest
                {
                    relationshipId = "profession-relationship.position.blacksmith",
                    personId = PersonId,
                    professionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                    specializationIds = new[] { PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId },
                    informalPractice = true,
                    formalPractice = true,
                    selfDeclared = true,
                    recognized = true,
                    recognizingAuthorityId = GuildAuthority,
                    active = true,
                    startWorldTime = "1",
                    transactionId = "tx.profession.position"
                });
            }

            public void EnsureSeniorSmithQualified(string slug)
            {
                IssueApprenticeshipCredential(slug);
                Promote(PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, $"{slug}.apprentice");
                Promote(PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, $"{slug}.journeyman");
                IssueGuildLicense(slug);
            }

            public ProfessionalRankOperationResult Promote(string rankId, string slug)
            {
                if (Ranks.QueryByPerson(PersonId, currentOnly: true).Any(rank => rank.rankDefinitionId == rankId))
                {
                    return ProfessionalRankOperationResult.Success("Rank already active.", Ranks.Revision, Ranks.Revision, duplicate: true);
                }

                if (rankId == PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId)
                {
                    CompleteTrainingAndExperience(slug);
                }
                else if (rankId == PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId)
                {
                    Promote(PrototypeProfessionDefinitionFactory.BlacksmithRankApprenticeId, $"{slug}.prior");
                    IssueApprenticeshipCredential($"{slug}.credential");
                }
                else if (rankId == PrototypeProfessionDefinitionFactory.BlacksmithRankMasterId)
                {
                    Promote(PrototypeProfessionDefinitionFactory.BlacksmithRankJourneymanId, $"{slug}.prior");
                    IssueGuildLicense($"{slug}.license");
                }

                ProfessionalRankAdvancementResult evaluation = Ranks.EvaluateAdvancement(PersonId, rankId, GuildAuthority, privilegedDiagnostics: true);
                ProfessionalRankOperationResult submit = Ranks.SubmitApplication($"rank-application.{slug}", PersonId, rankId, GuildAuthority, evaluation.Snapshot, "40", $"tx.rank.submit.{slug}");
                ProfessionalRankOperationResult approve = Ranks.ApprovePromotion(submit.Application?.applicationId, PersonId, evaluation.Snapshot, "41", $"tx.rank.approve.{slug}");
                Assert.That(evaluation.AuthoritativeEligible, Is.True, string.Join(",", evaluation.BlockingFailures));
                Assert.That(submit.Succeeded, Is.True, submit.Message);
                Assert.That(approve.Succeeded, Is.True, approve.Message);
                return Ranks.PromotePerson($"rank-record.{slug}", submit.Application.applicationId, evaluation.Snapshot, "42", $"tx.rank.promote.{slug}");
            }

            public CredentialOperationResult IssueApprenticeshipCredential(string slug)
            {
                if (Credentials.QueryByRecipient(PersonId, activeOnly: true).Any(item => item.credentialDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId))
                {
                    return CredentialOperationResult.Success("Apprenticeship credential already active.", Credentials.Revision, Credentials.Revision, duplicate: true);
                }

                CompleteTrainingAndExperience(slug);
                CredentialOperationResult exam = RecordExam($"{slug}.practical", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
                CredentialQualificationResult qualification = Credentials.EvaluateQualification(PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId);
                CredentialOperationResult apply = Credentials.SubmitApplication($"credential-application.{slug}.apprentice", PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, GuildIssuer(), qualification.Snapshot, "30", $"tx.credential.apply.{slug}");
                CredentialOperationResult approve = Credentials.ApproveApplication(apply.Application?.applicationId, GuildAuthority, qualification.Snapshot, "31", $"tx.credential.approve.{slug}");
                Assert.That(exam.Succeeded, Is.True, exam.Message);
                Assert.That(qualification.AuthoritativeQualified, Is.True, string.Join(",", qualification.BlockingFailures));
                Assert.That(apply.Succeeded, Is.True, apply.Message);
                Assert.That(approve.Succeeded, Is.True, approve.Message);
                return Credentials.IssueCredential($"credential-record.{slug}.apprentice", PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipCertificateCredentialId, PersonId, GuildIssuer(), apply.Application.applicationId, exam.ExaminationAttempt.attemptId, $"registration.{slug}.apprentice", qualification.Snapshot, "32", $"tx.credential.issue.{slug}");
            }

            public CredentialOperationResult IssueGuildLicense(string slug)
            {
                if (Credentials.QueryByRecipient(PersonId, activeOnly: true).Any(item => item.credentialDefinitionId == PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId))
                {
                    return CredentialOperationResult.Success("Guild license already active.", Credentials.Revision, Credentials.Revision, duplicate: true);
                }

                CompleteTrainingAndExperience($"{slug}.guild");
                CompleteSafetyTraining(slug);
                RecordIndependentActivity($"{slug}.independent");
                CredentialOperationResult practical = RecordExam($"{slug}.guild.practical", PrototypeProfessionDefinitionFactory.BlacksmithPracticalExaminationId, 850);
                CredentialOperationResult written = RecordExam($"{slug}.guild.written", PrototypeProfessionDefinitionFactory.BlacksmithWrittenExaminationId, 840);
                CredentialQualificationResult qualification = Credentials.EvaluateQualification(PersonId, PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, privilegedDiagnostics: true);
                CredentialOperationResult apply = Credentials.SubmitApplication($"credential-application.{slug}.guild", PersonId, PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, GuildIssuer(), qualification.Snapshot, "33", $"tx.credential.apply.guild.{slug}");
                CredentialOperationResult approve = Credentials.ApproveApplication(apply.Application?.applicationId, GuildAuthority, qualification.Snapshot, "34", $"tx.credential.approve.guild.{slug}");
                Assert.That(practical.Succeeded, Is.True, practical.Message);
                Assert.That(written.Succeeded, Is.True, written.Message);
                Assert.That(qualification.AuthoritativeQualified, Is.True, string.Join(",", qualification.BlockingFailures));
                Assert.That(apply.Succeeded, Is.True, apply.Message);
                Assert.That(approve.Succeeded, Is.True, approve.Message);
                return Credentials.IssueCredential($"credential-record.{slug}.guild", PrototypeProfessionDefinitionFactory.BlacksmithGuildLicenseCredentialId, PersonId, GuildIssuer(), apply.Application.applicationId, practical.ExaminationAttempt.attemptId, $"registration.{slug}.guild", qualification.Snapshot, "35", $"tx.credential.issue.guild.{slug}");
            }

            public void CompleteTrainingAndExperience(string slug)
            {
                if (!Training.QueryByProgram(PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId).Any(item => item.PersonId == PersonId && item.State == TrainingEnrollmentState.Completed))
                {
                    string enrollmentId = $"training-enrollment.{slug}.apprenticeship";
                    Training.ApplyToProgram(enrollmentId, PersonId, PrototypeProfessionDefinitionFactory.BlacksmithApprenticeshipProgramId, $"tx.training.apply.{slug}", worldTime: 1d);
                    Training.AcceptEnrollment(enrollmentId, $"tx.training.accept.{slug}");
                    Training.AssignInstructor(enrollmentId, $"training-instructor.{slug}", TrainingInstructorRoleKind.Master, PersonId, $"tx.training.instructor.{slug}", professionId: PrototypeProfessionDefinitionFactory.BlacksmithProfessionId, authorityId: GuildAuthority);
                    Training.BeginProgram(enrollmentId, $"tx.training.begin.{slug}");
                    Training.RunLearningSession($"training-session.{slug}.safety", enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyLessonId, $"tx.training.lesson.{slug}");
                    Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, $"tx.training.module.basics.{slug}");
                    Training.RunLearningSession($"training-session.{slug}.practice", enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticeModuleId, PrototypeProfessionDefinitionFactory.BlacksmithDemonstrationLessonId, $"tx.training.practice.{slug}");
                    Training.RecordPracticalAssignment($"training-practical.{slug}", enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticalAssignmentId, $"crafting-operation.{slug}.practice", TrainingAssignmentActivityCategory.Crafting, $"tx.training.practical.{slug}", quality: 750, supervisorPersonId: PersonId);
                    Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithPracticeModuleId, $"tx.training.module.practice.{slug}");
                    Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithHiddenAssessmentModuleId, $"tx.training.module.hidden.{slug}");
                    TrainingProgressResult progress = Training.EvaluateProgress(enrollmentId, perceived: false);
                    Training.CompleteProgram(enrollmentId, $"tx.training.complete.{slug}", progress.RuntimeToken, worldTime: 24d);
                }

                if (Activities.BuildExperienceSummary(PersonId, PrototypeProfessionDefinitionFactory.BlacksmithProfessionId).SupervisedCount == 0)
                {
                    Activities.RegisterAndValidateActivity(ActivityRequest($"activity.{slug}.supervised", PrototypeProfessionDefinitionFactory.BlacksmithSupervisedPracticeActivityDefinitionId, Source(ProfessionalActivitySourceType.TrainingPracticalAssignment, $"source.{slug}.supervised", "training.activity.practical"), ProfessionalResponsibilityLevel.SupervisedWorker, TrainingSupervisionLevel.CloselySupervised), $"evidence.{slug}.supervised", GuildAuthority, $"tx.activity.supervised.{slug}");
                }
            }

            public void CompleteSafetyTraining(string slug)
            {
                if (Training.QueryByProgram(PrototypeProfessionDefinitionFactory.BlacksmithSafetyProgramId).Any(item => item.PersonId == PersonId && item.State == TrainingEnrollmentState.Completed))
                {
                    return;
                }

                string enrollmentId = $"training-enrollment.{slug}.safety";
                Training.ApplyToProgram(enrollmentId, PersonId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyProgramId, $"tx.safety.apply.{slug}", worldTime: 25d);
                Training.AcceptEnrollment(enrollmentId, $"tx.safety.accept.{slug}");
                Training.BeginProgram(enrollmentId, $"tx.safety.begin.{slug}");
                Training.RunLearningSession($"training-session.{slug}.safety-only", enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, PrototypeProfessionDefinitionFactory.BlacksmithSafetyLessonId, $"tx.safety.lesson.{slug}");
                Training.CompleteModule(enrollmentId, PrototypeProfessionDefinitionFactory.BlacksmithBasicsModuleId, $"tx.safety.module.{slug}");
                TrainingProgressResult progress = Training.EvaluateProgress(enrollmentId, perceived: false);
                Training.CompleteProgram(enrollmentId, $"tx.safety.complete.{slug}", progress.RuntimeToken, worldTime: 26d);
            }

            public ProfessionalActivityOperationResult RecordIndependentActivity(string slug)
            {
                return Activities.RegisterAndValidateActivity(ActivityRequest($"activity.{slug}.independent", PrototypeProfessionDefinitionFactory.BlacksmithCraftingActivityDefinitionId, Source(ProfessionalActivitySourceType.CraftingOperation, $"source.{slug}.independent", "production.activity.forging", ProfessionalActivityDifficulty.Skilled), ProfessionalResponsibilityLevel.IndependentPractitioner, TrainingSupervisionLevel.IndependentWithReview), $"evidence.{slug}.independent", GuildAuthority, $"tx.activity.independent.{slug}");
            }

            public CredentialOperationResult RecordExam(string slug, string examinationDefinitionId, int score)
            {
                return Credentials.RecordExaminationAttempt(new CredentialExaminationAttemptData
                {
                    attemptId = $"credential-exam.{slug}",
                    examinationDefinitionId = examinationDefinitionId,
                    applicantPersonId = PersonId,
                    evaluatorPersonId = PersonId,
                    evaluatorAuthorityId = GuildAuthority,
                    startWorldTime = "27",
                    completionWorldTime = "28",
                    score = score,
                    sectionResults = new[]
                    {
                        new CredentialExaminationSectionResultData
                        {
                            sectionId = $"credential-section.{slug}",
                            displayName = "Prototype assessment",
                            score = score,
                            passed = score >= 700
                        }
                    },
                    provenance = $"credential-exam-provenance.{slug}"
                }, $"tx.exam.{slug}");
            }

            private static CredentialIssuerReferenceData GuildIssuer()
            {
                return new CredentialIssuerReferenceData
                {
                    issuerId = GuildAuthority,
                    issuerKind = CredentialIssuerAuthorityKind.Guild
                };
            }

            private static ProfessionalActivityRegistrationRequest ActivityRequest(string activityId, string definitionId, ProfessionalActivitySourceSnapshot source, ProfessionalResponsibilityLevel responsibility, TrainingSupervisionLevel supervision)
            {
                return new ProfessionalActivityRegistrationRequest
                {
                    ActivityId = activityId,
                    PersonId = PersonId,
                    ProfessionId = PrototypeProfessionDefinitionFactory.BlacksmithProfessionId,
                    SpecializationId = PrototypeProfessionDefinitionFactory.WeaponsmithSpecializationId,
                    ActivityDefinitionId = definitionId,
                    Source = source,
                    Responsibility = responsibility,
                    SupervisionLevel = supervision,
                    CompletionWorldTime = source.WorldTime,
                    QuantityOrDuration = source.QuantityOrDuration,
                    Quality = source.Quality,
                    Difficulty = source.Difficulty,
                    Outcome = source.Outcome,
                    AccessPolicyId = PrototypeProfessionDefinitionFactory.AccessPublicId,
                    Provenance = "test"
                };
            }

            private static ProfessionalActivitySourceSnapshot Source(ProfessionalActivitySourceType type, string sourceId, string tag, ProfessionalActivityDifficulty difficulty = ProfessionalActivityDifficulty.Routine)
            {
                return ProfessionalActivitySourceAdapters.FromCustom(type, sourceId, PersonId, ProfessionalActivityOutcomeState.Successful, quality: difficulty >= ProfessionalActivityDifficulty.Skilled ? 780 : 720, difficulty: difficulty, worldTime: $"time.{sourceId}", tags: tag);
            }
        }
    }
}
