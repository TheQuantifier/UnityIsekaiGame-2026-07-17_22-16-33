#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
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
    }
}
#endif
