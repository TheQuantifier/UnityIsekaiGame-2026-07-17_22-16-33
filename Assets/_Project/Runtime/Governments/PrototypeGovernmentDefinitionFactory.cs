using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Governments
{
    public static class PrototypeGovernmentDefinitionFactory
    {
        public const string KingdomPolityDefinitionId = "polity.prototype.hereditary-kingdom";
        public const string EmpirePolityDefinitionId = "polity.prototype.centralized-empire";
        public const string RepublicPolityDefinitionId = "polity.prototype.republic";
        public const string CityStatePolityDefinitionId = "polity.prototype.city-state";
        public const string ConfederationPolityDefinitionId = "polity.prototype.confederation";
        public const string TribalPolityDefinitionId = "polity.prototype.tribal-polity";
        public const string ReligiousPolityDefinitionId = "polity.prototype.religious-polity";
        public const string AutonomousPolityDefinitionId = "polity.prototype.autonomous-region";
        public const string DisputedPolityDefinitionId = "polity.prototype.disputed-polity";
        public const string ExiledCommunityPolityDefinitionId = "polity.prototype.exiled-community";

        public const string RoyalGovernmentDefinitionId = "government.prototype.central-royal";
        public const string RepublicanCouncilDefinitionId = "government.prototype.republican-council";
        public const string MunicipalCouncilDefinitionId = "government.prototype.municipal-council";
        public const string ProvincialAdministrationDefinitionId = "government.prototype.provincial-administration";
        public const string TribalCouncilDefinitionId = "government.prototype.tribal-council";
        public const string ReligiousGovernmentDefinitionId = "government.prototype.religious-government";
        public const string ProvisionalGovernmentDefinitionId = "government.prototype.provisional-government";
        public const string OccupationAdministrationDefinitionId = "government.prototype.occupation-administration";
        public const string ExileGovernmentDefinitionId = "government.prototype.government-in-exile";
        public const string ClaimantGovernmentDefinitionId = "government.prototype.claimant-government";

        public const string RealmTerritoryDefinitionId = "political-territory.prototype.realm";
        public const string ProvinceTerritoryDefinitionId = "political-territory.prototype.province";
        public const string MunicipalityTerritoryDefinitionId = "political-territory.prototype.municipality";
        public const string VillageTerritoryDefinitionId = "political-territory.prototype.village";
        public const string OccupiedTerritoryDefinitionId = "political-territory.prototype.occupied-area";
        public const string AutonomousTerritoryDefinitionId = "political-territory.prototype.autonomous-region";
        public const string ReligiousTerritoryDefinitionId = "political-territory.prototype.religious-jurisdiction";
        public const string MilitaryDistrictTerritoryDefinitionId = "political-territory.prototype.military-district";

        public const string SovereigntyClaimDefinitionId = "territorial-claim.prototype.sovereignty";
        public const string AdministrativeClaimDefinitionId = "territorial-claim.prototype.administration";
        public const string TreatyClaimDefinitionId = "territorial-claim.prototype.treaty-basis";
        public const string OccupationClaimDefinitionId = "territorial-claim.prototype.occupation";
        public const string AutonomyClaimDefinitionId = "territorial-claim.prototype.autonomy";

        public const string GeneralJurisdictionDefinitionId = "jurisdiction.prototype.general-government";
        public const string MunicipalJurisdictionDefinitionId = "jurisdiction.prototype.municipal";
        public const string MilitaryJurisdictionDefinitionId = "jurisdiction.prototype.military";
        public const string ReligiousJurisdictionDefinitionId = "jurisdiction.prototype.religious-internal";
        public const string CommercialJurisdictionDefinitionId = "jurisdiction.prototype.commercial";
        public const string PropertyJurisdictionDefinitionId = "jurisdiction.prototype.property";
        public const string EmergencyJurisdictionDefinitionId = "jurisdiction.prototype.emergency";

        public static DefinitionRegistry AddMissingPrototypeGovernmentDefinitions(DefinitionRegistry baseRegistry)
        {
            HashSet<string> ids = new HashSet<string>(baseRegistry?.DefinitionsById.Keys ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            if (baseRegistry != null) definitions.AddRange(baseRegistry.DefinitionsById.Values.Where(definition => definition != null));
            definitions.AddRange(CreateMissingPolityDefinitions(ids));
            definitions.AddRange(CreateMissingGovernmentDefinitions(ids));
            definitions.AddRange(CreateMissingTerritoryDefinitions(ids));
            definitions.AddRange(CreateMissingClaimDefinitions(ids));
            definitions.AddRange(CreateMissingJurisdictionDefinitions(ids));
            return new DefinitionRegistry(definitions);
        }

        public static IReadOnlyList<PolityDefinition> CreateMissingPolityDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = Set(existingIds);
            List<PolityDefinition> definitions = new List<PolityDefinition>();
            AddPolity(definitions, ids, KingdomPolityDefinitionId, "Prototype Hereditary Kingdom", PolityCategory.Kingdom, GovernmentCategory.MonarchicalGovernment);
            AddPolity(definitions, ids, EmpirePolityDefinitionId, "Prototype Centralized Empire", PolityCategory.Empire, GovernmentCategory.ImperialGovernment);
            AddPolity(definitions, ids, RepublicPolityDefinitionId, "Prototype Republic", PolityCategory.Republic, GovernmentCategory.RepublicanGovernment);
            AddPolity(definitions, ids, CityStatePolityDefinitionId, "Prototype City-State", PolityCategory.CityState, GovernmentCategory.MunicipalGovernment);
            AddPolity(definitions, ids, ConfederationPolityDefinitionId, "Prototype Confederation", PolityCategory.Confederation, GovernmentCategory.CouncilGovernment);
            AddPolity(definitions, ids, TribalPolityDefinitionId, "Prototype Tribal Polity", PolityCategory.TribalPolity, GovernmentCategory.TribalCouncil);
            AddPolity(definitions, ids, ReligiousPolityDefinitionId, "Prototype Religious Polity", PolityCategory.ReligiousPolity, GovernmentCategory.ReligiousGovernment, nonTerritorial: true);
            AddPolity(definitions, ids, AutonomousPolityDefinitionId, "Prototype Autonomous Region", PolityCategory.AutonomousPolity, GovernmentCategory.RegionalGovernment);
            AddPolity(definitions, ids, DisputedPolityDefinitionId, "Prototype Disputed Polity", PolityCategory.DisputedPolity, GovernmentCategory.ClaimantGovernment);
            AddPolity(definitions, ids, ExiledCommunityPolityDefinitionId, "Prototype Exiled Political Community", PolityCategory.StatelessPoliticalCommunity, GovernmentCategory.GovernmentInExile, territorial: false, nonTerritorial: true);
            return definitions;
        }

        public static IReadOnlyList<GovernmentDefinition> CreateMissingGovernmentDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = Set(existingIds);
            List<GovernmentDefinition> definitions = new List<GovernmentDefinition>();
            AddGovernment(definitions, ids, RoyalGovernmentDefinitionId, "Prototype Central Royal Government", GovernmentCategory.MonarchicalGovernment, GovernmentLevel.Central, PolityCategory.Kingdom);
            AddGovernment(definitions, ids, RepublicanCouncilDefinitionId, "Prototype Republican Council", GovernmentCategory.RepublicanGovernment, GovernmentLevel.Central, PolityCategory.Republic);
            AddGovernment(definitions, ids, MunicipalCouncilDefinitionId, "Prototype Municipal Council", GovernmentCategory.MunicipalGovernment, GovernmentLevel.Municipal, PolityCategory.CityState);
            AddGovernment(definitions, ids, ProvincialAdministrationDefinitionId, "Prototype Provincial Administration", GovernmentCategory.ProvincialGovernment, GovernmentLevel.Provincial, PolityCategory.Kingdom);
            AddGovernment(definitions, ids, TribalCouncilDefinitionId, "Prototype Tribal Council", GovernmentCategory.TribalCouncil, GovernmentLevel.Central, PolityCategory.TribalPolity);
            AddGovernment(definitions, ids, ReligiousGovernmentDefinitionId, "Prototype Religious Government", GovernmentCategory.ReligiousGovernment, GovernmentLevel.NonTerritorial, PolityCategory.ReligiousPolity, territorialRequired: false);
            AddGovernment(definitions, ids, ProvisionalGovernmentDefinitionId, "Prototype Provisional Government", GovernmentCategory.ProvisionalGovernment, GovernmentLevel.Central, PolityCategory.DisputedPolity, provisional: true);
            AddGovernment(definitions, ids, OccupationAdministrationDefinitionId, "Prototype Occupation Administration", GovernmentCategory.OccupationAdministration, GovernmentLevel.Regional, PolityCategory.Kingdom, occupation: true);
            AddGovernment(definitions, ids, ExileGovernmentDefinitionId, "Prototype Government in Exile", GovernmentCategory.GovernmentInExile, GovernmentLevel.NonTerritorial, PolityCategory.StatelessPoliticalCommunity, territorialRequired: false, exile: true);
            AddGovernment(definitions, ids, ClaimantGovernmentDefinitionId, "Prototype Claimant Government", GovernmentCategory.ClaimantGovernment, GovernmentLevel.Central, PolityCategory.DisputedPolity);
            return definitions;
        }

        public static IReadOnlyList<PoliticalTerritoryDefinition> CreateMissingTerritoryDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = Set(existingIds);
            List<PoliticalTerritoryDefinition> definitions = new List<PoliticalTerritoryDefinition>();
            AddTerritory(definitions, ids, RealmTerritoryDefinitionId, "Prototype Realm Territory", PoliticalTerritoryCategory.Realm);
            AddTerritory(definitions, ids, ProvinceTerritoryDefinitionId, "Prototype Province Territory", PoliticalTerritoryCategory.Province);
            AddTerritory(definitions, ids, MunicipalityTerritoryDefinitionId, "Prototype Municipality Territory", PoliticalTerritoryCategory.City);
            AddTerritory(definitions, ids, VillageTerritoryDefinitionId, "Prototype Village Territory", PoliticalTerritoryCategory.Village);
            AddTerritory(definitions, ids, OccupiedTerritoryDefinitionId, "Prototype Occupied Area", PoliticalTerritoryCategory.OccupiedArea);
            AddTerritory(definitions, ids, AutonomousTerritoryDefinitionId, "Prototype Autonomous Region", PoliticalTerritoryCategory.AutonomousRegion);
            AddTerritory(definitions, ids, ReligiousTerritoryDefinitionId, "Prototype Religious Jurisdiction Territory", PoliticalTerritoryCategory.ReligiousJurisdiction, nonTerritorial: true);
            AddTerritory(definitions, ids, MilitaryDistrictTerritoryDefinitionId, "Prototype Military District", PoliticalTerritoryCategory.MilitaryDistrict);
            return definitions;
        }

        public static IReadOnlyList<TerritorialClaimDefinition> CreateMissingClaimDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = Set(existingIds);
            List<TerritorialClaimDefinition> definitions = new List<TerritorialClaimDefinition>();
            AddClaim(definitions, ids, SovereigntyClaimDefinitionId, "Prototype Sovereignty Claim", TerritorialClaimCategory.Sovereignty, governmentRequired: true);
            AddClaim(definitions, ids, AdministrativeClaimDefinitionId, "Prototype Administrative Claim", TerritorialClaimCategory.Administration, governmentRequired: true);
            AddClaim(definitions, ids, TreatyClaimDefinitionId, "Prototype Treaty-Based Claim", TerritorialClaimCategory.TreatyBased, governmentRequired: true);
            AddClaim(definitions, ids, OccupationClaimDefinitionId, "Prototype Occupation Claim", TerritorialClaimCategory.Occupation, governmentRequired: true);
            AddClaim(definitions, ids, AutonomyClaimDefinitionId, "Prototype Autonomy Claim", TerritorialClaimCategory.Autonomy, governmentRequired: true);
            return definitions;
        }

        public static IReadOnlyList<JurisdictionDefinition> CreateMissingJurisdictionDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = Set(existingIds);
            List<JurisdictionDefinition> definitions = new List<JurisdictionDefinition>();
            AddJurisdiction(definitions, ids, GeneralJurisdictionDefinitionId, "Prototype General Government Jurisdiction", JurisdictionCategory.GeneralGovernment, JurisdictionScopeDimension.Territory | JurisdictionScopeDimension.Place | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.GeneralAdministration, JurisdictionSubjectMatter.PublicOrderPlaceholder, JurisdictionSubjectMatter.BorderAdministrationPlaceholder });
            AddJurisdiction(definitions, ids, MunicipalJurisdictionDefinitionId, "Prototype Municipal Jurisdiction", JurisdictionCategory.Municipal, JurisdictionScopeDimension.Territory | JurisdictionScopeDimension.Place | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.MunicipalServices, JurisdictionSubjectMatter.GeneralAdministration });
            AddJurisdiction(definitions, ids, MilitaryJurisdictionDefinitionId, "Prototype Military Jurisdiction", JurisdictionCategory.Military, JurisdictionScopeDimension.Person | JurisdictionScopeDimension.Organization | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.MilitaryDiscipline }, exclusive: true);
            AddJurisdiction(definitions, ids, ReligiousJurisdictionDefinitionId, "Prototype Religious Internal Jurisdiction", JurisdictionCategory.Religious, JurisdictionScopeDimension.Person | JurisdictionScopeDimension.Organization | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.ReligiousInternalAffairs });
            AddJurisdiction(definitions, ids, CommercialJurisdictionDefinitionId, "Prototype Commercial Jurisdiction", JurisdictionCategory.Commercial, JurisdictionScopeDimension.Organization | JurisdictionScopeDimension.Property | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.TradeRegulation });
            AddJurisdiction(definitions, ids, PropertyJurisdictionDefinitionId, "Prototype Property Jurisdiction", JurisdictionCategory.Property, JurisdictionScopeDimension.Property | JurisdictionScopeDimension.Place | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.PropertyAdministration });
            AddJurisdiction(definitions, ids, EmergencyJurisdictionDefinitionId, "Prototype Emergency Jurisdiction", JurisdictionCategory.Emergency, JurisdictionScopeDimension.Territory | JurisdictionScopeDimension.SubjectMatter, new[] { JurisdictionSubjectMatter.EmergencyAdministration }, conflictPolicy: JurisdictionConflictPolicy.HigherPriorityWins);
            return definitions;
        }

        private static void AddPolity(ICollection<PolityDefinition> definitions, ISet<string> ids, string id, string name, PolityCategory category, GovernmentCategory governmentCategory, bool territorial = true, bool nonTerritorial = false)
        {
            if (ids.Contains(id)) return;
            PolityDefinition definition = ScriptableObject.CreateInstance<PolityDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, new[] { governmentCategory }, territorial, nonTerritorial, tagIds: Tags("polity"));
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddGovernment(ICollection<GovernmentDefinition> definitions, ISet<string> ids, string id, string name, GovernmentCategory category, GovernmentLevel level, PolityCategory polityCategory, bool territorialRequired = true, bool exile = false, bool provisional = true, bool occupation = false)
        {
            if (ids.Contains(id)) return;
            GovernmentDefinition definition = ScriptableObject.CreateInstance<GovernmentDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, level, new[] { polityCategory }, territorialRequired: territorialRequired, exile: exile, provisional: provisional, occupation: occupation, tagIds: Tags("government"));
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddTerritory(ICollection<PoliticalTerritoryDefinition> definitions, ISet<string> ids, string id, string name, PoliticalTerritoryCategory category, bool nonTerritorial = false)
        {
            if (ids.Contains(id)) return;
            PoliticalTerritoryDefinition definition = ScriptableObject.CreateInstance<PoliticalTerritoryDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, nonTerritorial: nonTerritorial, tagIds: Tags("territory"));
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddClaim(ICollection<TerritorialClaimDefinition> definitions, ISet<string> ids, string id, string name, TerritorialClaimCategory category, bool governmentRequired)
        {
            if (ids.Contains(id)) return;
            TerritorialClaimDefinition definition = ScriptableObject.CreateInstance<TerritorialClaimDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, governmentRequired: governmentRequired, tagIds: Tags("claim"));
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddJurisdiction(ICollection<JurisdictionDefinition> definitions, ISet<string> ids, string id, string name, JurisdictionCategory category, JurisdictionScopeDimension dimensions, IEnumerable<JurisdictionSubjectMatter> subjectMatters, JurisdictionConflictPolicy conflictPolicy = JurisdictionConflictPolicy.SpecificOverridesGeneral, bool exclusive = false)
        {
            if (ids.Contains(id)) return;
            JurisdictionDefinition definition = ScriptableObject.CreateInstance<JurisdictionDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, dimensions, subjectMatters, conflictPolicy, exclusive: exclusive, tagIds: Tags("jurisdiction"));
            definitions.Add(definition);
            ids.Add(id);
        }

        private static HashSet<string> Set(IEnumerable<string> ids) => new HashSet<string>((ids ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
        private static string[] Tags(string domain) => new[] { "prototype", "government", domain };
    }
}
