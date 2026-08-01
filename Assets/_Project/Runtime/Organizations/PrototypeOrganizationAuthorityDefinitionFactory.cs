using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Organizations
{
    public static class PrototypeOrganizationAuthorityDefinitionFactory
    {
        public const string ViewPublicInformationPermissionId = "organization-permission.prototype.view-public-information";
        public const string ViewRestrictedInformationPermissionId = "organization-permission.prototype.view-restricted-information";
        public const string ViewSecretInformationPermissionId = "organization-permission.prototype.view-secret-information";
        public const string ReviewMembershipApplicationsPermissionId = "organization-permission.prototype.review-membership-applications";
        public const string InviteMembersPermissionId = "organization-permission.prototype.invite-members";
        public const string AdmitMembersPermissionId = "organization-permission.prototype.admit-members";
        public const string SuspendMembersPermissionId = "organization-permission.prototype.suspend-members";
        public const string ReinstateMembersPermissionId = "organization-permission.prototype.reinstate-members";
        public const string RemoveMembersPermissionId = "organization-permission.prototype.remove-members";
        public const string AssignRanksPermissionId = "organization-permission.prototype.assign-ranks";
        public const string PromoteMembersPermissionId = "organization-permission.prototype.promote-members";
        public const string DemoteMembersPermissionId = "organization-permission.prototype.demote-members";
        public const string CreateOfficesPermissionId = "organization-permission.prototype.create-offices";
        public const string AppointOfficeholdersPermissionId = "organization-permission.prototype.appoint-officeholders";
        public const string RemoveOfficeholdersPermissionId = "organization-permission.prototype.remove-officeholders";
        public const string AssignActingOfficeholdersPermissionId = "organization-permission.prototype.assign-acting-officeholders";
        public const string IssueOrdersPermissionId = "organization-permission.prototype.issue-orders";
        public const string RepresentExternallyPermissionId = "organization-permission.prototype.represent-externally";
        public const string ManageAliasesPermissionId = "organization-permission.prototype.manage-aliases";
        public const string ProposeHeadquartersPermissionId = "organization-permission.prototype.propose-headquarters-change";
        public const string AuthorizeHeadquartersPermissionId = "organization-permission.prototype.authorize-headquarters-change";
        public const string DelegatePermissionsPermissionId = "organization-permission.prototype.delegate-permissions";
        public const string RevokeDelegationsPermissionId = "organization-permission.prototype.revoke-delegations";
        public const string ManageResourcesPlaceholderPermissionId = "organization-permission.prototype.manage-resources-placeholder";
        public const string GovernanceVotePlaceholderPermissionId = "organization-permission.prototype.governance-vote-placeholder";
        public const string LegalAuthorityPlaceholderPermissionId = "organization-permission.prototype.legal-authority-placeholder";

        public const string ReviewMembershipApplicationActionId = "organization-action.prototype.review-membership-application";
        public const string InviteMemberActionId = "organization-action.prototype.invite-member";
        public const string AdmitMemberActionId = "organization-action.prototype.admit-member";
        public const string SuspendMemberActionId = "organization-action.prototype.suspend-member";
        public const string ReinstateMemberActionId = "organization-action.prototype.reinstate-member";
        public const string RemoveMemberActionId = "organization-action.prototype.remove-member";
        public const string AssignRankActionId = "organization-action.prototype.assign-rank";
        public const string PromoteMemberActionId = "organization-action.prototype.promote-member";
        public const string DemoteMemberActionId = "organization-action.prototype.demote-member";
        public const string CreateOfficeActionId = "organization-action.prototype.create-office";
        public const string AppointOfficeholderActionId = "organization-action.prototype.appoint-officeholder";
        public const string RemoveOfficeholderActionId = "organization-action.prototype.remove-officeholder";
        public const string AssignActingOfficeholderActionId = "organization-action.prototype.assign-acting-officeholder";
        public const string RenameOrganizationActionId = "organization-action.prototype.rename-organization";
        public const string ChangeHeadquartersActionId = "organization-action.prototype.change-headquarters";
        public const string CreateBranchActionId = "organization-action.prototype.create-branch";
        public const string EndBranchLinkActionId = "organization-action.prototype.end-branch-link";
        public const string IssueOrderActionId = "organization-action.prototype.issue-order";
        public const string AccessRestrictedRecordActionId = "organization-action.prototype.access-restricted-record";
        public const string AccessSecretRecordActionId = "organization-action.prototype.access-secret-record";
        public const string DelegatePermissionActionId = "organization-action.prototype.delegate-permission";
        public const string RevokeDelegationActionId = "organization-action.prototype.revoke-delegation";

        public const string GeneralMemberRoleId = "organization-authority-role.prototype.general-member";
        public const string MembershipOfficerRoleId = "organization-authority-role.prototype.membership-officer";
        public const string RankOfficerRoleId = "organization-authority-role.prototype.rank-officer";
        public const string TreasurerRoleId = "organization-authority-role.prototype.treasurer";
        public const string ChapterMasterRoleId = "organization-authority-role.prototype.chapter-master";
        public const string GuildmasterRoleId = "organization-authority-role.prototype.guildmaster";
        public const string RecordClerkRoleId = "organization-authority-role.prototype.record-clerk";
        public const string ActingExecutiveRoleId = "organization-authority-role.prototype.acting-executive";
        public const string SecretMemberRoleId = "organization-authority-role.prototype.secret-member";
        public const string MilitaryOfficerRoleId = "organization-authority-role.prototype.military-officer";

        public const string GuildFullMemberBindingId = "organization-authority-binding.prototype.guild.full-member";
        public const string GuildMasterRankBindingId = "organization-authority-binding.prototype.guild.master-rank";
        public const string GuildmasterOfficeBindingId = "organization-authority-binding.prototype.guild.guildmaster-office";
        public const string GuildTreasurerOfficeBindingId = "organization-authority-binding.prototype.guild.treasurer-office";
        public const string BranchChapterMasterOfficeBindingId = "organization-authority-binding.prototype.branch.chapter-master-office";
        public const string GuardCaptainOfficeBindingId = "organization-authority-binding.prototype.military.guard-captain-office";
        public const string SecretMemberBindingId = "organization-authority-binding.prototype.secret.member";

        public static DefinitionRegistry AddMissingPrototypeOrganizationAuthorityDefinitions(DefinitionRegistry baseRegistry)
        {
            HashSet<string> ids = new HashSet<string>(baseRegistry?.DefinitionsById.Keys ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            if (baseRegistry != null)
            {
                definitions.AddRange(baseRegistry.DefinitionsById.Values.Where(definition => definition != null));
            }

            definitions.AddRange(CreateMissingPermissionDefinitions(ids));
            definitions.AddRange(CreateMissingActionDefinitions(ids));
            definitions.AddRange(CreateMissingRoleDefinitions(ids));
            definitions.AddRange(CreateMissingBindingDefinitions(ids));
            return new DefinitionRegistry(definitions);
        }

        public static IReadOnlyList<OrganizationPermissionDefinition> CreateMissingPermissionDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationPermissionDefinition> definitions = new List<OrganizationPermissionDefinition>();
            OrganizationAuthorityScopeType[] generalScopes = { OrganizationAuthorityScopeType.EntireOrganization, OrganizationAuthorityScopeType.OrganizationBranch, OrganizationAuthorityScopeType.SpecificOrganizationSubtree };
            AddPermission(definitions, ids, ViewPublicInformationPermissionId, "View Public Organization Information", OrganizationPermissionCategory.ViewInformation, generalScopes);
            AddPermission(definitions, ids, ViewRestrictedInformationPermissionId, "View Restricted Organization Information", OrganizationPermissionCategory.ViewInformation, generalScopes);
            AddPermission(definitions, ids, ViewSecretInformationPermissionId, "View Secret Organization Information", OrganizationPermissionCategory.ViewInformation, generalScopes, visibility: OrganizationVisibility.Restricted);
            AddPermission(definitions, ids, ReviewMembershipApplicationsPermissionId, "Review Membership Applications", OrganizationPermissionCategory.ManageMembership, generalScopes);
            AddPermission(definitions, ids, InviteMembersPermissionId, "Invite Organization Members", OrganizationPermissionCategory.ManageMembership, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, AdmitMembersPermissionId, "Admit Organization Members", OrganizationPermissionCategory.ManageMembership, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, SuspendMembersPermissionId, "Suspend Organization Members", OrganizationPermissionCategory.ManageMembership, generalScopes);
            AddPermission(definitions, ids, ReinstateMembersPermissionId, "Reinstate Organization Members", OrganizationPermissionCategory.ManageMembership, generalScopes);
            AddPermission(definitions, ids, RemoveMembersPermissionId, "Remove Organization Members", OrganizationPermissionCategory.ManageMembership, generalScopes);
            AddPermission(definitions, ids, AssignRanksPermissionId, "Assign Organization Ranks", OrganizationPermissionCategory.ManageRanks, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, PromoteMembersPermissionId, "Promote Organization Members", OrganizationPermissionCategory.ManageRanks, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, DemoteMembersPermissionId, "Demote Organization Members", OrganizationPermissionCategory.ManageRanks, generalScopes);
            AddPermission(definitions, ids, CreateOfficesPermissionId, "Create Organization Offices", OrganizationPermissionCategory.ManageOffices, generalScopes);
            AddPermission(definitions, ids, AppointOfficeholdersPermissionId, "Appoint Officeholders", OrganizationPermissionCategory.ManageOffices, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, RemoveOfficeholdersPermissionId, "Remove Officeholders", OrganizationPermissionCategory.ManageOffices, generalScopes);
            AddPermission(definitions, ids, AssignActingOfficeholdersPermissionId, "Assign Acting Officeholders", OrganizationPermissionCategory.ManageOffices, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, IssueOrdersPermissionId, "Issue Institutional Orders", OrganizationPermissionCategory.IssueInstitutionalOrders, generalScopes, canDelegate: true, canRedelegate: true);
            AddPermission(definitions, ids, RepresentExternallyPermissionId, "Represent Organization Externally", OrganizationPermissionCategory.RepresentOrganization, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, ManageAliasesPermissionId, "Manage Organization Aliases", OrganizationPermissionCategory.ManageInformation, generalScopes);
            AddPermission(definitions, ids, ProposeHeadquartersPermissionId, "Propose Headquarters Change", OrganizationPermissionCategory.ManagePropertyAssociation, generalScopes);
            AddPermission(definitions, ids, AuthorizeHeadquartersPermissionId, "Authorize Headquarters Change", OrganizationPermissionCategory.ManagePropertyAssociation, generalScopes, jointAllowed: true);
            AddPermission(definitions, ids, DelegatePermissionsPermissionId, "Delegate Institutional Permissions", OrganizationPermissionCategory.ManageAccess, generalScopes, canDelegate: true);
            AddPermission(definitions, ids, RevokeDelegationsPermissionId, "Revoke Delegated Permissions", OrganizationPermissionCategory.ManageAccess, generalScopes);
            AddPermission(definitions, ids, ManageResourcesPlaceholderPermissionId, "Manage Resources Placeholder", OrganizationPermissionCategory.ManageResourcesPlaceholder, generalScopes);
            AddPermission(definitions, ids, GovernanceVotePlaceholderPermissionId, "Governance Vote Placeholder", OrganizationPermissionCategory.ParticipateInGovernancePlaceholder, generalScopes);
            AddPermission(definitions, ids, LegalAuthorityPlaceholderPermissionId, "Legal Authority Placeholder", OrganizationPermissionCategory.ExerciseLegalAuthorityPlaceholder, generalScopes);
            return definitions;
        }

        public static IReadOnlyList<InstitutionalActionDefinition> CreateMissingActionDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<InstitutionalActionDefinition> definitions = new List<InstitutionalActionDefinition>();
            AddAction(definitions, ids, ReviewMembershipApplicationActionId, "Review Membership Application", InstitutionalActionCategory.Membership, new[] { ReviewMembershipApplicationsPermissionId }, target: "organization-membership");
            AddAction(definitions, ids, InviteMemberActionId, "Invite Member", InstitutionalActionCategory.Membership, new[] { InviteMembersPermissionId }, target: "person");
            AddAction(definitions, ids, AdmitMemberActionId, "Admit Member", InstitutionalActionCategory.Membership, new[] { AdmitMembersPermissionId }, target: "organization-membership");
            AddAction(definitions, ids, SuspendMemberActionId, "Suspend Member", InstitutionalActionCategory.Membership, new[] { SuspendMembersPermissionId }, target: "organization-membership");
            AddAction(definitions, ids, ReinstateMemberActionId, "Reinstate Member", InstitutionalActionCategory.Membership, new[] { ReinstateMembersPermissionId }, target: "organization-membership");
            AddAction(definitions, ids, RemoveMemberActionId, "Remove Member", InstitutionalActionCategory.Membership, new[] { RemoveMembersPermissionId }, target: "organization-membership");
            AddAction(definitions, ids, AssignRankActionId, "Assign Rank", InstitutionalActionCategory.Rank, new[] { AssignRanksPermissionId }, target: "organization-rank");
            AddAction(definitions, ids, PromoteMemberActionId, "Promote Member", InstitutionalActionCategory.Rank, new[] { PromoteMembersPermissionId }, target: "organization-rank");
            AddAction(definitions, ids, DemoteMemberActionId, "Demote Member", InstitutionalActionCategory.Rank, new[] { DemoteMembersPermissionId }, target: "organization-rank");
            AddAction(definitions, ids, CreateOfficeActionId, "Create Office", InstitutionalActionCategory.Office, new[] { CreateOfficesPermissionId }, target: "organization-office");
            AddAction(definitions, ids, AppointOfficeholderActionId, "Appoint Officeholder", InstitutionalActionCategory.Office, new[] { AppointOfficeholdersPermissionId }, target: "organization-office");
            AddAction(definitions, ids, RemoveOfficeholderActionId, "Remove Officeholder", InstitutionalActionCategory.Office, new[] { RemoveOfficeholdersPermissionId }, target: "organization-office");
            AddAction(definitions, ids, AssignActingOfficeholderActionId, "Assign Acting Officeholder", InstitutionalActionCategory.Office, new[] { AssignActingOfficeholdersPermissionId }, target: "organization-office");
            AddAction(definitions, ids, RenameOrganizationActionId, "Rename Organization", InstitutionalActionCategory.OrganizationIdentity, new[] { ManageAliasesPermissionId }, target: "organization");
            AddAction(definitions, ids, ChangeHeadquartersActionId, "Change Headquarters", InstitutionalActionCategory.OrganizationIdentity, new[] { ProposeHeadquartersPermissionId, AuthorizeHeadquartersPermissionId }, OrganizationPermissionCombinationPolicy.JointApproval, target: "place", approvals: 2);
            AddAction(definitions, ids, CreateBranchActionId, "Create Organization Branch", InstitutionalActionCategory.OrganizationHierarchy, new[] { IssueOrdersPermissionId }, target: "organization");
            AddAction(definitions, ids, EndBranchLinkActionId, "End Organization Branch Link", InstitutionalActionCategory.OrganizationHierarchy, new[] { IssueOrdersPermissionId }, target: "organization");
            AddAction(definitions, ids, IssueOrderActionId, "Issue Institutional Order", InstitutionalActionCategory.Command, new[] { IssueOrdersPermissionId }, target: "order");
            AddAction(definitions, ids, AccessRestrictedRecordActionId, "Access Restricted Record", InstitutionalActionCategory.InformationAccess, new[] { ViewRestrictedInformationPermissionId }, target: "record", audit: OrganizationAuthorityAuditPolicy.Always);
            AddAction(definitions, ids, AccessSecretRecordActionId, "Access Secret Record", InstitutionalActionCategory.InformationAccess, new[] { ViewSecretInformationPermissionId }, target: "record", audit: OrganizationAuthorityAuditPolicy.Always);
            AddAction(definitions, ids, DelegatePermissionActionId, "Delegate Permission", InstitutionalActionCategory.Delegation, new[] { DelegatePermissionsPermissionId }, target: "authority-grant");
            AddAction(definitions, ids, RevokeDelegationActionId, "Revoke Delegation", InstitutionalActionCategory.Delegation, new[] { RevokeDelegationsPermissionId }, target: "authority-grant");
            return definitions;
        }

        public static IReadOnlyList<OrganizationAuthorityRoleDefinition> CreateMissingRoleDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationAuthorityRoleDefinition> definitions = new List<OrganizationAuthorityRoleDefinition>();
            AddRole(definitions, ids, GeneralMemberRoleId, "General Member Authority", new[] { ViewPublicInformationPermissionId }, priority: 10);
            AddRole(definitions, ids, MembershipOfficerRoleId, "Membership Officer Authority", new[] { ViewPublicInformationPermissionId, ReviewMembershipApplicationsPermissionId, InviteMembersPermissionId, AdmitMembersPermissionId, SuspendMembersPermissionId, ReinstateMembersPermissionId, RemoveMembersPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation, priority: 80);
            AddRole(definitions, ids, RankOfficerRoleId, "Rank Officer Authority", new[] { ViewPublicInformationPermissionId, AssignRanksPermissionId, PromoteMembersPermissionId, DemoteMembersPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation, priority: 90);
            AddRole(definitions, ids, TreasurerRoleId, "Treasurer Authority", new[] { ViewPublicInformationPermissionId, ViewRestrictedInformationPermissionId, ProposeHeadquartersPermissionId, ManageResourcesPlaceholderPermissionId }, priority: 90);
            AddRole(definitions, ids, ChapterMasterRoleId, "Chapter Master Authority", new[] { ViewPublicInformationPermissionId, ReviewMembershipApplicationsPermissionId, InviteMembersPermissionId, AdmitMembersPermissionId, AssignActingOfficeholdersPermissionId, IssueOrdersPermissionId, RepresentExternallyPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation, priority: 100);
            AddRole(definitions, ids, GuildmasterRoleId, "Guildmaster Authority", new[] { ViewPublicInformationPermissionId, ViewRestrictedInformationPermissionId, ReviewMembershipApplicationsPermissionId, InviteMembersPermissionId, AdmitMembersPermissionId, SuspendMembersPermissionId, ReinstateMembersPermissionId, RemoveMembersPermissionId, AssignRanksPermissionId, PromoteMembersPermissionId, DemoteMembersPermissionId, CreateOfficesPermissionId, AppointOfficeholdersPermissionId, RemoveOfficeholdersPermissionId, AssignActingOfficeholdersPermissionId, IssueOrdersPermissionId, RepresentExternallyPermissionId, ManageAliasesPermissionId, ProposeHeadquartersPermissionId, AuthorizeHeadquartersPermissionId, DelegatePermissionsPermissionId, RevokeDelegationsPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.Redelegable, priority: 150);
            AddRole(definitions, ids, RecordClerkRoleId, "Record Clerk Authority", new[] { ViewPublicInformationPermissionId, ViewRestrictedInformationPermissionId }, priority: 70);
            AddRole(definitions, ids, ActingExecutiveRoleId, "Acting Executive Authority", new[] { ViewPublicInformationPermissionId, ReviewMembershipApplicationsPermissionId, InviteMembersPermissionId, AdmitMembersPermissionId, AssignActingOfficeholdersPermissionId, IssueOrdersPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation, priority: 120);
            AddRole(definitions, ids, SecretMemberRoleId, "Secret Member Authority", new[] { ViewPublicInformationPermissionId, ViewRestrictedInformationPermissionId, ViewSecretInformationPermissionId }, visibility: OrganizationVisibility.Hidden, priority: 60);
            AddRole(definitions, ids, MilitaryOfficerRoleId, "Military Officer Authority", new[] { ViewPublicInformationPermissionId, IssueOrdersPermissionId, RepresentExternallyPermissionId }, delegation: OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation, priority: 100);
            return definitions;
        }

        public static IReadOnlyList<OrganizationAuthorityBindingDefinition> CreateMissingBindingDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = Set(existingDefinitionIds);
            List<OrganizationAuthorityBindingDefinition> definitions = new List<OrganizationAuthorityBindingDefinition>();
            AddBinding(definitions, ids, GuildFullMemberBindingId, "Guild Full Member Authority", OrganizationAuthorityBindingSourceType.MembershipDefinition, PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, GeneralMemberRoleId, OrganizationAuthorityScopeType.EntireOrganization);
            AddBinding(definitions, ids, GuildMasterRankBindingId, "Guild Master Rank Authority", OrganizationAuthorityBindingSourceType.RankDefinition, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, RankOfficerRoleId, OrganizationAuthorityScopeType.EntireOrganization, priority: 95);
            AddBinding(definitions, ids, GuildmasterOfficeBindingId, "Guildmaster Office Authority", OrganizationAuthorityBindingSourceType.OfficeDefinition, PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId, GuildmasterRoleId, OrganizationAuthorityScopeType.EntireOrganization, priority: 160);
            AddBinding(definitions, ids, GuildTreasurerOfficeBindingId, "Guild Treasurer Office Authority", OrganizationAuthorityBindingSourceType.OfficeDefinition, PrototypeOrganizationMembershipDefinitionFactory.GuildTreasurerOfficeId, TreasurerRoleId, OrganizationAuthorityScopeType.EntireOrganization, priority: 95);
            AddBinding(definitions, ids, BranchChapterMasterOfficeBindingId, "Branch Chapter Master Office Authority", OrganizationAuthorityBindingSourceType.OfficeDefinition, PrototypeOrganizationMembershipDefinitionFactory.BranchChapterMasterOfficeId, ChapterMasterRoleId, OrganizationAuthorityScopeType.EntireOrganization, priority: 110);
            AddBinding(definitions, ids, GuardCaptainOfficeBindingId, "Guard Captain Office Authority", OrganizationAuthorityBindingSourceType.OfficeDefinition, PrototypeOrganizationMembershipDefinitionFactory.GuardCaptainOfficeId, MilitaryOfficerRoleId, OrganizationAuthorityScopeType.EntireOrganization, priority: 110);
            AddBinding(definitions, ids, SecretMemberBindingId, "Secret Membership Authority", OrganizationAuthorityBindingSourceType.MembershipDefinition, PrototypeOrganizationMembershipDefinitionFactory.SecretMemberId, SecretMemberRoleId, OrganizationAuthorityScopeType.EntireOrganization, bindingVisibility: OrganizationVisibility.Hidden, priority: 60);
            return definitions;
        }

        private static void AddPermission(ICollection<OrganizationPermissionDefinition> definitions, ISet<string> ids, string id, string name, OrganizationPermissionCategory category, IEnumerable<OrganizationAuthorityScopeType> scopes, bool canDelegate = false, bool canRedelegate = false, bool jointAllowed = false, OrganizationVisibility visibility = OrganizationVisibility.Public)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationPermissionDefinition definition = ScriptableObject.CreateInstance<OrganizationPermissionDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, scopeTypes: scopes, canDelegate: canDelegate, canRedelegate: canRedelegate, jointAllowed: jointAllowed, permissionVisibility: visibility, tagIds: new[] { "prototype", "organization", "authority" });
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddAction(ICollection<InstitutionalActionDefinition> definitions, ISet<string> ids, string id, string name, InstitutionalActionCategory category, IEnumerable<string> permissions, OrganizationPermissionCombinationPolicy policy = OrganizationPermissionCombinationPolicy.AllRequiredPermissions, string target = "", int approvals = 1, OrganizationAuthorityAuditPolicy audit = OrganizationAuthorityAuditPolicy.SuccessfulActions)
        {
            if (ids.Contains(id))
            {
                return;
            }

            InstitutionalActionDefinition definition = ScriptableObject.CreateInstance<InstitutionalActionDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, category, permissions, policy, target, approvals: approvals, audit: audit, tagIds: new[] { "prototype", "organization", "authority" });
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddRole(ICollection<OrganizationAuthorityRoleDefinition> definitions, ISet<string> ids, string id, string name, IEnumerable<string> grants, OrganizationAuthorityDelegationPolicy delegation = OrganizationAuthorityDelegationPolicy.NonDelegable, int priority = 100, OrganizationVisibility visibility = OrganizationVisibility.Public)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationAuthorityRoleDefinition definition = ScriptableObject.CreateInstance<OrganizationAuthorityRoleDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, grants, scopeType: OrganizationAuthorityScopeType.EntireOrganization, delegation: delegation, rolePriority: priority, roleVisibility: visibility, tagIds: new[] { "prototype", "organization", "authority" });
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddBinding(ICollection<OrganizationAuthorityBindingDefinition> definitions, ISet<string> ids, string id, string name, OrganizationAuthorityBindingSourceType sourceType, string sourceDefinition, string authorityRole, OrganizationAuthorityScopeType scopeType, int priority = 100, OrganizationVisibility bindingVisibility = OrganizationVisibility.Public)
        {
            if (ids.Contains(id))
            {
                return;
            }

            OrganizationAuthorityBindingDefinition definition = ScriptableObject.CreateInstance<OrganizationAuthorityBindingDefinition>();
            definition.name = name;
            definition.DevelopmentConfigure(id, name, sourceType, sourceDefinition, authorityRole, authorityScopeType: scopeType, bindingPriority: priority, bindingVisibility: bindingVisibility, tagIds: new[] { "prototype", "organization", "authority" });
            definitions.Add(definition);
            ids.Add(id);
        }

        private static HashSet<string> Set(IEnumerable<string> ids)
        {
            return ids == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(ids.Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
        }
    }
}
