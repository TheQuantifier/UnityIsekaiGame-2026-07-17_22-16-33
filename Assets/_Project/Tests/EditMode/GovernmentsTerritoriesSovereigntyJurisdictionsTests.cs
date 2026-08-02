using System;
using System.Linq;
using NUnit.Framework;
using UnityIsekaiGame.Diplomacy;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Governments;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Persistence;
using UnityEngine;

namespace UnityIsekaiGame.Tests
{
    public sealed class GovernmentsTerritoriesSovereigntyJurisdictionsTests
    {
        private const string PersonId = "person.prototype.player";

        [Test]
        public void PrototypeCatalog_ResolvesGovernmentDefinitionsAndValidates()
        {
            DefinitionRegistry registry = CreateRegistry();
            DefinitionCatalog catalog = ClassificationTestFactory.CreateCatalog(registry.DefinitionsById.Values.OfType<ScriptableObject>().ToArray());
            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.ErrorCount, Is.Zero, report.ToString());
            Assert.That(report.WarningCount, Is.Zero, report.ToString());
            Assert.That(registry.TryGet(PrototypeGovernmentDefinitionFactory.KingdomPolityDefinitionId, out PolityDefinition polity), Is.True);
            Assert.That(registry.TryGet(PrototypeGovernmentDefinitionFactory.RoyalGovernmentDefinitionId, out GovernmentDefinition government), Is.True);
            Assert.That(registry.TryGet(PrototypeGovernmentDefinitionFactory.RealmTerritoryDefinitionId, out PoliticalTerritoryDefinition territory), Is.True);
            Assert.That(registry.TryGet(PrototypeGovernmentDefinitionFactory.GeneralJurisdictionDefinitionId, out JurisdictionDefinition jurisdiction), Is.True);
            Assert.That(polity.Category, Is.EqualTo(PolityCategory.Kingdom));
            Assert.That(government.Category, Is.EqualTo(GovernmentCategory.MonarchicalGovernment));
            Assert.That(territory.Category, Is.EqualTo(PoliticalTerritoryCategory.Realm));
            Assert.That(jurisdiction.Category, Is.EqualTo(JurisdictionCategory.GeneralGovernment));
        }

        [Test]
        public void PolityGovernmentTerritoryAndPropertyRemainDistinct()
        {
            RuntimeFixture fixture = CreateFixture();
            CreatePoliticalGraph(fixture, PoliticalVisibility.Public);

            Assert.That(fixture.Governments.PolityCount, Is.EqualTo(1));
            Assert.That(fixture.Governments.GovernmentCount, Is.EqualTo(1));
            Assert.That(fixture.Governments.TerritoryCount, Is.EqualTo(1));
            Assert.That(fixture.Governments.TryGetPolity("polity.test.kingdom", out PolityRecordData polity), Is.True);
            Assert.That(fixture.Governments.TryGetGovernment("government.test.royal", out GovernmentRecordData government), Is.True);
            Assert.That(fixture.Governments.TryGetTerritory("political-territory.test.realm", out PoliticalTerritoryRecordData territory), Is.True);
            Assert.That(polity.currentGovernmentId, Is.EqualTo(government.governmentId));
            Assert.That(territory.primaryGovernmentId, Is.EqualTo(government.governmentId));
            Assert.That(territory.placeIds, Does.Contain("place.test.capital"));
        }

        [Test]
        public void JurisdictionResolutionUsesScopePriorityAndEffectiveTimeDeterministically()
        {
            RuntimeFixture fixture = CreateFixture();
            CreatePoliticalGraph(fixture, PoliticalVisibility.Public);
            PoliticalOperationResult general = fixture.Governments.CreateJurisdiction(Jurisdiction("general", PrototypeGovernmentDefinitionFactory.GeneralJurisdictionDefinitionId, 10, 10d, -1d));
            PoliticalOperationResult municipal = fixture.Governments.CreateJurisdiction(Jurisdiction("municipal", PrototypeGovernmentDefinitionFactory.MunicipalJurisdictionDefinitionId, 20, 20d, -1d));

            JurisdictionResolutionResult beforeMunicipal = fixture.Governments.ResolveJurisdiction(Resolution(15d));
            JurisdictionResolutionResult afterMunicipal = fixture.Governments.ResolveJurisdiction(Resolution(25d));
            JurisdictionResolutionResult repeated = fixture.Governments.ResolveJurisdiction(Resolution(25d));

            Assert.That(general.Succeeded, Is.True, general.Message);
            Assert.That(municipal.Succeeded, Is.True, municipal.Message);
            Assert.That(beforeMunicipal.SelectedJurisdiction.jurisdictionId, Is.EqualTo("jurisdiction.test.general"));
            Assert.That(afterMunicipal.SelectedJurisdiction.jurisdictionId, Is.EqualTo("jurisdiction.test.municipal"));
            Assert.That(repeated.SelectedJurisdiction.jurisdictionId, Is.EqualTo(afterMunicipal.SelectedJurisdiction.jurisdictionId));
            Assert.That(repeated.ApplicableJurisdictions.Select(item => item.jurisdictionId), Is.EqualTo(afterMunicipal.ApplicableJurisdictions.Select(item => item.jurisdictionId)));
        }

