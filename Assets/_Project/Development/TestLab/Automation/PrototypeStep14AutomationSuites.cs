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

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.14.3.entity-location-occupancy",
                "Entity Location and Occupancy",
                "14.3",
                "Authoritative exact entity placements, derived occupancy, Person/body physical resolution, relocation history, persistence validation, and inventory/world-placement boundaries.",
                14030,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "EntityLocationRuntime", "LocationRuntime", "LocationContainmentLink", "EntityLocationPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("73.1-readiness-and-seeded-placements", "Entity placement runtime is seeded and valid", 10, Step("step14-entity-location-readiness", "Resolve seeded entities and placements", EntityLocationReadiness)),
                    Scenario("73.2-single-active-exact-placement", "One entity cannot have two active exact placements", 20, Step("step14-entity-location-single-active", "Reject conflicting active placement", SingleActiveExactPlacement)),
                    Scenario("73.3-person-resolves-through-body", "Person physical location resolves through active body", 30, Step("step14-entity-location-person-body", "Resolve Person through body placement", PersonResolvesThroughBody)),
                    Scenario("73.4-direct-and-recursive-occupancy", "Direct occupancy is stored while recursive occupancy is derived", 40, Step("step14-entity-location-occupancy", "Query direct and recursive occupancy", DirectAndRecursiveOccupancy)),
                    Scenario("73.5-relocation-history-and-diff", "Relocation ends prior placement and reports hierarchy diff", 50, Step("step14-entity-location-relocate", "Relocate entity atomically", RelocationHistoryAndDiff)),
                    Scenario("73.6-unplacement-last-known", "Unplacement is explicit and last-known remains queryable", 60, Step("step14-entity-location-unplace", "End active placement", UnplacementLastKnown)),
                    Scenario("73.7-location-lifecycle-rejection", "Unavailable locations reject new ordinary placement", 70, Step("step14-entity-location-lifecycle", "Reject placement into closed location", LocationLifecycleRejection)),
                    Scenario("73.8-capacity-and-type-rules", "Capacity and occupant-type rules reject without mutation", 80, Step("step14-entity-location-capacity", "Evaluate capacity rules", CapacityAndTypeRules)),
                    Scenario("73.9-inventory-world-exclusion", "Inventory-held items cannot also occupy world locations", 90, Step("step14-entity-location-inventory", "Reject inventory/world overlap", InventoryWorldExclusion)),
                    Scenario("73.10-persistence-round-trip", "Entity placements persist and restore deterministically", 100, Step("step14-entity-location-persistence", "Save and restore placements", EntityLocationPersistenceRoundTrip)),
                    Scenario("73.11-corrupt-restore-rejection", "Corrupt placement payloads reject before commit", 110, Step("step14-entity-location-corrupt", "Reject corrupt placement restore", EntityLocationCorruptRestore)),
                    Scenario("73.12-fixture-snapshot", "Fixture snapshots restore entity placement mutations", 120, Step("step14-entity-location-fixture", "Snapshot and restore placement state", EntityLocationFixtureSnapshot))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.14.4.interaction-points-functional-locations",
                "Interaction Points and Functional Locations",
                "14.4",
                "Authoritative logical interaction points, service bindings, provider/presence eligibility, sessions, reservations, persistence, and scene-independent routing.",
                14040,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "InteractionPointRuntime", "LocationRuntime", "EntityLocationRuntime", "InteractionPointPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("74.1-readiness-and-definitions", "Interaction point definitions and seeded points are available", 10, Step("step14-interaction-readiness", "Resolve seeded interaction points", InteractionReadiness)),
                    Scenario("74.2-definition-vs-instance", "Point definitions remain separate from runtime records", 20, Step("step14-interaction-definition-separation", "Create multiple points from one definition", InteractionDefinitionVsInstance)),
                    Scenario("74.3-host-validation-and-reassignment", "Hosts validate against location categories and history is preserved", 30, Step("step14-interaction-hosts", "Reject invalid host and reassign valid host", InteractionHostValidation)),
                    Scenario("74.4-subject-provider-boundaries", "Subject links and providers remain references to owning systems", 40, Step("step14-interaction-subject-provider", "Add links and provider assignments", InteractionSubjectProviderBoundaries)),
                    Scenario("74.5-presence-eligibility", "Consumer and provider presence uses entity location authority", 50, Step("step14-interaction-presence", "Evaluate presence-based eligibility", InteractionPresenceEligibility)),
                    Scenario("74.6-capacity-reservation-session", "Capacity, reservations, and sessions are deterministic", 60, Step("step14-interaction-capacity", "Start exclusive use and reject overflow", InteractionCapacityReservationSession)),
                    Scenario("74.7-visibility-and-scene-independence", "Hidden points and scene bindings do not leak or require GameObjects", 70, Step("step14-interaction-scene-independence", "Query visibility and binding keys", InteractionVisibilitySceneIndependence)),
                    Scenario("74.8-destination-routing", "Invocation validates context without owning destination mutation", 80, Step("step14-interaction-routing", "Invoke validated service route", InteractionDestinationRouting)),
                    Scenario("74.9-persistence-validation", "Interaction point persistence round-trips and rejects corrupt graphs", 90, Step("step14-interaction-persistence", "Save, restore, and reject corrupt payload", InteractionPersistenceValidation)),
                    Scenario("74.10-fixture-snapshot", "Fixture snapshots restore interaction point mutations", 100, Step("step14-interaction-fixture", "Snapshot and restore interaction state", InteractionFixtureSnapshot))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.14.5.entrances-exits-connections-access",
                "Entrances, Exits, Connections, and Access",
                "14.5",
                "Scene-independent traversable location connections, endpoints, state gates, access policies, grants, traversal, persistence validation, and visibility-safe projections.",
                14050,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "LocationConnectionRuntime", "LocationRuntime", "EntityLocationRuntime", "LocationConnectionPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("75.1-readiness-and-seeded-connections", "Connection definitions and seeded prototype graph are available", 10, Step("step14-connection-readiness", "Resolve seeded connections and access policies", ConnectionReadiness)),
                    Scenario("75.2-definition-and-adjacency-boundaries", "Connection records stay separate from definitions and spatial adjacency", 20, Step("step14-connection-boundaries", "Verify definition/runtime and adjacency boundaries", ConnectionDefinitionAdjacencyBoundaries)),
                    Scenario("75.3-state-gated-traversal", "Open, lock, blockage, and lifecycle states gate traversal atomically", 30, Step("step14-connection-state-gates", "Evaluate state gates before traversal", ConnectionStateGates)),
                    Scenario("75.4-access-policy-matrix", "Access policies consume external authority references without owning them", 40, Step("step14-connection-access-matrix", "Evaluate representative access inputs", ConnectionAccessPolicyMatrix)),
                    Scenario("75.5-hidden-one-way-and-grants", "Hidden, one-way, and explicit grants are authoritative but projection-safe", 50, Step("step14-connection-visibility-grants", "Evaluate visibility, directionality, and grants", ConnectionVisibilityOneWayGrants)),
                    Scenario("75.6-persistence-validation", "Connection persistence round-trips and rejects corrupt graphs", 60, Step("step14-connection-persistence", "Save, restore, and reject corrupt connection payload", ConnectionPersistenceValidation)),
                    Scenario("75.7-fixture-snapshot", "Fixture snapshots restore connection mutations", 70, Step("step14-connection-fixture", "Snapshot and restore connection state", ConnectionFixtureSnapshot))
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

        private static TestLabAutomationStepResult EntityLocationReadiness(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            bool validRuntime = runtime.ValidateRuntime(out string failure);
            bool hasBody = runtime.TryGetActivePlacement(Body(PrototypeEntityLocationFactory.PlayerBodyId, context), out EntityPlacementSnapshot playerBody);
            EntityLocationResolutionResult player = runtime.ResolvePhysicalLocation(Person(PrototypeEntityLocationFactory.PlayerPersonId, context));
            bool valid = runtime != null && validRuntime && hasBody && player.Succeeded && player.LocationId == "location.prototype.village" && runtime.ActivePlacementCount >= 8 && runtime.KnownEntityCount >= 12;
            return TestLabAssertions.True("step14-entity-location-readiness", "Seeded entity placements validate", valid, $"Valid={validRuntime} Failure={failure} Active={runtime?.ActivePlacementCount} Known={runtime?.KnownEntityCount} Player={player.Status}:{player.LocationId} Body={playerBody?.ExactLocationId}");
        }

        private static TestLabAutomationStepResult SingleActiveExactPlacement(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationReferenceData entity = Body(PrototypeEntityLocationFactory.GuildMasterBodyId, context);
            long before = runtime.Revision;
            EntityLocationOperationResult rejected = runtime.Place(new EntityPlacementRequest
            {
                transactionId = Tx(context, "entity-single-active"),
                placementId = context.ScenarioContext.ScopedId("placement.test", "single-active"),
                entity = entity,
                exactLocationId = "location.prototype.market-district",
                worldTime = 50d
            });
            bool valid = rejected.Status == EntityLocationOperationStatus.ConflictingActivePlacement && runtime.Revision == before && runtime.ResolvePhysicalLocation(entity).LocationId == "location.prototype.guildmaster-office";
            return TestLabAssertions.True("step14-entity-location-single-active", "Conflicting active placement rejects without mutation", valid, $"Status={rejected.Status} Revision={before}->{runtime.Revision} Location={runtime.ResolvePhysicalLocation(entity).LocationId}");
        }

        private static TestLabAutomationStepResult PersonResolvesThroughBody(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationResolutionResult person = runtime.ResolvePhysicalLocation(Person(PrototypeEntityLocationFactory.PrisonerPersonId, context));
            bool directPerson = runtime.TryGetActivePlacement(Person(PrototypeEntityLocationFactory.PrisonerPersonId, context), out _);
            bool valid = person.Status == EntityPhysicalLocationResolutionStatus.ResolvedThroughBody && person.LocationId == "location.prototype.basement-prison" && !directPerson;
            return TestLabAssertions.True("step14-entity-location-person-body", "Person resolves through active body without duplicate Person placement", valid, $"Status={person.Status} Location={person.LocationId} DirectPerson={directPerson}");
        }

        private static TestLabAutomationStepResult DirectAndRecursiveOccupancy(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            LocationOccupancySnapshot direct = runtime.GetDirectOccupancy("location.prototype.adventurers-guild");
            LocationOccupancySnapshot recursive = runtime.GetRecursiveOccupancy("location.prototype.adventurers-guild");
            bool directHasChest = direct.Placements.Any(item => item.EntityId == PrototypeEntityLocationFactory.GuildChestEntityId);
            bool recursiveHasGuildmaster = recursive.Placements.Any(item => item.EntityId == PrototypeEntityLocationFactory.GuildMasterBodyId);
            bool deterministic = recursive.Placements.Select(item => item.EntityKey).SequenceEqual(recursive.Placements.Select(item => item.EntityKey).OrderBy(id => id, StringComparer.Ordinal));
            bool valid = directHasChest && recursiveHasGuildmaster && recursive.Count > direct.Count && deterministic;
            return TestLabAssertions.True("step14-entity-location-occupancy", "Recursive occupancy is derived through location descendants", valid, $"Direct={direct.Count} Recursive={recursive.Count} Chest={directHasChest} Guildmaster={recursiveHasGuildmaster} Deterministic={deterministic}");
        }

        private static TestLabAutomationStepResult RelocationHistoryAndDiff(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationReferenceData entity = Body(PrototypeEntityLocationFactory.MerchantBodyId, context);
            EntityLocationOperationResult move = runtime.Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "entity-relocate"),
                newPlacementId = context.ScenarioContext.ScopedId("placement.test", "merchant-civic"),
                entity = entity,
                expectedOriginLocationId = "location.prototype.merchant-counter",
                destinationLocationId = "location.prototype.civic-office",
                category = EntityPlacementCategory.Visiting,
                worldTime = 100d
            });
            bool oldAtTime = runtime.GetPlacementAtTime(entity, 50d)?.ExactLocationId == "location.prototype.merchant-counter";
            bool now = runtime.ResolvePhysicalLocation(entity).LocationId == "location.prototype.civic-office";
            bool diff = move.TransitionDiff.EnteredLocationIds.Contains("location.prototype.civic-office") && move.TransitionDiff.ExitedLocationIds.Contains("location.prototype.merchant-counter");
            bool valid = move.Succeeded && oldAtTime && now && diff;
            return TestLabAssertions.True("step14-entity-location-relocate", "Relocation preserves history and hierarchy transition diff", valid, $"Move={move.Status} OldAt50={oldAtTime} Now={now} Entered={string.Join(",", move.TransitionDiff.EnteredLocationIds)} Exited={string.Join(",", move.TransitionDiff.ExitedLocationIds)}");
        }

        private static TestLabAutomationStepResult UnplacementLastKnown(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationReferenceData entity = Item(PrototypeEntityLocationFactory.ArrowItemInstanceId, context);
            EntityLocationOperationResult unplace = runtime.Unplace(new EntityUnplacementRequest
            {
                transactionId = Tx(context, "entity-unplace"),
                entity = entity,
                worldTime = 120d,
                sourceEventId = "testlab.entity-location.unplace"
            });
            EntityLocationResolutionResult active = runtime.ResolvePhysicalLocation(entity);
            EntityPlacementSnapshot lastKnown = runtime.GetLastKnownPlacement(entity);
            bool valid = unplace.Succeeded && active.Status == EntityPhysicalLocationResolutionStatus.Unplaced && lastKnown != null && lastKnown.ExactLocationId == "location.prototype.market-district" && lastKnown.LifecycleState == EntityPlacementLifecycleState.Ended;
            return TestLabAssertions.True("step14-entity-location-unplace", "Unplacement ends active occupancy but preserves last-known location", valid, $"Unplace={unplace.Status} Active={active.Status} Last={lastKnown?.ExactLocationId}:{lastKnown?.LifecycleState}");
        }

        private static TestLabAutomationStepResult LocationLifecycleRejection(TestLabAutomationContext context)
        {
            LocationRuntime locations = Runtime(context);
            EntityLocationRuntime runtime = EntityRuntime(context);
            LocationOperationResult room = Create(context, "closed-occupancy", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Closed Occupancy Room", tags: new[] { "room", "interior" });
            LocationOperationResult close = locations.TransitionLifecycle(new LocationLifecycleTransitionRequest { transactionId = Tx(context, "closed-occupancy-location"), locationId = room.Snapshot?.LocationId, targetState = LocationLifecycleState.Closed, worldTime = 130d });
            long before = runtime.Revision;
            EntityLocationOperationResult place = runtime.Place(new EntityPlacementRequest
            {
                transactionId = Tx(context, "entity-closed-place"),
                entity = Item(PrototypeEntityLocationFactory.ArrowItemInstanceId, context),
                exactLocationId = room.Snapshot?.LocationId,
                category = EntityPlacementCategory.Dropped,
                worldTime = 131d
            });
            bool valid = room.Succeeded && close.Succeeded && place.Status == EntityLocationOperationStatus.InactiveLocation && runtime.Revision == before;
            return TestLabAssertions.True("step14-entity-location-lifecycle", "Closed locations reject new ordinary placement without evicting elsewhere", valid, $"Room={room.Status} Close={close.Status} Place={place.Status} Revision={before}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult CapacityAndTypeRules(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            LocationOperationResult room = Create(context, "capacity", PrototypeLocationDefinitionFactory.RoomDefinitionId, "Capacity Room", tags: new[] { "room", "interior" });
            runtime.ConfigureCapacity(new EntityLocationCapacityRuleData
            {
                locationId = room.Snapshot?.LocationId,
                maxDirectOccupants = 1,
                allowedEntityTypes = new[] { LocationOccupantEntityType.Body }
            });
            EntityLocationReferenceData body = new EntityLocationReferenceData { entityType = LocationOccupantEntityType.Body, entityId = context.ScenarioContext.ScopedId("body.prototype.test", "capacity"), worldId = context.ScenarioContext.Runtimes.WorldId };
            EntityLocationReferenceData itemEntity = new EntityLocationReferenceData { entityType = LocationOccupantEntityType.ItemInstance, entityId = context.ScenarioContext.ScopedId("item-instance.test", "capacity"), worldId = context.ScenarioContext.Runtimes.WorldId };
            runtime.RegisterKnownEntity(body);
            runtime.RegisterKnownEntity(itemEntity);
            EntityLocationOperationResult first = runtime.Place(new EntityPlacementRequest { transactionId = Tx(context, "capacity-first"), entity = body, exactLocationId = room.Snapshot?.LocationId, category = EntityPlacementCategory.Present, worldTime = 140d });
            EntityLocationOperationResult item = runtime.Place(new EntityPlacementRequest { transactionId = Tx(context, "capacity-item"), entity = itemEntity, exactLocationId = room.Snapshot?.LocationId, category = EntityPlacementCategory.Dropped, worldTime = 141d });
            bool valid = room.Succeeded && first.Succeeded && item.Status == EntityLocationOperationStatus.OccupantTypeNotAllowed && runtime.GetDirectOccupancy(room.Snapshot.LocationId).Count == 1;
            return TestLabAssertions.True("step14-entity-location-capacity", "Capacity/type rules reject disallowed occupants without mutation", valid, $"Room={room.Status} First={first.Status} Item={item.Status} Count={runtime.GetDirectOccupancy(room.Snapshot?.LocationId).Count}");
        }

        private static TestLabAutomationStepResult InventoryWorldExclusion(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationReferenceData held = new EntityLocationReferenceData { entityType = LocationOccupantEntityType.ItemInstance, entityId = context.ScenarioContext.ScopedId("item-instance.test", "held"), worldId = context.ScenarioContext.Runtimes.WorldId };
            runtime.RegisterKnownEntity(held);
            runtime.MarkInventoryHeld(held, true);
            long before = runtime.Revision;
            EntityLocationOperationResult place = runtime.Place(new EntityPlacementRequest { transactionId = Tx(context, "inventory-held"), entity = held, exactLocationId = "location.prototype.market-district", category = EntityPlacementCategory.Dropped, worldTime = 150d });
            bool valid = place.Status == EntityLocationOperationStatus.InventoryConflict && runtime.Revision == before && !runtime.TryGetActivePlacement(held, out _);
            return TestLabAssertions.True("step14-entity-location-inventory", "Inventory-held item rejects world placement", valid, $"Status={place.Status} Revision={before}->{runtime.Revision}");
        }

        private static TestLabAutomationStepResult EntityLocationPersistenceRoundTrip(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationRuntimeSaveData save = runtime.CreateSaveData();
            EntityLocationRuntime restored = new EntityLocationRuntime();
            EntityLocationOperationResult restore = restored.RestoreFromSaveData(save, Runtime(context), context.ScenarioContext.Runtimes.WorldId, restoring: true);
            EntityLocationResolutionResult player = restored.ResolvePhysicalLocation(Person(PrototypeEntityLocationFactory.PlayerPersonId, context));
            bool valid = restore.Succeeded
                && player.LocationId == "location.prototype.village"
                && restored.CreateSaveData().placements.Select(item => item.placementId).SequenceEqual(save.placements.Select(item => item.placementId));
            return TestLabAssertions.True("step14-entity-location-persistence", "Entity locations save and restore deterministically", valid, $"Restore={restore.Status} Player={player.Status}:{player.LocationId} Count={restored.PlacementCount}/{runtime.PlacementCount}");
        }

        private static TestLabAutomationStepResult EntityLocationCorruptRestore(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            EntityLocationRuntimeSaveData before = runtime.CreateSaveData();
            EntityLocationRuntimeSaveData corrupt = before.Clone();
            corrupt.placements[0].exactLocationId = "location.prototype.missing";
            EntityLocationOperationResult rejected = runtime.RestoreFromSaveData(corrupt, Runtime(context), context.ScenarioContext.Runtimes.WorldId, restoring: true);
            bool unchanged = runtime.CreateSaveData().placements.Select(item => item.exactLocationId).SequenceEqual(before.placements.Select(item => item.exactLocationId));
            bool valid = rejected.Status == EntityLocationOperationStatus.PersistenceInvalid && unchanged;
            return TestLabAssertions.True("step14-entity-location-corrupt", "Corrupt entity placement restore rejects before commit", valid, $"Rejected={rejected.Status} Unchanged={unchanged} Message={rejected.Message}");
        }

        private static TestLabAutomationStepResult EntityLocationFixtureSnapshot(TestLabAutomationContext context)
        {
            EntityLocationRuntime runtime = EntityRuntime(context);
            TestLabRuntimeBundleSnapshot snapshot = context.ScenarioContext.Runtimes.CreateSnapshot();
            int before = runtime.ActivePlacementCount;
            EntityLocationReferenceData entity = new EntityLocationReferenceData { entityType = LocationOccupantEntityType.WorldEntity, entityId = context.ScenarioContext.ScopedId("world-entity.test", "fixture"), worldId = context.ScenarioContext.Runtimes.WorldId };
            runtime.RegisterKnownEntity(entity);
            runtime.Place(new EntityPlacementRequest { transactionId = Tx(context, "entity-fixture"), entity = entity, exactLocationId = "location.prototype.village", category = EntityPlacementCategory.Present, worldTime = 160d });
            bool restored = context.ScenarioContext.Runtimes.RestoreSnapshot(snapshot, out string failure);
            bool missing = !runtime.TryGetActivePlacement(entity, out _);
            bool valid = restored && missing && runtime.ActivePlacementCount == before;
            return TestLabAssertions.True("step14-entity-location-fixture", "Fixture snapshot restores entity placement mutations", valid, $"Restored={restored} Missing={missing} Active={runtime.ActivePlacementCount}/{before} Failure={failure}");
        }

        private static TestLabAutomationStepResult InteractionReadiness(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool hasPointDefinition = registry.TryGet(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterDefinitionId, out InteractionPointDefinition pointDefinition);
            bool hasServiceDefinition = registry.TryGet(PrototypeInteractionPointDefinitionFactory.QuestBoardBrowseServiceId, out InteractionServiceDefinition serviceDefinition);
            bool hasSeededCounter = runtime.TryGetPoint(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId, out InteractionPointSnapshot counter);
            bool hasSeededBoard = runtime.TryGetPoint(PrototypeInteractionPointDefinitionFactory.QuestBoardPointId, out InteractionPointSnapshot board);
            bool validRuntime = runtime.ValidateCurrent(out string failure);
            bool valid = runtime != null
                && hasPointDefinition
                && hasServiceDefinition
                && hasSeededCounter
                && hasSeededBoard
                && validRuntime
                && pointDefinition.Category == InteractionPointCategory.GuildCounter
                && serviceDefinition.DestinationRuntime == InteractionDestinationRuntime.QuestPlaceholder
                && counter.ActiveHostLocationId == "location.prototype.adventurers-guild"
                && board.ServiceDefinitionIds.Contains(PrototypeInteractionPointDefinitionFactory.QuestBoardBrowseServiceId);
            return TestLabAssertions.True("step14-interaction-readiness", "Seeded interaction points and definitions resolve", valid, $"Definitions={hasPointDefinition}/{hasServiceDefinition} Seeded={hasSeededCounter}/{hasSeededBoard} Points={runtime?.PointCount} Validation={validRuntime}:{failure}");
        }

        private static TestLabAutomationStepResult InteractionDefinitionVsInstance(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            InteractionPointOperationResult first = CreateInteractionPoint(context, "shop-a", PrototypeInteractionPointDefinitionFactory.MerchantStallCounterDefinitionId, "Shop Counter A", "location.prototype.merchant-counter", new[] { PrototypeInteractionPointDefinitionFactory.ShopSaleServiceId });
            InteractionPointOperationResult second = CreateInteractionPoint(context, "shop-b", PrototypeInteractionPointDefinitionFactory.MerchantStallCounterDefinitionId, "Shop Counter B", "location.prototype.merchant-counter", new[] { PrototypeInteractionPointDefinitionFactory.ShopSaleServiceId });
            bool valid = first.Succeeded
                && second.Succeeded
                && first.Point.InteractionPointId != second.Point.InteractionPointId
                && first.Point.InteractionPointDefinitionId == second.Point.InteractionPointDefinitionId
                && runtime.GetPointsByDefinition(PrototypeInteractionPointDefinitionFactory.MerchantStallCounterDefinitionId).Count >= 3;
            return TestLabAssertions.True("step14-interaction-definition-separation", "One point definition can back distinct runtime points", valid, $"First={first.Status} Second={second.Status} Count={runtime.GetPointsByDefinition(PrototypeInteractionPointDefinitionFactory.MerchantStallCounterDefinitionId).Count}");
        }

        private static TestLabAutomationStepResult InteractionHostValidation(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            InteractionPointOperationResult invalid = CreateInteractionPoint(context, "invalid-host", PrototypeInteractionPointDefinitionFactory.WorkstationDefinitionId, "Invalid Wilderness Workstation", "location.prototype.wilderness-ring", new[] { PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId });
            InteractionPointOperationResult point = CreateInteractionPoint(context, "valid-host", PrototypeInteractionPointDefinitionFactory.WorkstationDefinitionId, "Valid Workstation", "location.prototype.merchant-counter", new[] { PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId });
            InteractionPointOperationResult reassign = runtime.ReassignHost(new InteractionPointHostReassignmentRequest
            {
                transactionId = Tx(context, "interaction-host-reassign"),
                interactionPointId = point.Point?.InteractionPointId,
                newHostAssignmentId = context.ScenarioContext.ScopedId("interaction-host.test", "reassign"),
                newHostLocationId = "location.prototype.adventurers-guild",
                worldTime = 80d
            });
            bool history = runtime.CreateSaveData().hostAssignments.Count(assignment => assignment.interactionPointId == point.Point?.InteractionPointId) == 2;
            bool valid = invalid.Status == InteractionPointOperationStatus.InvalidHostLocation && point.Succeeded && reassign.Succeeded && reassign.Point.ActiveHostLocationId == "location.prototype.adventurers-guild" && history;
            return TestLabAssertions.True("step14-interaction-hosts", "Invalid hosts reject and valid host reassignments preserve history", valid, $"Invalid={invalid.Status} Point={point.Status} Reassign={reassign.Status} History={history}");
        }

        private static TestLabAutomationStepResult InteractionSubjectProviderBoundaries(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            InteractionPointOperationResult link = runtime.AddSubjectLink(new InteractionSubjectLinkRequest
            {
                transactionId = Tx(context, "interaction-link"),
                linkId = context.ScenarioContext.ScopedId("interaction-subject.test", "guild-counter"),
                interactionPointId = PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId,
                role = InteractionSubjectLinkRole.AssociatedRecordsCollection,
                subject = Subject("KnowledgeRecordCollection", "records.prototype.guild-public", context),
                worldTime = 90d
            });
            InteractionPointOperationResult provider = runtime.AssignProvider(new InteractionProviderAssignmentRequest
            {
                transactionId = Tx(context, "interaction-provider"),
                assignmentId = context.ScenarioContext.ScopedId("interaction-provider.test", "guild-info"),
                interactionPointId = PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.AdventurerInformationServiceId,
                requirementKind = InteractionProviderRequirementKind.AssignedPerson,
                providerEntity = Person(PrototypeEntityLocationFactory.GuildMasterPersonId, context),
                providerOrganizationId = "organization.prototype.guild",
                presencePolicy = InteractionPhysicalPresencePolicy.WithinHostLocation,
                worldTime = 90d
            });
            bool linkFound = runtime.GetSubjectLinks(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId).Any(item => item.Subject.subjectId == "records.prototype.guild-public");
            bool providerFound = runtime.GetProviderAssignments(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId).Any(item => item.ServiceDefinitionId == PrototypeInteractionPointDefinitionFactory.AdventurerInformationServiceId);
            bool valid = link.Succeeded && provider.Succeeded && linkFound && providerFound && context.ScenarioContext.Runtimes.OrganizationMemberships != null && context.ScenarioContext.Runtimes.Records != null;
            return TestLabAssertions.True("step14-interaction-subject-provider", "Subject links and provider assignments reference owning runtimes without owning them", valid, $"Link={link.Status} Provider={provider.Status} Links={runtime.GetSubjectLinks(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId).Count} Providers={runtime.GetProviderAssignments(PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId).Count}");
        }

        private static TestLabAutomationStepResult InteractionPresenceEligibility(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            EntityLocationRuntime entityRuntime = EntityRuntime(context);
            EntityLocationOperationResult movePlayer = entityRuntime.Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "interaction-player-guild"),
                newPlacementId = context.ScenarioContext.ScopedId("placement.test", "interaction-player-guild"),
                entity = Body(PrototypeEntityLocationFactory.PlayerBodyId, context),
                expectedOriginLocationId = "location.prototype.village",
                destinationLocationId = "location.prototype.adventurers-guild",
                category = EntityPlacementCategory.Visiting,
                worldTime = 100d
            });
            InteractionPointOperationResult provider = runtime.AssignProvider(new InteractionProviderAssignmentRequest
            {
                transactionId = Tx(context, "interaction-presence-provider"),
                assignmentId = context.ScenarioContext.ScopedId("interaction-provider.test", "guild-info-presence"),
                interactionPointId = PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.AdventurerInformationServiceId,
                providerEntity = Person(PrototypeEntityLocationFactory.GuildMasterPersonId, context),
                presencePolicy = InteractionPhysicalPresencePolicy.WithinHostLocation,
                worldTime = 100d
            });
            InteractionEligibilityResult eligible = runtime.EvaluateEligibility(new InteractionEligibilityRequest
            {
                interactionPointId = PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.AdventurerInformationServiceId,
                consumerEntity = Person(PrototypeEntityLocationFactory.PlayerPersonId, context)
            });
            InteractionEligibilityResult absent = runtime.EvaluateEligibility(new InteractionEligibilityRequest
            {
                interactionPointId = PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.AdventurerInformationServiceId,
                consumerEntity = Person(PrototypeEntityLocationFactory.MerchantPersonId, context)
            });
            bool valid = movePlayer.Succeeded && provider.Succeeded && eligible.Eligible && absent.Status == InteractionPointOperationStatus.ConsumerAbsent;
            return TestLabAssertions.True("step14-interaction-presence", "Interaction eligibility consults entity location presence", valid, $"Move={movePlayer.Status} Provider={provider.Status} Eligible={eligible.Status}:{eligible.Fingerprint} Absent={absent.Status}:{string.Join(",", absent.FailureReasons)}");
        }

        private static TestLabAutomationStepResult InteractionCapacityReservationSession(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            EntityLocationRuntime entityRuntime = EntityRuntime(context);
            entityRuntime.Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "interaction-player-board"),
                newPlacementId = context.ScenarioContext.ScopedId("placement.test", "interaction-player-board"),
                entity = Body(PrototypeEntityLocationFactory.PlayerBodyId, context),
                expectedOriginLocationId = "location.prototype.village",
                destinationLocationId = "location.prototype.merchant-counter",
                category = EntityPlacementCategory.Visiting,
                worldTime = 110d
            });
            InteractionPointOperationResult point = CreateInteractionPoint(context, "capacity-workstation", PrototypeInteractionPointDefinitionFactory.WorkstationDefinitionId, "Capacity Workstation", "location.prototype.merchant-counter", new[] { PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId });
            InteractionPointOperationResult reservation = runtime.Reserve(new InteractionReservationRequest
            {
                transactionId = Tx(context, "interaction-reserve"),
                reservationId = context.ScenarioContext.ScopedId("interaction-reservation.test", "quest-board"),
                interactionPointId = point.Point?.InteractionPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId,
                reservingSubject = Subject("Person", PrototypeEntityLocationFactory.PlayerPersonId, context),
                startWorldTime = 111d,
                endWorldTime = 120d
            });
            InteractionPointOperationResult session = runtime.StartSession(new InteractionSessionStartRequest
            {
                transactionId = Tx(context, "interaction-session"),
                sessionId = context.ScenarioContext.ScopedId("interaction-session.test", "quest-board"),
                interactionPointId = point.Point?.InteractionPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId,
                consumerEntity = Person(PrototypeEntityLocationFactory.PlayerPersonId, context),
                reservationId = reservation.Reservation?.ReservationId,
                startWorldTime = 112d
            });
            InteractionPointOperationResult duplicate = runtime.StartSession(new InteractionSessionStartRequest
            {
                transactionId = Tx(context, "interaction-session-second"),
                sessionId = context.ScenarioContext.ScopedId("interaction-session.test", "quest-board-second"),
                interactionPointId = point.Point?.InteractionPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId,
                consumerEntity = Person(PrototypeEntityLocationFactory.MerchantPersonId, context),
                startWorldTime = 113d
            });
            bool valid = point.Succeeded && reservation.Succeeded && session.Succeeded && duplicate.Status == InteractionPointOperationStatus.CapacityFull && runtime.GetUseSessions(point.Point.InteractionPointId).Count == 1;
            return TestLabAssertions.True("step14-interaction-capacity", "Capacity and reservation/session records are enforced deterministically", valid, $"Point={point.Status} Reservation={reservation.Status} Session={session.Status} Second={duplicate.Status} Sessions={runtime.GetUseSessions(point.Point?.InteractionPointId).Count}");
        }

        private static TestLabAutomationStepResult InteractionVisibilitySceneIndependence(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            InteractionPointOperationResult hidden = CreateInteractionPoint(context, "hidden", PrototypeInteractionPointDefinitionFactory.PrisonCellDefinitionId, "Hidden Cell Point", "location.prototype.basement-prison", new[] { PrototypeInteractionPointDefinitionFactory.PrisonCellInspectServiceId }, InteractionPointVisibility.Hidden, "prototype.scene.binding.hidden-cell");
            IReadOnlyList<InteractionPointSnapshot> normal = runtime.GetPointsByHost("location.prototype.basement-prison");
            IReadOnlyList<InteractionPointSnapshot> privileged = runtime.GetPointsByHost("location.prototype.basement-prison", includeHidden: true);
            bool normalOmitted = normal.All(item => item.InteractionPointId != hidden.Point?.InteractionPointId);
            bool privilegedFound = privileged.Any(item => item.InteractionPointId == hidden.Point?.InteractionPointId && item.SceneBindingKey == "prototype.scene.binding.hidden-cell");
            bool valid = hidden.Succeeded && normalOmitted && privilegedFound;
            return TestLabAssertions.True("step14-interaction-scene-independence", "Scene binding keys are persisted without requiring scene objects and hidden points can be omitted", valid, $"Hidden={hidden.Status} Normal={normal.Count} Privileged={privileged.Count} Binding={hidden.Point?.SceneBindingKey}");
        }

        private static TestLabAutomationStepResult InteractionDestinationRouting(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            EntityRuntime(context).Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "interaction-route-player"),
                newPlacementId = context.ScenarioContext.ScopedId("placement.test", "interaction-route-player"),
                entity = Body(PrototypeEntityLocationFactory.PlayerBodyId, context),
                expectedOriginLocationId = "location.prototype.village",
                destinationLocationId = "location.prototype.adventurers-guild",
                category = EntityPlacementCategory.Visiting,
                worldTime = 125d
            });
            long before = runtime.Revision;
            InteractionInvocationResult result = runtime.Invoke(new InteractionRequest
            {
                transactionId = Tx(context, "interaction-route"),
                interactionPointId = PrototypeInteractionPointDefinitionFactory.QuestBoardPointId,
                serviceDefinitionId = PrototypeInteractionPointDefinitionFactory.QuestBoardBrowseServiceId,
                consumerEntity = Person(PrototypeEntityLocationFactory.PlayerPersonId, context),
                preview = false
            });
            bool valid = result.Success && result.DestinationRuntime == InteractionDestinationRuntime.QuestPlaceholder && runtime.Revision == before && result.RevisionBefore == result.RevisionAfter;
            return TestLabAssertions.True("step14-interaction-routing", "Interaction invocation validates route and leaves destination mutation to owning runtime", valid, $"Success={result.Success} Destination={result.DestinationRuntime} Revision={before}->{runtime.Revision} Message={result.Message}");
        }

        private static TestLabAutomationStepResult InteractionPersistenceValidation(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            InteractionPointPersistenceParticipant participant = new InteractionPointPersistenceParticipant(runtime, () => context.ScenarioContext.Runtimes.DefinitionRegistry, () => Runtime(context), () => EntityRuntime(context), context.ScenarioContext.Runtimes.WorldId);
            var save = participant.CapturePayload();
            var prepared = participant.PreparePayload(save.PayloadJson, InteractionPointPersistenceParticipant.CurrentParticipantSchemaVersion);
            InteractionPointRuntime restored = new InteractionPointRuntime();
            restored.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, Runtime(context), EntityRuntime(context), context.ScenarioContext.Runtimes.WorldId);
            InteractionPointOperationResult restore = restored.RestoreFromSaveData(runtime.CreateSaveData(), Runtime(context), EntityRuntime(context), context.ScenarioContext.Runtimes.WorldId, restoring: true);
            InteractionPointRuntimeSaveData corrupt = runtime.CreateSaveData();
            corrupt.points[0].activeHostLocationId = "location.prototype.missing";
            var rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), InteractionPointPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool valid = save.Succeeded && prepared.Succeeded && restore.Succeeded && rejected != null && !rejected.Succeeded && restored.PointCount == runtime.PointCount;
            return TestLabAssertions.True("step14-interaction-persistence", "Interaction point save data round-trips and corrupt graphs reject before commit", valid, $"Save={save.Succeeded} Prepare={prepared.Succeeded} Restore={restore.Status} Rejected={rejected?.Succeeded == false} Count={restored.PointCount}/{runtime.PointCount}");
        }

        private static TestLabAutomationStepResult InteractionFixtureSnapshot(TestLabAutomationContext context)
        {
            InteractionPointRuntime runtime = InteractionRuntime(context);
            TestLabRuntimeBundleSnapshot snapshot = context.ScenarioContext.Runtimes.CreateSnapshot();
            int before = runtime.PointCount;
            InteractionPointOperationResult created = CreateInteractionPoint(context, "fixture", PrototypeInteractionPointDefinitionFactory.WorkstationDefinitionId, "Fixture Workstation", "location.prototype.merchant-counter", new[] { PrototypeInteractionPointDefinitionFactory.WorkstationUseServiceId });
            bool restored = context.ScenarioContext.Runtimes.RestoreSnapshot(snapshot, out string failure);
            bool missing = !runtime.TryGetPoint(created.Point?.InteractionPointId, out _);
            bool valid = created.Succeeded && restored && missing && runtime.PointCount == before;
            return TestLabAssertions.True("step14-interaction-fixture", "Fixture snapshot restores interaction point mutations", valid, $"Created={created.Status} Restored={restored} Missing={missing} Count={runtime.PointCount}/{before} Failure={failure}");
        }

        private static TestLabAutomationStepResult ConnectionReadiness(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            DefinitionRegistry registry = context.ScenarioContext.Runtimes.DefinitionRegistry;
            bool hasDoor = registry.TryGet(PrototypeLocationConnectionDefinitionFactory.LockableDoorDefinitionId, out LocationConnectionDefinition door);
            bool hasPolicy = registry.TryGet(PrototypeLocationConnectionDefinitionFactory.GuildMemberAccessPolicyId, out LocationAccessPolicyDefinition policy);
            bool hasVillage = runtime.TryGetConnection(PrototypeLocationConnectionDefinitionFactory.VillageGuildEntranceConnectionId, out LocationConnectionSnapshot village);
            bool hasHidden = runtime.GetOutgoingConnections("location.prototype.guildmaster-office", includeHidden: true).Any(item => item.ConnectionId == PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId);
            bool validation = runtime.ValidateCurrent(out string failure);
            bool valid = hasDoor && hasPolicy && hasVillage && hasHidden && validation && door.SupportsLockState && policy.Category == LocationAccessPolicyCategory.OrganizationMembers && village.SceneBindingCategory == LocationConnectionSceneBindingCategory.PrototypeMarker;
            return TestLabAssertions.True("step14-connection-readiness", "Connection definitions and seeded records resolve", valid, $"Door={hasDoor} Policy={hasPolicy} Village={hasVillage} Hidden={hasHidden} Count={runtime.ConnectionCount} Validation={validation} Failure={failure}");
        }

        private static TestLabAutomationStepResult ConnectionDefinitionAdjacencyBoundaries(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            LocationConnectionOperationResult preview = CreateConnection(context, "preview", "location.prototype.adventurers-guild", "location.prototype.merchant-counter", preview: true);
            LocationConnectionOperationResult first = CreateConnection(context, "runtime-a", "location.prototype.adventurers-guild", "location.prototype.merchant-counter");
            LocationConnectionOperationResult second = CreateConnection(context, "runtime-b", "location.prototype.adventurers-guild", "location.prototype.merchant-counter");
            LocationConnectionOperationResult duplicate = CreateConnection(context, "runtime-a", "location.prototype.adventurers-guild", "location.prototype.merchant-counter");
            bool adjacencyDoesNotConnectGuild = Runtime(context).GetSpatialRelationships("location.prototype.market-district").Any()
                && !runtime.GetOutgoingConnections("location.prototype.market-district").Any(item => item.DestinationLocationId == "location.prototype.adventurers-guild");
            bool valid = preview.Status == LocationConnectionOperationStatus.Preview
                && !runtime.TryGetConnection(preview.Connection?.ConnectionId, out _)
                && first.Succeeded
                && second.Succeeded
                && first.Connection.ConnectionDefinitionId == second.Connection.ConnectionDefinitionId
                && first.Connection.ConnectionId != second.Connection.ConnectionId
                && duplicate.Status == LocationConnectionOperationStatus.Duplicate
                && adjacencyDoesNotConnectGuild;
            return TestLabAssertions.True("step14-connection-boundaries", "Connections are runtime records and do not derive from spatial adjacency", valid, $"Preview={preview.Status} First={first.Status} Second={second.Status} Duplicate={duplicate.Status} AdjacencySeparated={adjacencyDoesNotConnectGuild}");
        }

        private static TestLabAutomationStepResult ConnectionStateGates(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            EntityLocationRuntime entities = EntityRuntime(context);
            EntityLocationReferenceData actor = Body(PrototypeEntityLocationFactory.PlayerBodyId, context);
            EntityLocationOperationResult moveToGuild = entities.Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "connection-move-guild"),
                newPlacementId = context.ScenarioContext.ScopedId("placement.test", "connection-move-guild"),
                entity = actor,
                destinationLocationId = "location.prototype.adventurers-guild",
                worldTime = 19d
            });
            long entityBefore = entities.Revision;
            LocationConnectionOperationResult locked = runtime.Traverse(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.guildmaster-office", AccessContext(context, actor, organizations: new[] { "organization.prototype.guild" })));
            long entityAfterLocked = entities.Revision;
            LocationConnectionOperationResult unlock = UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId);
            LocationConnectionOperationResult block = runtime.MutateState(new LocationConnectionStateMutationRequest
            {
                transactionId = Tx(context, "connection-block"),
                connectionId = PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId,
                blockageState = LocationConnectionBlockageState.TemporarilyBlocked,
                worldTime = 20d
            });
            LocationConnectionOperationResult blocked = runtime.Traverse(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.guildmaster-office", AccessContext(context, actor, offices: new[] { "office.prototype.guild-head" }, authorities: new[] { "permission.prototype.guild.rank-admin" })));
            LocationConnectionOperationResult clear = runtime.MutateState(new LocationConnectionStateMutationRequest
            {
                transactionId = Tx(context, "connection-clear"),
                connectionId = PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId,
                blockageState = LocationConnectionBlockageState.Clear,
                worldTime = 21d
            });
            LocationConnectionOperationResult traversed = runtime.Traverse(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.guildmaster-office", AccessContext(context, actor, offices: new[] { "office.prototype.guild-head" }, authorities: new[] { "permission.prototype.guild.rank-admin" })));
            bool placed = entities.TryGetActivePlacement(actor, out EntityPlacementSnapshot placement);
            bool valid = moveToGuild.Succeeded
                && locked.Status == LocationConnectionOperationStatus.MissingKey
                && entityBefore == entityAfterLocked
                && unlock.Succeeded
                && block.Succeeded
                && blocked.Status == LocationConnectionOperationStatus.DeniedByBlockage
                && clear.Succeeded
                && traversed.Succeeded
                && traversed.PlacementResult?.Succeeded == true
                && placed
                && placement.ExactLocationId == "location.prototype.guildmaster-office";
            return TestLabAssertions.True("step14-connection-state-gates", "Connection states gate traversal without partial movement", valid, $"Move={moveToGuild.Status} Locked={locked.Status} Unlock={unlock.Status} Block={block.Status} Blocked={blocked.Status} Clear={clear.Status} Traversed={traversed.Status} Placement={placement?.ExactLocationId}");
        }

        private static TestLabAutomationStepResult ConnectionAccessPolicyMatrix(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            EntityLocationReferenceData actor = Body(PrototypeEntityLocationFactory.PlayerBodyId, context);
            UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId);
            UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.MayorOfficeConnectionId);
            UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.RecordsOfficeConnectionId);
            UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.GuildStorageConnectionId);
            UnlockConnection(context, PrototypeLocationConnectionDefinitionFactory.PrisonCellConnectionId);

            LocationConnectionAccessResult guildDenied = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.guildmaster-office", AccessContext(context, actor)));
            LocationConnectionAccessResult guildOffice = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildHeadOfficeConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.guildmaster-office", AccessContext(context, actor, offices: new[] { "office.prototype.guild-head" }, authorities: new[] { "permission.prototype.guild.rank-admin" })));
            LocationConnectionAccessResult mayor = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.MayorOfficeConnectionId, actor, "location.prototype.civic-office", "location.prototype.mayor-office", AccessContext(context, actor, offices: new[] { "office.prototype.mayor" }, authorities: new[] { "authority.government.prototype" })));
            LocationConnectionAccessResult recordsEmployment = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.RecordsOfficeConnectionId, actor, "location.prototype.civic-office", "location.prototype.mayor-office", AccessContext(context, actor, employments: new[] { "employment.prototype.records-clerk" })));
            LocationConnectionAccessResult recordsPermit = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.RecordsOfficeConnectionId, actor, "location.prototype.civic-office", "location.prototype.mayor-office", AccessContext(context, actor, permits: new[] { "legal-right.prototype.records.restricted-read" })));
            LocationConnectionAccessResult recordsWarrant = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.RecordsOfficeConnectionId, actor, "location.prototype.civic-office", "location.prototype.mayor-office", AccessContext(context, actor, warrants: new[] { "warrant.prototype.search" })));
            LocationConnectionAccessResult storageOwner = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildStorageConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.merchant-counter", AccessContext(context, actor, properties: new[] { "property.prototype.guild-storage" })));
            LocationConnectionAccessResult storageKey = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.GuildStorageConnectionId, actor, "location.prototype.adventurers-guild", "location.prototype.merchant-counter", AccessContext(context, actor, keyDefinitions: new[] { "item.prototype-storage-key" })));
            LocationConnectionAccessResult custody = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.PrisonCellConnectionId, actor, "location.prototype.civic-office", "location.prototype.basement-prison", AccessContext(context, actor, custodyRoles: new[] { "custody-role.prototype.guard" })));

            bool valid = guildDenied.accessState == LocationConnectionAccessState.MissingAuthority
                && guildOffice.Allowed
                && mayor.Allowed
                && recordsEmployment.Allowed
                && recordsPermit.Allowed
                && recordsWarrant.Allowed
                && storageOwner.Allowed
                && storageKey.Allowed
                && custody.Allowed;
            return TestLabAssertions.True("step14-connection-access-matrix", "Access policies consume external references without owning external records", valid, $"GuildDenied={guildDenied.accessState} GuildOffice={guildOffice.accessState} Mayor={mayor.accessState} Employment={recordsEmployment.accessState} Permit={recordsPermit.accessState} Warrant={recordsWarrant.accessState} Owner={storageOwner.accessState} Key={storageKey.accessState} Custody={custody.accessState}");
        }

        private static TestLabAutomationStepResult ConnectionVisibilityOneWayGrants(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            EntityLocationRuntime entities = EntityRuntime(context);
            EntityLocationReferenceData actor = Body(PrototypeEntityLocationFactory.PlayerBodyId, context);
            bool hiddenOmitted = !runtime.GetOutgoingConnections("location.prototype.guildmaster-office").Any(item => item.ConnectionId == PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId);
            bool hiddenIncluded = runtime.GetOutgoingConnections("location.prototype.guildmaster-office", includeHidden: true).Any(item => item.ConnectionId == PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId);
            LocationConnectionAccessResult forward = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.DungeonOneWayDropConnectionId, actor, "location.prototype.wilderness-ring", "location.prototype.dungeon-entry", AccessContext(context, actor, privileged: true)));
            LocationConnectionAccessResult reverse = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.DungeonOneWayDropConnectionId, actor, "location.prototype.dungeon-entry", "location.prototype.wilderness-ring", AccessContext(context, actor, privileged: true)));
            EntityLocationOperationResult move = entities.Relocate(new EntityRelocationRequest
            {
                transactionId = Tx(context, "connection-hidden-move"),
                entity = actor,
                destinationLocationId = "location.prototype.guildmaster-office",
                worldTime = 30d
            });
            LocationConnectionAccessResult denied = runtime.EvaluateAccess(Traversal(context, PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId, actor, "location.prototype.guildmaster-office", "location.prototype.basement-prison", AccessContext(context, actor)));
            LocationConnectionOperationResult grant = runtime.GrantAccess(new LocationAccessGrantRequest
            {
                transactionId = Tx(context, "connection-hidden-grant"),
                grantId = context.ScenarioContext.ScopedId("location-access-grant.test", "hidden"),
                connectionId = PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId,
                grantee = actor,
                startWorldTime = 30d,
                endWorldTime = 40d
            });
            LocationConnectionOperationResult traverse = runtime.Traverse(Traversal(context, PrototypeLocationConnectionDefinitionFactory.HiddenPassageConnectionId, actor, "location.prototype.guildmaster-office", "location.prototype.basement-prison", AccessContext(context, actor), worldTime: 35d));
            bool valid = hiddenOmitted
                && hiddenIncluded
                && forward.Allowed
                && reverse.accessState == LocationConnectionAccessState.DeniedByDirection
                && move.Succeeded
                && !denied.Allowed
                && grant.Succeeded
                && traverse.Succeeded;
            return TestLabAssertions.True("step14-connection-visibility-grants", "Hidden, one-way, and explicit grant rules are authoritative", valid, $"Hidden={hiddenOmitted}/{hiddenIncluded} OneWay={forward.accessState}/{reverse.accessState} Move={move.Status} Denied={denied.accessState} Grant={grant.Status} Traverse={traverse.Status}");
        }

        private static TestLabAutomationStepResult ConnectionPersistenceValidation(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            LocationConnectionPersistenceParticipant participant = new LocationConnectionPersistenceParticipant(runtime, () => context.ScenarioContext.Runtimes.DefinitionRegistry, () => Runtime(context), () => EntityRuntime(context), () => InteractionRuntime(context), context.ScenarioContext.Runtimes.WorldId);
            PersistenceParticipantSaveResult save = participant.CapturePayload();
            var prepared = participant.PreparePayload(save.PayloadJson, LocationConnectionPersistenceParticipant.CurrentParticipantSchemaVersion);
            LocationConnectionRuntime restored = new LocationConnectionRuntime();
            restored.Configure(context.ScenarioContext.Runtimes.DefinitionRegistry, Runtime(context), EntityRuntime(context), InteractionRuntime(context), context.ScenarioContext.Runtimes.WorldId);
            LocationConnectionOperationResult restore = restored.RestoreFromSaveData(JsonUtility.FromJson<LocationConnectionRuntimeSaveData>(save.PayloadJson), Runtime(context), EntityRuntime(context), InteractionRuntime(context), context.ScenarioContext.Runtimes.WorldId);
            LocationConnectionRuntimeSaveData before = runtime.CreateSaveData();
            LocationConnectionRuntimeSaveData corrupt = before.Clone();
            corrupt.connections[0].destinationLocationId = "location.prototype.missing";
            var rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), LocationConnectionPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool unchanged = runtime.CreateSaveData().connections.Select(item => item.destinationLocationId).SequenceEqual(before.connections.Select(item => item.destinationLocationId));
            bool valid = save.Succeeded && prepared.Succeeded && restore.Succeeded && restored.ConnectionCount == runtime.ConnectionCount && !rejected.Succeeded && unchanged;
            return TestLabAssertions.True("step14-connection-persistence", "Persistence round-trips and rejects corrupt graphs before commit", valid, $"Save={save.Succeeded}:{save.Message} Prepare={prepared.Succeeded}:{prepared.Message} Restore={restore.Status} Rejected={rejected.Succeeded}:{rejected.Message} Unchanged={unchanged} Count={restored.ConnectionCount}/{runtime.ConnectionCount}");
        }

        private static TestLabAutomationStepResult ConnectionFixtureSnapshot(TestLabAutomationContext context)
        {
            LocationConnectionRuntime runtime = ConnectionRuntime(context);
            TestLabRuntimeBundleSnapshot snapshot = context.ScenarioContext.Runtimes.CreateSnapshot();
            int before = runtime.ConnectionCount;
            LocationConnectionOperationResult created = CreateConnection(context, "fixture", "location.prototype.adventurers-guild", "location.prototype.merchant-counter");
            bool restored = context.ScenarioContext.Runtimes.RestoreSnapshot(snapshot, out string failure);
            bool missing = !runtime.TryGetConnection(created.Connection?.ConnectionId, out _);
            bool valid = created.Succeeded && restored && missing && runtime.ConnectionCount == before;
            return TestLabAssertions.True("step14-connection-fixture", "Fixture snapshot restores connection mutations", valid, $"Created={created.Status} Restored={restored} Missing={missing} Count={runtime.ConnectionCount}/{before} Failure={failure}");
        }

        private static LocationRuntime Runtime(TestLabAutomationContext context)
        {
            return context?.ScenarioContext?.Runtimes?.Locations;
        }

        private static EntityLocationRuntime EntityRuntime(TestLabAutomationContext context)
        {
            return context?.ScenarioContext?.Runtimes?.EntityLocations;
        }

        private static InteractionPointRuntime InteractionRuntime(TestLabAutomationContext context)
        {
            return context?.ScenarioContext?.Runtimes?.InteractionPoints;
        }

        private static LocationConnectionRuntime ConnectionRuntime(TestLabAutomationContext context)
        {
            return context?.ScenarioContext?.Runtimes?.LocationConnections;
        }

        private static EntityLocationReferenceData Person(string id, TestLabAutomationContext context)
        {
            return PrototypeEntityLocationFactory.Person(id, context.ScenarioContext.Runtimes.WorldId);
        }

        private static EntityLocationReferenceData Body(string id, TestLabAutomationContext context)
        {
            return PrototypeEntityLocationFactory.Body(id, context.ScenarioContext.Runtimes.WorldId);
        }

        private static EntityLocationReferenceData Item(string id, TestLabAutomationContext context)
        {
            return PrototypeEntityLocationFactory.Item(id, context.ScenarioContext.Runtimes.WorldId);
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

        private static LocationConnectionOperationResult CreateConnection(TestLabAutomationContext context, string suffix, string source, string destination, bool preview = false, string transactionId = null)
        {
            string connectionId = context.ScenarioContext.ScopedId("location-connection.test", suffix);
            return ConnectionRuntime(context).CreateConnection(new LocationConnectionCreateRequest
            {
                transactionId = transactionId ?? Tx(context, $"connection-{suffix}"),
                connectionId = connectionId,
                connectionDefinitionId = PrototypeLocationConnectionDefinitionFactory.PublicDoorwayDefinitionId,
                displayName = connectionId,
                sourceLocationId = source,
                destinationLocationId = destination,
                accessPolicyDefinitionIds = new[] { PrototypeLocationConnectionDefinitionFactory.PublicAccessPolicyId },
                sceneBindingKey = $"prototype.connection.{suffix}",
                sceneBindingCategory = LocationConnectionSceneBindingCategory.PrototypeMarker,
                worldTime = 10d,
                sourceEventId = "testlab.feature.14.5",
                provenanceId = "testlab.feature.14.5",
                preview = preview
            });
        }

        private static LocationConnectionOperationResult UnlockConnection(TestLabAutomationContext context, string connectionId)
        {
            return ConnectionRuntime(context).MutateState(new LocationConnectionStateMutationRequest
            {
                transactionId = Tx(context, $"connection-unlock-{connectionId}"),
                connectionId = connectionId,
                openState = LocationConnectionOpenState.Open,
                lockState = LocationConnectionLockState.Unlocked,
                blockageState = LocationConnectionBlockageState.Clear,
                worldTime = 15d
            });
        }

        private static LocationConnectionTraversalRequest Traversal(TestLabAutomationContext context, string connectionId, EntityLocationReferenceData actor, string from, string to, LocationConnectionAccessContextData accessContext, double worldTime = 20d)
        {
            return new LocationConnectionTraversalRequest
            {
                transactionId = Tx(context, $"connection-traverse-{connectionId}-{worldTime}"),
                connectionId = connectionId,
                actor = actor,
                fromLocationId = from,
                toLocationId = to,
                accessContext = accessContext,
                worldTime = worldTime,
                sourceEventId = "testlab.feature.14.5",
                provenanceId = "testlab.feature.14.5"
            };
        }

        private static LocationConnectionAccessContextData AccessContext(
            TestLabAutomationContext context,
            EntityLocationReferenceData actor,
            bool privileged = false,
            string[] organizations = null,
            string[] ranks = null,
            string[] offices = null,
            string[] authorities = null,
            string[] employments = null,
            string[] properties = null,
            string[] permits = null,
            string[] warrants = null,
            string[] custodyRoles = null,
            string[] keyDefinitions = null)
        {
            return new LocationConnectionAccessContextData
            {
                actor = actor,
                personId = PrototypeEntityLocationFactory.PlayerPersonId,
                organizationIds = organizations ?? Array.Empty<string>(),
                rankIds = ranks ?? Array.Empty<string>(),
                officeIds = offices ?? Array.Empty<string>(),
                authorityIds = authorities ?? Array.Empty<string>(),
                employmentIds = employments ?? Array.Empty<string>(),
                propertyIds = properties ?? Array.Empty<string>(),
                permitIds = permits ?? Array.Empty<string>(),
                warrantIds = warrants ?? Array.Empty<string>(),
                custodyRoleIds = custodyRoles ?? Array.Empty<string>(),
                keyDefinitionIds = keyDefinitions ?? Array.Empty<string>(),
                privileged = privileged
            };
        }

        private static InteractionSubjectReferenceData Subject(string subjectType, string subjectId, TestLabAutomationContext context)
        {
            return new InteractionSubjectReferenceData
            {
                subjectType = subjectType,
                subjectId = subjectId,
                worldId = context.ScenarioContext.Runtimes.WorldId
            };
        }

        private static InteractionPointOperationResult CreateInteractionPoint(
            TestLabAutomationContext context,
            string suffix,
            string definitionId,
            string displayName,
            string hostLocationId,
            IEnumerable<string> serviceIds,
            InteractionPointVisibility visibility = InteractionPointVisibility.Public,
            string sceneBindingKey = null,
            bool preview = false)
        {
            return InteractionRuntime(context).CreatePoint(new InteractionPointCreateRequest
            {
                transactionId = Tx(context, $"interaction-{suffix}"),
                interactionPointId = context.ScenarioContext.ScopedId("interaction-point.test", suffix),
                interactionPointDefinitionId = definitionId,
                displayName = displayName,
                hostLocationId = hostLocationId,
                hostAssignmentId = context.ScenarioContext.ScopedId("interaction-host.test", suffix),
                serviceDefinitionIds = serviceIds ?? Array.Empty<string>(),
                visibility = visibility,
                sceneBindingKey = sceneBindingKey,
                sceneBindingCategory = string.IsNullOrWhiteSpace(sceneBindingKey) ? InteractionSceneBindingCategory.None : InteractionSceneBindingCategory.PrototypeMarker,
                worldTime = 70d,
                sourceEventId = "testlab.feature.14.4",
                provenanceId = "testlab.feature.14.4",
                preview = preview
            });
        }
    }
}
#endif
