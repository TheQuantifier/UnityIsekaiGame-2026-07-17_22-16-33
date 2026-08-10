# Step 14 World, Location, and Travel Integration Finalization

Feature 14.12 finalizes Step 14 as a coordinated world/location/travel layer without replacing the specialized owner runtimes introduced by Features 14.1 through 14.11.

## Authority

`LocationRuntime` owns world identity, location records, lifecycle, names, containment, and spatial relationships.

`EntityLocationRuntime` owns exact entity placement, relocation history, inventory/world exclusion, occupancy derivation, and Person-to-body physical resolution.

`InteractionPointRuntime` owns logical functional locations, provider/consumer eligibility, reservations, and use sessions.

`LocationConnectionRuntime` owns entrances, exits, traversable connection state, access grants, and traversal gate evaluation.

`LocationRouteRuntime` owns route segments, route networks, route planning, plan revalidation, and deterministic route graph projections.

`TravelJourneyRuntime` owns accepted journeys, lifecycle, progress, scheduler-derived state, pause/resume/cancel behavior, and replan history.

`TravelConditionRuntime` owns travel restrictions, movement/cost modifiers, hazards, and explicit travel encounters.

`PoliticalTravelRuntime` owns checkpoint, authorization, and border-crossing records while consuming Step 13 government, law, crime, and justice state by reference.

`WorldSceneBindingRuntime` is transient. It binds Unity scene objects to authoritative records but never owns logical world state. Scene transforms, lights, colliders, and meshes remain presentation/physics concerns.

`MovementHistoryService` is derived. It reconstructs current and historical movement from authoritative Step 14 sources and does not persist independent movement truth.

`InformationAccessRuntime` from Step 8 remains responsible for visibility and redaction decisions.

## Integration Layer

`Step14WorldIntegrationValidator` is a read-only finalization and diagnostics layer. It:

- builds on `Step14PersistenceManifestBuilder`;
- validates dependency ordering and ownership uniqueness;
- verifies world-scope consistency across Step 14 save participants;
- checks stable ID uniqueness and representative cross-runtime references;
- catches containment cycles, duplicate active placements, missing exact locations, missing endpoints, invalid route metrics, missing journey references, missing travel-condition references, and missing political route references;
- imports scene-binding validation without making scene bindings authoritative;
- creates deterministic canonical fingerprints from sorted save graph content;
- exposes the Step 15 handoff contract.

The integration layer is not a mega-runtime and does not mutate game state.

## Step 15 Handoff

Step 15 and later systems should use stable Step 14 references and query/command contracts instead of inspecting raw runtime collections directly.

Stable references include world IDs, location IDs, location paths, entity-location references, interaction-point IDs, connection IDs, route segment IDs, journey IDs, travel-condition IDs, encounter IDs, checkpoint IDs, authorization IDs, and scene-binding keys.

Queries include current location, location-at-time, containment paths, occupants, interaction availability, connection access, route planning, route revalidation, active journeys, movement history, travel conditions, political travel requirements, and scene binding resolution.

Commands include creating locations, assigning containment, relocating entities, reserving interaction points, traversing connections, granting access, creating route segments, starting journeys, journey lifecycle control, applying travel conditions, triggering explicit encounters, and recording border crossings.

Deferred systems include quests, dialogue behavior, autonomous NPC decisions, world streaming, multiplayer authority, final UI visibility, procedural settlement generation, and scene rendering.

## Validation

Feature 14.12 adds Edit Mode tests and Test Lab automation for:

- authority separation;
- readiness evaluation;
- world-scope drift rejection;
- containment and placement invariant checks;
- deterministic fingerprinting;
- scene binding non-authority;
- Step 15 handoff coverage.

Completion remains conditional on successful Unity Edit Mode and command-side Test Lab automation execution.
