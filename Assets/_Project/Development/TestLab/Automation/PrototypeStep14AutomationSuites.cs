#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.WorldLocations;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(14, "World Locations", 1400)]
    public static class PrototypeStep14AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.14.1.world-location-identity-foundation",
                "World and Location Identity Foundation",
                "14.1",
                "Authoritative runtime location identity, definition separation, lifecycle, references, persistence, validation, and Test Lab coverage.",
                14010,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "LocationRuntime", "LocationDefinition", "LocationPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("60.1-readiness-and-definitions", "Location definitions and prototype records are available", 10, Step("step14-location-readiness", "Resolve location definitions and seeded records", ReadinessAndDefinitions)),
                    Scenario("60.2-definition-vs-instance", "Definitions remain separate from runtime instances", 20, Step("step14-location-definition-separation", "Create multiple instances from one definition", DefinitionVsInstance)),
                    Scenario("60.3-stable-identity", "Location IDs are stable through rename and scene binding changes", 30, Step("step14-location-stable-identity", "Rename without changing identity", StableIdentity)),
                    Scenario("60.4-lifecycle", "Lifecycle transitions are explicit and deterministic", 40, Step("step14-location-lifecycle", "Transition active to closed and historical", Lifecycle)),
                    Scenario("60.5-names-tags", "Names, aliases, and semantic tags are indexed deterministically", 50, Step("step14-location-names-tags", "Query by tag and definition", NamesTagsQueries)),
                    Scenario("60.6-association-references", "Property, organization, government, and territory associations stay as references", 60, Step("step14-location-associations", "Create association references without owning external records", Associations)),
                    Scenario("60.7-scene-independence", "Locations do not depend on GameObject or Transform identity", 70, Step("step14-location-scene-independence", "Save and restore scene binding keys only", SceneIndependence)),
                    Scenario("60.8-visibility-subject", "Location projections expose Step 8 subject references", 80, Step("step14-location-visibility-subject", "Create redacted-capable subject reference", VisibilitySubject)),
                    Scenario("60.9-preview-idempotence", "Preview and duplicate transactions do not mutate state", 90, Step("step14-location-preview-idempotence", "Preview create and duplicate commit", PreviewIdempotence)),
                    Scenario("60.10-revision-safety", "Revision conflicts reject stale writes", 100, Step("step14-location-revision", "Reject stale expected revision", RevisionSafety)),
                    Scenario("60.11-persistence-validation", "Persistence rejects corrupt graphs before commit", 110, Step("step14-location-persistence", "Save, restore, and reject invalid payload", PersistenceValidation)),
                    Scenario("60.12-fixture-snapshot", "Fixture snapshots restore location mutations", 120, Step("step14-location-fixture", "Snapshot and restore location state", FixtureSnapshot))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.14.2.location-hierarchy-containment-spatial-relationships",
                "Location Hierarchy, Containment, and Spatial Relationships",
                "14.2",
                "Runtime-authoritative location containment, spatial relationship records, deterministic traversal, persistence validation, and scene-independent projections.",
                14020,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "LocationRuntime", "LocationContainmentLink", "LocationSpatialRelationship" },
                scenarios: new[]
                {
                    Scenario("72.1-readiness-and-seeded-graph", "Seeded prototype containment graph is authoritative and valid", 10, Step("step14-location-hierarchy-readiness", "Resolve seeded hierarchy and validation", HierarchyReadiness)),
                    Scenario("72.2-parent-child-traversal", "Parent, child, ancestor, and descendant queries are deterministic", 20, Step("step14-location-hierarchy-traversal", "Query containment traversal", ParentChildTraversal)),
                    Scenario("72.3-cycle-prevention", "Containment cycles reject without mutation", 30, Step("step14-location-hierarchy-cycle", "Reject containment cycle", CyclePrevention)),
                    Scenario("72.4-active-parent-constraint", "Ordinary locations have only one active primary parent", 40, Step("step14-location-hierarchy-active-parent", "Reject second active parent", ActiveParentConstraint)),
                    Scenario("72.5-reparent-history", "Reparenting ends the previous link and preserves history", 50, Step("step14-location-hierarchy-reparent", "Atomic reparent and historical link", ReparentHistory)),
                    Scenario("72.6-spatial-directionality", "Spatial relationships resolve directional, inverse, and symmetric semantics", 60, Step("step14-location-spatial-directionality", "Evaluate spatial directionality", SpatialDirectionality)),
                    Scenario("72.7-spatial-no-routing", "Spatial relationships do not imply travel or occupancy state", 70, Step("step14-location-spatial-boundary", "Verify spatial boundary is descriptive only", SpatialNoRouting)),
                    Scenario("72.8-preview-idempotence", "Preview and duplicate hierarchy/spatial transactions do not mutate twice", 80, Step("step14-location-hierarchy-preview", "Preview and duplicate graph operations", HierarchyPreviewIdempotence)),
                    Scenario("72.9-persistence-round-trip", "Containment and spatial relationships persist deterministically", 90, Step("step14-location-hierarchy-persistence", "Save and restore graph records", HierarchyPersistence)),
                    Scenario("72.10-corrupt-restore-rejection", "Corrupt containment payloads reject before commit", 100, Step("step14-location-hierarchy-corrupt", "Reject corrupt graph restore", CorruptHierarchyRejection)),
                    Scenario("72.11-visibility-projections", "Hidden hierarchy links can be omitted from normal projections", 110, Step("step14-location-hierarchy-visibility", "Evaluate visibility-safe graph queries", VisibilityProjections)),
                    Scenario("72.12-fixture-snapshot", "Fixture snapshots restore location graph mutations", 120, Step("step14-location-hierarchy-fixture", "Snapshot and restore containment/spatial state", HierarchyFixtureSnapshot))
                }), out _);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                order <= 20 ? TestLabAutomationCategory.Quick : TestLabAutomationCategory.Standard,
                includeInQuickRun: order <= 20,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.WorldLocations,
                requiredHostFeatures: TestLabHostFeature.AutomatedExecution,
                requiredDefinitionIds: new[]
                {
                    PrototypeLocationDefinitionFactory.SettlementDefinitionId,
                    PrototypeLocationDefinitionFactory.GuildHallDefinitionId,
                    PrototypeLocationDefinitionFactory.OfficeDefinitionId,
                    PrototypeLocationDefinitionFactory.DungeonDefinitionId
                });
        }

        private static ITestLabScenarioStep Step(string id, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(id, displayName, action);
        }

        private static TestLabAutomationStepResult ReadinessAndDefinitions(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool hasSettlement = registry.TryGet(PrototypeLocationDefinitionFactory.SettlementDefinitionId, out LocationDefinition settlement);
            bool hasGuildHall = registry.TryGet(PrototypeLocationDefinitionFactory.GuildHallDefinitionId, out LocationDefinition guildHall);
            bool hasDetention = registry.TryGet(PrototypeLocationDefinitionFactory.DetentionAreaDefinitionId, out LocationDefinition detention);
            bool definitions = hasSettlement && hasGuildHall && hasDetention;
            bool hasVillage = runtime.TryGetSnapshot("location.prototype.village", out LocationSnapshot village);
            bool hasGuild = runtime.TryGetSnapshot("location.prototype.adventurers-guild", out LocationSnapshot guild);
            bool seeded = hasVillage && hasGuild;
            LocationValidationReport report = runtime.ValidateRuntime();
            bool valid = definitions && seeded && report.Succeeded && settlement.Category == LocationCategory.Settlement && guildHall.SupportsOrganizationAssociation && detention.SupportsVisibility(LocationVisibility.Restricted) && village.WorldId == context.ScenarioContext.Runtimes.WorldId && guild.AssociatedOrganizationId == "organization.prototype.guild";
            return TestLabAssertions.True("step14-location-readiness", "Location definitions and seeded records resolve", valid, $"Definitions={definitions} Seeded={seeded} Validation={report.Summary}");
        }

        private static TestLabAutomationStepResult DefinitionVsInstance(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult first = Create(context, "market-a", PrototypeLocationDefinitionFactory.MarketStallDefinitionId, "Market Stall A");
            LocationOperationResult second = Create(context, "market-b", PrototypeLocationDefinitionFactory.MarketStallDefinitionId, "Market Stall B");
            bool valid = first.Succeeded && second.Succeeded && first.Snapshot.LocationId != second.Snapshot.LocationId && first.Snapshot.LocationDefinitionId == second.Snapshot.LocationDefinitionId && runtime.QueryByDefinition(PrototypeLocationDefinitionFactory.MarketStallDefinitionId).Count >= 2;
            return TestLabAssertions.True("step14-location-definition-separation", "One definition can back distinct runtime locations", valid, $"First={first.Status} Second={second.Status} Count={runtime.QueryByDefinition(PrototypeLocationDefinitionFactory.MarketStallDefinitionId).Count}");
        }

        private static TestLabAutomationStepResult StableIdentity(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = Create(context, "stable-create", PrototypeLocationDefinitionFactory.GuildHallDefinitionId, "Old Guild Hall", binding: "scene.old");
            LocationOperationResult rename = runtime.RenameLocation(new LocationRenameRequest { transactionId = Tx(context, "stable-rename"), locationId = create.Snapshot?.LocationId, newName = "Renamed Guild Hall", category = LocationNameCategory.Official, effectiveWorldTime = 10d });
            bool valid = create.Succeeded && rename.Succeeded && rename.Snapshot.LocationId == create.Snapshot.LocationId && rename.Snapshot.LocationDefinitionId == create.Snapshot.LocationDefinitionId && rename.Snapshot.PrototypeSceneBindingKey == "scene.old";
            return TestLabAssertions.True("step14-location-stable-identity", "Rename preserves runtime location identity", valid, $"Create={create.Status} Rename={rename.Status} Id={rename.Snapshot?.LocationId}");
        }

        private static TestLabAutomationStepResult Lifecycle(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = Create(context, "lifecycle", PrototypeLocationDefinitionFactory.OfficeDefinitionId, "Lifecycle Office");
            LocationOperationResult close = runtime.TransitionLifecycle(new LocationLifecycleTransitionRequest { transactionId = Tx(context, "close"), locationId = create.Snapshot?.LocationId, targetState = LocationLifecycleState.Closed, worldTime = 12d });
            LocationOperationResult historical = runtime.TransitionLifecycle(new LocationLifecycleTransitionRequest { transactionId = Tx(context, "historical"), locationId = create.Snapshot?.LocationId, targetState = LocationLifecycleState.Historical, worldTime = 20d });
            LocationReferenceResolutionResult resolution = runtime.ResolveReference(create.Snapshot?.ToReference());
            bool valid = create.Succeeded && close.Succeeded && historical.Succeeded && historical.Snapshot.LifecycleState == LocationLifecycleState.Historical && resolution.Succeeded;
            return TestLabAssertions.True("step14-location-lifecycle", "Lifecycle transitions keep stable references", valid, $"Create={create.Status} Close={close.Status} Historical={historical.Status} Resolve={resolution.Status}");
        }

        private static TestLabAutomationStepResult NamesTagsQueries(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = Create(context, "tags", PrototypeLocationDefinitionFactory.WildernessDefinitionId, "Northern Ring", tags: new[] { "wilderness", "outdoor" });
            LocationOperationResult alias = runtime.RenameLocation(new LocationRenameRequest { transactionId = Tx(context, "alias"), locationId = create.Snapshot?.LocationId, newName = "Old North Ring", category = LocationNameCategory.Alias, effectiveWorldTime = 15d });
            bool valid = create.Succeeded && alias.Succeeded && alias.Snapshot.Aliases.Contains("Old North Ring") && runtime.QueryByTag("wilderness").Any(item => item.LocationId == create.Snapshot.LocationId) && runtime.QueryByCategory(LocationCategory.Wilderness).Any(item => item.LocationId == create.Snapshot.LocationId);
            return TestLabAssertions.True("step14-location-names-tags", "Names, aliases, tags, and indexes are deterministic", valid, $"Create={create.Status} Alias={alias.Status} Wilderness={runtime.QueryByTag("wilderness").Count}");
        }

        private static TestLabAutomationStepResult Associations(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = runtime.CreateLocation(new LocationCreateRequest
            {
                transactionId = Tx(context, "assoc"),
                locationId = Id(context, "assoc"),
                locationDefinitionId = PrototypeLocationDefinitionFactory.GovernmentBuildingDefinitionId,
                officialName = "Associated Civic Building",
                associatedPropertyId = "property.prototype.guild-building",
                associatedOrganizationId = "organization.prototype.government",
                associatedGovernmentId = "government.prototype.civic",
                associatedTerritoryIds = new[] { "territory.prototype.village" },
                associations = new[] { new LocationAssociationReferenceData { kind = LocationAssociationKind.Provenance, referenceId = $"location-record.charter.{context.RunId}", worldId = context.ScenarioContext.Runtimes.WorldId } },
                semanticTagIds = new[] { "government", "building", "civic" }
            });
            bool valid = create.Succeeded && create.Snapshot.AssociatedPropertyId == "property.prototype.guild-building" && create.Snapshot.AssociatedOrganizationId == "organization.prototype.government" && create.Snapshot.AssociatedGovernmentId == "government.prototype.civic" && create.Snapshot.AssociatedTerritoryIds.Contains("territory.prototype.village") && create.Snapshot.Associations.Count == 1;
            return TestLabAssertions.True("step14-location-associations", "External systems remain references, not owned records", valid, $"Create={create.Status} Property={create.Snapshot?.AssociatedPropertyId} Org={create.Snapshot?.AssociatedOrganizationId}");
        }

        private static TestLabAutomationStepResult SceneIndependence(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = Create(context, "scene-free", PrototypeLocationDefinitionFactory.DungeonDefinitionId, "Scene Free Dungeon", visibility: LocationVisibility.Secret, binding: "prototype.marker.dungeon");
            LocationRuntimeSaveData save = runtime.CreateSaveData();
            LocationRuntime restored = new LocationRuntime();
            restored.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            LocationOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            bool found = restored.TryGetSnapshot(create.Snapshot?.LocationId, out LocationSnapshot snapshot);
            bool valid = create.Succeeded && restore.Succeeded && found && snapshot.PrototypeSceneBindingKey == "prototype.marker.dungeon" && snapshot.LocationId == create.Snapshot.LocationId;
            return TestLabAssertions.True("step14-location-scene-independence", "Save data carries binding keys without depending on scene objects", valid, $"Create={create.Status} Restore={restore.Status} Found={found}");
        }

        private static TestLabAutomationStepResult VisibilitySubject(TestLabAutomationContext context)
        {
            LocationOperationResult create = Create(context, "hidden-subject", PrototypeLocationDefinitionFactory.DungeonDefinitionId, "Hidden Dungeon", visibility: LocationVisibility.Hidden);
            bool valid = create.Succeeded && create.Snapshot.Visibility == LocationVisibility.Hidden && create.Snapshot.ToInformationSubject().subjectType == UnityIsekaiGame.Knowledge.Access.InformationSubjectType.Location && create.Snapshot.ToInformationSubject().subjectId == create.Snapshot.LocationId;
            return TestLabAssertions.True("step14-location-visibility-subject", "Location snapshots expose stable Step 8 subject references", valid, $"Create={create.Status} Subject={create.Snapshot?.ToInformationSubject().subjectId}");
        }

        private static TestLabAutomationStepResult PreviewIdempotence(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            long before = runtime.Revision;
            LocationOperationResult preview = Create(context, "preview", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Preview Room", preview: true);
            bool absentAfterPreview = !runtime.TryGetSnapshot(Id(context, "preview"), out _);
            LocationOperationResult execute = Create(context, "preview", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Preview Room");
            LocationOperationResult duplicate = Create(context, "preview", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Preview Room");
            bool valid = preview.Preview && absentAfterPreview && execute.Succeeded && duplicate.Duplicate && runtime.Revision == before + 1L;
            return TestLabAssertions.True("step14-location-preview-idempotence", "Preview and duplicate create do not mutate more than once", valid, $"Preview={preview.Status} Execute={execute.Status} Duplicate={duplicate.Status} Revision={before}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult RevisionSafety(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult create = Create(context, "revision", PrototypeLocationDefinitionFactory.OfficeDefinitionId, "Revision Office");
            LocationOperationResult stale = runtime.RenameLocation(new LocationRenameRequest { transactionId = Tx(context, "stale"), locationId = create.Snapshot?.LocationId, newName = "Stale Office", expectedRevision = 0L });
            bool valid = create.Succeeded && !stale.Succeeded && stale.Status == LocationOperationStatus.RevisionConflict && runtime.TryGetSnapshot(create.Snapshot.LocationId, out LocationSnapshot snapshot) && snapshot.OfficialName == "Revision Office";
            return TestLabAssertions.True("step14-location-revision", "Stale expected revisions reject without mutation", valid, $"Create={create.Status} Stale={stale.Status}");
        }

        private static TestLabAutomationStepResult PersistenceValidation(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            Create(context, "persist", PrototypeLocationDefinitionFactory.GuildHallDefinitionId, "Persistent Guild Hall", organizationId: "organization.prototype.guild");
            LocationPersistenceParticipant participant = new LocationPersistenceParticipant(runtime, () => context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            var save = participant.CapturePayload();
            var prepared = participant.PreparePayload(save.PayloadJson, LocationPersistenceParticipant.CurrentParticipantSchemaVersion);
            LocationRuntimeSaveData corrupt = runtime.CreateSaveData();
            corrupt.records[0].locationDefinitionId = "location-definition.missing";
            var rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), LocationPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool unchanged = runtime.Count >= PrototypeLocationDefinitionFactory.PrototypeLocationIds.Length + 1;
            bool valid = save.Succeeded && prepared.Succeeded && rejected != null && !rejected.Succeeded && unchanged;
            return TestLabAssertions.True("step14-location-persistence", "Persistence validates before commit and rejects corrupt graphs", valid, $"Save={save.Succeeded} Prepare={prepared.Succeeded} Rejected={rejected?.Succeeded == false} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult FixtureSnapshot(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            TestLabRuntimeBundleSnapshot snapshot = context.ScenarioContext.Runtimes.CreateSnapshot();
            int before = runtime.Count;
            Create(context, "fixture", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Fixture Room");
            bool restored = context.ScenarioContext.Runtimes.RestoreSnapshot(snapshot, out string failure);
            bool missing = !runtime.TryGetSnapshot(Id(context, "fixture"), out _);
            bool valid = restored && missing && runtime.Count == before;
            return TestLabAssertions.True("step14-location-fixture", "Fixture snapshot restore removes undeclared location mutations", valid, $"Restored={restored} Missing={missing} Count={runtime.Count}/{before} Failure={failure}");
        }

        private static TestLabAutomationStepResult HierarchyReadiness(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationValidationReport report = runtime.ValidateRuntime();
            LocationContainmentSnapshot villageParent = runtime.GetActiveParentLink("location.prototype.village");
            bool spatial = runtime.AreSpatiallyRelated("location.prototype.market-district", "location.prototype.adventurers-guild", LocationSpatialRelationshipKind.Near);
            bool valid = report.Succeeded
                && villageParent != null
                && villageParent.ParentLocationId == "location.prototype.region"
                && runtime.GetRoots().Any(root => root.LocationId == "location.prototype.world")
                && runtime.ContainmentLinkCount >= 10
                && runtime.SpatialRelationshipCount >= 4
                && spatial;
            return TestLabAssertions.True("step14-location-hierarchy-readiness", "Seeded location graph is valid and queryable", valid, $"Validation={report.Summary} Links={runtime.ContainmentLinkCount} Spatial={runtime.SpatialRelationshipCount} VillageParent={villageParent?.ParentLocationId}");
        }

        private static TestLabAutomationStepResult ParentChildTraversal(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            IReadOnlyList<LocationSnapshot> path = runtime.GetHierarchyPath("location.prototype.guildmaster-office").Path;
            IReadOnlyList<LocationSnapshot> children = runtime.GetChildren("location.prototype.village");
            IReadOnlyList<LocationSnapshot> descendants = runtime.GetDescendants("location.prototype.village");
            bool deterministic = descendants.Select(item => item.LocationId).SequenceEqual(descendants.Select(item => item.LocationId).OrderBy(id => id, StringComparer.Ordinal));
            bool valid = path.Select(item => item.LocationId).SequenceEqual(new[] { "location.prototype.world", "location.prototype.region", "location.prototype.village", "location.prototype.adventurers-guild", "location.prototype.guildmaster-office" })
                && children.Any(item => item.LocationId == "location.prototype.adventurers-guild")
                && descendants.Any(item => item.LocationId == "location.prototype.basement-prison")
                && deterministic;
            return TestLabAssertions.True("step14-location-hierarchy-traversal", "Hierarchy traversal returns deterministic paths and descendants", valid, $"Path={string.Join("/", path.Select(item => item.LocationId))} Children={children.Count} Descendants={descendants.Count}");
        }

        private static TestLabAutomationStepResult CyclePrevention(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult parent = Create(context, "cycle-parent", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Cycle Parent Room", tags: new[] { "room", "interior" });
            LocationOperationResult child = Create(context, "cycle-child", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Cycle Child Room", tags: new[] { "room", "interior" });
            LocationOperationResult initial = runtime.AssignContainment(new LocationContainmentRequest
            {
                transactionId = Tx(context, "cycle-initial"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "cycle-initial"),
                parentLocationId = parent.Snapshot?.LocationId,
                childLocationId = child.Snapshot?.LocationId,
                kind = LocationContainmentKind.Interior,
                effectiveWorldTime = 70d
            });
            long before = runtime.Revision;
            LocationOperationResult cycle = runtime.AssignContainment(new LocationContainmentRequest
            {
                transactionId = Tx(context, "cycle"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "cycle"),
                parentLocationId = child.Snapshot?.LocationId,
                childLocationId = parent.Snapshot?.LocationId,
                kind = LocationContainmentKind.Interior,
                effectiveWorldTime = 71d
            });
            bool valid = parent.Succeeded && child.Succeeded && initial.Succeeded && cycle.Status == LocationOperationStatus.CycleDetected && runtime.Revision == before && runtime.GetActiveParentLink(parent.Snapshot.LocationId) == null;
            return TestLabAssertions.True("step14-location-hierarchy-cycle", "Cycle creation rejects without mutation", valid, $"Parent={parent.Status} Child={child.Status} Initial={initial.Status} Cycle={cycle.Status} Revision={before}->{runtime.Revision} Message={cycle.Message}");
        }

        private static TestLabAutomationStepResult ActiveParentConstraint(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult second = runtime.AssignContainment(new LocationContainmentRequest
            {
                transactionId = Tx(context, "active-parent"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "second-parent"),
                parentLocationId = "location.prototype.market-district",
                childLocationId = "location.prototype.adventurers-guild",
                effectiveWorldTime = 75d
            });
            bool valid = second.Status == LocationOperationStatus.ActiveParentConflict && runtime.GetActiveParentLink("location.prototype.adventurers-guild").ParentLocationId == "location.prototype.village";
            return TestLabAssertions.True("step14-location-hierarchy-active-parent", "Second active primary parent rejects", valid, $"Status={second.Status} Parent={runtime.GetActiveParentLink("location.prototype.adventurers-guild")?.ParentLocationId}");
        }

        private static TestLabAutomationStepResult ReparentHistory(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult room = Create(context, "reparent-room", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Reparent Room", tags: new[] { "room", "interior" });
            LocationOperationResult initial = runtime.AssignContainment(new LocationContainmentRequest
            {
                transactionId = Tx(context, "reparent-initial"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "reparent-initial"),
                parentLocationId = "location.prototype.adventurers-guild",
                childLocationId = room.Snapshot?.LocationId,
                kind = LocationContainmentKind.Interior,
                effectiveWorldTime = 80d
            });
            LocationOperationResult reparent = runtime.ReparentLocation(new LocationReparentRequest
            {
                transactionId = Tx(context, "reparent-new"),
                oldParentLocationId = "location.prototype.adventurers-guild",
                newParentLocationId = "location.prototype.civic-office",
                childLocationId = room.Snapshot?.LocationId,
                newLinkId = context.ScenarioContext.ScopedId("location-containment.test", "reparent-new"),
                kind = LocationContainmentKind.Interior,
                effectiveWorldTime = 90d
            });
            bool oldEnded = runtime.ContainmentLinks.Any(link => link.ChildLocationId == room.Snapshot.LocationId && link.ParentLocationId == "location.prototype.adventurers-guild" && link.State == LocationLinkState.Ended);
            bool valid = room.Succeeded && initial.Succeeded && reparent.Succeeded && oldEnded && runtime.GetActiveParentLink(room.Snapshot.LocationId).ParentLocationId == "location.prototype.civic-office";
            return TestLabAssertions.True("step14-location-hierarchy-reparent", "Reparenting preserves ended link history and active parent", valid, $"Room={room.Status} Initial={initial.Status} Reparent={reparent.Status} OldEnded={oldEnded}");
        }

        private static TestLabAutomationStepResult SpatialDirectionality(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult directional = runtime.CreateSpatialRelationship(new LocationSpatialRelationshipRequest
            {
                transactionId = Tx(context, "spatial-directional"),
                relationshipId = context.ScenarioContext.ScopedId("location-spatial.test", "above"),
                sourceLocationId = "location.prototype.guildmaster-office",
                targetLocationId = "location.prototype.basement-prison",
                kind = LocationSpatialRelationshipKind.Above,
                directionality = LocationSpatialDirectionality.Directional
            });
            bool above = runtime.AreSpatiallyRelated("location.prototype.guildmaster-office", "location.prototype.basement-prison", LocationSpatialRelationshipKind.Above);
            bool inverse = runtime.AreSpatiallyRelated("location.prototype.basement-prison", "location.prototype.guildmaster-office", LocationSpatialRelationshipKind.Below);
            bool symmetric = runtime.AreSpatiallyRelated("location.prototype.adventurers-guild", "location.prototype.market-district", LocationSpatialRelationshipKind.Near);
            bool valid = directional.Succeeded && above && inverse && symmetric;
            return TestLabAssertions.True("step14-location-spatial-directionality", "Spatial relationships resolve directional inverse and symmetric semantics", valid, $"Create={directional.Status} Above={above} Inverse={inverse} Symmetric={symmetric}");
        }

        private static TestLabAutomationStepResult SpatialNoRouting(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationSpatialRelationshipSnapshot spatial = runtime.GetSpatialRelationships("location.prototype.dungeon-entry", includeIncoming: true, includeHidden: true).FirstOrDefault(item => item.Kind == LocationSpatialRelationshipKind.PartOfComplex);
            bool noRouteFields = spatial != null && spatial.ToSaveData().GetType().GetField("routeCost") == null && spatial.ToSaveData().GetType().GetField("travelMode") == null;
            bool valid = spatial != null && noRouteFields && runtime.TryGetSnapshot(spatial.SourceLocationId, out _) && runtime.TryGetSnapshot(spatial.TargetLocationId, out _);
            return TestLabAssertions.True("step14-location-spatial-boundary", "Spatial relationships remain descriptive and do not carry travel semantics", valid, $"Spatial={spatial?.RelationshipId} NoRouteFields={noRouteFields}");
        }

        private static TestLabAutomationStepResult HierarchyPreviewIdempotence(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationOperationResult room = Create(context, "preview-room", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Preview Link Room", tags: new[] { "room", "interior" });
            long before = runtime.Revision;
            LocationContainmentRequest request = new LocationContainmentRequest
            {
                transactionId = Tx(context, "preview-link"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "preview-link"),
                parentLocationId = "location.prototype.adventurers-guild",
                childLocationId = room.Snapshot?.LocationId,
                kind = LocationContainmentKind.Interior,
                effectiveWorldTime = 100d,
                preview = true
            };
            LocationOperationResult preview = runtime.AssignContainment(request);
            request.preview = false;
            LocationOperationResult execute = runtime.AssignContainment(request);
            LocationOperationResult duplicate = runtime.AssignContainment(request);
            bool valid = room.Succeeded && preview.Preview && execute.Succeeded && duplicate.Duplicate && runtime.Revision == before + 1L;
            return TestLabAssertions.True("step14-location-hierarchy-preview", "Preview and duplicate graph operations mutate exactly once", valid, $"Preview={preview.Status} Execute={execute.Status} Duplicate={duplicate.Status} Revision={before}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult HierarchyPersistence(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationRuntimeSaveData save = runtime.CreateSaveData();
            LocationRuntime restored = new LocationRuntime();
            restored.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            LocationOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            bool valid = restore.Succeeded
                && restored.GetActiveParentLink("location.prototype.guildmaster-office")?.ParentLocationId == "location.prototype.adventurers-guild"
                && restored.AreSpatiallyRelated("location.prototype.market-district", "location.prototype.adventurers-guild", LocationSpatialRelationshipKind.Near)
                && restored.CreateSaveData().containmentLinks.Select(link => link.linkId).SequenceEqual(save.containmentLinks.Select(link => link.linkId));
            return TestLabAssertions.True("step14-location-hierarchy-persistence", "Save and restore preserve graph records deterministically", valid, $"Restore={restore.Status} Links={restored.ContainmentLinkCount}/{runtime.ContainmentLinkCount} Spatial={restored.SpatialRelationshipCount}/{runtime.SpatialRelationshipCount}");
        }

        private static TestLabAutomationStepResult CorruptHierarchyRejection(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            LocationRuntimeSaveData corrupt = runtime.CreateSaveData();
            corrupt.containmentLinks[0].parentLocationId = "location.prototype.missing";
            LocationRuntimeSaveData before = runtime.CreateSaveData();
            LocationOperationResult rejected = runtime.RestoreFromSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, context.ScenarioContext.Runtimes.WorldId);
            bool unchanged = runtime.CreateSaveData().containmentLinks.Select(link => link.parentLocationId).SequenceEqual(before.containmentLinks.Select(link => link.parentLocationId));
            bool valid = rejected.Status == LocationOperationStatus.PersistenceInvalid && unchanged;
            return TestLabAssertions.True("step14-location-hierarchy-corrupt", "Corrupt hierarchy restore rejects without mutation", valid, $"Rejected={rejected.Status} Unchanged={unchanged} Message={rejected.Message}");
        }

        private static TestLabAutomationStepResult VisibilityProjections(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            IReadOnlyList<LocationSnapshot> normal = runtime.GetChildren("location.prototype.wilderness-ring", includeHidden: false);
            IReadOnlyList<LocationSnapshot> privileged = runtime.GetChildren("location.prototype.wilderness-ring", includeHidden: true);
            bool hiddenOmitted = normal.All(item => item.LocationId != "location.prototype.dungeon-entry");
            bool hiddenVisible = privileged.Any(item => item.LocationId == "location.prototype.dungeon-entry");
            bool valid = hiddenOmitted && hiddenVisible;
            return TestLabAssertions.True("step14-location-hierarchy-visibility", "Hidden containment links are omitted unless privileged", valid, $"Normal={normal.Count} Privileged={privileged.Count} HiddenOmitted={hiddenOmitted}");
        }

        private static TestLabAutomationStepResult HierarchyFixtureSnapshot(TestLabAutomationContext context)
        {
            LocationRuntime runtime = Runtime(context);
            TestLabRuntimeBundleSnapshot snapshot = context.ScenarioContext.Runtimes.CreateSnapshot();
            int beforeLinks = runtime.ContainmentLinkCount;
            LocationOperationResult room = Create(context, "fixture-room", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Fixture Link Room", tags: new[] { "room", "interior" });
            runtime.AssignContainment(new LocationContainmentRequest
            {
                transactionId = Tx(context, "fixture-link"),
                linkId = context.ScenarioContext.ScopedId("location-containment.test", "fixture-link"),
                parentLocationId = "location.prototype.adventurers-guild",
                childLocationId = room.Snapshot?.LocationId,
                kind = LocationContainmentKind.Interior
            });
            bool restored = context.ScenarioContext.Runtimes.RestoreSnapshot(snapshot, out string failure);
            bool valid = restored && runtime.ContainmentLinkCount == beforeLinks && !runtime.TryGetSnapshot(room.Snapshot?.LocationId, out _);
            return TestLabAssertions.True("step14-location-hierarchy-fixture", "Fixture snapshot restores graph mutations", valid, $"Restored={restored} Links={runtime.ContainmentLinkCount}/{beforeLinks} Failure={failure}");
        }

        private static LocationRuntime Runtime(TestLabAutomationContext context)
        {
            return context?.ScenarioContext?.Runtimes?.Locations;
        }

        private static LocationOperationResult Create(TestLabAutomationContext context, string suffix, string definitionId, string officialName, IEnumerable<string> tags = null, string organizationId = null, LocationVisibility visibility = LocationVisibility.Public, string binding = null, bool preview = false)
        {
            LocationRuntime runtime = Runtime(context);
            return runtime.CreateLocation(new LocationCreateRequest
            {
                transactionId = Tx(context, suffix),
                locationId = Id(context, suffix),
                locationDefinitionId = definitionId,
                officialName = officialName,
                commonName = officialName,
                semanticTagIds = tags ?? Array.Empty<string>(),
                associatedOrganizationId = organizationId,
                visibility = visibility,
                prototypeSceneBindingKey = binding,
                createdWorldTime = 10d,
                sourceEventId = $"location-source.{context.RunId}",
                provenanceId = "testlab.feature.14.1",
                preview = preview
            });
        }

        private static string Id(TestLabAutomationContext context, string suffix)
        {
            return context.ScenarioContext.ScopedId("location.prototype.test", suffix);
        }

        private static string Tx(TestLabAutomationContext context, string suffix)
        {
            return context.ScenarioContext.ScopedId("location.tx", suffix);
        }
    }
}
#endif
