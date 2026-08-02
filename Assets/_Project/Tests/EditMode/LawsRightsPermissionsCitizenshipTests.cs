using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityIsekaiGame.Diplomacy;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Governments;
using UnityIsekaiGame.Laws;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Tests
{
    public sealed class LawsRightsPermissionsCitizenshipTests
    {
        private const string PersonId = "person.prototype.player";

        [Test]
        public void PrototypeLegalDefinitionsValidateWithoutWarnings()
        {
            DefinitionRegistry registry = CreateRegistry();
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(registry.DefinitionsById.Values.OfType<ScriptableObject>().ToArray());
            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.ErrorCount, Is.Zero, report.ToString());
            Assert.That(report.WarningCount, Is.Zero, report.ToString());
            Assert.That(registry.TryGet(PrototypeLegalDefinitionFactory.SovereignAuthorityId, out LegalAuthorityDefinition authority), Is.True);
            Assert.That(registry.TryGet(PrototypeLegalDefinitionFactory.CentralStatuteId, out LegalInstrumentDefinition instrument), Is.True);
            Assert.That(registry.TryGet(PrototypeLegalDefinitionFactory.CitizenshipId, out CitizenshipDefinition citizenship), Is.True);
            Assert.That(authority.Category, Is.EqualTo(LegalAuthorityCategory.SovereignLegislative));
            Assert.That(instrument.Precedence, Is.GreaterThan(0));
            Assert.That(citizenship.Routes, Does.Contain(CitizenshipAcquisitionRoute.Grant));
        }

        [Test]
        public void EnactmentIsAtomicStableAndRequiresExplicitAuthority()
        {
            RuntimeFixture fixture = CreateFixture();
            EnactLegalInstrumentRequest request = InstrumentRequest("atomic", "activity.trade", LegalEffectCategory.Permission);
            request.trustedSystemOperation = false;
            long before = fixture.Laws.Revision;

            LegalOperationResult denied = fixture.Laws.Enact(request);
            request.trustedSystemOperation = true;
            LegalOperationResult preview = fixture.Laws.Enact(CloneRequest(request, preview: true));
            int events = 0;
            fixture.Laws.OperationCommitted += _ => events++;
            LegalOperationResult enacted = fixture.Laws.Enact(request);
            LegalOperationResult duplicate = fixture.Laws.Enact(request);

            Assert.That(denied.Code, Is.EqualTo(LegalOperationCode.MissingAuthority));
            Assert.That(preview.Preview, Is.True);
            Assert.That(enacted.Succeeded, Is.True, enacted.Message);
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(fixture.Laws.Revision, Is.EqualTo(before + 1L));
            Assert.That(fixture.Laws.Instruments.Count, Is.EqualTo(1));
            Assert.That(fixture.Laws.Provisions.Count, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void ApplicabilityUsesScopeHierarchyAndIndividualImmunity()
        {
            RuntimeFixture fixture = CreateFixture();
            fixture.Laws.Enact(InstrumentRequest("prohibit", "activity.entry", LegalEffectCategory.Prohibition));
            LegalApplicabilityResult prohibited = fixture.Laws.Evaluate(Applicability("activity.entry", 11d));
            LegalOperationResult immunity = fixture.Laws.GrantEntitlement(new LegalEntitlementRequest { transactionId = "tx.immunity", entitlementId = "legal-immunity.test", effect = LegalEffectCategory.Immunity, personId = PersonId, actionId = "activity.entry", territoryId = "political-territory.test.realm", effectiveWorldTime = 10d, trustedSystemOperation = true });
            LegalApplicabilityResult immune = fixture.Laws.Evaluate(Applicability("activity.entry", 11d));

            Assert.That(prohibited.Status, Is.EqualTo(LegalApplicabilityStatus.Prohibited));
            Assert.That(immunity.Succeeded, Is.True, immunity.Message);
            Assert.That(immune.Status, Is.EqualTo(LegalApplicabilityStatus.Immune));
            Assert.That(fixture.Laws.Provisions.Single().versions.Single().effect, Is.EqualTo(LegalEffectCategory.Prohibition));
        }

        [Test]
        public void AmendmentPreservesHistoricalVersionAndImmutableSnapshots()
        {
            RuntimeFixture fixture = CreateFixture();
            fixture.Laws.Enact(InstrumentRequest("history", "activity.trade", LegalEffectCategory.Permission));
            LegalProvisionRecordData snapshot = fixture.Laws.Provisions.Single();
            LegalOperationResult amended = fixture.Laws.AmendProvision(new AmendLegalProvisionRequest { transactionId = "tx.amend", amendmentId = "legal-amendment.test", provisionId = "legal-provision.test.history", effectiveWorldTime = 20d, trustedSystemOperation = true, version = new LegalProvisionVersionData { actionId = "activity.travel", territoryIds = new[] { "political-territory.test.realm" } } });
            snapshot.versions[0].actionId = "mutated.outside.runtime";

            Assert.That(amended.Succeeded, Is.True, amended.Message);
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.trade", 15d)).Status, Is.EqualTo(LegalApplicabilityStatus.Permitted));
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.travel", 25d)).Status, Is.EqualTo(LegalApplicabilityStatus.Permitted));
            Assert.That(fixture.Laws.Provisions.Single().versions[0].actionId, Is.EqualTo("activity.trade"));
            Assert.That(fixture.Laws.Provisions.Single().versions.Count, Is.EqualTo(2));
        }

        [Test]
        public void CitizenshipRequiresConsentAndPreservesLifecycleHistory()
        {
            RuntimeFixture fixture = CreateFixture();
            LegalStatusGrantRequest request = CitizenshipRequest(consent: false);
            long before = fixture.Laws.Revision;
            LegalOperationResult denied = fixture.Laws.GrantLegalStatus(request);
            request.consentGiven = true;
            LegalOperationResult granted = fixture.Laws.GrantLegalStatus(request);
            LegalOperationResult renouncedWithoutConsent = fixture.Laws.TransitionLegalStatus(new LegalStatusTransitionRequest { transactionId = "tx.renounce.denied", statusId = request.statusId, targetState = LegalStatusLifecycleState.Renounced, worldTime = 20d, trustedSystemOperation = true });
            LegalOperationResult renounced = fixture.Laws.TransitionLegalStatus(new LegalStatusTransitionRequest { transactionId = "tx.renounce", statusId = request.statusId, targetState = LegalStatusLifecycleState.Renounced, personConsent = true, worldTime = 20d, trustedSystemOperation = true });

            Assert.That(denied.Succeeded, Is.False);
            Assert.That(fixture.Laws.Revision, Is.EqualTo(before + 2L));
            Assert.That(granted.Succeeded, Is.True, granted.Message);
            Assert.That(renouncedWithoutConsent.Succeeded, Is.False);
            Assert.That(renounced.Succeeded, Is.True, renounced.Message);
            Assert.That(fixture.Laws.TryGetStatus(request.statusId, out PersonLegalStatusRecordData status), Is.True);
            Assert.That(status.lifecycleState, Is.EqualTo(LegalStatusLifecycleState.Renounced));
            Assert.That(status.endedWorldTime, Is.EqualTo(20d));
        }

        [Test]
        public void SchedulingIsDeterministicAndSameBoundaryIsIdempotent()
        {
            RuntimeFixture fixture = CreateFixture();
            EnactLegalInstrumentRequest request = InstrumentRequest("scheduled", "activity.travel", LegalEffectCategory.Permission);
            request.effectiveWorldTime = 20d;
            request.provisions[0].version.effectiveWorldTime = 20d;
            fixture.Laws.Enact(request);
            LegalOperationResult first = fixture.Laws.ProcessWorldTime(new LegalTimeEvaluationRequest { transactionId = "tx.time.20", boundaryId = "legal-boundary.20", worldTime = 20d });
            long revision = fixture.Laws.Revision;
            LegalOperationResult second = fixture.Laws.ProcessWorldTime(new LegalTimeEvaluationRequest { transactionId = "tx.time.20", boundaryId = "legal-boundary.20", worldTime = 20d });

            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(second.Duplicate, Is.True);
            Assert.That(fixture.Laws.Revision, Is.EqualTo(revision));
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.travel", 20d)).Status, Is.EqualTo(LegalApplicabilityStatus.Permitted));
        }

        [Test]
        public void PublicationAndPartialRepealUseExplicitLifecycleOperations()
        {
            RuntimeFixture fixture = CreateFixture();
            EnactLegalInstrumentRequest request = InstrumentRequest("publication", "activity.publish", LegalEffectCategory.Permission);
            request.published = false;
            request.publicationWorldTime = -1d;
            Assert.That(fixture.Laws.Enact(request).Succeeded, Is.True);
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.publish", 11d)).Status, Is.EqualTo(LegalApplicabilityStatus.NoApplicableLaw));

            LegalOperationResult published = fixture.Laws.PublishInstrument(new PublishLegalInstrumentRequest { transactionId = "tx.publish", instrumentId = request.instrumentId, worldTime = 8d, trustedSystemOperation = true });
            LegalOperationResult repealed = fixture.Laws.TransitionProvision(new LegalProvisionTransitionRequest { transactionId = "tx.partial-repeal", provisionId = request.provisions[0].provisionId, targetState = LegalProvisionLifecycleState.Repealed, worldTime = 12d, trustedSystemOperation = true });

            Assert.That(published.Succeeded, Is.True, published.Message);
            Assert.That(repealed.Succeeded, Is.True, repealed.Message);
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.publish", 13d)).Status, Is.EqualTo(LegalApplicabilityStatus.NoApplicableLaw));
            Assert.That(fixture.Laws.GetLawsByLifecycle(LegalInstrumentLifecycleState.Scheduled).Count, Is.EqualTo(1));
        }

        [Test]
        public void ScheduledProcessingIsBoundedAndStableAcrossBatches()
        {
            RuntimeFixture fixture = CreateFixture();
            EnactLegalInstrumentRequest first = InstrumentRequest("batch-a", "activity.batch-a", LegalEffectCategory.Permission);
            EnactLegalInstrumentRequest second = InstrumentRequest("batch-b", "activity.batch-b", LegalEffectCategory.Permission);
            first.effectiveWorldTime = second.effectiveWorldTime = 20d;
            first.provisions[0].version.effectiveWorldTime = second.provisions[0].version.effectiveWorldTime = 20d;
            fixture.Laws.Enact(first);
            fixture.Laws.Enact(second);

            LegalOperationResult batchOne = fixture.Laws.ProcessWorldTime(new LegalTimeEvaluationRequest { transactionId = "tx.batch.1", boundaryId = "boundary.batch.1", worldTime = 20d, maximumOperations = 1 });
            Assert.That(batchOne.Message, Does.Contain("1 remain pending"));
            Assert.That(fixture.Laws.GetLawsByLifecycle(LegalInstrumentLifecycleState.Active).Count, Is.EqualTo(1));
            Assert.That(fixture.Laws.GetLawsByLifecycle(LegalInstrumentLifecycleState.Scheduled).Count, Is.EqualTo(1));

            LegalOperationResult batchTwo = fixture.Laws.ProcessWorldTime(new LegalTimeEvaluationRequest { transactionId = "tx.batch.2", boundaryId = "boundary.batch.2", worldTime = 20d, maximumOperations = 1 });
            Assert.That(batchTwo.Succeeded, Is.True, batchTwo.Message);
            Assert.That(fixture.Laws.GetLawsByLifecycle(LegalInstrumentLifecycleState.Active).Count, Is.EqualTo(2));
        }

        [Test]
        public void SharedValidationServiceRejectsBrokenIndexesWithoutMutation()
        {
            RuntimeFixture fixture = CreateFixture();
            fixture.Laws.Enact(InstrumentRequest("validation", "activity.validation", LegalEffectCategory.Right));
            LegalRuntimeValidationService validator = new LegalRuntimeValidationService();
            Assert.That(validator.Validate(fixture.Laws, fixture.Registry, fixture.Governments, fixture.Organizations, fixture.Authority, fixture.Decisions, fixture.Diplomacy, null, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>()).IsValid, Is.True);

            LegalRuntimeSaveData corrupt = fixture.Laws.CreateSaveData();
            corrupt.instruments[0].provisionIds = Array.Empty<string>();
            long revision = fixture.Laws.Revision;
            LegalValidationReport report = validator.Validate(corrupt, fixture.Registry, fixture.Governments, fixture.Organizations, fixture.Authority, fixture.Decisions, fixture.Diplomacy, null, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Single(), Does.Contain("provision index"));
            Assert.That(fixture.Laws.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void VisibilityProjectionDoesNotChangeAuthoritativeApplicability()
        {
            RuntimeFixture fixture = CreateFixture();
            EnactLegalInstrumentRequest request = InstrumentRequest("hidden", "activity.secret", LegalEffectCategory.Prohibition);
            request.visibility = PoliticalVisibility.Hidden;
            request.published = false;
            fixture.Laws.Enact(request);

            LegalProjectionResult<LegalInstrumentRecordData> publicView = fixture.Laws.ProjectInstrument(request.instrumentId, privileged: false);
            LegalProjectionResult<LegalInstrumentRecordData> privileged = fixture.Laws.ProjectInstrument(request.instrumentId, privileged: true);

            Assert.That(publicView.Succeeded, Is.False);
            Assert.That(privileged.Succeeded, Is.True);
            Assert.That(fixture.Laws.Evaluate(Applicability("activity.secret", 11d)).Status, Is.EqualTo(LegalApplicabilityStatus.Prohibited));
        }

        [Test]
        public void PersistencePrepareRejectsCorruptGraphWithoutLiveMutation()
        {
            RuntimeFixture fixture = CreateFixture();
            fixture.Laws.Enact(InstrumentRequest("persist", "activity.trade", LegalEffectCategory.Right));
            LegalPersistenceParticipant participant = new LegalPersistenceParticipant(fixture.Laws, () => fixture.Registry, () => fixture.Governments, () => fixture.Organizations, () => fixture.Authority, () => fixture.Decisions, () => fixture.Diplomacy, () => null, PersistenceService.LocalWorldId, () => new[] { PersonId }, () => Array.Empty<string>());
            PersistenceParticipantSaveResult captured = participant.CapturePayload();
            LegalRuntimeSaveData corrupt = fixture.Laws.CreateSaveData();
            corrupt.provisions[0].instrumentId = "legal-instrument.missing";
            long before = fixture.Laws.Revision;

            PersistenceParticipantPrepareResult rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), LegalPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantPrepareResult prepared = participant.PreparePayload(captured.PayloadJson, LegalPersistenceParticipant.CurrentParticipantSchemaVersion);

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(fixture.Laws.Revision, Is.EqualTo(before));
            Assert.That(prepared.Succeeded, Is.True, prepared.Message);
        }

        private static RuntimeFixture CreateFixture()
        {
            DefinitionRegistry registry = CreateRegistry();
            OrganizationRuntime organizations = new OrganizationRuntime();
            PrototypeOrganizationDefinitionFactory.SeedPrototypeOrganizations(organizations, registry, PersistenceService.LocalWorldId);
            organizations.Configure(registry, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());
            OrganizationMembershipRuntime memberships = new OrganizationMembershipRuntime(); memberships.Configure(registry, organizations, PersistenceService.LocalWorldId, new[] { PersonId }, PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds);
            OrganizationAuthorityRuntime authority = new OrganizationAuthorityRuntime(); authority.Configure(registry, organizations, memberships, PersistenceService.LocalWorldId, new[] { PersonId }, PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds);
            OrganizationResourceRuntime resources = new OrganizationResourceRuntime(); resources.Configure(registry, organizations, authority, null, PersistenceService.LocalWorldId);
            OrganizationDecisionRuntime decisions = new OrganizationDecisionRuntime(); decisions.Configure(registry, organizations, memberships, authority, resources, PersistenceService.LocalWorldId, new[] { PersonId }, null);
            FactionRuntime factions = new FactionRuntime(); factions.Configure(registry, organizations, memberships, authority, resources, decisions, PersistenceService.LocalWorldId, new[] { PersonId });
            DiplomacyRuntime diplomacy = new DiplomacyRuntime(); diplomacy.Configure(registry, organizations, factions, authority, decisions, resources, PersistenceService.LocalWorldId, new[] { PersonId });
            GovernmentRuntime governments = new GovernmentRuntime(); governments.Configure(registry, organizations, memberships, authority, decisions, resources, factions, diplomacy, null, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());
            Assert.That(governments.CreatePolity(new PolityCreateRequest { transactionId = "tx.polity", polityId = "polity.test.kingdom", polityDefinitionId = PrototypeGovernmentDefinitionFactory.KingdomPolityDefinitionId, officialName = "Test Kingdom", worldTime = 1d }).Succeeded, Is.True);
            Assert.That(governments.RegisterGovernment(new GovernmentRegisterRequest { transactionId = "tx.government", governmentId = "government.test.royal", governmentDefinitionId = PrototypeGovernmentDefinitionFactory.RoyalGovernmentDefinitionId, polityId = "polity.test.kingdom", officialName = "Test Government", primaryGoverningOrganizationId = "organization.prototype.guild", governingOrganizationIds = new[] { "organization.prototype.guild" }, level = GovernmentLevel.Central, worldTime = 2d }).Succeeded, Is.True);
            Assert.That(governments.CreateTerritory(new TerritoryCreateRequest { transactionId = "tx.territory", territoryId = "political-territory.test.realm", territoryDefinitionId = PrototypeGovernmentDefinitionFactory.RealmTerritoryDefinitionId, displayName = "Test Realm", polityId = "polity.test.kingdom", primaryGovernmentId = "government.test.royal", placeIds = new[] { "place.test.capital" }, worldTime = 3d }).Succeeded, Is.True);
            Assert.That(governments.CreateJurisdiction(new JurisdictionCreateRequest { transactionId = "tx.jurisdiction", jurisdictionId = "jurisdiction.test.general", jurisdictionDefinitionId = PrototypeGovernmentDefinitionFactory.GeneralJurisdictionDefinitionId, governmentId = "government.test.royal", category = JurisdictionCategory.GeneralGovernment, scopeDimensions = JurisdictionScopeDimension.Territory, territoryIds = new[] { "political-territory.test.realm" }, priority = 100, worldTime = 4d }).Succeeded, Is.True);
            LegalRuntime laws = new LegalRuntime(); laws.Configure(registry, governments, organizations, authority, decisions, diplomacy, null, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());
            return new RuntimeFixture(registry, organizations, authority, decisions, diplomacy, governments, laws);
        }

        private static DefinitionRegistry CreateRegistry() => PrototypeLegalDefinitionFactory.AddMissingPrototypeLegalDefinitions(PrototypeGovernmentDefinitionFactory.AddMissingPrototypeGovernmentDefinitions(PrototypeDiplomacyDefinitionFactory.AddMissingPrototypeDiplomacyDefinitions(PrototypeFactionDefinitionFactory.AddMissingPrototypeFactionDefinitions(PrototypeOrganizationDecisionDefinitionFactory.AddMissingPrototypeOrganizationDecisionDefinitions(PrototypeOrganizationResourceDefinitionFactory.AddMissingPrototypeOrganizationResourceDefinitions(PrototypeOrganizationAuthorityDefinitionFactory.AddMissingPrototypeOrganizationAuthorityDefinitions(PrototypeOrganizationMembershipDefinitionFactory.AddMissingPrototypeOrganizationMembershipDefinitions(PrototypeOrganizationDefinitionFactory.AddMissingPrototypeOrganizationDefinitions(new DefinitionRegistry(Array.Empty<IGameDefinition>()))))))))));

        private static EnactLegalInstrumentRequest InstrumentRequest(string suffix, string actionId, LegalEffectCategory effect)
        {
            string definitionId = effect switch { LegalEffectCategory.Right => PrototypeLegalDefinitionFactory.RightProvisionId, LegalEffectCategory.Prohibition => PrototypeLegalDefinitionFactory.ProhibitionProvisionId, _ => PrototypeLegalDefinitionFactory.PermissionProvisionId };
            return new EnactLegalInstrumentRequest { transactionId = $"tx.enact.{suffix}", instrumentId = $"legal-instrument.test.{suffix}", instrumentDefinitionId = PrototypeLegalDefinitionFactory.CentralStatuteId, authorityDefinitionId = PrototypeLegalDefinitionFactory.SovereignAuthorityId, title = "Test Law", governmentId = "government.test.royal", organizationId = "organization.prototype.guild", jurisdictionIds = new[] { "jurisdiction.test.general" }, enactmentWorldTime = 5d, publicationWorldTime = 5d, effectiveWorldTime = 10d, published = true, visibility = PoliticalVisibility.Public, trustedSystemOperation = true, provisions = new[] { new LegalProvisionCreateRequest { provisionId = $"legal-provision.test.{suffix}", provisionDefinitionId = definitionId, version = new LegalProvisionVersionData { actionId = actionId, territoryIds = new[] { "political-territory.test.realm" }, effectiveWorldTime = 10d } } } };
        }

        private static EnactLegalInstrumentRequest CloneRequest(EnactLegalInstrumentRequest source, bool preview) { EnactLegalInstrumentRequest clone = InstrumentRequest(source.instrumentId.Split('.').Last(), source.provisions[0].version.actionId, source.provisions[0].version.effect); clone.transactionId = source.transactionId; clone.preview = preview; return clone; }
        private static LegalApplicabilityRequest Applicability(string actionId, double time) => new LegalApplicabilityRequest { personId = PersonId, territoryId = "political-territory.test.realm", actionId = actionId, worldTime = time };
        private static LegalStatusGrantRequest CitizenshipRequest(bool consent) => new LegalStatusGrantRequest { transactionId = "tx.citizenship", statusId = "legal-status.test.citizen", statusDefinitionId = PrototypeLegalDefinitionFactory.CitizenStatusId, citizenshipDefinitionId = PrototypeLegalDefinitionFactory.CitizenshipId, personId = PersonId, polityId = "polity.test.kingdom", recognizingGovernmentId = "government.test.royal", acquisitionRoute = CitizenshipAcquisitionRoute.Grant, consentGiven = consent, effectiveWorldTime = 10d, trustedSystemOperation = true };

        private sealed class RuntimeFixture
        {
            public RuntimeFixture(DefinitionRegistry registry, OrganizationRuntime organizations, OrganizationAuthorityRuntime authority, OrganizationDecisionRuntime decisions, DiplomacyRuntime diplomacy, GovernmentRuntime governments, LegalRuntime laws) { Registry = registry; Organizations = organizations; Authority = authority; Decisions = decisions; Diplomacy = diplomacy; Governments = governments; Laws = laws; }
            public DefinitionRegistry Registry { get; } public OrganizationRuntime Organizations { get; } public OrganizationAuthorityRuntime Authority { get; } public OrganizationDecisionRuntime Decisions { get; } public DiplomacyRuntime Diplomacy { get; } public GovernmentRuntime Governments { get; } public LegalRuntime Laws { get; }
        }
    }
}
