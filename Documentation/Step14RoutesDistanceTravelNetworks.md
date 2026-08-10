# Step 14.6 Routes, Distance, and Travel Networks

Feature 14.6 adds route planning on top of the Step 14 world-location model.

## Ownership

`LocationRouteRuntime` owns authored route segments and route networks. These are logical world records such as roads, trails, corridors, bridge routes, and dungeon paths.

`LocationConnectionRuntime` remains the owner of entrances, doors, exits, locks, open state, traversal grants, blockage, destruction, and local access state. Route planning consumes connection snapshots as graph edges but does not copy or mutate connection ownership.

`LocationRuntime` remains the owner of location identity, hierarchy, spatial relationships, and visibility. Route endpoints reference location IDs and are rejected if those locations are missing.

## Persistent State

The route save graph stores:

- Route segment records.
- Route network records.
- Route segment history records.
- Idempotent transaction records.

It does not store route plans. Plans are immutable derived results that contain the route runtime revision and connection runtime revision used to produce them. A plan must be revalidated before reuse if either graph changes.

## Planning

`LocationRouteRuntime.PlanRoute` supports:

- `ShortestDistance`
- `LowestCost`
- `FewestEdges`
- `AnyValidRoute`

Search is deterministic and bounded by visited node, expanded edge, and depth limits. Parallel edges, cycles, and mixed route/connection paths are safe. Ties resolve by edge count, distance, cost, then stable edge sort key.

## Access and Knowledge

Structural route availability is separate from traveler access:

- Structural route checks evaluate lifecycle and blockage.
- Access-aware checks ask `LocationConnectionRuntime` for connection edges.
- Route segment policy checks consume `LocationAccessPolicyDefinition` references.
- Unlockable planning may return required actions such as `open:<edgeId>` or `unlock:<edgeId>` without mutating state.

Knowledge-safe planning filters hidden, secret, and diagnostic edges. If no visible route remains, it returns an unknown route result rather than revealing hidden edge counts.

## Prototype Definitions

Prototype route definitions are centralized in `PrototypeLocationRouteDefinitionFactory`, matching the existing location and connection definition pattern. Test Lab, command automation, and persistence validation all use the same route definition source.

## Test Lab

The automation suite is:

`feature.14.6.routes-distance-travel-networks`

It validates seeded route readiness, mixed route/connection planning, deterministic objectives, access and unlockable edges, knowledge-safe hidden filtering, stale plan revalidation, route persistence, and fixture snapshot restore.
