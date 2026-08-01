using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Organizations
{
    public static class PrototypeOrganizationMembershipDefinitionFactory
    {
        public const string GuildFullMemberId = "organization-membership.prototype.guild.full-member";
        public const string GuildApplicantId = "organization-membership.prototype.guild.applicant";
        public const string GuildInviteeId = "organization-membership.prototype.guild.invitee";
        public const string GuildAssociateId = "organization-membership.prototype.guild.associate";
        public const string BranchMemberId = "organization-membership.prototype.branch.member";
        public const string ForgeEmployeeMemberId = "organization-membership.prototype.forge.employee-member";
        public const string TempleClergyMemberId = "organization-membership.prototype.temple.clergy-member";
        public const string MilitaryMemberId = "organization-membership.prototype.military.member";
        public const string SecretMemberId = "organization-membership.prototype.secret.member";

        public const string GuildCraftTrackId = "organization-rank-track.prototype.guild.craft";
        public const string GuildNoviceRankId = "organization-rank.prototype.guild.novice";
        public const string GuildJourneymanRankId = "organization-rank.prototype.guild.journeyman";
        public const string GuildMasterRankId = "organization-rank.prototype.guild.master";
        public const string MilitaryTrackId = "organization-rank-track.prototype.military.command";
        public const string MilitaryRecruitRankId = "organization-rank.prototype.military.recruit";
        public const string MilitarySergeantRankId = "organization-rank.prototype.military.sergeant";
        public const string MilitaryCaptainRankId = "organization-rank.prototype.military.captain";
        public const string TempleTrackId = "organization-rank-track.prototype.temple.clergy";
        public const string TempleAcolyteRankId = "organization-rank.prototype.temple.acolyte";
        public const string TemplePriestRankId = "organization-rank.prototype.temple.priest";

        public const string GuildmasterOfficeId = "organization-office.prototype.guild.guildmaster";
        public const string GuildTreasurerOfficeId = "organization-office.prototype.guild.treasurer";
        public const string BranchChapterMasterOfficeId = "organization-office.prototype.branch.chapter-master";
        public const string GuardCaptainOfficeId = "organization-office.prototype.military.guard-captain";
        public const string ChiefPriestOfficeId = "organization-office.prototype.temple.chief-priest";

        public static DefinitionRegistry AddMissingPrototypeOrganizationMembershipDefinitions(DefinitionRegistry baseRegistry)
        {
            HashSet<string> ids = new HashSet<string>(baseRegistry?.DefinitionsById.Keys ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            if (baseRegistry != null)
            {
                definitions.AddRange(baseRegistry.DefinitionsById.Values.Where(definition => definition != null));
            }

            definitions.AddRange(CreateMissingMembershipDefinitions(ids));
            definitions.AddRange(CreateMissingRankTrackDefinitions(ids));
            definitions.AddRange(CreateMissingRankDefinitions(ids));
            definitions.AddRange(CreateMissingOfficeDefinitions(ids));
            return new DefinitionRegistry(definitions);
        }

        public static IReadOnlyList<OrganizationMembershipDefinition> CreateMissingMembershipDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationMembershipDefinition> definitions = new List<OrganizationMembershipDefinition>();
            AddMembership(definitions, ids, GuildFullMemberId, "Guild Full Member", OrganizationMembershipCategory.FullMember, new[] { PrototypeOrganizationDefinitionFactory.GuildDefinitionId }, null, OrganizationMembershipStatus.Active);
            AddMembership(definitions, ids, GuildApplicantId, "Guild Applicant", OrganizationMembershipCategory.Applicant, new[] { PrototypeOrganizationDefinitionFactory.GuildDefinitionId }, null, OrganizationMembershipStatus.Applied, ranks: false, offices: false, invitation: false);
            AddMembership(definitions, ids, GuildInviteeId, "Guild Invitee", OrganizationMembershipCategory.Invitee, new[] { PrototypeOrganizationDefinitionFactory.GuildDefinitionId }, null, OrganizationMembershipStatus.Invited, ranks: false, offices: false, application: false);
            AddMembership(definitions, ids, GuildAssociateId, "Guild Associate", OrganizationMembershipCategory.AssociateMember, new[] { PrototypeOrganizationDefinitionFactory.GuildDefinitionId }, null, OrganizationMembershipStatus.Provisional);
            AddMembership(definitions, ids, BranchMemberId, "Branch Member", OrganizationMembershipCategory.FullMember, new[] { PrototypeOrganizationDefinitionFactory.BranchDefinitionId }, null, OrganizationMembershipStatus.Active, requireParent: true);
            AddMembership(definitions, ids, ForgeEmployeeMemberId, "Forge Employee Member", OrganizationMembershipCategory.EmployeeMember, new[] { PrototypeOrganizationDefinitionFactory.CompanyDefinitionId }, null, OrganizationMembershipStatus.Active);
            AddMembership(definitions, ids, TempleClergyMemberId, "Temple Clergy Member", OrganizationMembershipCategory.ClergyMember, new[] { PrototypeOrganizationDefinitionFactory.ReligiousOrderDefinitionId }, null, OrganizationMembershipStatus.Active);
            AddMembership(definitions, ids, MilitaryMemberId, "Military Member", OrganizationMembershipCategory.MilitaryMember, new[] { PrototypeOrganizationDefinitionFactory.MilitaryOrderDefinitionId }, null, OrganizationMembershipStatus.Active);
            AddMembership(definitions, ids, SecretMemberId, "Secret Member", OrganizationMembershipCategory.SecretMember, new[] { PrototypeOrganizationDefinitionFactory.SecretSocietyDefinitionId }, null, OrganizationMembershipStatus.Active, visibility: OrganizationVisibility.Hidden);
            return definitions;
        }

        public static IReadOnlyList<OrganizationRankTrackDefinition> CreateMissingRankTrackDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationRankTrackDefinition> definitions = new List<OrganizationRankTrackDefinition>();
            AddTrack(definitions, ids, GuildCraftTrackId, "Guild Craft Track", PrototypeOrganizationDefinitionFactory.GuildDefinitionId, new[] { GuildFullMemberId, GuildAssociateId });
            AddTrack(definitions, ids, MilitaryTrackId, "Military Command Track", PrototypeOrganizationDefinitionFactory.MilitaryOrderDefinitionId, new[] { MilitaryMemberId });
            AddTrack(definitions, ids, TempleTrackId, "Temple Clergy Track", PrototypeOrganizationDefinitionFactory.ReligiousOrderDefinitionId, new[] { TempleClergyMemberId });
            return definitions;
        }

        public static IReadOnlyList<OrganizationRankDefinition> CreateMissingRankDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationRankDefinition> definitions = new List<OrganizationRankDefinition>();
            AddRank(definitions, ids, GuildNoviceRankId, "Guild Novice", GuildCraftTrackId, 10);
            AddRank(definitions, ids, GuildJourneymanRankId, "Guild Journeyman", GuildCraftTrackId, 20, new[] { GuildNoviceRankId });
            AddRank(definitions, ids, GuildMasterRankId, "Guild Master", GuildCraftTrackId, 30, new[] { GuildJourneymanRankId }, terminal: true);
            AddRank(definitions, ids, MilitaryRecruitRankId, "Recruit", MilitaryTrackId, 10);
            AddRank(definitions, ids, MilitarySergeantRankId, "Sergeant", MilitaryTrackId, 20, new[] { MilitaryRecruitRankId });
            AddRank(definitions, ids, MilitaryCaptainRankId, "Captain", MilitaryTrackId, 30, new[] { MilitarySergeantRankId }, terminal: true);
            AddRank(definitions, ids, TempleAcolyteRankId, "Acolyte", TempleTrackId, 10);
            AddRank(definitions, ids, TemplePriestRankId, "Priest", TempleTrackId, 20, new[] { TempleAcolyteRankId });
            return definitions;
        }

        public static IReadOnlyList<OrganizationOfficeDefinition> CreateMissingOfficeDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationOfficeDefinition> definitions = new List<OrganizationOfficeDefinition>();
            AddOffice(definitions, ids, GuildmasterOfficeId, "Guildmaster", PrototypeOrganizationDefinitionFactory.GuildDefinitionId, new[] { GuildFullMemberId }, new[] { GuildMasterRankId }, 1);
            AddOffice(definitions, ids, GuildTreasurerOfficeId, "Guild Treasurer", PrototypeOrganizationDefinitionFactory.GuildDefinitionId, new[] { GuildFullMemberId, GuildAssociateId }, Array.Empty<string>(), 2, joint: true);
            AddOffice(definitions, ids, BranchChapterMasterOfficeId, "Chapter Master", PrototypeOrganizationDefinitionFactory.BranchDefinitionId, new[] { BranchMemberId }, Array.Empty<string>(), 1);
            AddOffice(definitions, ids, GuardCaptainOfficeId, "Guard Captain", PrototypeOrganizationDefinitionFactory.MilitaryOrderDefinitionId, new[] { MilitaryMemberId }, new[] { MilitaryCaptainRankId }, 1);
            AddOffice(definitions, ids, ChiefPriestOfficeId, "Chief Priest", PrototypeOrganizationDefinitionFactory.ReligiousOrderDefinitionId, new[] { TempleClergyMemberId }, new[] { TemplePriestRankId }, 1);
            return definitions;
        }

        private static void AddMembership(ICollection<OrganizationMembershipDefinition> definitions, ISet<string> ids, string id, string name, OrganizationMembershipCategory category, IEnumerable<string> orgDefinitions, IEnumerable<OrganizationCategory> orgCategories, OrganizationMembershipStatus initial, bool ranks = true, bool offices = true, bool application = true, bool invitation = true, bool requireParent = false, OrganizationVisibility visibility = OrganizationVisibility.Public)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationMembershipDefinition definition = ScriptableObject.CreateInstance<OrganizationMembershipDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, orgDefinitions, orgCategories, initial, rankSupport: ranks, officeSupport: offices, application: application, invitation: invitation, requireParent: requireParent, membershipVisibility: visibility, tagIds: new[] { "prototype", "organization" });
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddTrack(ICollection<OrganizationRankTrackDefinition> definitions, ISet<string> ids, string id, string name, string organizationDefinitionId, IEnumerable<string> memberships)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationRankTrackDefinition definition = ScriptableObject.CreateInstance<OrganizationRankTrackDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, organizationDefinitionId, memberships);
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddRank(ICollection<OrganizationRankDefinition> definitions, ISet<string> ids, string id, string name, string trackId, int order, IEnumerable<string> prior = null, bool terminal = false)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationRankDefinition definition = ScriptableObject.CreateInstance<OrganizationRankDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, trackId, order, prior, terminal: terminal);
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddOffice(ICollection<OrganizationOfficeDefinition> definitions, ISet<string> ids, string id, string name, string organizationDefinitionId, IEnumerable<string> memberships, IEnumerable<string> ranks, int capacity, bool joint = false)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationOfficeDefinition definition = ScriptableObject.CreateInstance<OrganizationOfficeDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, organizationDefinitionId, memberships: memberships, ranks: ranks, maximumHolders: capacity, joint: joint);
            definitions.Add(definition);
            ids.Add(id);
        }

        private static HashSet<string> Set(IEnumerable<string> ids)
        {
            return ids == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(ids.Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
        }
    }
}