        [Test]
        public void LifecycleCommandsPreserveStableIdentityAndImmutableSnapshots()
        {
            RuntimeFixture fixture = CreateFixture();
            CreatePoliticalGraph(fixture, PoliticalVisibility.Public);
            PolityRecordData snapshot = fixture.Governments.Polities.Single();

            PoliticalOperationResult renamed = fixture.Governments.RenamePolity(new PolityRenameRequest { transactionId = "tx.polity.rename", polityId = snapshot.polityId, nameRecordId = "polity.test.kingdom.name.common", name = "The Test Realm", category = PoliticalNameCategory.Common, makeOfficial = true, worldTime = 4d });
            PoliticalOperationResult membership = fixture.Governments.ChangeTerritoryPlaceMembership(new TerritoryPlaceMembershipRequest { transactionId = "tx.territory.place", membershipId = "political-territory.test.realm.place.002", territoryId = "political-territory.test.realm", placeId = "place.test.harbor", membershipKind = TerritoryMembershipKind.ContainsPlace, worldTime = 5d });
            snapshot.officialName = "Mutated outside runtime";
            snapshot.claimedTerritoryIds = Array.Empty<string>();

            Assert.That(renamed.Succeeded, Is.True, renamed.Message);
            Assert.That(membership.Succeeded, Is.True, membership.Message);
            Assert.That(fixture.Governments.TryGetPolity("polity.test.kingdom", out PolityRecordData current), Is.True);
            Assert.That(current.polityId, Is.EqualTo("polity.test.kingdom"));
            Assert.That(current.officialName, Is.EqualTo("The Test Realm"));
            Assert.That(current.claimedTerritoryIds, Does.Contain("political-territory.test.realm"));
            Assert.That(fixture.Governments.TryGetTerritory("political-territory.test.realm", out PoliticalTerritoryRecordData territory), Is.True);
            Assert.That(territory.placeIds, Does.Contain("place.test.harbor"));
        }

        [Test]
        public void TerritorialTransferCommitsAtomicallyAndPublishesOnce()
        {
            RuntimeFixture fixture = CreateFixture();
            CreatePoliticalGraph(fixture, PoliticalVisibility.Public);
            Assert.That(fixture.Governments.RegisterGovernment(new GovernmentRegisterRequest { transactionId = "tx.government.provincial", governmentId = "government.test.provincial", governmentDefinitionId = PrototypeGovernmentDefinitionFactory.ProvincialAdministrationDefinitionId, polityId = "polity.test.kingdom", officialName = "Provincial Government", primaryGoverningOrganizationId = "organization.prototype.guild", governingOrganizationIds = new[] { "organization.prototype.guild" }, level = GovernmentLevel.Provincial, worldTime = 4d }).Succeeded, Is.True);
            Assert.That(fixture.Governments.RecordControl(new TerritorialControlRequest { transactionId = "tx.control.initial", controlId = "control.test.initial", territoryId = "political-territory.test.realm", controllingGovernmentId = "government.test.royal", worldTime = 5d }).Succeeded, Is.True);
            int events = 0;
            fixture.Governments.OperationCommitted += _ => events++;
            long before = fixture.Governments.Revision;

            PoliticalOperationResult transfer = fixture.Governments.TransferTerritory(new TerritorialTransferRequest { transactionId = "tx.transfer", transitionId = "transition.test.transfer", sourceGovernmentId = "government.test.royal", targetGovernmentId = "government.test.provincial", territoryIds = new[] { "political-territory.test.realm" }, worldTime = 6d });

            Assert.That(transfer.Succeeded, Is.True, transfer.Message);
            Assert.That(fixture.Governments.Revision, Is.EqualTo(before + 1L));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(fixture.Governments.Controls.Single(item => item.controlId == "control.test.initial").state, Is.EqualTo(TerritorialControlState.Historical));
            Assert.That(fixture.Governments.Controls.Single(item => item.controlId.Contains("transition.test.transfer")).controllingGovernmentId, Is.EqualTo("government.test.provincial"));
            Assert.That(fixture.Governments.TryGetTerritory("political-territory.test.realm", out PoliticalTerritoryRecordData territory), Is.True);
            Assert.That(territory.primaryGovernmentId, Is.EqualTo("government.test.provincial"));
        }

