#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.Economy;
using UnityIsekaiGame.Economy.Businesses;
using UnityIsekaiGame.Economy.Properties;
using UnityIsekaiGame.GameData;
using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Progression;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(13, "Organizations", 1300)]
    public static class PrototypeStep13AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.13.1.organization-identity-records",
                "Organization Identity and Records",
                "13.1",
                "Persistent organization records with stable identity, lifecycle, hierarchy, visibility projections, and persistence.",
                13010,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "OrganizationRuntime", "OrganizationDefinition", "OrganizationPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("readiness-and-prototype-definitions", "Organization definitions and prototype records are available", 10,
                        Step("step13-organization-readiness", "Resolve definitions and seeded records", ReadinessAndPrototypeDefinitions)),
                    Scenario("create-rename-lifecycle", "Organizations create, rename, and transition lifecycle deterministically", 20,
                        Step("step13-organization-lifecycle", "Create, rename, duplicate, and transition", CreateRenameLifecycle)),
                    Scenario("links-and-projections", "Organization links and visibility projections enforce boundaries", 30,
                        Step("step13-organization-links", "Link hierarchy and read projections", LinksAndProjections)),
                    Scenario("persistence-validation", "Organization persistence validates before restoring", 40,
                        Step("step13-organization-persistence", "Save, restore, and reject invalid payloads", PersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.13.2.organization-memberships-ranks-offices",
                "Organization Memberships, Ranks, and Offices",
                "13.2",
                "Persistent organization membership, rank, and office records with idempotent lifecycle operations and persistence validation.",
                13020,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "OrganizationMembershipRuntime", "OrganizationRuntime", "OrganizationMembershipDefinition", "OrganizationRankDefinition", "OrganizationOfficeDefinition", "OrganizationMembershipPersistenceParticipant" },
                scenarios: new[]
                {
                    MembershipScenario("readiness-and-definitions", "Membership, rank, and office definitions are available", 10,
                        Step("step13-membership-readiness", "Resolve organization membership definitions", MembershipReadiness)),
                    MembershipScenario("application-invitation-consent", "Applications, invitations, and acceptance preserve consent boundaries", 20,
                        Step("step13-membership-consent", "Create pending records and require consent", ApplicationInvitationConsent)),
                    MembershipScenario("branch-membership", "Branch membership depends on a parent membership", 30,
                        Step("step13-membership-branch", "Create parent and branch membership", BranchMembership)),
                    MembershipScenario("rank-progression", "Rank assignments are deterministic and ordered", 40,
                        Step("step13-membership-ranks", "Assign and compare organization ranks", RankProgression)),
                    MembershipScenario("office-appointments", "Offices support vacancy, capacity, acting, and joint holders", 50,
                        Step("step13-membership-offices", "Create and assign organization offices", OfficeAppointments)),
                    MembershipScenario("ending-dependencies-idempotence", "Ending membership respects active assignments and duplicate transactions", 60,
                        Step("step13-membership-ending", "End membership with assignment policy", EndingDependenciesAndIdempotence)),
                    MembershipScenario("projection-and-persistence-validation", "Membership projections and persistence reject corrupt graphs without mutation", 70,
                        Step("step13-membership-persistence", "Project, save, restore, and reject invalid membership state", MembershipProjectionAndPersistence))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.13.3.organizational-roles-permissions-authority",
                "Organizational Roles, Permissions, and Institutional Authority",
                "13.3",
                "Definition-backed organization permissions, authority roles, grants, delegations, approvals, projections, and persistence.",
                13030,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "OrganizationAuthorityRuntime", "OrganizationRuntime", "OrganizationMembershipRuntime", "OrganizationPermissionDefinition", "InstitutionalActionDefinition", "OrganizationAuthorityRoleDefinition", "OrganizationAuthorityBindingDefinition", "OrganizationAuthorityPersistenceParticipant" },
                scenarios: new[]
                {
                    AuthorityScenario("readiness-and-definitions", "Authority permissions, roles, actions, and bindings are available", 10,
                        Step("step13-authority-readiness", "Resolve organization authority definitions", AuthorityReadiness)),
                    AuthorityScenario("membership-rank-office-authority", "Membership, rank, and office bindings produce effective authority", 20,
                        Step("step13-authority-bindings", "Evaluate bound membership authority", AuthorityFromMembershipRankOffice)),
                    AuthorityScenario("direct-grants-delegation", "Direct grants and delegations are scoped, expiring, and idempotent", 30,
                        Step("step13-authority-delegation", "Create and delegate scoped authority", AuthorityDirectGrantsDelegation)),
                    AuthorityScenario("branch-scope-boundaries", "Branch authority requires explicit scoped bindings", 40,
                        Step("step13-authority-branch", "Evaluate parent and branch authority boundaries", AuthorityBranchScopeBoundaries)),
                    AuthorityScenario("joint-approval-audits", "Joint approvals are consumed and audited explicitly", 50,
                        Step("step13-authority-approvals", "Authorize joint institutional action", AuthorityJointApprovalAudits)),
                    AuthorityScenario("persistence-validation", "Authority persistence validates before restoring", 60,
                        Step("step13-authority-persistence", "Save, restore, and reject invalid authority state", AuthorityPersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.13.4.organizational-resources-treasuries-property",
                "Organizational Resources, Treasuries, and Property",
                "13.4",
                "Organization treasury metadata coordinates authoritative Economy, Property, Business, and item identity state under institutional authority.",
                13040,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "OrganizationResourceRuntime", "OrganizationAuthorityRuntime", "EconomyRuntime", "PropertyRuntime", "BusinessRuntime", "ItemInstanceIdentityRuntime", "OrganizationResourcePersistenceParticipant" },
                scenarios: new[]
                {
                    ResourceScenario("runtime-readiness", "Organization resource definitions and runtimes are ready", 10,
                        Step("step13-resources-readiness", "Validate resource runtime dependencies", ResourceRuntimeReadiness)),
                    ResourceScenario("treasury-creation", "Treasury and account identities are stable and queryable", 20,
                        Step("step13-resources-treasury", "Create treasury and organization accounts", ResourceTreasuryCreation)),
                    ResourceScenario("basic-transfer", "Deposits and transfers use Economy and remain idempotent", 30,
                        Step("step13-resources-transfer", "Deposit and transfer authoritative funds", ResourceTransferIdempotence)),
                    ResourceScenario("authority-denial", "Unauthorized resource mutations fail without financial mutation", 40,
                        Step("step13-resources-authority", "Enforce organization financial authority", ResourceAuthorityDenial)),
                    ResourceScenario("restricted-funds", "Restricted and budgeted funds remain distinct", 50,
                        Step("step13-resources-allocations", "Validate fund allocation boundaries", ResourceAllocations)),
                    ResourceScenario("reservation", "Resource reservations are explicit and time-bound", 60,
                        Step("step13-resources-reservation", "Reserve and release authoritative funds", ResourceReservation)),
                    ResourceScenario("joint-approval", "Large transfers require independent joint approval", 70,
                        Step("step13-resources-joint-approval", "Authorize a joint financial action", ResourceJointApproval)),
                    ResourceScenario("inventory-and-custody", "Inventory associations and custody preserve Step 9 identity", 80,
                        Step("step13-resources-custody", "Associate inventory and track item custody", ResourceInventoryCustody)),
                    ResourceScenario("property-associations", "Property links require authoritative ownership", 90,
                        Step("step13-resources-assets", "Associate Step 11 property and business records", ResourcePropertyBusinessAssociations)),
                    ResourceScenario("business-and-revenue", "Business ownership and revenue routing stay delegated", 100,
                        Step("step13-resources-revenue", "Route business revenue through Economy accounts", ResourceRevenueRouting)),
                    ResourceScenario("payroll-funding", "Payroll funding uses an organization Economy account", 110,
                        Step("step13-resources-payroll", "Expose payroll funding without duplicating payroll state", ResourcePayrollFunding)),
                    ResourceScenario("branch-finances", "Branch accounts remain separate in consolidated views", 120,
                        Step("step13-resources-branch", "Query branch and parent finances", ResourceBranchFinances)),
                    ResourceScenario("dissolution-boundary", "Dissolution plans freeze resources without inventing beneficiaries", 130,
                        Step("step13-resources-dissolution", "Execute explicit dissolution resource plan", ResourceDissolutionBoundary)),
                    ResourceScenario("reconciliation", "Reconciliation and redacted projections are deterministic and read-only", 140,
                        Step("step13-resources-reconcile", "Reconcile and project resource state", ResourceReconciliationProjection)),
                    ResourceScenario("persistence-validation", "Resource persistence restores all metadata without replaying money", 150,
                        Step("step13-resources-persistence", "Save, restore, and reject resource graph drift", ResourcePersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.13.5.organizational-goals-policies-decisions",
                "Organizational Goals, Policies, Proposals, and Internal Decisions",
                "13.5",
                "Definition-backed organization goals, policies, proposals, voting, resolutions, execution plans, persistence, and projections.",
                13050,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "OrganizationDecisionRuntime", "OrganizationAuthorityRuntime", "OrganizationResourceRuntime", "OrganizationDecisionPersistenceParticipant" },
                scenarios: new[]
                {
                    DecisionScenario("runtime-readiness", "Organization decision definitions and runtimes are ready", 10,
                        Step("step13-decisions-readiness", "Validate organization decision definitions", DecisionRuntimeReadiness)),
                    DecisionScenario("goals-and-policies", "Goals and policies create, resolve, conflict, and progress deterministically", 20,
                        Step("step13-decisions-goals-policies", "Create goals and resolve policies", DecisionGoalsPolicies)),
                    DecisionScenario("proposal-vote-resolution", "Proposals, amendments, votes, and resolutions follow procedure", 30,
                        Step("step13-decisions-proposal", "Submit, amend, vote, and close proposal", DecisionProposalVoteResolution)),
                    DecisionScenario("execution-persistence-projection", "Resolution execution, persistence, and projections preserve authoritative ownership", 40,
                        Step("step13-decisions-execution", "Execute resolution and validate persistence", DecisionExecutionPersistenceProjection))
                }), out _);
        }

        private static TestLabAutomationStepResult ReadinessAndPrototypeDefinitions(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out OrganizationRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-organization-readiness", "Resolve definitions and seeded records", "OrganizationRuntime", "Present", "Missing", failure);
            }

            bool guildDefinition = context.ScenarioContext.Runtimes.DefinitionRegistry.TryGet(PrototypeOrganizationDefinitionFactory.GuildDefinitionId, out OrganizationDefinition guild);
            bool secretDefinition = context.ScenarioContext.Runtimes.DefinitionRegistry.TryGet(PrototypeOrganizationDefinitionFactory.SecretSocietyDefinitionId, out OrganizationDefinition secret);
            bool seededGuild = runtime.TryGetSnapshot(PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds[0], out OrganizationSnapshot guildSnapshot);
            bool seededForge = runtime.TryGetSnapshot("organization.prototype.royal-forge", out _);
            bool valid = guildDefinition
                && secretDefinition
                && guild.Category == OrganizationCategory.Guild
                && secret.SupportsVisibility(OrganizationVisibility.Hidden)
                && seededGuild
                && seededForge
                && guildSnapshot.CurrentName.Length > 0;

            return TestLabAssertions.True("step13-organization-readiness", "Resolve definitions and seeded records", valid, $"Definitions={guildDefinition}/{secretDefinition} Seeded={runtime.Count} Guild={guildSnapshot?.CurrentName}");
        }

        private static TestLabAutomationStepResult CreateRenameLifecycle(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out OrganizationRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-organization-lifecycle", "Create, rename, duplicate, and transition", "OrganizationRuntime", "Present", "Missing", failure);
            }

            long before = runtime.Revision;
            string organizationId = $"organization.testlab.guild.{context.RunId}";
            OrganizationOperationResult preview = runtime.CreateOrganization(CreateGuildRequest(organizationId, "Test Lab Guild", context.RunId, preview: true));
            OrganizationOperationResult create = runtime.CreateOrganization(CreateGuildRequest(organizationId, "Test Lab Guild", context.RunId));
            OrganizationOperationResult duplicate = runtime.CreateOrganization(CreateGuildRequest(organizationId, "Test Lab Guild", context.RunId));
            OrganizationOperationResult rename = runtime.RenameOrganization(new OrganizationRenameRequest
            {
                organizationId = organizationId,
                newOfficialName = "Test Lab Guild Office",
                effectiveWorldTime = 20d,
                transactionId = $"testlab.organization.rename.{context.RunId}"
            });
            OrganizationOperationResult dormant = runtime.TransitionLifecycle(new OrganizationLifecycleTransitionRequest
            {
                organizationId = organizationId,
                targetState = OrganizationLifecycleState.Dormant,
                worldTime = 30d,
                transactionId = $"testlab.organization.lifecycle.{context.RunId}"
            });
            runtime.TryGetSnapshot(organizationId, out OrganizationSnapshot snapshot);

            bool valid = preview.Status == OrganizationOperationStatus.Preview
                && create.Succeeded
                && duplicate.Duplicate
                && rename.Succeeded
                && dormant.Succeeded
                && snapshot != null
                && snapshot.CurrentName == "Test Lab Guild Office"
                && snapshot.LifecycleState == OrganizationLifecycleState.Dormant
                && runtime.Revision > before;
            return TestLabAssertions.True("step13-organization-lifecycle", "Create, rename, duplicate, and transition", valid, $"Preview={preview.Status} Create={create.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate} Rename={rename.Status} Dormant={dormant.Status} Revision={before}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult LinksAndProjections(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out OrganizationRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-organization-links", "Link hierarchy and read projections", "OrganizationRuntime", "Present", "Missing", failure);
            }

            string parentId = $"organization.testlab.parent.{context.RunId}";
            string childId = $"organization.testlab.branch.{context.RunId}";
            string hiddenId = $"organization.testlab.hidden.{context.RunId}";
            OrganizationOperationResult parent = runtime.CreateOrganization(CreateGuildRequest(parentId, "Parent Test Guild", context.RunId));
            OrganizationOperationResult child = runtime.CreateOrganization(CreateGuildRequest(childId, "Branch Test Guild", context.RunId));
            OrganizationOperationResult hidden = runtime.CreateOrganization(new OrganizationCreateRequest
            {
                organizationId = hiddenId,
                organizationDefinitionId = PrototypeOrganizationDefinitionFactory.SecretSocietyDefinitionId,
                officialName = "Hidden Test Circle",
                initialLifecycleState = OrganizationLifecycleState.Active,
                visibility = OrganizationVisibility.Hidden,
                transactionId = $"testlab.organization.create.hidden.{context.RunId}"
            });
            OrganizationOperationResult link = runtime.LinkOrganizations(new OrganizationLinkRequest
            {
                sourceOrganizationId = childId,
                targetOrganizationId = parentId,
                kind = OrganizationLinkKind.Parent,
                transactionId = $"testlab.organization.link.parent.{context.RunId}"
            });
            OrganizationOperationResult cycle = runtime.LinkOrganizations(new OrganizationLinkRequest
            {
                sourceOrganizationId = parentId,
                targetOrganizationId = childId,
                kind = OrganizationLinkKind.Parent,
                transactionId = $"testlab.organization.link.cycle.{context.RunId}"
            });
            OrganizationProjection redacted = runtime.ProjectOrganization(childId, PersistenceService.LocalPlayerId);
            OrganizationProjection concealed = runtime.ProjectOrganization(hiddenId, PersistenceService.LocalPlayerId);

            bool valid = parent.Succeeded
                && child.Succeeded
                && hidden.Succeeded
                && link.Succeeded
                && cycle.Status == OrganizationOperationStatus.CycleDetected
                && runtime.QueryByParent(parentId).Any(snapshot => snapshot.OrganizationId == childId)
                && redacted.Access == OrganizationProjectionAccess.Full
                && concealed.Access == OrganizationProjectionAccess.Concealed;
            return TestLabAssertions.True("step13-organization-links", "Link hierarchy and read projections", valid, $"Parent={parent.Status} Child={child.Status} Hidden={hidden.Status} Link={link.Status} Cycle={cycle.Status} Redacted={redacted.Access} Concealed={concealed.Access}");
        }

        private static TestLabAutomationStepResult PersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out OrganizationRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-organization-persistence", "Save, restore, and reject invalid payloads", "OrganizationRuntime", "Present", "Missing", failure);
            }

            string organizationId = $"organization.testlab.persisted.{context.RunId}";
            OrganizationOperationResult create = runtime.CreateOrganization(CreateGuildRequest(organizationId, "Persisted Test Guild", context.RunId));
            OrganizationRuntimeSaveData save = runtime.CreateSaveData();
            OrganizationRuntime restored = new OrganizationRuntime();
            OrganizationOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, PersistenceService.LocalWorldId, context.ScenarioContext.Runtimes.KnownPersonIds, Array.Empty<string>(), restoring: true);
            OrganizationRuntimeSaveData corrupt = JsonUtility.FromJson<OrganizationRuntimeSaveData>(JsonUtility.ToJson(save));
            OrganizationRecordData record = corrupt.records.First(item => item.organizationId == organizationId);
            record.organizationDefinitionId = "organization-definition.missing";
            bool rejected = !OrganizationRuntime.ValidateSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, PersistenceService.LocalWorldId, context.ScenarioContext.Runtimes.KnownPersonIds, Array.Empty<string>(), out string validationFailure);

            bool valid = create.Succeeded
                && restore.Succeeded
                && restored.TryGetSnapshot(organizationId, out OrganizationSnapshot restoredSnapshot)
                && restoredSnapshot.CurrentName == "Persisted Test Guild"
                && rejected
                && runtime.TryGetSnapshot(organizationId, out OrganizationSnapshot liveSnapshot)
                && liveSnapshot.CurrentName == "Persisted Test Guild";
            return TestLabAssertions.True("step13-organization-persistence", "Save, restore, and reject invalid payloads", valid, $"Create={create.Status} Restore={restore.Status} Rejected={rejected}:{validationFailure} Count={runtime.Count}/{restored.Count}");
        }

        private static TestLabAutomationStepResult MembershipReadiness(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-readiness", "Resolve organization membership definitions", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool full = registry.TryGet(PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, out OrganizationMembershipDefinition fullMember);
            bool invitee = registry.TryGet(PrototypeOrganizationMembershipDefinitionFactory.GuildInviteeId, out OrganizationMembershipDefinition inviteeMember);
            bool track = registry.TryGet(PrototypeOrganizationMembershipDefinitionFactory.GuildCraftTrackId, out OrganizationRankTrackDefinition craftTrack);
            bool master = registry.TryGet(PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, out OrganizationRankDefinition masterRank);
            bool office = registry.TryGet(PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId, out OrganizationOfficeDefinition guildmasterOffice);
            bool valid = runtime != null
                && full
                && invitee
                && track
                && master
                && office
                && fullMember.SupportsRanks
                && fullMember.SupportsOffices
                && inviteeMember.InitialStatus == OrganizationMembershipStatus.Invited
                && craftTrack.SupportedMembershipDefinitionIds.Contains(PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId)
                && guildmasterOffice.RequiredRankDefinitionIds.Contains(PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId);

            return TestLabAssertions.True("step13-membership-readiness", "Resolve organization membership definitions", valid, $"Definitions={full}/{invitee}/{track}/{master}/{office} Runtime={runtime.MembershipCount}/{runtime.OfficeCount}");
        }

        private static TestLabAutomationStepResult ApplicationInvitationConsent(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-consent", "Create pending records and require consent", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            string applicantId = "person.prototype.friend";
            string inviteeId = "person.prototype.rival";
            OrganizationMembershipOperationResult application = runtime.ApplyMembership(MembershipRequest(
                $"organization-membership.testlab.application.{context.RunId}",
                "organization.prototype.guild",
                applicantId,
                PrototypeOrganizationMembershipDefinitionFactory.GuildApplicantId,
                OrganizationMembershipStatus.Applied,
                OrganizationMembershipSourceKind.Application,
                $"testlab.membership.application.{context.RunId}"));
            OrganizationMembershipOperationResult invitation = runtime.ApplyMembership(MembershipRequest(
                $"organization-membership.testlab.invitation.{context.RunId}",
                "organization.prototype.guild",
                inviteeId,
                PrototypeOrganizationMembershipDefinitionFactory.GuildInviteeId,
                OrganizationMembershipStatus.Invited,
                OrganizationMembershipSourceKind.Invitation,
                $"testlab.membership.invitation.{context.RunId}"));
            OrganizationMembershipOperationResult denied = runtime.ApplyMembership(MembershipRequest(
                invitation.Membership?.MembershipId,
                "organization.prototype.guild",
                inviteeId,
                PrototypeOrganizationMembershipDefinitionFactory.GuildInviteeId,
                OrganizationMembershipStatus.Active,
                OrganizationMembershipSourceKind.Invitation,
                $"testlab.membership.invitation.denied.{context.RunId}"));
            OrganizationMembershipRequest accept = MembershipRequest(
                invitation.Membership?.MembershipId,
                "organization.prototype.guild",
                inviteeId,
                PrototypeOrganizationMembershipDefinitionFactory.GuildInviteeId,
                OrganizationMembershipStatus.Active,
                OrganizationMembershipSourceKind.Invitation,
                $"testlab.membership.invitation.accept.{context.RunId}");
            accept.explicitConsent = true;
            OrganizationMembershipOperationResult accepted = runtime.ApplyMembership(accept);
            OrganizationMembershipOperationResult duplicate = runtime.ApplyMembership(accept);

            bool valid = application.Succeeded
                && application.Membership.Status == OrganizationMembershipStatus.Applied
                && invitation.Succeeded
                && invitation.Membership.Status == OrganizationMembershipStatus.Invited
                && denied.Status == OrganizationMembershipOperationStatus.ConsentRequired
                && accepted.Succeeded
                && accepted.Membership.Status == OrganizationMembershipStatus.Active
                && duplicate.Duplicate;

            return TestLabAssertions.True("step13-membership-consent", "Create pending records and require consent", valid, $"Application={application.Status}/{application.Membership?.Status} Invitation={invitation.Status}/{invitation.Membership?.Status} Denied={denied.Status} Accepted={accepted.Status}/{accepted.Membership?.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate}");
        }

        private static TestLabAutomationStepResult BranchMembership(TestLabAutomationContext context)
        {
            bool hasMemberships = TryGetMembershipRuntime(context, out OrganizationMembershipRuntime memberships, out string membershipFailure);
            bool hasOrganizations = TryGetRuntime(context, out OrganizationRuntime organizations, out string organizationFailure);
            if (!hasMemberships || !hasOrganizations)
            {
                return TestLabAssertions.Fail("step13-membership-branch", "Create parent and branch membership", "OrganizationMembershipRuntime", "Present", "Missing", $"{membershipFailure} {organizationFailure}".Trim());
            }

            string personId = "person.prototype.student";
            string branchId = $"organization.testlab.branch.{context.RunId}";
            OrganizationOperationResult branch = organizations.CreateOrganization(new OrganizationCreateRequest
            {
                organizationId = branchId,
                organizationDefinitionId = PrototypeOrganizationDefinitionFactory.BranchDefinitionId,
                officialName = "Test Lab Guild Branch",
                initialLifecycleState = OrganizationLifecycleState.Active,
                visibility = OrganizationVisibility.Public,
                transactionId = $"testlab.organization.branch.{context.RunId}"
            });
            OrganizationOperationResult link = organizations.LinkOrganizations(new OrganizationLinkRequest
            {
                sourceOrganizationId = branchId,
                targetOrganizationId = "organization.prototype.guild",
                kind = OrganizationLinkKind.Parent,
                transactionId = $"testlab.organization.branch.link.{context.RunId}"
            });
            memberships.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, organizations, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, organizations.Snapshots.Select(snapshot => snapshot.OrganizationId));
            OrganizationMembershipOperationResult missingParent = memberships.ApplyMembership(MembershipRequest(
                $"organization-membership.testlab.branch.missing.{context.RunId}",
                branchId,
                personId,
                PrototypeOrganizationMembershipDefinitionFactory.BranchMemberId,
                OrganizationMembershipStatus.Active,
                OrganizationMembershipSourceKind.WorldSetup,
                $"testlab.membership.branch.missing.{context.RunId}",
                consent: true));
            OrganizationMembershipOperationResult parent = memberships.ApplyMembership(MembershipRequest(
                $"organization-membership.testlab.parent.{context.RunId}",
                "organization.prototype.guild",
                personId,
                PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId,
                OrganizationMembershipStatus.Active,
                OrganizationMembershipSourceKind.WorldSetup,
                $"testlab.membership.parent.{context.RunId}",
                consent: true));
            OrganizationMembershipRequest branchMembershipRequest = MembershipRequest(
                $"organization-membership.testlab.branch.{context.RunId}",
                branchId,
                personId,
                PrototypeOrganizationMembershipDefinitionFactory.BranchMemberId,
                OrganizationMembershipStatus.Active,
                OrganizationMembershipSourceKind.WorldSetup,
                $"testlab.membership.branch.{context.RunId}",
                consent: true);
            branchMembershipRequest.parentMembershipId = parent.Membership?.MembershipId;
            branchMembershipRequest.branchOrganizationId = branchId;
            OrganizationMembershipOperationResult child = memberships.ApplyMembership(branchMembershipRequest);

            bool valid = branch.Succeeded
                && link.Succeeded
                && missingParent.Status == OrganizationMembershipOperationStatus.InvalidDependency
                && parent.Succeeded
                && child.Succeeded
                && child.Membership.Data.parentMembershipId == parent.Membership.MembershipId
                && memberships.QueryMemberships(personId).Select(snapshot => snapshot.MembershipId).SequenceEqual(memberships.QueryMemberships(personId).Select(snapshot => snapshot.MembershipId));

            return TestLabAssertions.True("step13-membership-branch", "Create parent and branch membership", valid, $"Branch={branch.Status} Link={link.Status} MissingParent={missingParent.Status} Parent={parent.Status} Child={child.Status} Query={memberships.QueryMemberships(personId).Count}");
        }

        private static TestLabAutomationStepResult RankProgression(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-ranks", "Assign and compare organization ranks", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            string membershipId = $"organization-membership.testlab.rank.{context.RunId}";
            OrganizationMembershipOperationResult member = runtime.ApplyMembership(MembershipRequest(membershipId, "organization.prototype.guild", "person.prototype.mentor", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.membership.rank.member.{context.RunId}", consent: true));
            OrganizationMembershipOperationResult novice = runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.novice.{context.RunId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId, $"testlab.membership.rank.novice.{context.RunId}"));
            OrganizationMembershipOperationResult journeyman = runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.journeyman.{context.RunId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildJourneymanRankId, $"testlab.membership.rank.journeyman.{context.RunId}"));
            OrganizationMembershipOperationResult master = runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.master.{context.RunId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, $"testlab.membership.rank.master.{context.RunId}"));
            OrganizationMembershipOperationResult duplicate = runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.master.{context.RunId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, $"testlab.membership.rank.master.{context.RunId}"));
            runtime.TryGetMembership(membershipId, out OrganizationMembershipSnapshot snapshot);

            bool valid = member.Succeeded
                && novice.Succeeded
                && journeyman.Succeeded
                && master.Succeeded
                && duplicate.Duplicate
                && runtime.CompareRanks(PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId) < 0
                && snapshot.RankAssignments.Count == 3
                && snapshot.RankAssignments.Count(item => item.state == OrganizationRankAssignmentState.Active) == 1
                && snapshot.RankAssignments.Single(item => item.state == OrganizationRankAssignmentState.Active).rankDefinitionId == PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId;

            return TestLabAssertions.True("step13-membership-ranks", "Assign and compare organization ranks", valid, $"Member={member.Status} Novice={novice.Status} Journey={journeyman.Status} Master={master.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate} Active={snapshot?.RankAssignments.Count(item => item.state == OrganizationRankAssignmentState.Active)}");
        }

        private static TestLabAutomationStepResult OfficeAppointments(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-offices", "Create and assign organization offices", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            string masterMembershipId = $"organization-membership.testlab.office.master.{context.RunId}";
            string associateOneId = $"organization-membership.testlab.office.associate1.{context.RunId}";
            string associateTwoId = $"organization-membership.testlab.office.associate2.{context.RunId}";
            OrganizationMembershipOperationResult masterMember = runtime.ApplyMembership(MembershipRequest(masterMembershipId, "organization.prototype.guild", "person.prototype.friend", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.office.member.master.{context.RunId}", consent: true));
            runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.office.novice.{context.RunId}", masterMembershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId, $"testlab.office.rank.novice.{context.RunId}"));
            runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.office.journey.{context.RunId}", masterMembershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildJourneymanRankId, $"testlab.office.rank.journey.{context.RunId}"));
            runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.office.master.{context.RunId}", masterMembershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, $"testlab.office.rank.master.{context.RunId}"));
            OrganizationMembershipOperationResult guildmaster = runtime.CreateOffice(OfficeRequest($"organization-office-record.testlab.guildmaster.{context.RunId}", "organization.prototype.guild", PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId, $"testlab.office.guildmaster.{context.RunId}"));
            OrganizationMembershipOperationResult assignGuildmaster = runtime.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.guildmaster.{context.RunId}", guildmaster.Office?.OfficeId, masterMembershipId, $"testlab.office.assign.guildmaster.{context.RunId}"));
            OrganizationMembershipOperationResult duplicateGuildmaster = runtime.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.guildmaster.duplicate.{context.RunId}", guildmaster.Office?.OfficeId, masterMembershipId, $"testlab.office.assign.guildmaster.duplicate.{context.RunId}"));

            OrganizationMembershipOperationResult associateOne = runtime.ApplyMembership(MembershipRequest(associateOneId, "organization.prototype.guild", "person.prototype.cousin", PrototypeOrganizationMembershipDefinitionFactory.GuildAssociateId, OrganizationMembershipStatus.Provisional, OrganizationMembershipSourceKind.WorldSetup, $"testlab.office.member.associate1.{context.RunId}", consent: true));
            OrganizationMembershipOperationResult associateTwo = runtime.ApplyMembership(MembershipRequest(associateTwoId, "organization.prototype.guild", "person.prototype.student", PrototypeOrganizationMembershipDefinitionFactory.GuildAssociateId, OrganizationMembershipStatus.Provisional, OrganizationMembershipSourceKind.WorldSetup, $"testlab.office.member.associate2.{context.RunId}", consent: true));
            OrganizationMembershipOperationResult treasurer = runtime.CreateOffice(OfficeRequest($"organization-office-record.testlab.treasurer.{context.RunId}", "organization.prototype.guild", PrototypeOrganizationMembershipDefinitionFactory.GuildTreasurerOfficeId, $"testlab.office.treasurer.{context.RunId}", maximumHolders: 2));
            OrganizationMembershipOperationResult assignOne = runtime.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.treasurer1.{context.RunId}", treasurer.Office?.OfficeId, associateOneId, $"testlab.office.assign.treasurer1.{context.RunId}", acting: true));
            OrganizationMembershipOperationResult assignTwo = runtime.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.treasurer2.{context.RunId}", treasurer.Office?.OfficeId, associateTwoId, $"testlab.office.assign.treasurer2.{context.RunId}"));
            runtime.TryGetOffice(treasurer.Office?.OfficeId, out OrganizationOfficeSnapshot treasurerSnapshot);

            bool valid = masterMember.Succeeded
                && guildmaster.Succeeded
                && assignGuildmaster.Succeeded
                && duplicateGuildmaster.Status == OrganizationMembershipOperationStatus.CapacityFull
                && associateOne.Succeeded
                && associateTwo.Succeeded
                && treasurer.Succeeded
                && assignOne.Succeeded
                && assignOne.OfficeAssignment.acting
                && assignTwo.Succeeded
                && treasurerSnapshot.Assignments.Count(item => item.IsActive) == 2
                && !treasurerSnapshot.IsVacant;

            return TestLabAssertions.True("step13-membership-offices", "Create and assign organization offices", valid, $"Master={masterMember.Status} Guildmaster={guildmaster.Status}/{assignGuildmaster.Status}/{duplicateGuildmaster.Status} Treasurer={treasurer.Status} Assignments={assignOne.Status}/{assignTwo.Status} Active={treasurerSnapshot?.Assignments.Count(item => item.IsActive)}");
        }

        private static TestLabAutomationStepResult EndingDependenciesAndIdempotence(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-ending", "End membership with assignment policy", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            string membershipId = $"organization-membership.testlab.end.{context.RunId}";
            OrganizationMembershipOperationResult member = runtime.ApplyMembership(MembershipRequest(membershipId, "organization.prototype.guild", "person.prototype.dependent", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.membership.end.member.{context.RunId}", consent: true));
            runtime.AssignRank(RankRequest($"organization-rank-assignment.testlab.end.novice.{context.RunId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId, $"testlab.membership.end.rank.{context.RunId}"));
            OrganizationMembershipOperationResult office = runtime.CreateOffice(OfficeRequest($"organization-office-record.testlab.end.treasurer.{context.RunId}", "organization.prototype.guild", PrototypeOrganizationMembershipDefinitionFactory.GuildTreasurerOfficeId, $"testlab.membership.end.office.{context.RunId}", maximumHolders: 2));
            runtime.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.end.treasurer.{context.RunId}", office.Office?.OfficeId, membershipId, $"testlab.membership.end.office.assign.{context.RunId}"));
            OrganizationMembershipRequest blockedRequest = MembershipRequest(membershipId, "organization.prototype.guild", "person.prototype.dependent", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Resigned, OrganizationMembershipSourceKind.ScriptedEvent, $"testlab.membership.end.blocked.{context.RunId}");
            OrganizationMembershipOperationResult blocked = runtime.ApplyMembership(blockedRequest);
            OrganizationMembershipRequest endRequest = MembershipRequest(membershipId, "organization.prototype.guild", "person.prototype.dependent", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Resigned, OrganizationMembershipSourceKind.ScriptedEvent, $"testlab.membership.end.apply.{context.RunId}");
            endRequest.endingPolicy = OrganizationMembershipEndingPolicy.EndActiveAssignments;
            OrganizationMembershipOperationResult ended = runtime.ApplyMembership(endRequest);
            OrganizationMembershipOperationResult duplicate = runtime.ApplyMembership(endRequest);
            runtime.TryGetMembership(membershipId, out OrganizationMembershipSnapshot snapshot);

            bool valid = member.Succeeded
                && blocked.Status == OrganizationMembershipOperationStatus.ActiveAssignmentsBlockEnding
                && ended.Succeeded
                && ended.Membership.Status == OrganizationMembershipStatus.Resigned
                && duplicate.Duplicate
                && snapshot.RankAssignments.All(rank => !rank.IsActive)
                && snapshot.OfficeAssignments.All(assignment => !assignment.IsActive);

            return TestLabAssertions.True("step13-membership-ending", "End membership with assignment policy", valid, $"Member={member.Status} Blocked={blocked.Status} Ended={ended.Status}/{ended.Membership?.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate} Ranks={snapshot?.RankAssignments.Count} Offices={snapshot?.OfficeAssignments.Count}");
        }

        private static TestLabAutomationStepResult MembershipProjectionAndPersistence(TestLabAutomationContext context)
        {
            if (!TryGetMembershipRuntime(context, out OrganizationMembershipRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-membership-persistence", "Project, save, restore, and reject invalid membership state", "OrganizationMembershipRuntime", "Present", "Missing", failure);
            }

            string visibleMembershipId = $"organization-membership.testlab.persist.visible.{context.RunId}";
            string hiddenMembershipId = $"organization-membership.testlab.persist.hidden.{context.RunId}";
            OrganizationMembershipOperationResult visible = runtime.ApplyMembership(MembershipRequest(visibleMembershipId, "organization.prototype.guild", "person.prototype.partner", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.membership.persist.visible.{context.RunId}", consent: true));
            OrganizationMembershipRequest hiddenRequest = MembershipRequest(hiddenMembershipId, "organization.prototype.guild", "person.prototype.spouse", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.membership.persist.hidden.{context.RunId}", consent: true);
            hiddenRequest.visibility = OrganizationVisibility.Hidden;
            OrganizationMembershipOperationResult hidden = runtime.ApplyMembership(hiddenRequest);
            OrganizationMembershipProjection publicProjection = runtime.ProjectMembership(visibleMembershipId, "person.prototype.friend");
            OrganizationMembershipProjection hiddenProjection = runtime.ProjectMembership(hiddenMembershipId, "person.prototype.friend");
            OrganizationMembershipProjection privilegedProjection = runtime.ProjectMembership(hiddenMembershipId, "person.prototype.friend", privileged: true);

            OrganizationMembershipRuntimeSaveData save = runtime.CreateSaveData();
            OrganizationMembershipRuntime restored = new OrganizationMembershipRuntime();
            OrganizationMembershipOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, context.ScenarioContext.Runtimes.Organizations.Snapshots.Select(snapshot => snapshot.OrganizationId), restoring: true);
            OrganizationMembershipRuntimeSaveData corrupt = JsonUtility.FromJson<OrganizationMembershipRuntimeSaveData>(JsonUtility.ToJson(save));
            corrupt.memberships.First(item => item.membershipId == visibleMembershipId).membershipDefinitionId = "organization-membership.missing";
            bool rejected = !OrganizationMembershipRuntime.ValidateSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, context.ScenarioContext.Runtimes.Organizations.Snapshots.Select(snapshot => snapshot.OrganizationId), out string validationFailure);
            bool liveUnchanged = runtime.TryGetMembership(visibleMembershipId, out OrganizationMembershipSnapshot liveVisible)
                && liveVisible.Status == OrganizationMembershipStatus.Active
                && runtime.MembershipCount == save.memberships.Count;

            bool valid = visible.Succeeded
                && hidden.Succeeded
                && publicProjection.Access == OrganizationMembershipProjectionAccess.Full
                && hiddenProjection.Access == OrganizationMembershipProjectionAccess.Concealed
                && privilegedProjection.Access == OrganizationMembershipProjectionAccess.Full
                && restore.Succeeded
                && restored.TryGetMembership(visibleMembershipId, out OrganizationMembershipSnapshot restoredVisible)
                && restoredVisible.Status == OrganizationMembershipStatus.Active
                && rejected
                && liveUnchanged;

            return TestLabAssertions.True("step13-membership-persistence", "Project, save, restore, and reject invalid membership state", valid, $"Visible={visible.Status} Hidden={hidden.Status} Projection={publicProjection.Access}/{hiddenProjection.Access}/{privilegedProjection.Access} Restore={restore.Status} Rejected={rejected}:{validationFailure} Live={liveUnchanged}");
        }

        private static TestLabAutomationStepResult AuthorityReadiness(TestLabAutomationContext context)
        {
            if (!TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime runtime, out string failure))
            {
                return TestLabAssertions.Fail("step13-authority-readiness", "Resolve organization authority definitions", "OrganizationAuthorityRuntime", "Present", "Missing", failure);
            }

            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool permission = registry.TryGet(PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholdersPermissionId, out OrganizationPermissionDefinition appoint)
                && appoint.Category == OrganizationPermissionCategory.ManageOffices;
            bool action = registry.TryGet(PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholderActionId, out InstitutionalActionDefinition appointAction)
                && appointAction.RequiredPermissionIds.Contains(PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholdersPermissionId);
            bool role = registry.TryGet(PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId, out OrganizationAuthorityRoleDefinition guildmaster)
                && guildmaster.GrantedPermissionIds.Contains(PrototypeOrganizationAuthorityDefinitionFactory.PromoteMembersPermissionId);
            bool binding = registry.TryGet(PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterOfficeBindingId, out OrganizationAuthorityBindingDefinition bindingDefinition)
                && bindingDefinition.AuthorityRoleDefinitionId == PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId;
            string actorId = PrimaryAuthorityActorId(context);
            OrganizationEffectiveAuthoritySnapshot first = runtime.QueryEffectiveAuthority(actorId, "organization.prototype.guild", 10d);
            OrganizationEffectiveAuthoritySnapshot second = runtime.QueryEffectiveAuthority(actorId, "organization.prototype.guild", 10d);

            bool valid = permission
                && action
                && role
                && binding
                && first.RuntimeRevision == second.RuntimeRevision
                && first.Sources.Count == second.Sources.Count;

            return TestLabAssertions.True("step13-authority-readiness", "Resolve organization authority definitions", valid, $"Permission={permission} Action={action} Role={role} Binding={binding} Sources={first.Sources.Count}/{second.Sources.Count}");
        }

        private static TestLabAutomationStepResult AuthorityFromMembershipRankOffice(TestLabAutomationContext context)
        {
            bool hasAuthority = TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime authority, out string authorityFailure);
            bool hasMemberships = TryGetMembershipRuntime(context, out OrganizationMembershipRuntime memberships, out string membershipFailure);
            if (!hasAuthority || !hasMemberships)
            {
                string details = authorityFailure.Length > 0 ? authorityFailure : membershipFailure;
                return TestLabAssertions.Fail("step13-authority-bindings", "Evaluate bound membership authority", "OrganizationAuthorityRuntime", "Present", "Missing", details);
            }

            string actorId = PrimaryAuthorityActorId(context);
            CreateAuthorityGuildmaster(context, actorId, "master");
            memberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.authority.general.{context.RunId}", "organization.prototype.guild", "person.prototype.friend", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.member.general.{context.RunId}", consent: true));

            OrganizationAuthorizationResult guildmaster = authority.EvaluateAuthorization(AuthorityRequest(actorId, "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholderActionId, $"testlab.authority.auth.guildmaster.{context.RunId}"));
            OrganizationAuthorizationResult general = authority.EvaluateAuthorization(AuthorityRequest("person.prototype.friend", "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholderActionId, $"testlab.authority.auth.general.{context.RunId}"));
            OrganizationEffectiveAuthoritySnapshot effective = authority.QueryEffectiveAuthority(actorId, "organization.prototype.guild", 100d);

            bool valid = guildmaster.Succeeded
                && general.Status == OrganizationAuthorizationStatus.MissingPermission
                && effective.Sources.Any(source => source.permissionDefinitionId == PrototypeOrganizationAuthorityDefinitionFactory.PromoteMembersPermissionId)
                && memberships.MembershipCount >= 2;

            return TestLabAssertions.True("step13-authority-bindings", "Evaluate bound membership authority", valid, $"Guildmaster={guildmaster.Status} General={general.Status} Sources={effective.Sources.Count} Memberships={memberships.MembershipCount}");
        }

        private static TestLabAutomationStepResult AuthorityDirectGrantsDelegation(TestLabAutomationContext context)
        {
            if (!TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime authority, out string failure))
            {
                return TestLabAssertions.Fail("step13-authority-delegation", "Create and delegate scoped authority", "OrganizationAuthorityRuntime", "Present", "Missing", failure);
            }

            string actorId = PrimaryAuthorityActorId(context);
            CreateAuthorityGuildmaster(context, actorId, "master");
            context.ScenarioContext.Runtimes.OrganizationMemberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.authority.direct.friend.{context.RunId}", "organization.prototype.guild", "person.prototype.friend", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.direct.friend.member.{context.RunId}", consent: true));
            OrganizationAuthorityOperationResult direct = authority.CreateDirectGrant(new OrganizationAuthorityGrantRequest
            {
                grantId = $"organization-authority-grant.testlab.direct.{context.RunId}",
                organizationId = "organization.prototype.guild",
                granteePersonId = "person.prototype.friend",
                grantorPersonId = actorId,
                permissionDefinitionIds = new[] { PrototypeOrganizationAuthorityDefinitionFactory.IssueOrdersPermissionId },
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                startWorldTime = 20d,
                expirationWorldTime = 40d,
                delegationPolicy = OrganizationAuthorityDelegationPolicy.DelegableNoRedelegation,
                transactionId = $"testlab.authority.direct.{context.RunId}"
            });
            OrganizationAuthorityOperationResult duplicate = authority.CreateDirectGrant(new OrganizationAuthorityGrantRequest
            {
                grantId = direct.Grant?.GrantId,
                organizationId = "organization.prototype.guild",
                granteePersonId = "person.prototype.friend",
                grantorPersonId = actorId,
                permissionDefinitionIds = new[] { PrototypeOrganizationAuthorityDefinitionFactory.IssueOrdersPermissionId },
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                startWorldTime = 20d,
                expirationWorldTime = 40d,
                transactionId = $"testlab.authority.direct.{context.RunId}"
            });
            OrganizationAuthorizationResult authorized = authority.EvaluateAuthorization(AuthorityRequest("person.prototype.friend", "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId, $"testlab.authority.friend.orders.{context.RunId}", 30d));
            OrganizationAuthorizationResult expired = authority.EvaluateAuthorization(AuthorityRequest("person.prototype.friend", "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId, $"testlab.authority.friend.orders.expired.{context.RunId}", 50d));
            OrganizationAuthorityOperationResult delegated = authority.DelegateAuthority(new OrganizationDelegationRequest
            {
                delegationGrantId = $"organization-authority-grant.testlab.delegated.{context.RunId}",
                organizationId = "organization.prototype.guild",
                delegatorPersonId = "person.prototype.friend",
                recipientPersonId = "person.prototype.student",
                sourceAuthorityId = direct.Grant?.GrantId,
                permissionDefinitionIds = new[] { PrototypeOrganizationAuthorityDefinitionFactory.IssueOrdersPermissionId },
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                startWorldTime = 25d,
                expirationWorldTime = 35d,
                transactionId = $"testlab.authority.delegate.{context.RunId}"
            });
            OrganizationAuthorityOperationResult redelegated = authority.DelegateAuthority(new OrganizationDelegationRequest
            {
                delegationGrantId = $"organization-authority-grant.testlab.redelegated.{context.RunId}",
                organizationId = "organization.prototype.guild",
                delegatorPersonId = "person.prototype.student",
                recipientPersonId = "person.prototype.rival",
                sourceAuthorityId = delegated.Grant?.GrantId,
                permissionDefinitionIds = new[] { PrototypeOrganizationAuthorityDefinitionFactory.IssueOrdersPermissionId },
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                startWorldTime = 26d,
                expirationWorldTime = 30d,
                transactionId = $"testlab.authority.redelegate.{context.RunId}"
            });

            bool valid = direct.Succeeded
                && duplicate.Duplicate
                && authorized.Succeeded
                && expired.Status == OrganizationAuthorizationStatus.MissingPermission
                && delegated.Succeeded
                && !redelegated.Succeeded
                && redelegated.Status == OrganizationAuthorizationStatus.InvalidDependency;

            return TestLabAssertions.True("step13-authority-delegation", "Create and delegate scoped authority", valid, $"Direct={direct.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate} Authorized={authorized.Status} Expired={expired.Status} Delegated={delegated.Status} Redelegated={redelegated.Status}");
        }

        private static TestLabAutomationStepResult AuthorityBranchScopeBoundaries(TestLabAutomationContext context)
        {
            bool hasAuthority = TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime authority, out string authorityFailure);
            bool hasOrganizations = TryGetRuntime(context, out OrganizationRuntime organizations, out string organizationFailure);
            bool hasMemberships = TryGetMembershipRuntime(context, out OrganizationMembershipRuntime memberships, out string membershipFailure);
            if (!hasAuthority || !hasOrganizations || !hasMemberships)
            {
                string details = authorityFailure.Length > 0 ? authorityFailure : organizationFailure.Length > 0 ? organizationFailure : membershipFailure;
                return TestLabAssertions.Fail("step13-authority-branch", "Evaluate parent and branch authority boundaries", "OrganizationAuthorityRuntime", "Present", "Missing", details);
            }

            string branchId = $"organization.testlab.branch.authority.{context.RunId}";
            organizations.CreateOrganization(new OrganizationCreateRequest
            {
                organizationId = branchId,
                organizationDefinitionId = PrototypeOrganizationDefinitionFactory.BranchDefinitionId,
                officialName = "Authority Branch",
                initialLifecycleState = OrganizationLifecycleState.Active,
                transactionId = $"testlab.authority.branch.create.{context.RunId}"
            });
            organizations.LinkOrganizations(new OrganizationLinkRequest
            {
                sourceOrganizationId = branchId,
                targetOrganizationId = "organization.prototype.guild",
                kind = OrganizationLinkKind.Parent,
                transactionId = $"testlab.authority.branch.link.{context.RunId}"
            });
            authority.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, organizations, memberships, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, organizations.Snapshots.Select(snapshot => snapshot.OrganizationId));
            string actorId = PrimaryAuthorityActorId(context);
            OrganizationMembershipOperationResult parentMembership = memberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.branch.parent.{context.RunId}", "organization.prototype.guild", actorId, PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.branch.parent.member.{context.RunId}", consent: true));
            AssignGuildMasterRank(memberships, parentMembership.Membership?.MembershipId, context.RunId, "branch-parent");
            OrganizationMembershipRequest branchRequest = MembershipRequest($"organization-membership.testlab.branch.member.{context.RunId}", branchId, "person.prototype.friend", PrototypeOrganizationMembershipDefinitionFactory.BranchMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.branch.member.{context.RunId}", consent: true);
            branchRequest.parentMembershipId = parentMembership.Membership?.MembershipId;
            branchRequest.branchOrganizationId = branchId;
            OrganizationMembershipOperationResult branchMembership = memberships.ApplyMembership(branchRequest);
            OrganizationMembershipOperationResult branchOffice = memberships.CreateOffice(OfficeRequest($"organization-office-record.testlab.branch.master.{context.RunId}", branchId, PrototypeOrganizationMembershipDefinitionFactory.BranchChapterMasterOfficeId, $"testlab.authority.branch.office.{context.RunId}"));
            OrganizationMembershipOperationResult branchAssignment = memberships.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.branch.master.{context.RunId}", branchOffice.Office?.OfficeId, branchMembership.Membership?.MembershipId, $"testlab.authority.branch.office.assign.{context.RunId}"));

            OrganizationAuthorizationResult parentOnBranch = authority.EvaluateAuthorization(AuthorityRequest(actorId, branchId, PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId, $"testlab.authority.parent.branch.{context.RunId}"));
            OrganizationAuthorizationResult branchOnBranch = authority.EvaluateAuthorization(AuthorityRequest("person.prototype.friend", branchId, PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId, $"testlab.authority.branch.branch.{context.RunId}"));
            OrganizationAuthorizationResult branchOnParent = authority.EvaluateAuthorization(AuthorityRequest("person.prototype.friend", "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId, $"testlab.authority.branch.parent.{context.RunId}"));

            bool valid = parentMembership.Succeeded
                && branchMembership.Succeeded
                && branchOffice.Succeeded
                && branchAssignment.Succeeded
                && parentOnBranch.Status == OrganizationAuthorizationStatus.MissingPermission
                && branchOnBranch.Succeeded
                && branchOnParent.Status == OrganizationAuthorizationStatus.MissingPermission;

            return TestLabAssertions.True("step13-authority-branch", "Evaluate parent and branch authority boundaries", valid, $"Parent={parentOnBranch.Status} Branch={branchOnBranch.Status} Reverse={branchOnParent.Status} Memberships={parentMembership.Status}/{branchMembership.Status} Office={branchOffice.Status}/{branchAssignment.Status}");
        }

        private static TestLabAutomationStepResult AuthorityJointApprovalAudits(TestLabAutomationContext context)
        {
            if (!TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime authority, out string failure))
            {
                return TestLabAssertions.Fail("step13-authority-approvals", "Authorize joint institutional action", "OrganizationAuthorityRuntime", "Present", "Missing", failure);
            }

            string actorId = PrimaryAuthorityActorId(context);
            CreateAuthorityGuildmaster(context, actorId, "approver-master");
            context.ScenarioContext.Runtimes.OrganizationMemberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.approver.mentor.{context.RunId}", "organization.prototype.guild", "person.prototype.mentor", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.approver.mentor.{context.RunId}", consent: true));
            context.ScenarioContext.Runtimes.OrganizationMemberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.approver.partner.{context.RunId}", "organization.prototype.guild", "person.prototype.partner", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.approver.partner.{context.RunId}", consent: true));
            OrganizationAuthorityOperationResult mentorGrant = GrantGuildmasterRole(authority, actorId, "person.prototype.mentor", context.RunId, "mentor");
            OrganizationAuthorityOperationResult partnerGrant = GrantGuildmasterRole(authority, actorId, "person.prototype.partner", context.RunId, "partner");
            string operationId = $"testlab.authority.operation.headquarters.{context.RunId}";
            OrganizationAuthorityOperationResult approvalOne = authority.RecordApproval(ApprovalRequest($"organization-authority-approval.testlab.one.{context.RunId}", operationId, "person.prototype.mentor"));
            OrganizationAuthorityOperationResult approvalTwo = authority.RecordApproval(ApprovalRequest($"organization-authority-approval.testlab.two.{context.RunId}", operationId, "person.prototype.partner"));
            OrganizationAuthorizationRequest deniedRequest = AuthorityRequest("person.prototype.friend", "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.ChangeHeadquartersActionId, operationId);
            deniedRequest.consumeApprovals = true;
            OrganizationAuthorizationRequest authorizedRequest = AuthorityRequest(actorId, "organization.prototype.guild", PrototypeOrganizationAuthorityDefinitionFactory.ChangeHeadquartersActionId, operationId);
            authorizedRequest.consumeApprovals = true;

            OrganizationAuthorizationResult denied = authority.EvaluateAuthorization(deniedRequest);
            OrganizationAuthorizationResult authorized = authority.EvaluateAuthorization(authorizedRequest);
            OrganizationAuthorityOperationResult audit = authority.RecordAuthorizationAudit(authorized, $"organization-authority-audit.testlab.headquarters.{context.RunId}", 120d);

            bool valid = mentorGrant.Succeeded
                && partnerGrant.Succeeded
                && approvalOne.Succeeded
                && approvalTwo.Succeeded
                && !denied.Succeeded
                && authorized.Succeeded
                && authorized.ApprovalIds.Count == 2
                && authority.Approvals.All(approval => approval.LifecycleState == OrganizationApprovalLifecycleState.Consumed)
                && audit.Succeeded
                && authority.Audits.Any(item => item.Status == OrganizationAuthorizationStatus.Authorized);

            return TestLabAssertions.True("step13-authority-approvals", "Authorize joint institutional action", valid, $"Grants={mentorGrant.Status}/{partnerGrant.Status} Approvals={approvalOne.Status}/{approvalTwo.Status} Denied={denied.Status} Authorized={authorized.Status} Audit={audit.Status}");
        }

        private static TestLabAutomationStepResult AuthorityPersistenceValidation(TestLabAutomationContext context)
        {
            bool hasAuthority = TryGetAuthorityRuntime(context, out OrganizationAuthorityRuntime authority, out string authorityFailure);
            bool hasOrganizations = TryGetRuntime(context, out OrganizationRuntime organizations, out string organizationFailure);
            bool hasMemberships = TryGetMembershipRuntime(context, out OrganizationMembershipRuntime memberships, out string membershipFailure);
            if (!hasAuthority || !hasOrganizations || !hasMemberships)
            {
                string details = authorityFailure.Length > 0 ? authorityFailure : organizationFailure.Length > 0 ? organizationFailure : membershipFailure;
                return TestLabAssertions.Fail("step13-authority-persistence", "Save, restore, and reject invalid authority state", "OrganizationAuthorityRuntime", "Present", "Missing", details);
            }

            OrganizationAuthorityOperationResult grant = authority.CreateDirectGrant(new OrganizationAuthorityGrantRequest
            {
                grantId = $"organization-authority-grant.testlab.persist.{context.RunId}",
                organizationId = "organization.prototype.guild",
                granteePersonId = "person.prototype.friend",
                grantorPersonId = PrimaryAuthorityActorId(context),
                permissionDefinitionIds = new[] { PrototypeOrganizationAuthorityDefinitionFactory.ViewRestrictedInformationPermissionId },
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                startWorldTime = 0d,
                transactionId = $"testlab.authority.persist.{context.RunId}"
            });
            OrganizationAuthorityRuntimeSaveData save = authority.CreateSaveData();
            OrganizationAuthorityRuntime restored = new OrganizationAuthorityRuntime();
            OrganizationAuthorityOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, organizations, memberships, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, organizations.Snapshots.Select(snapshot => snapshot.OrganizationId));
            OrganizationAuthorityRuntimeSaveData corrupt = save.Clone();
            corrupt.grants[0].permissionDefinitionIds = new[] { "organization-permission.missing" };
            bool rejected = !OrganizationAuthorityRuntime.ValidateSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, organizations, memberships, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, organizations.Snapshots.Select(snapshot => snapshot.OrganizationId), out string validationFailure);
            bool liveUnchanged = authority.TryGetGrant(grant.Grant?.GrantId, out OrganizationAuthoritySnapshot live)
                && live.Data.permissionDefinitionIds.Contains(PrototypeOrganizationAuthorityDefinitionFactory.ViewRestrictedInformationPermissionId)
                && authority.GrantCount == save.grants.Count;

            bool valid = grant.Succeeded
                && restore.Succeeded
                && restored.TryGetGrant(grant.Grant?.GrantId, out _)
                && rejected
                && liveUnchanged;

            return TestLabAssertions.True("step13-authority-persistence", "Save, restore, and reject invalid authority state", valid, $"Grant={grant.Status} Restore={restore.Status} Rejected={rejected}:{validationFailure} Live={liveUnchanged}");
        }

        private static TestLabAutomationStepResult ResourceRuntimeReadiness(TestLabAutomationContext context)
        {
            TestLabRuntimeBundle runtimes = context?.ScenarioContext?.Runtimes;
            OrganizationResourceTypeDefinition currencyType = null;
            bool resourceType = runtimes?.DefinitionRegistry?.TryGet(PrototypeOrganizationResourceDefinitionFactory.CurrencyResourceTypeId, out currencyType) == true;
            bool currency = TryResourceCurrency(context, out CurrencyDefinition currencyDefinition);
            bool valid = runtimes?.OrganizationResources?.IsReady == true
                && runtimes.Organizations != null
                && runtimes.OrganizationMemberships != null
                && runtimes.OrganizationAuthority != null
                && runtimes.Economy != null
                && runtimes.Properties != null
                && runtimes.Businesses != null
                && runtimes.ItemInstances != null
                && resourceType
                && currency
                && currencyType.Category == OrganizationResourceCategory.Currency;
            return TestLabAssertions.True("step13-resources-readiness", "Validate resource runtime dependencies", valid, $"Ready={runtimes?.OrganizationResources?.IsReady} Type={resourceType}:{currencyType?.Category} Currency={currencyDefinition?.Id} Dependencies={runtimes?.Organizations != null}/{runtimes?.OrganizationAuthority != null}/{runtimes?.Economy != null}/{runtimes?.Properties != null}/{runtimes?.Businesses != null}/{runtimes?.ItemInstances != null}");
        }

        private static TestLabAutomationStepResult ResourceTreasuryCreation(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 500L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            bool queried = prepared
                && resources.QueryTreasuries("organization.prototype.guild", activeOnly: true).Count == 1
                && resources.QueryAccounts("organization.prototype.guild").Count == 2
                && resources.GetBalance(ResourceOperatingAccountId(context), 10d)?.BalanceUnits == 500L
                && resources.GetBalance(ResourceReserveAccountId(context), 10d)?.BalanceUnits == 0L;
            return TestLabAssertions.True("step13-resources-treasury", "Create treasury and organization accounts", prepared && queried, $"Prepared={prepared}:{failure} Actor={actorId} Currency={currency?.Id} Treasuries={resources?.TreasuryCount} Accounts={resources?.AccountCount}");
        }

        private static TestLabAutomationStepResult ResourceTransferIdempotence(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 100L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-transfer", "Deposit and transfer authoritative funds", "ResourceFixture", "Prepared", "Failed", failure);
            string depositId = $"testlab.resources.deposit.{context.RunId}";
            OrganizationResourceOperationResult deposit = resources.DepositFunds(new OrganizationFinancialTransactionRequest
            {
                transactionId = depositId, organizationId = "organization.prototype.guild", destinationAccountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id,
                units = 50L, actorPersonId = actorId, relatedRecordId = $"revenue.testlab.{context.RunId}", purpose = "test revenue", worldTime = 10d
            });
            OrganizationFinancialTransactionRequest transferRequest = ResourceTransferRequest(context, currency.Id, actorId, 40L, $"testlab.resources.transfer.{context.RunId}", 11d);
            OrganizationResourceOperationResult transfer = resources.TransferFunds(transferRequest);
            OrganizationResourceOperationResult duplicate = resources.TransferFunds(transferRequest);
            EconomyRuntimeSaveData economy = context.ScenarioContext.Runtimes.Economy.CreateSaveData();
            EconomyLedgerEntryData[] ledger = economy.ledgerEntries.Where(entry => entry.transactionId == transferRequest.transactionId).ToArray();
            bool balanced = ledger.Where(entry => entry.kind == EconomyLedgerEntryKind.Debit).Sum(entry => entry.units) == ledger.Where(entry => entry.kind == EconomyLedgerEntryKind.Credit).Sum(entry => entry.units);
            bool valid = deposit.Succeeded && transfer.Succeeded && duplicate.Duplicate && balanced
                && resources.GetBalance(ResourceOperatingAccountId(context), 12d).BalanceUnits == 110L
                && resources.GetBalance(ResourceReserveAccountId(context), 12d).BalanceUnits == 40L
                && economy.transactions.Count(entry => entry.transactionId == transferRequest.transactionId) == 1;
            return TestLabAssertions.True("step13-resources-transfer", "Deposit and transfer authoritative funds", valid, $"Deposit={deposit.Code} Transfer={transfer.Code} Duplicate={duplicate.Code}/{duplicate.Duplicate} Ledger={ledger.Length}/{balanced} Balances={resources.GetBalance(ResourceOperatingAccountId(context), 12d)?.BalanceUnits}/{resources.GetBalance(ResourceReserveAccountId(context), 12d)?.BalanceUnits}");
        }

        private static TestLabAutomationStepResult ResourceAuthorityDenial(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 100L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-authority", "Enforce organization financial authority", "ResourceFixture", "Prepared", "Failed", failure);
            long before = resources.GetBalance(ResourceOperatingAccountId(context), 10d).BalanceUnits;
            OrganizationFinancialTransactionRequest deniedRequest = new OrganizationFinancialTransactionRequest
            {
                transactionId = $"testlab.resources.withdraw.denied.{context.RunId}", organizationId = "organization.prototype.guild", sourceAccountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id,
                units = 10L, actorPersonId = "person.prototype.friend", relatedRecordId = $"settlement.denied.{context.RunId}", purpose = "unauthorized", worldTime = 10d
            };
            OrganizationResourceOperationResult denied = resources.WithdrawFunds(deniedRequest);
            deniedRequest.transactionId = $"testlab.resources.withdraw.allowed.{context.RunId}";
            deniedRequest.actorPersonId = actorId;
            deniedRequest.relatedRecordId = $"settlement.allowed.{context.RunId}";
            OrganizationResourceOperationResult allowed = resources.WithdrawFunds(deniedRequest);
            long after = resources.GetBalance(ResourceOperatingAccountId(context), 10d).BalanceUnits;
            bool valid = !denied.Succeeded && denied.Code == OrganizationResourceOperationCode.Unauthorized && allowed.Succeeded && before == 100L && after == 90L;
            return TestLabAssertions.True("step13-resources-authority", "Enforce organization financial authority", valid, $"Denied={denied.Code} Allowed={allowed.Code} Balance={before}->{after}");
        }

        private static TestLabAutomationStepResult ResourceAllocations(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 1000L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-allocations", "Validate fund allocation boundaries", "ResourceFixture", "Prepared", "Failed", failure);
            string restrictionId = $"organization-restriction.testlab.{context.RunId}";
            OrganizationResourceOperationResult restriction = resources.AddFundRestriction(new OrganizationFundRestrictionRequest
            {
                transactionId = $"testlab.resources.restrict.{context.RunId}", restrictionId = restrictionId, organizationId = "organization.prototype.guild", accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id,
                units = 700L, allowedPurpose = "healing", sourceReferenceId = $"donation.testlab.{context.RunId}", actorPersonId = actorId, startWorldTime = 10d
            });
            OrganizationResourceOperationResult blocked = resources.TransferFunds(ResourceTransferRequest(context, currency.Id, actorId, 400L, $"testlab.resources.restricted.blocked.{context.RunId}", 11d));
            OrganizationFinancialTransactionRequest allowedRequest = ResourceTransferRequest(context, currency.Id, actorId, 400L, $"testlab.resources.restricted.allowed.{context.RunId}", 12d);
            allowedRequest.restrictionId = restrictionId;
            allowedRequest.purpose = "healing";
            OrganizationResourceOperationResult allowed = resources.TransferFunds(allowedRequest);
            string reservationId = $"organization-reservation.testlab.{context.RunId}";
            OrganizationResourceOperationResult reservation = resources.ReserveResource(new OrganizationReservationRequest
            {
                transactionId = $"testlab.resources.reserve.{context.RunId}", reservationId = reservationId, organizationId = "organization.prototype.guild", accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id,
                amountUnits = 100L, category = OrganizationReservationCategory.Contract, purpose = "contract", requestingOperationId = $"contract.testlab.{context.RunId}", actorPersonId = actorId, startWorldTime = 13d, expirationWorldTime = 30d
            });
            string budgetId = $"organization-budget.testlab.{context.RunId}";
            OrganizationResourceOperationResult budget = resources.CreateBudget(new OrganizationBudgetRequest
            {
                transactionId = $"testlab.resources.budget.{context.RunId}", budgetId = budgetId, organizationId = "organization.prototype.guild", treasuryId = ResourceTreasuryId(context), accountId = ResourceOperatingAccountId(context),
                category = OrganizationBudgetCategory.Procurement, enforcementPolicy = OrganizationBudgetEnforcementPolicy.HardMaximum, currencyDefinitionId = currency.Id, authorizedUnits = 50L, purpose = "procurement", actorPersonId = actorId, startWorldTime = 13d
            });
            OrganizationFinancialTransactionRequest overBudgetRequest = ResourceTransferRequest(context, currency.Id, actorId, 51L, $"testlab.resources.budget.blocked.{context.RunId}", 14d);
            overBudgetRequest.budgetId = budgetId;
            overBudgetRequest.purpose = "procurement";
            OrganizationResourceOperationResult overBudget = resources.TransferFunds(overBudgetRequest);
            OrganizationAccountBalanceSnapshot balance = resources.GetBalance(ResourceOperatingAccountId(context), 14d);
            bool valid = restriction.Succeeded && blocked.Code == OrganizationResourceOperationCode.InsufficientFunds && allowed.Succeeded && reservation.Succeeded && budget.Succeeded && overBudget.Code == OrganizationResourceOperationCode.BudgetExceeded
                && balance.RestrictedUnits == 300L && balance.EncumberedUnits == 100L && balance.AvailableUnits == 200L;
            return TestLabAssertions.True("step13-resources-allocations", "Validate fund allocation boundaries", valid, $"Restriction={restriction.Code} Blocked={blocked.Code} Allowed={allowed.Code} Reservation={reservation.Code} Budget={budget.Code}/{overBudget.Code} Balance={balance?.BalanceUnits}/{balance?.AvailableUnits}/{balance?.RestrictedUnits}/{balance?.ReservedUnits}/{balance?.EncumberedUnits}");
        }

        private static TestLabAutomationStepResult ResourceInventoryCustody(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 0L, out OrganizationResourceRuntime resources, out _, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-custody", "Associate inventory and track item custody", "ResourceFixture", "Prepared", "Failed", failure);
            OrganizationResourceOperationResult inventory = resources.AssociateInventory(new OrganizationAssociationRequest
            {
                transactionId = $"testlab.resources.inventory.{context.RunId}", associationId = $"organization-inventory-association.testlab.{context.RunId}", organizationId = "organization.prototype.guild", resourceId = $"inventory.organization.testlab.{context.RunId}",
                category = (int)OrganizationInventoryCategory.Armory, actorPersonId = actorId, startWorldTime = 10d
            });
            ItemDefinition itemDefinition = context.ScenarioContext.Runtimes.DefinitionRegistry.DefinitionsById.Values.OfType<ItemDefinition>().FirstOrDefault();
            ItemInstanceOperationResult created = itemDefinition == null ? null : context.ScenarioContext.Runtimes.ItemInstances.CreateItem(itemDefinition, ownerPersonId: "person.prototype.friend", custodianPersonId: "person.prototype.friend", creationSourceId: context.RunId);
            string itemId = created?.Snapshot?.ItemInstanceId;
            OrganizationResourceOperationResult custody = resources.AssignCustody(new OrganizationCustodyRequest
            {
                transactionId = $"testlab.resources.custody.{context.RunId}", custodyId = $"organization-custody.testlab.{context.RunId}", organizationId = "organization.prototype.guild",
                asset = new OrganizationAssetReferenceData { kind = OrganizationAssetReferenceKind.ItemInstance, resourceId = itemId, definitionId = itemDefinition?.Id, worldId = context.ScenarioContext.Runtimes.WorldId },
                custodianPersonId = "person.prototype.student", actorPersonId = actorId, sourceInventoryId = $"inventory.organization.testlab.{context.RunId}", destinationInventoryId = "person.prototype.student", startWorldTime = 11d
            });
            OrganizationResourceOperationResult returned = resources.ReturnCustody($"organization-custody.testlab.{context.RunId}", $"testlab.resources.custody.return.{context.RunId}", actorId, 12d);
            bool unchanged = context.ScenarioContext.Runtimes.ItemInstances.TryGetSnapshot(itemId, out ItemInstanceSnapshot item) && item.OwnerPersonId == "person.prototype.friend" && item.CustodianPersonId == "person.prototype.friend";
            bool valid = inventory.Succeeded && created?.Succeeded == true && custody.Succeeded && returned.Succeeded && unchanged;
            return TestLabAssertions.True("step13-resources-custody", "Associate inventory and track item custody", valid, $"Inventory={inventory.Code} Item={created?.Status}:{itemId} Custody={custody.Code}/{returned.Code} OwnershipUnchanged={unchanged}");
        }

        private static TestLabAutomationStepResult ResourceReservation(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 200L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-reservation", "Reserve and release authoritative funds", "ResourceFixture", "Prepared", "Failed", failure);
            string reservationId = $"organization-reservation.testlab.explicit.{context.RunId}";
            OrganizationResourceOperationResult reserve = resources.ReserveResource(new OrganizationReservationRequest
            {
                transactionId = $"testlab.resources.reservation.explicit.{context.RunId}", reservationId = reservationId, organizationId = "organization.prototype.guild",
                accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, amountUnits = 75L, category = OrganizationReservationCategory.Contract,
                purpose = "contract", requestingOperationId = $"contract.testlab.{context.RunId}", actorPersonId = actorId, startWorldTime = 10d, expirationWorldTime = 20d
            });
            long reserved = resources.GetBalance(ResourceOperatingAccountId(context), 10d)?.EncumberedUnits ?? -1L;
            OrganizationResourceOperationResult release = resources.ReleaseReservation(reservationId, $"testlab.resources.reservation.release.{context.RunId}", actorId, PrototypeOrganizationAuthorityDefinitionFactory.ManageResourceReservationActionId, 12d);
            long released = resources.GetBalance(ResourceOperatingAccountId(context), 12d)?.EncumberedUnits ?? -1L;
            bool valid = reserve.Succeeded && reserved == 75L && release.Succeeded && released == 0L && resources.Reservations.Single().lifecycleState == OrganizationReservationLifecycleState.Released;
            return TestLabAssertions.True("step13-resources-reservation", "Reserve and release authoritative funds", valid, $"Reserve={reserve.Code}:{reserved} Release={release.Code}:{released}");
        }

        private static TestLabAutomationStepResult ResourceJointApproval(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 500L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-joint-approval", "Authorize a joint financial action", "ResourceFixture", "Prepared", "Failed", failure);
            OrganizationAuthorityRuntime authority = context.ScenarioContext.Runtimes.OrganizationAuthority;
            OrganizationAuthorityOperationResult mentor = GrantGuildmasterRole(authority, actorId, "person.prototype.mentor", context.RunId, "resource-mentor");
            OrganizationAuthorityOperationResult partner = GrantGuildmasterRole(authority, actorId, "person.prototype.partner", context.RunId, "resource-partner");
            string operationId = $"testlab.resources.joint.transfer.{context.RunId}";
            Func<string, string, OrganizationApprovalRequest> approval = (id, approver) => new OrganizationApprovalRequest
            {
                approvalId = id, operationId = operationId, organizationId = "organization.prototype.guild", actionDefinitionId = PrototypeOrganizationAuthorityDefinitionFactory.LargeOrganizationTransferActionId,
                approverPersonId = approver, scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"), approvedWorldTime = 10d, transactionId = $"tx.{id}"
            };
            OrganizationAuthorityOperationResult first = authority.RecordApproval(approval($"organization-approval.resources.first.{context.RunId}", "person.prototype.mentor"));
            OrganizationAuthorityOperationResult second = authority.RecordApproval(approval($"organization-approval.resources.second.{context.RunId}", "person.prototype.partner"));
            OrganizationFinancialTransactionRequest request = ResourceTransferRequest(context, currency.Id, actorId, 100L, operationId, 11d);
            request.actionDefinitionId = PrototypeOrganizationAuthorityDefinitionFactory.LargeOrganizationTransferActionId;
            request.approvalPersonIds = new[] { "person.prototype.mentor", "person.prototype.partner" };
            OrganizationResourceOperationResult transfer = resources.TransferFunds(request);
            bool consumed = authority.Approvals.Where(item => item.Data.operationId == operationId).All(item => item.LifecycleState == OrganizationApprovalLifecycleState.Consumed);
            bool valid = mentor.Succeeded && partner.Succeeded && first.Succeeded && second.Succeeded && transfer.Succeeded && consumed;
            return TestLabAssertions.True("step13-resources-joint-approval", "Authorize a joint financial action", valid, $"Grants={mentor.Status}/{partner.Status} Approvals={first.Status}/{second.Status} Transfer={transfer.Code} Consumed={consumed}");
        }

        private static TestLabAutomationStepResult ResourceRevenueRouting(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 400L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-revenue", "Route business revenue through Economy accounts", "ResourceFixture", "Prepared", "Failed", failure);
            string sourceId = $"business-revenue.testlab.{context.RunId}";
            OrganizationResourceOperationResult rule = resources.CreateRevenueRoutingRule(new OrganizationRevenueRoutingRequest
            {
                transactionId = $"testlab.resources.route.rule.{context.RunId}", routingRuleId = $"organization-routing.testlab.{context.RunId}", organizationId = "organization.prototype.guild",
                revenueSourceId = sourceId, destinationAccountId = ResourceReserveAccountId(context), percentageBasisPoints = 2500L, priority = 10,
                purpose = "institutional reserve", actorPersonId = actorId, startWorldTime = 10d
            });
            OrganizationResourceOperationResult routed = resources.ApplyRevenueRouting(new OrganizationRevenueRoutingExecutionRequest
            {
                transactionId = $"testlab.resources.route.execute.{context.RunId}", organizationId = "organization.prototype.guild", revenueSourceId = sourceId,
                sourceAccountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, grossUnits = 200L, actorPersonId = actorId, worldTime = 11d
            });
            bool valid = rule.Succeeded && routed.Succeeded && resources.GetBalance(ResourceOperatingAccountId(context), 11d)?.BalanceUnits == 350L && resources.GetBalance(ResourceReserveAccountId(context), 11d)?.BalanceUnits == 50L;
            return TestLabAssertions.True("step13-resources-revenue", "Route business revenue through Economy accounts", valid, $"Rule={rule.Code} Route={routed.Code} Balances={resources.GetBalance(ResourceOperatingAccountId(context), 11d)?.BalanceUnits}/{resources.GetBalance(ResourceReserveAccountId(context), 11d)?.BalanceUnits}");
        }

        private static TestLabAutomationStepResult ResourcePayrollFunding(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 300L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-payroll", "Expose payroll funding without duplicating payroll state", "ResourceFixture", "Prepared", "Failed", failure);
            OrganizationResourceOperationResult reservation = resources.ReserveResource(new OrganizationReservationRequest
            {
                transactionId = $"testlab.resources.payroll.reserve.{context.RunId}", reservationId = $"organization-reservation.payroll.{context.RunId}", organizationId = "organization.prototype.guild",
                accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, amountUnits = 80L, category = OrganizationReservationCategory.Payroll,
                purpose = "payroll", requestingOperationId = $"payroll-run.testlab.{context.RunId}", actorPersonId = actorId, startWorldTime = 10d
            });
            OrganizationAccountBalanceSnapshot balance = resources.GetBalance(ResourceOperatingAccountId(context), 10d);
            bool valid = context.ScenarioContext.Runtimes.Payroll != null && reservation.Succeeded && balance.ReservedUnits == 80L && resources.QueryLiabilities("organization.prototype.guild").Count == 0;
            return TestLabAssertions.True("step13-resources-payroll", "Expose payroll funding without duplicating payroll state", valid, $"PayrollReady={context.ScenarioContext.Runtimes.Payroll != null} Reservation={reservation.Code} Reserved={balance?.ReservedUnits} DelegatedLiabilities={resources.QueryLiabilities("organization.prototype.guild").Count}");
        }

        private static TestLabAutomationStepResult ResourceBranchFinances(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 150L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-branch", "Query branch and parent finances", "ResourceFixture", "Prepared", "Failed", failure);
            OrganizationConsolidatedResourceSnapshot view = resources.GetConsolidatedView("organization.prototype.guild", 10d);
            bool separate = view.AccountBalances.Select(item => item.Account.accountId).Distinct(StringComparer.Ordinal).Count() == view.AccountBalances.Count;
            bool valid = separate && view.OrganizationIds.Contains("organization.prototype.guild") && view.Total(currency.Id) == 150L && resources.QueryAccounts("organization.prototype.guild").Count == 2;
            return TestLabAssertions.True("step13-resources-branch", "Query branch and parent finances", valid, $"Organizations=[{string.Join(",", view.OrganizationIds)}] Accounts={view.AccountBalances.Count} Separate={separate} Total={view.Total(currency.Id)} Actor={actorId}");
        }

        private static TestLabAutomationStepResult ResourceDissolutionBoundary(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 100L, out OrganizationResourceRuntime resources, out _, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-dissolution", "Execute explicit dissolution resource plan", "ResourceFixture", "Prepared", "Failed", failure);
            string planId = $"organization-dissolution-plan.testlab.{context.RunId}";
            OrganizationResourceOperationResult create = resources.CreateDissolutionResourcePlan(new OrganizationDissolutionResourcePlanRequest
            {
                transactionId = $"testlab.resources.dissolution.create.{context.RunId}", planId = planId, organizationId = "organization.prototype.guild",
                accountIdsToFreeze = new[] { ResourceOperatingAccountId(context), ResourceReserveAccountId(context) }, preservedObligationIds = new[] { $"obligation.unresolved.{context.RunId}" }, actorPersonId = actorId, worldTime = 20d
            });
            OrganizationResourceOperationResult execute = resources.ExecuteDissolutionResourcePlan(planId, $"testlab.resources.dissolution.execute.{context.RunId}", actorId, Array.Empty<string>(), 21d);
            bool valid = create.Succeeded && execute.Succeeded && resources.Accounts.All(item => item.lifecycleState == OrganizationAccountLifecycleState.Frozen)
                && resources.DissolutionPlans.Single().assetInstructions.Length == 0 && resources.DissolutionPlans.Single().preservedObligationIds.Length == 1;
            return TestLabAssertions.True("step13-resources-dissolution", "Execute explicit dissolution resource plan", valid, $"Create={create.Code} Execute={execute.Code} States=[{string.Join(",", resources.Accounts.Select(item => item.lifecycleState))}] Preserved={resources.DissolutionPlans.SingleOrDefault()?.preservedObligationIds.Length}");
        }

        private static TestLabAutomationStepResult ResourcePropertyBusinessAssociations(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 0L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-assets", "Associate Step 11 property and business records", "ResourceFixture", "Prepared", "Failed", failure);
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            PropertyDefinition propertyDefinition = ScriptableObject.CreateInstance<PropertyDefinition>();
            propertyDefinition.Initialize($"property.testlab.organization-definition.{context.RunId}", "Test Lab Organization Property", PropertyCategory.CommercialBuilding);
            propertyDefinition.SetPolicies(Array.Empty<PropertyCategory>(), new[] { PropertyOwnershipModel.Sole, PropertyOwnershipModel.SharedFractional }, new[] { PropertyUseCategory.Commercial }, currency.Id);
            BusinessDefinition businessDefinition = ScriptableObject.CreateInstance<BusinessDefinition>();
            businessDefinition.Initialize($"business.testlab.organization-definition.{context.RunId}", "Test Lab Organization Business", BusinessCategory.MerchantShop);
            DefinitionRegistry extendedRegistry = new DefinitionRegistry(runtimes.DefinitionRegistry.DefinitionsById.Values.Concat(new IGameDefinition[] { propertyDefinition, businessDefinition }));
            runtimes.Properties.Configure(extendedRegistry, runtimes.WorldId);
            runtimes.Businesses.Configure(extendedRegistry, runtimes.WorldId);
            runtimes.Economy.Configure(extendedRegistry, runtimes.WorldId);
            resources.Configure(extendedRegistry, runtimes.Organizations, runtimes.OrganizationAuthority, runtimes.Economy, runtimes.WorldId, runtimes.Properties, runtimes.Businesses, runtimes.ItemInstances);
            string propertyId = $"property.testlab.organization.{context.RunId}";
            PropertyOperationResult property = runtimes.Properties.RegisterProperty(new PropertyInstanceData { propertyId = propertyId, propertyDefinitionId = propertyDefinition.Id, displayName = "Test Lab Guild Property", spatialReferenceId = $"place.testlab.{context.RunId}", currentUses = new[] { PropertyUseCategory.Commercial }, creationWorldTime = 1d });
            string propertyOwnershipId = $"property-ownership.testlab.organization.{context.RunId}";
            PropertyOperationResult propertyOwner = property?.Succeeded == true ? runtimes.Properties.CreateOwnership(new PropertyOwnershipInterestData
            {
                ownershipInterestId = propertyOwnershipId, propertyId = propertyId, owner = new PropertySubjectReferenceData { kind = PropertySubjectKind.Organization, subjectId = "organization.prototype.guild" }, ownershipModel = PropertyOwnershipModel.Sole,
                ownershipShare = PropertyShareData.Full(), votingShare = PropertyShareData.Full(), economicBenefitShare = PropertyShareData.Full(), effectiveStartWorldTime = 1d
            }, 1d) : null;
            OrganizationResourceOperationResult propertyAssociation = resources.AssociateProperty(new OrganizationAssociationRequest
            {
                transactionId = $"testlab.resources.property.{context.RunId}", associationId = $"organization-property-association.testlab.{context.RunId}", organizationId = "organization.prototype.guild", resourceId = propertyId,
                sourceRecordId = propertyOwnershipId, category = (int)OrganizationPropertyAssociationCategory.Owner, actorPersonId = actorId, startWorldTime = 10d
            });

            string businessId = $"business.testlab.organization.{context.RunId}";
            BusinessOperationResult business = runtimes.Businesses.CreateBusiness(new BusinessInstanceData
            {
                businessId = businessId, businessDefinitionId = businessDefinition.Id, displayName = "Test Lab Guild Business", linkedOrganizationId = "organization.prototype.guild", founderSubjectIds = new[] { actorId }, operatingCurrencyIds = new[] { currency.Id }, state = BusinessState.Active, createdWorldTime = 1d
            });
            string businessOwnershipId = $"business-ownership.testlab.organization.{context.RunId}";
            BusinessOperationResult businessOwner = business?.Succeeded == true ? runtimes.Businesses.AddOwnership(new BusinessOwnershipRecordData
            {
                ownershipRecordId = businessOwnershipId, businessId = businessId, owner = new BusinessSubjectReferenceData { kind = BusinessOwnerSubjectKind.Organization, subjectId = "organization.prototype.guild" }, category = BusinessOwnershipCategory.SoleOwner,
                economicShare = new BusinessRationalData { numerator = 10000L, denominator = 10000L }, votingShare = new BusinessRationalData { numerator = 10000L, denominator = 10000L }, effectiveStartWorldTime = 1d
            }, 1d) : null;
            OrganizationResourceOperationResult businessAssociation = resources.AssociateBusiness(new OrganizationAssociationRequest
            {
                transactionId = $"testlab.resources.business.{context.RunId}", associationId = $"organization-business-association.testlab.{context.RunId}", organizationId = "organization.prototype.guild", resourceId = businessId,
                sourceRecordId = businessOwnershipId, category = (int)OrganizationBusinessAssociationCategory.Owner, shareBasisPoints = 10000L, actorPersonId = actorId, startWorldTime = 10d
            });
            OrganizationResourceOperationResult fabricated = resources.AssociateProperty(new OrganizationAssociationRequest
            {
                transactionId = $"testlab.resources.property.fabricated.{context.RunId}", associationId = $"organization-property-association.testlab.fabricated.{context.RunId}", organizationId = "organization.prototype.guild", resourceId = propertyId,
                sourceRecordId = "property-ownership.missing", category = (int)OrganizationPropertyAssociationCategory.Owner, actorPersonId = actorId, startWorldTime = 10d
            });
            bool valid = property?.Succeeded == true && propertyOwner?.Succeeded == true && propertyAssociation.Succeeded && business?.Succeeded == true && businessOwner?.Succeeded == true && businessAssociation.Succeeded && !fabricated.Succeeded;
            return TestLabAssertions.True("step13-resources-assets", "Associate Step 11 property and business records", valid, $"Property={property?.Code}/{propertyOwner?.Code}/{propertyAssociation.Code} Business={business?.Code}/{businessOwner?.Code}/{businessAssociation.Code} Fabricated={fabricated.Code}");
        }

        private static TestLabAutomationStepResult ResourceLifecycleBoundaries(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 100L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-lifecycle", "Freeze and reactivate an organization account", "ResourceFixture", "Prepared", "Failed", failure);
            OrganizationAccountLifecycleRequest request = new OrganizationAccountLifecycleRequest { transactionId = $"testlab.resources.freeze.{context.RunId}", accountId = ResourceOperatingAccountId(context), targetState = OrganizationAccountLifecycleState.Frozen, actorPersonId = actorId, worldTime = 10d };
            OrganizationResourceOperationResult frozen = resources.ChangeAccountLifecycle(request);
            OrganizationResourceOperationResult blocked = resources.TransferFunds(ResourceTransferRequest(context, currency.Id, actorId, 10L, $"testlab.resources.freeze.blocked.{context.RunId}", 11d));
            request.transactionId = $"testlab.resources.reactivate.{context.RunId}";
            request.targetState = OrganizationAccountLifecycleState.Active;
            request.worldTime = 12d;
            OrganizationResourceOperationResult reactivated = resources.ChangeAccountLifecycle(request);
            bool economyActive = context.ScenarioContext.Runtimes.Economy.TryGetAccount(ResourceOperatingEconomyAccountId(context), out EconomyAccountSnapshot account) && account.Data.state == EconomyAccountState.Active;
            bool valid = frozen.Succeeded && blocked.Code == OrganizationResourceOperationCode.AccountFrozen && reactivated.Succeeded && economyActive && resources.GetBalance(ResourceOperatingAccountId(context), 12d).BalanceUnits == 100L;
            return TestLabAssertions.True("step13-resources-lifecycle", "Freeze and reactivate an organization account", valid, $"Frozen={frozen.Code} Blocked={blocked.Code} Reactivated={reactivated.Code} EconomyActive={economyActive}");
        }

        private static TestLabAutomationStepResult ResourceReconciliationProjection(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 250L, out OrganizationResourceRuntime resources, out _, out _, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-reconcile", "Reconcile and project resource state", "ResourceFixture", "Prepared", "Failed", failure);
            long revision = resources.Revision;
            OrganizationReconciliationResult first = resources.Reconcile("organization.prototype.guild", 10d);
            OrganizationReconciliationResult second = resources.Reconcile("organization.prototype.guild", 10d);
            OrganizationResourceProjection redacted = resources.ProjectAccount(ResourceOperatingAccountId(context), OrganizationResourceProjectionAccess.Redacted, 10d);
            OrganizationResourceProjection full = resources.ProjectAccount(ResourceOperatingAccountId(context), OrganizationResourceProjectionAccess.Full, 10d);
            bool valid = first.IsReconciled && second.IsReconciled && first.Discrepancies.Count == second.Discrepancies.Count && redacted.Redacted && redacted.Balance.BalanceUnits == 0L && full.Balance.BalanceUnits == 250L && resources.Revision == revision;
            return TestLabAssertions.True("step13-resources-reconcile", "Reconcile and project resource state", valid, $"Reconciled={first.IsReconciled}/{second.IsReconciled} Diagnostics={first.Discrepancies.Count}/{second.Discrepancies.Count} Redacted={redacted.Access}:{redacted.Balance?.BalanceUnits} Full={full.Access}:{full.Balance?.BalanceUnits} Revision={revision}->{resources.Revision}");
        }

        private static TestLabAutomationStepResult ResourcePersistenceValidation(TestLabAutomationContext context)
        {
            bool prepared = PrepareResourceAccounts(context, 300L, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure);
            if (!prepared) return TestLabAssertions.Fail("step13-resources-persistence", "Save, restore, and reject resource graph drift", "ResourceFixture", "Prepared", "Failed", failure);
            TestLabRuntimeBundle runtimes = context.ScenarioContext.Runtimes;
            resources.AddFundRestriction(new OrganizationFundRestrictionRequest { transactionId = $"testlab.resources.persist.restriction.{context.RunId}", restrictionId = $"restriction.persist.{context.RunId}", organizationId = "organization.prototype.guild", accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, units = 25L, allowedPurpose = "preserved", actorPersonId = actorId, startWorldTime = 5d });
            resources.CreateBudget(new OrganizationBudgetRequest { transactionId = $"testlab.resources.persist.budget.{context.RunId}", budgetId = $"budget.persist.{context.RunId}", organizationId = "organization.prototype.guild", treasuryId = ResourceTreasuryId(context), accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, authorizedUnits = 50L, purpose = "preserved", actorPersonId = actorId, startWorldTime = 5d });
            resources.ReserveResource(new OrganizationReservationRequest { transactionId = $"testlab.resources.persist.reservation.{context.RunId}", reservationId = $"reservation.persist.{context.RunId}", organizationId = "organization.prototype.guild", accountId = ResourceOperatingAccountId(context), currencyDefinitionId = currency.Id, amountUnits = 20L, category = OrganizationReservationCategory.Contract, actorPersonId = actorId, startWorldTime = 5d });
            resources.CreateRevenueRoutingRule(new OrganizationRevenueRoutingRequest { transactionId = $"testlab.resources.persist.routing.{context.RunId}", routingRuleId = $"routing.persist.{context.RunId}", organizationId = "organization.prototype.guild", revenueSourceId = $"revenue.persist.{context.RunId}", destinationAccountId = ResourceReserveAccountId(context), fixedUnits = 1L, actorPersonId = actorId, startWorldTime = 5d });
            resources.CreateDissolutionResourcePlan(new OrganizationDissolutionResourcePlanRequest { transactionId = $"testlab.resources.persist.dissolution.{context.RunId}", planId = $"dissolution.persist.{context.RunId}", organizationId = "organization.prototype.guild", accountIdsToFreeze = new[] { ResourceOperatingAccountId(context) }, actorPersonId = actorId, worldTime = 5d });
            OrganizationResourceRuntimeSaveData save = resources.CreateSaveData();
            long economyRevision = runtimes.Economy.Revision;
            OrganizationResourceRuntime restored = new OrganizationResourceRuntime();
            OrganizationResourceOperationResult restore = restored.RestoreFromSaveData(save, runtimes.DefinitionRegistry, runtimes.Organizations, runtimes.OrganizationAuthority, runtimes.Economy, runtimes.WorldId, runtimes.Properties, runtimes.Businesses, runtimes.ItemInstances, restoring: true, contractRuntime: runtimes.Contracts, payrollRuntime: runtimes.Payroll);
            OrganizationResourceRuntimeSaveData corrupt = save.Clone();
            corrupt.accounts[0].economyAccountId = "economy-account.missing";
            bool rejected = !OrganizationResourceRuntime.ValidateSaveData(corrupt, runtimes.DefinitionRegistry, runtimes.Organizations, runtimes.Economy, runtimes.WorldId, runtimes.Properties, runtimes.Businesses, runtimes.ItemInstances, out string validationFailure);
            bool allRecords = restored.RestrictionCount == 1 && restored.BudgetCount == 1 && restored.ReservationCount == 1 && restored.RevenueRoutingRules.Count == 1 && restored.DissolutionPlans.Count == 1;
            bool valid = restore.Succeeded && restored.AccountCount == 2 && allRecords && restored.GetBalance(ResourceOperatingAccountId(context), 10d)?.BalanceUnits == 300L && runtimes.Economy.Revision == economyRevision && rejected && resources.AccountCount == 2;
            return TestLabAssertions.True("step13-resources-persistence", "Save, restore, and reject resource graph drift", valid, $"Restore={restore.Code} Accounts={resources.AccountCount}/{restored.AccountCount} Records={restored.RestrictionCount}/{restored.BudgetCount}/{restored.ReservationCount}/{restored.RevenueRoutingRules.Count}/{restored.DissolutionPlans.Count} Balance={restored.GetBalance(ResourceOperatingAccountId(context), 10d)?.BalanceUnits} EconomyRevision={economyRevision}->{runtimes.Economy.Revision} Rejected={rejected}:{validationFailure}");
        }

        private static TestLabAutomationStepResult DecisionRuntimeReadiness(TestLabAutomationContext context)
        {
            OrganizationDecisionRuntime decisions = context?.ScenarioContext?.Runtimes?.OrganizationDecisions;
            DefinitionRegistry registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            bool definitions = registry != null
                && registry.TryGet(PrototypeOrganizationDecisionDefinitionFactory.RecruitmentGoalId, out OrganizationGoalDefinition goal)
                && registry.TryGet(PrototypeOrganizationDecisionDefinitionFactory.ConfidentialityPolicyId, out OrganizationPolicyDefinition policy)
                && registry.TryGet(PrototypeOrganizationDecisionDefinitionFactory.SimpleMajorityProcedureId, out OrganizationDecisionProcedureDefinition procedure)
                && registry.TryGet(PrototypeOrganizationDecisionDefinitionFactory.EstablishGoalProposalId, out OrganizationProposalDefinition proposal)
                && goal.ProgressSourceKind == OrganizationGoalProgressSourceKind.ActiveMembershipCount
                && policy.ParameterSchema.Count >= 2
                && procedure.VoterEligibility == OrganizationVoterEligibilityKind.ActiveMembers
                && proposal.SupportedExecutionOperations.Contains(OrganizationDecisionExecutionOperationKind.EstablishGoal);
            bool valid = decisions?.IsReady == true && definitions;
            return TestLabAssertions.True("step13-decisions-readiness", "Validate organization decision definitions", valid, $"Ready={decisions?.IsReady} Definitions={definitions} Counts={decisions?.GoalCount}/{decisions?.PolicyCount}/{decisions?.ProposalCount}");
        }

        private static TestLabAutomationStepResult DecisionGoalsPolicies(TestLabAutomationContext context)
        {
            if (!PrepareDecisionFixture(context, 100L, out OrganizationDecisionRuntime decisions, out _, out _, out string actorId, out string failure))
            {
                return TestLabAssertions.Fail("step13-decisions-goals-policies", "Create goals and resolve policies", "DecisionFixture", "Ready", "Missing", failure);
            }

            OrganizationDecisionOperationResult goal = decisions.CreateGoal(new OrganizationGoalRequest
            {
                transactionId = $"testlab.decisions.goal.{context.RunId}",
                goalId = $"organization-goal-record.testlab.recruit.{context.RunId}",
                organizationId = "organization.prototype.guild",
                goalDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.RecruitmentGoalId,
                targetValue = 3L,
                priority = 25,
                actorPersonId = actorId,
                worldTime = 10d
            });
            OrganizationDecisionOperationResult policy = decisions.CreatePolicy(PolicyRequest(context, $"organization-policy-record.testlab.confidentiality.{context.RunId}", actorId, 12d));
            OrganizationDecisionOperationResult conflict = decisions.CreatePolicy(PolicyRequest(context, $"organization-policy-record.testlab.confidentiality.conflict.{context.RunId}", actorId, 13d));
            OrganizationPolicyRequest replacementRequest = PolicyRequest(context, $"organization-policy-record.testlab.confidentiality.replacement.{context.RunId}", actorId, 14d);
            replacementRequest.supersedesPolicyId = policy.Policy?.policyId;
            replacementRequest.parameters = new[] { PolicyParam("visibility", OrganizationPolicyParameterType.EnumValue, stringValue: OrganizationVisibility.Secret.ToString()), PolicyParam("reshare_allowed", OrganizationPolicyParameterType.Boolean, boolValue: false) };
            OrganizationDecisionOperationResult replacement = decisions.CreatePolicy(replacementRequest);
            OrganizationPolicyResolutionResult resolved = decisions.ResolvePolicies(new OrganizationPolicyQuery
            {
                organizationId = "organization.prototype.guild",
                policyDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.ConfidentialityPolicyId,
                scope = OrganizationPolicyScopeData.EntireOrganization("organization.prototype.guild"),
                worldTime = 15d
            });
            OrganizationPolicyRecordData supersededPolicy = decisions.Policies.FirstOrDefault(item => item.policyId == policy.Policy?.policyId);
            bool valid = goal.Succeeded
                && goal.Goal?.lifecycleState == OrganizationGoalLifecycleState.Completed
                && policy.Succeeded
                && conflict.Code == OrganizationDecisionOperationCode.InvalidConflict
                && replacement.Succeeded
                && supersededPolicy?.lifecycleState == OrganizationPolicyLifecycleState.Superseded
                && supersededPolicy?.supersededByPolicyId == replacement.Policy?.policyId
                && resolved.EffectivePolicy?.policyId == replacement.Policy?.policyId
                && resolved.SuppressedPolicies.Count == 0;
            return TestLabAssertions.True("step13-decisions-goals-policies", "Create goals and resolve policies", valid, $"Goal={goal.Code}:{goal.Goal?.currentValue}/{goal.Goal?.targetValue}:{goal.Goal?.lifecycleState} Policy={policy.Code} Conflict={conflict.Code} Replacement={replacement.Code} Superseded={supersededPolicy?.lifecycleState}:{supersededPolicy?.supersededByPolicyId} Effective={resolved.EffectivePolicy?.policyId} Suppressed={resolved.SuppressedPolicies.Count}");
        }

        private static TestLabAutomationStepResult DecisionProposalVoteResolution(TestLabAutomationContext context)
        {
            if (!PrepareDecisionFixture(context, 100L, out OrganizationDecisionRuntime decisions, out _, out _, out string actorId, out string failure))
            {
                return TestLabAssertions.Fail("step13-decisions-proposal", "Submit, amend, vote, and close proposal", "DecisionFixture", "Ready", "Missing", failure);
            }

            string proposalId = $"organization-proposal.testlab.goal.{context.RunId}";
            OrganizationDecisionOperationResult submit = decisions.SubmitProposal(new OrganizationProposalRequest
            {
                transactionId = $"testlab.decisions.proposal.submit.{context.RunId}",
                proposalId = proposalId,
                organizationId = "organization.prototype.guild",
                proposalDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.EstablishGoalProposalId,
                title = "Create recruitment goal",
                proposerPersonId = actorId,
                requestedExecutionOperations = new[] { GoalOperation(context, "initial", $"organization-goal-record.testlab.execution.initial.{context.RunId}", 5L) },
                submittedWorldTime = 10d,
                votingStartWorldTime = 10d,
                votingEndWorldTime = 20d
            });
            OrganizationDecisionOperationResult amend = decisions.SubmitAmendment(new OrganizationAmendmentRequest
            {
                transactionId = $"testlab.decisions.proposal.amend.{context.RunId}",
                amendmentId = $"organization-amendment.testlab.goal.{context.RunId}",
                proposalId = proposalId,
                proposerPersonId = "person.prototype.friend",
                summary = "Reduce target to current membership.",
                replacementExecutionOperations = new[] { GoalOperation(context, "amended", $"organization-goal-record.testlab.execution.amended.{context.RunId}", 3L) },
                worldTime = 11d
            });
            OrganizationDecisionOperationResult voteOne = decisions.CastVote(VoteRequest(context, proposalId, actorId, "actor", OrganizationVoteChoice.Approve));
            OrganizationDecisionOperationResult voteTwo = decisions.CastVote(VoteRequest(context, proposalId, "person.prototype.friend", "friend", OrganizationVoteChoice.Approve));
            OrganizationDecisionOperationResult voteReplacement = decisions.CastVote(VoteRequest(context, proposalId, "person.prototype.friend", "friend-replace", OrganizationVoteChoice.Reject));
            OrganizationDecisionTallySnapshot tally = decisions.TallyProposal(proposalId);
            OrganizationDecisionOperationResult close = decisions.CloseVote(new OrganizationCloseVoteRequest
            {
                transactionId = $"testlab.decisions.proposal.close.{context.RunId}",
                proposalId = proposalId,
                resolutionId = $"organization-resolution.testlab.goal.{context.RunId}",
                actorPersonId = actorId,
                worldTime = 21d
            });
            bool valid = submit.Succeeded
                && amend.Succeeded
                && voteOne.Succeeded
                && voteTwo.Succeeded
                && voteReplacement.Succeeded
                && tally.ParticipatingCount == 2
                && tally.ApproveWeight == 1L
                && tally.RejectWeight == 1L
                && close.Succeeded
                && close.Resolution?.outcome == OrganizationResolutionOutcome.Tied;
            return TestLabAssertions.True("step13-decisions-proposal", "Submit, amend, vote, and close proposal", valid, $"Submit={submit.Code} Amend={amend.Code} Votes={voteOne.Code}/{voteTwo.Code}/{voteReplacement.Code} Tally={tally.ApproveWeight}/{tally.RejectWeight}/{tally.ParticipatingCount} Close={close.Code}:{close.Resolution?.outcome}");
        }

        private static TestLabAutomationStepResult DecisionExecutionPersistenceProjection(TestLabAutomationContext context)
        {
            if (!PrepareDecisionFixture(context, 250L, out OrganizationDecisionRuntime decisions, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure))
            {
                return TestLabAssertions.Fail("step13-decisions-execution", "Execute resolution and validate persistence", "DecisionFixture", "Ready", "Missing", failure);
            }

            string proposalId = $"organization-proposal.testlab.execute.{context.RunId}";
            string resolutionId = $"organization-resolution.testlab.execute.{context.RunId}";
            OrganizationDecisionOperationResult submit = decisions.SubmitProposal(new OrganizationProposalRequest
            {
                transactionId = $"testlab.decisions.execute.submit.{context.RunId}",
                proposalId = proposalId,
                organizationId = "organization.prototype.guild",
                proposalDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.ApproveBudgetProposalId,
                title = "Approve training budget",
                proposerPersonId = actorId,
                requestedExecutionOperations = new[] { BudgetOperation(context, currency.Id, 40L) },
                submittedWorldTime = 10d,
                votingStartWorldTime = 10d,
                votingEndWorldTime = 20d
            });
            OrganizationDecisionOperationResult voteActor = decisions.CastVote(VoteRequest(context, proposalId, actorId, "execute-actor", OrganizationVoteChoice.Approve));
            OrganizationDecisionOperationResult voteFriend = decisions.CastVote(VoteRequest(context, proposalId, "person.prototype.friend", "execute-friend", OrganizationVoteChoice.Approve));
            OrganizationDecisionOperationResult close = decisions.CloseVote(new OrganizationCloseVoteRequest { transactionId = $"testlab.decisions.execute.close.{context.RunId}", proposalId = proposalId, resolutionId = resolutionId, actorPersonId = actorId, worldTime = 21d });
            OrganizationDecisionOperationResult preview = decisions.ExecuteResolution(new OrganizationDecisionExecutionRequest { transactionId = $"testlab.decisions.execute.preview.{context.RunId}", executionId = $"organization-execution.testlab.preview.{context.RunId}", resolutionId = resolutionId, actorPersonId = actorId, worldTime = 22d, preview = true });
            bool previewNoMutation = resources.BudgetCount == 0;
            OrganizationDecisionOperationResult execute = decisions.ExecuteResolution(new OrganizationDecisionExecutionRequest { transactionId = $"testlab.decisions.execute.apply.{context.RunId}", executionId = $"organization-execution.testlab.apply.{context.RunId}", resolutionId = resolutionId, actorPersonId = actorId, worldTime = 23d });
            OrganizationDecisionProjection redacted = decisions.GetProposalProjection(proposalId, OrganizationDecisionProjectionAccess.Redacted);
            OrganizationDecisionProjection denied = decisions.GetProposalProjection(proposalId, OrganizationDecisionProjectionAccess.Denied);
            OrganizationDecisionRuntimeSaveData save = decisions.CreateSaveData();
            OrganizationDecisionRuntime restored = new OrganizationDecisionRuntime();
            restored.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, context.ScenarioContext.Runtimes.OrganizationMemberships, context.ScenarioContext.Runtimes.OrganizationAuthority, context.ScenarioContext.Runtimes.OrganizationResources, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, context.ScenarioContext.Runtimes.Economy);
            OrganizationDecisionOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, context.ScenarioContext.Runtimes.OrganizationMemberships, context.ScenarioContext.Runtimes.OrganizationAuthority, context.ScenarioContext.Runtimes.OrganizationResources, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds);
            OrganizationDecisionRuntimeSaveData corrupt = save.Clone();
            if (corrupt.proposals.Count > 0) corrupt.proposals[0].organizationId = "organization.missing";
            bool rejected = !OrganizationDecisionRuntime.ValidateSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, context.ScenarioContext.Runtimes.OrganizationMemberships, context.ScenarioContext.Runtimes.OrganizationAuthority, context.ScenarioContext.Runtimes.OrganizationResources, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);
            OrganizationDecisionPersistenceParticipant participant = new OrganizationDecisionPersistenceParticipant(decisions, () => context.ScenarioContext.Runtimes.DefinitionRegistry, () => context.ScenarioContext.Runtimes.Organizations, () => context.ScenarioContext.Runtimes.OrganizationMemberships, () => context.ScenarioContext.Runtimes.OrganizationAuthority, () => context.ScenarioContext.Runtimes.OrganizationResources, context.ScenarioContext.Runtimes.WorldId, () => context.ScenarioContext.Runtimes.KnownPersonIds.ToArray());
            PersistenceParticipantPrepareResult prepared = participant.PreparePayload(JsonUtility.ToJson(save), OrganizationDecisionPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool valid = submit.Succeeded
                && voteActor.Succeeded
                && voteFriend.Succeeded
                && close.Succeeded
                && close.Resolution?.outcome == OrganizationResolutionOutcome.Adopted
                && preview.Succeeded
                && previewNoMutation
                && execute.Succeeded
                && resources.BudgetCount == 1
                && redacted.Succeeded
                && redacted.Redacted
                && !denied.Succeeded
                && restore.Succeeded
                && restored.ProposalCount == decisions.ProposalCount
                && corrupt.proposals.Count > 0
                && rejected
                && prepared.Succeeded;
            return TestLabAssertions.True("step13-decisions-execution", "Execute resolution and validate persistence", valid, $"Submit={submit.Code} Votes={voteActor.Code}/{voteFriend.Code} Close={close.Code}:{close.Resolution?.outcome} Preview={preview.Code}/{previewNoMutation} Execute={execute.Code} Budgets={resources.BudgetCount} Projection={redacted.Access}/{denied.Access} Restore={restore.Code} Rejected={rejected}:{validationFailure} Prepare={prepared.Succeeded}");
        }

        private static bool PrepareResourceAccounts(TestLabAutomationContext context, long openingBalance, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure)
        {
            resources = context?.ScenarioContext?.Runtimes?.OrganizationResources;
            currency = null;
            actorId = PrimaryAuthorityActorId(context);
            failure = string.Empty;
            if (resources == null || !resources.IsReady) { failure = "OrganizationResourceRuntime is missing or not ready."; return false; }
            if (!TryResourceCurrency(context, out currency)) { failure = "No active CurrencyDefinition is available."; return false; }
            CreateAuthorityGuildmaster(context, actorId, "resources");
            OrganizationResourceOperationResult treasury = resources.CreateTreasury(new OrganizationTreasuryRequest
            {
                transactionId = $"testlab.resources.treasury.{context.RunId}", treasuryId = ResourceTreasuryId(context), organizationId = "organization.prototype.guild", resourceTypeDefinitionId = PrototypeOrganizationResourceDefinitionFactory.CurrencyResourceTypeId,
                officialName = "Test Lab Guild Treasury", actorPersonId = actorId, worldTime = 1d
            });
            OrganizationResourceOperationResult operating = resources.CreateAccount(new OrganizationAccountRequest
            {
                transactionId = $"testlab.resources.account.operating.{context.RunId}", accountId = ResourceOperatingAccountId(context), treasuryId = ResourceTreasuryId(context), organizationId = "organization.prototype.guild", economyAccountId = ResourceOperatingEconomyAccountId(context),
                officialName = "Test Lab Operating", currencyDefinitionId = currency.Id, openingBalanceUnits = openingBalance, actorPersonId = actorId, worldTime = 2d
            });
            OrganizationResourceOperationResult reserve = resources.CreateAccount(new OrganizationAccountRequest
            {
                transactionId = $"testlab.resources.account.reserve.{context.RunId}", accountId = ResourceReserveAccountId(context), treasuryId = ResourceTreasuryId(context), organizationId = "organization.prototype.guild", economyAccountId = ResourceReserveEconomyAccountId(context),
                officialName = "Test Lab Reserve", category = OrganizationAccountCategory.Reserve, currencyDefinitionId = currency.Id, openingBalanceUnits = 0L, actorPersonId = actorId, worldTime = 2d
            });
            if (!treasury.Succeeded || !operating.Succeeded || !reserve.Succeeded)
            {
                failure = $"Treasury={treasury.Code}:{treasury.Message} Operating={operating.Code}:{operating.Message} Reserve={reserve.Code}:{reserve.Message}";
                return false;
            }
            return true;
        }

        private static bool TryResourceCurrency(TestLabAutomationContext context, out CurrencyDefinition currency)
        {
            currency = context?.ScenarioContext?.Runtimes?.DefinitionRegistry?.DefinitionsById.Values.OfType<CurrencyDefinition>().OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            return currency != null;
        }

        private static OrganizationFinancialTransactionRequest ResourceTransferRequest(TestLabAutomationContext context, string currencyId, string actorId, long units, string transactionId, double worldTime) => new OrganizationFinancialTransactionRequest
        {
            transactionId = transactionId, organizationId = "organization.prototype.guild", sourceAccountId = ResourceOperatingAccountId(context), destinationAccountId = ResourceReserveAccountId(context), currencyDefinitionId = currencyId,
            units = units, transactionKind = EconomyTransactionKind.Transfer, actorPersonId = actorId, purpose = "reserve allocation", worldTime = worldTime
        };

        private static string ResourceTreasuryId(TestLabAutomationContext context) => $"organization-treasury.testlab.{context.RunId}";
        private static string ResourceOperatingAccountId(TestLabAutomationContext context) => $"organization-account.testlab.operating.{context.RunId}";
        private static string ResourceReserveAccountId(TestLabAutomationContext context) => $"organization-account.testlab.reserve.{context.RunId}";
        private static string ResourceOperatingEconomyAccountId(TestLabAutomationContext context) => $"economy.organization.testlab.operating.{context.RunId}";
        private static string ResourceReserveEconomyAccountId(TestLabAutomationContext context) => $"economy.organization.testlab.reserve.{context.RunId}";

        private static bool PrepareDecisionFixture(TestLabAutomationContext context, long openingBalance, out OrganizationDecisionRuntime decisions, out OrganizationResourceRuntime resources, out CurrencyDefinition currency, out string actorId, out string failure)
        {
            decisions = context?.ScenarioContext?.Runtimes?.OrganizationDecisions;
            if (!PrepareResourceAccounts(context, openingBalance, out resources, out currency, out actorId, out failure))
            {
                decisions = null;
                return false;
            }

            if (decisions == null || !decisions.IsReady)
            {
                failure = "OrganizationDecisionRuntime is missing or not ready.";
                return false;
            }

            OrganizationMembershipRuntime memberships = context.ScenarioContext.Runtimes.OrganizationMemberships;
            memberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.decision.friend.{context.RunId}", "organization.prototype.guild", "person.prototype.friend", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.decision.member.friend.{context.RunId}", consent: true));
            memberships.ApplyMembership(MembershipRequest($"organization-membership.testlab.decision.mentor.{context.RunId}", "organization.prototype.guild", "person.prototype.mentor", PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.decision.member.mentor.{context.RunId}", consent: true));
            decisions.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.Organizations, memberships, context.ScenarioContext.Runtimes.OrganizationAuthority, resources, context.ScenarioContext.Runtimes.WorldId, context.ScenarioContext.Runtimes.KnownPersonIds, context.ScenarioContext.Runtimes.Economy);
            return true;
        }

        private static OrganizationPolicyRequest PolicyRequest(TestLabAutomationContext context, string policyId, string actorId, double worldTime) => new OrganizationPolicyRequest
        {
            transactionId = $"testlab.decisions.policy.{policyId}.{context.RunId}",
            policyId = policyId,
            organizationId = "organization.prototype.guild",
            policyDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.ConfidentialityPolicyId,
            scope = OrganizationPolicyScopeData.EntireOrganization("organization.prototype.guild"),
            parameters = new[]
            {
                PolicyParam("visibility", OrganizationPolicyParameterType.EnumValue, stringValue: OrganizationVisibility.Restricted.ToString()),
                PolicyParam("reshare_allowed", OrganizationPolicyParameterType.Boolean, boolValue: true)
            },
            priority = 100,
            actorPersonId = actorId,
            adoptedWorldTime = worldTime,
            effectiveStartWorldTime = worldTime,
            visibility = OrganizationVisibility.Restricted
        };

        private static OrganizationPolicyParameterValueData PolicyParam(string parameterId, OrganizationPolicyParameterType type, string stringValue = "", long longValue = 0L, bool boolValue = false) => new OrganizationPolicyParameterValueData
        {
            parameterId = parameterId,
            type = type,
            stringValue = stringValue ?? string.Empty,
            longValue = longValue,
            boolValue = boolValue
        };

        private static OrganizationVoteRequest VoteRequest(TestLabAutomationContext context, string proposalId, string voterId, string suffix, OrganizationVoteChoice choice) => new OrganizationVoteRequest
        {
            transactionId = $"testlab.decisions.vote.{suffix}.{context.RunId}",
            voteId = $"organization-vote.testlab.{suffix}.{context.RunId}",
            proposalId = proposalId,
            voterPersonId = voterId,
            choice = choice,
            worldTime = 12d
        };

        private static OrganizationDecisionExecutionOperationData GoalOperation(TestLabAutomationContext context, string suffix, string goalId, long targetValue) => new OrganizationDecisionExecutionOperationData
        {
            operationId = $"decision-operation.goal.{suffix}.{context.RunId}",
            kind = OrganizationDecisionExecutionOperationKind.EstablishGoal,
            targetId = goalId,
            definitionId = PrototypeOrganizationDecisionDefinitionFactory.RecruitmentGoalId,
            goalPayload = new OrganizationGoalRecordData
            {
                goalId = goalId,
                organizationId = "organization.prototype.guild",
                goalDefinitionId = PrototypeOrganizationDecisionDefinitionFactory.RecruitmentGoalId,
                displayName = $"Recruitment Goal {suffix}",
                targetValue = targetValue,
                priority = 50,
                visibility = OrganizationVisibility.Restricted
            },
            required = true
        };

        private static OrganizationDecisionExecutionOperationData BudgetOperation(TestLabAutomationContext context, string currencyId, long units) => new OrganizationDecisionExecutionOperationData
        {
            operationId = $"decision-operation.budget.{context.RunId}",
            kind = OrganizationDecisionExecutionOperationKind.ApproveBudget,
            targetId = $"organization-budget.testlab.decision.{context.RunId}",
            treasuryId = ResourceTreasuryId(context),
            accountId = ResourceOperatingAccountId(context),
            currencyDefinitionId = currencyId,
            units = units,
            purpose = "decision-approved training budget",
            required = true
        };

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, displayName, action);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Organizations | TestLabRuntimeArea.KnowledgeHistory,
                requiredDefinitionIds: new[]
                {
                    PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                    PrototypeOrganizationDefinitionFactory.SecretSocietyDefinitionId
                });
        }

        private static ITestLabAutomationScenario MembershipScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Organizations | TestLabRuntimeArea.OrganizationMemberships | TestLabRuntimeArea.KnowledgeHistory,
                requiredDefinitionIds: new[]
                {
                    PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                    PrototypeOrganizationDefinitionFactory.BranchDefinitionId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildApplicantId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildInviteeId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildAssociateId,
                    PrototypeOrganizationMembershipDefinitionFactory.BranchMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildCraftTrackId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildJourneymanRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildTreasurerOfficeId
                });
        }

        private static ITestLabAutomationScenario AuthorityScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Organizations | TestLabRuntimeArea.OrganizationMemberships | TestLabRuntimeArea.OrganizationAuthority | TestLabRuntimeArea.KnowledgeHistory,
                requiredDefinitionIds: new[]
                {
                    PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                    PrototypeOrganizationDefinitionFactory.BranchDefinitionId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.BranchMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildCraftTrackId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId,
                    PrototypeOrganizationMembershipDefinitionFactory.BranchChapterMasterOfficeId,
                    PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholdersPermissionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.IssueOrdersPermissionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.ViewRestrictedInformationPermissionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId,
                    PrototypeOrganizationAuthorityDefinitionFactory.AppointOfficeholderActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.IssueOrderActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.ChangeHeadquartersActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterOfficeBindingId,
                    PrototypeOrganizationAuthorityDefinitionFactory.BranchChapterMasterOfficeBindingId
                });
        }

        private static ITestLabAutomationScenario ResourceScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Organizations | TestLabRuntimeArea.OrganizationMemberships | TestLabRuntimeArea.OrganizationAuthority | TestLabRuntimeArea.OrganizationResources | TestLabRuntimeArea.Economy | TestLabRuntimeArea.Items,
                requiredDefinitionIds: new[]
                {
                    PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId,
                    PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId,
                    PrototypeOrganizationAuthorityDefinitionFactory.CreateTreasuryActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.TransferOrganizationFundsActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.ManageRestrictedFundsActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.AssignAssetCustodyActionId,
                    PrototypeOrganizationResourceDefinitionFactory.CurrencyResourceTypeId,
                    "currency.gold"
                });
        }

        private static ITestLabAutomationScenario DecisionScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Organizations | TestLabRuntimeArea.OrganizationMemberships | TestLabRuntimeArea.OrganizationAuthority | TestLabRuntimeArea.OrganizationResources | TestLabRuntimeArea.OrganizationDecisions | TestLabRuntimeArea.Economy | TestLabRuntimeArea.Items,
                requiredDefinitionIds: new[]
                {
                    PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId,
                    PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId,
                    PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId,
                    PrototypeOrganizationAuthorityDefinitionFactory.SubmitDecisionProposalActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.AmendDecisionProposalActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.CastOrganizationVoteActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.CloseOrganizationVoteActionId,
                    PrototypeOrganizationAuthorityDefinitionFactory.ExecuteOrganizationResolutionActionId,
                    PrototypeOrganizationDecisionDefinitionFactory.RecruitmentGoalId,
                    PrototypeOrganizationDecisionDefinitionFactory.ReserveFundGoalId,
                    PrototypeOrganizationDecisionDefinitionFactory.ConfidentialityPolicyId,
                    PrototypeOrganizationDecisionDefinitionFactory.BudgetLimitPolicyId,
                    PrototypeOrganizationDecisionDefinitionFactory.SimpleMajorityProcedureId,
                    PrototypeOrganizationDecisionDefinitionFactory.SecretBallotProcedureId,
                    PrototypeOrganizationDecisionDefinitionFactory.AdoptPolicyProposalId,
                    PrototypeOrganizationDecisionDefinitionFactory.EstablishGoalProposalId,
                    PrototypeOrganizationDecisionDefinitionFactory.ApproveBudgetProposalId,
                    PrototypeOrganizationResourceDefinitionFactory.CurrencyResourceTypeId,
                    "currency.gold"
                });
        }

        private static OrganizationCreateRequest CreateGuildRequest(string organizationId, string name, string runId, bool preview = false)
        {
            return new OrganizationCreateRequest
            {
                organizationId = organizationId,
                organizationDefinitionId = PrototypeOrganizationDefinitionFactory.GuildDefinitionId,
                officialName = name,
                shortName = "Guild",
                aliases = new[] { "Guildhouse" },
                initialLifecycleState = OrganizationLifecycleState.Active,
                visibility = OrganizationVisibility.Public,
                transactionId = $"testlab.organization.create.{organizationId}.{runId}",
                preview = preview
            };
        }

        private static OrganizationMembershipRequest MembershipRequest(
            string membershipId,
            string organizationId,
            string personId,
            string membershipDefinitionId,
            OrganizationMembershipStatus targetStatus,
            OrganizationMembershipSourceKind sourceKind,
            string transactionId,
            bool consent = false)
        {
            return new OrganizationMembershipRequest
            {
                membershipId = membershipId,
                organizationId = organizationId,
                personId = personId,
                membershipDefinitionId = membershipDefinitionId,
                targetStatus = targetStatus,
                sourceKind = sourceKind,
                worldTime = 10d,
                explicitConsent = consent,
                visibility = OrganizationVisibility.Public,
                transactionId = transactionId
            };
        }

        private static OrganizationRankAssignmentRequest RankRequest(string assignmentId, string membershipId, string rankDefinitionId, string transactionId)
        {
            return new OrganizationRankAssignmentRequest
            {
                rankAssignmentId = assignmentId,
                membershipId = membershipId,
                rankDefinitionId = rankDefinitionId,
                worldTime = 20d,
                assignedById = PersistenceService.LocalPlayerId,
                transactionId = transactionId
            };
        }

        private static OrganizationOfficeRequest OfficeRequest(string officeId, string organizationId, string officeDefinitionId, string transactionId, int maximumHolders = 0)
        {
            return new OrganizationOfficeRequest
            {
                officeId = officeId,
                organizationId = organizationId,
                officeDefinitionId = officeDefinitionId,
                maximumActiveHolders = maximumHolders,
                vacancyAllowed = true,
                worldTime = 30d,
                visibility = OrganizationVisibility.Public,
                transactionId = transactionId
            };
        }

        private static OrganizationOfficeAssignmentRequest OfficeAssignmentRequest(string assignmentId, string officeId, string membershipId, string transactionId, bool acting = false)
        {
            return new OrganizationOfficeAssignmentRequest
            {
                officeAssignmentId = assignmentId,
                officeId = officeId,
                membershipId = membershipId,
                acting = acting,
                worldTime = 40d,
                appointedById = PersistenceService.LocalPlayerId,
                transactionId = transactionId
            };
        }

        private static OrganizationAuthorizationRequest AuthorityRequest(string personId, string organizationId, string actionDefinitionId, string operationId, double worldTime = 100d)
        {
            return new OrganizationAuthorizationRequest
            {
                actorPersonId = personId,
                organizationId = organizationId,
                actionDefinitionId = actionDefinitionId,
                operationId = operationId,
                scope = OrganizationAuthorityScopeData.ForOrganization(organizationId),
                worldTime = worldTime
            };
        }

        private static OrganizationApprovalRequest ApprovalRequest(string approvalId, string operationId, string approverId)
        {
            return new OrganizationApprovalRequest
            {
                approvalId = approvalId,
                operationId = operationId,
                organizationId = "organization.prototype.guild",
                actionDefinitionId = PrototypeOrganizationAuthorityDefinitionFactory.ChangeHeadquartersActionId,
                approverPersonId = approverId,
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                approvedWorldTime = 90d,
                transactionId = $"tx.{approvalId}"
            };
        }

        private static void CreateAuthorityGuildmaster(TestLabAutomationContext context, string personId, string suffix)
        {
            OrganizationMembershipRuntime memberships = context.ScenarioContext.Runtimes.OrganizationMemberships;
            string membershipId = $"organization-membership.testlab.authority.guildmaster.{suffix}.{context.RunId}";
            OrganizationMembershipOperationResult member = memberships.ApplyMembership(MembershipRequest(membershipId, "organization.prototype.guild", personId, PrototypeOrganizationMembershipDefinitionFactory.GuildFullMemberId, OrganizationMembershipStatus.Active, OrganizationMembershipSourceKind.WorldSetup, $"testlab.authority.guildmaster.member.{suffix}.{context.RunId}", consent: true));
            AssignGuildMasterRank(memberships, member.Membership?.MembershipId, context.RunId, suffix);
            OrganizationMembershipOperationResult office = memberships.CreateOffice(OfficeRequest($"organization-office-record.testlab.guildmaster.{suffix}.{context.RunId}", "organization.prototype.guild", PrototypeOrganizationMembershipDefinitionFactory.GuildmasterOfficeId, $"testlab.authority.guildmaster.office.{suffix}.{context.RunId}"));
            memberships.AssignOffice(OfficeAssignmentRequest($"organization-office-assignment.testlab.guildmaster.{suffix}.{context.RunId}", office.Office?.OfficeId, member.Membership?.MembershipId, $"testlab.authority.guildmaster.office.assign.{suffix}.{context.RunId}"));
        }

        private static void AssignGuildMasterRank(OrganizationMembershipRuntime memberships, string membershipId, string runId, string suffix)
        {
            if (string.IsNullOrWhiteSpace(membershipId))
            {
                return;
            }

            memberships.AssignRank(RankRequest($"organization-rank-assignment.testlab.novice.{suffix}.{runId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildNoviceRankId, $"testlab.authority.rank.novice.{suffix}.{runId}"));
            memberships.AssignRank(RankRequest($"organization-rank-assignment.testlab.journeyman.{suffix}.{runId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildJourneymanRankId, $"testlab.authority.rank.journeyman.{suffix}.{runId}"));
            memberships.AssignRank(RankRequest($"organization-rank-assignment.testlab.master.{suffix}.{runId}", membershipId, PrototypeOrganizationMembershipDefinitionFactory.GuildMasterRankId, $"testlab.authority.rank.master.{suffix}.{runId}"));
        }

        private static OrganizationAuthorityOperationResult GrantGuildmasterRole(OrganizationAuthorityRuntime authority, string grantorPersonId, string granteePersonId, string runId, string suffix)
        {
            return authority.CreateDirectGrant(new OrganizationAuthorityGrantRequest
            {
                grantId = $"organization-authority-grant.testlab.guildmaster.{suffix}.{runId}",
                organizationId = "organization.prototype.guild",
                granteePersonId = granteePersonId,
                grantorPersonId = grantorPersonId,
                authorityRoleDefinitionId = PrototypeOrganizationAuthorityDefinitionFactory.GuildmasterRoleId,
                scope = OrganizationAuthorityScopeData.ForOrganization("organization.prototype.guild"),
                transactionId = $"testlab.authority.guildmaster.grant.{suffix}.{runId}"
            });
        }

        private static string PrimaryAuthorityActorId(TestLabAutomationContext context)
        {
            string personId = context?.ScenarioContext?.Runtimes?.PersonId;
            return string.IsNullOrWhiteSpace(personId) ? PersistenceService.LocalPlayerId : personId;
        }

        private static bool TryGetRuntime(TestLabAutomationContext context, out OrganizationRuntime runtime, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.Organizations;
            if (runtime == null)
            {
                failure = "OrganizationRuntime is missing from the Test Lab runtime bundle.";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryGetMembershipRuntime(TestLabAutomationContext context, out OrganizationMembershipRuntime runtime, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.OrganizationMemberships;
            if (runtime == null)
            {
                failure = "OrganizationMembershipRuntime is missing from the Test Lab runtime bundle.";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryGetAuthorityRuntime(TestLabAutomationContext context, out OrganizationAuthorityRuntime runtime, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.OrganizationAuthority;
            if (runtime == null)
            {
                failure = "OrganizationAuthorityRuntime is missing from the Test Lab runtime bundle.";
                return false;
            }

            failure = string.Empty;
            return true;
        }
    }
}
#endif
