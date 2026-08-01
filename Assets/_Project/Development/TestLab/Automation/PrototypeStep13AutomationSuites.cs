#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Persistence;

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
    }
}
#endif