        [Test]
        public void PersistenceParticipantRejectsCorruptGraphWithoutMutation()
        {
            RuntimeFixture fixture = CreateFixture();
            CreatePoliticalGraph(fixture, PoliticalVisibility.Secret);
            GovernmentPersistenceParticipant participant = new GovernmentPersistenceParticipant(
                fixture.Governments,
                () => fixture.Registry,
                () => fixture.Organizations,
                () => fixture.Memberships,
                () => fixture.Authority,
                () => fixture.Decisions,
                () => fixture.Resources,
                () => fixture.Factions,
                () => fixture.Diplomacy,
                () => null,
                PersistenceService.LocalWorldId,
                () => new[] { PersonId },
                () => Array.Empty<string>());
            var captured = participant.CapturePayload();
            GovernmentRuntimeSaveData corrupt = fixture.Governments.CreateSaveData();
            corrupt.governments[0].polityId = "polity.missing";
            long before = fixture.Governments.Revision;

            PersistenceParticipantPrepareResult rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), GovernmentPersistenceParticipant.CurrentParticipantSchemaVersion);
            PersistenceParticipantPrepareResult accepted = participant.PreparePayload(captured.PayloadJson, GovernmentPersistenceParticipant.CurrentParticipantSchemaVersion);

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(fixture.Governments.Revision, Is.EqualTo(before));
            Assert.That(accepted.Succeeded, Is.True, accepted.Message);
            Assert.That(fixture.Governments.ProjectGovernment("government.test.royal", privileged: false).Redacted, Is.True);
        }

        private static RuntimeFixture CreateFixture()
        {
            DefinitionRegistry registry = CreateRegistry();
            OrganizationRuntime organizations = new OrganizationRuntime();
            PrototypeOrganizationDefinitionFactory.SeedPrototypeOrganizations(organizations, registry, PersistenceService.LocalWorldId);
            organizations.Configure(registry, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());
            OrganizationMembershipRuntime memberships = new OrganizationMembershipRuntime();
            memberships.Configure(registry, organizations, PersistenceService.LocalWorldId, new[] { PersonId }, PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds);
            OrganizationAuthorityRuntime authority = new OrganizationAuthorityRuntime();
            authority.Configure(registry, organizations, memberships, PersistenceService.LocalWorldId, new[] { PersonId }, PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds);
            OrganizationResourceRuntime resources = new OrganizationResourceRuntime();
            resources.Configure(registry, organizations, authority, null, PersistenceService.LocalWorldId);
            OrganizationDecisionRuntime decisions = new OrganizationDecisionRuntime();
            decisions.Configure(registry, organizations, memberships, authority, resources, PersistenceService.LocalWorldId, new[] { PersonId }, null);
            FactionRuntime factions = new FactionRuntime();
            factions.Configure(registry, organizations, memberships, authority, resources, decisions, PersistenceService.LocalWorldId, new[] { PersonId });
            DiplomacyRuntime diplomacy = new DiplomacyRuntime();
            diplomacy.Configure(registry, organizations, factions, authority, decisions, resources, PersistenceService.LocalWorldId, new[] { PersonId });
            GovernmentRuntime governments = new GovernmentRuntime();
            governments.Configure(registry, organizations, memberships, authority, decisions, resources, factions, diplomacy, null, PersistenceService.LocalWorldId, new[] { PersonId }, Array.Empty<string>());
            return new RuntimeFixture(registry, organizations, memberships, authority, resources, decisions, factions, diplomacy, governments);
        }

        private static DefinitionRegistry CreateRegistry()
        {
            return PrototypeGovernmentDefinitionFactory.AddMissingPrototypeGovernmentDefinitions(
                PrototypeDiplomacyDefinitionFactory.AddMissingPrototypeDiplomacyDefinitions(
                    PrototypeFactionDefinitionFactory.AddMissingPrototypeFactionDefinitions(
                        PrototypeOrganizationDecisionDefinitionFactory.AddMissingPrototypeOrganizationDecisionDefinitions(
                            PrototypeOrganizationResourceDefinitionFactory.AddMissingPrototypeOrganizationResourceDefinitions(
                                PrototypeOrganizationAuthorityDefinitionFactory.AddMissingPrototypeOrganizationAuthorityDefinitions(
                                    PrototypeOrganizationMembershipDefinitionFactory.AddMissingPrototypeOrganizationMembershipDefinitions(
                                        PrototypeOrganizationDefinitionFactory.AddMissingPrototypeOrganizationDefinitions(new DefinitionRegistry(Array.Empty<IGameDefinition>())))))))));
        }

        private static void CreatePoliticalGraph(RuntimeFixture fixture, PoliticalVisibility visibility)
        {
            Assert.That(fixture.Governments.CreatePolity(new PolityCreateRequest { transactionId = "tx.polity", polityId = "polity.test.kingdom", polityDefinitionId = PrototypeGovernmentDefinitionFactory.KingdomPolityDefinitionId, officialName = "Test Kingdom", worldTime = 1d, visibility = visibility }).Succeeded, Is.True);
            Assert.That(fixture.Governments.RegisterGovernment(new GovernmentRegisterRequest { transactionId = "tx.government", governmentId = "government.test.royal", governmentDefinitionId = PrototypeGovernmentDefinitionFactory.RoyalGovernmentDefinitionId, polityId = "polity.test.kingdom", officialName = "Test Royal Government", primaryGoverningOrganizationId = "organization.prototype.guild", governingOrganizationIds = new[] { "organization.prototype.guild" }, level = GovernmentLevel.Central, worldTime = 2d, visibility = visibility }).Succeeded, Is.True);
            Assert.That(fixture.Governments.CreateTerritory(new TerritoryCreateRequest { transactionId = "tx.territory", territoryId = "political-territory.test.realm", territoryDefinitionId = PrototypeGovernmentDefinitionFactory.RealmTerritoryDefinitionId, displayName = "Test Realm", polityId = "polity.test.kingdom", primaryGovernmentId = "government.test.royal", placeIds = new[] { "place.test.capital" }, worldTime = 3d, visibility = visibility }).Succeeded, Is.True);
        }

        private static JurisdictionCreateRequest Jurisdiction(string suffix, string definitionId, int priority, double worldTime, double expiration)
        {
            return new JurisdictionCreateRequest { transactionId = $"tx.jurisdiction.{suffix}", jurisdictionId = $"jurisdiction.test.{suffix}", jurisdictionDefinitionId = definitionId, governmentId = "government.test.royal", category = suffix == "general" ? JurisdictionCategory.GeneralGovernment : JurisdictionCategory.Municipal, scopeDimensions = JurisdictionScopeDimension.Territory | JurisdictionScopeDimension.SubjectMatter, subjectMatters = new[] { JurisdictionSubjectMatter.GeneralAdministration }, territoryIds = new[] { "political-territory.test.realm" }, priority = priority, conflictPolicy = JurisdictionConflictPolicy.HigherPriorityWins, worldTime = worldTime, expirationWorldTime = expiration };
        }

        private static JurisdictionResolutionRequest Resolution(double worldTime) => new JurisdictionResolutionRequest { requesterGovernmentId = "government.test.royal", territoryId = "political-territory.test.realm", subjectMatter = JurisdictionSubjectMatter.GeneralAdministration, worldTime = worldTime };

        private sealed class RuntimeFixture
        {
            public RuntimeFixture(DefinitionRegistry registry, OrganizationRuntime organizations, OrganizationMembershipRuntime memberships, OrganizationAuthorityRuntime authority, OrganizationResourceRuntime resources, OrganizationDecisionRuntime decisions, FactionRuntime factions, DiplomacyRuntime diplomacy, GovernmentRuntime governments)
            {
                Registry = registry; Organizations = organizations; Memberships = memberships; Authority = authority; Resources = resources; Decisions = decisions; Factions = factions; Diplomacy = diplomacy; Governments = governments;
            }

            public DefinitionRegistry Registry { get; }
            public OrganizationRuntime Organizations { get; }
            public OrganizationMembershipRuntime Memberships { get; }
            public OrganizationAuthorityRuntime Authority { get; }
            public OrganizationResourceRuntime Resources { get; }
            public OrganizationDecisionRuntime Decisions { get; }
            public FactionRuntime Factions { get; }
            public DiplomacyRuntime Diplomacy { get; }
            public GovernmentRuntime Governments { get; }
        }
    }
}
