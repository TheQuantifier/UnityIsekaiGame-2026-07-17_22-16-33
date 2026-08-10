# Feature 14.10 - World/Location Persistence and Historical Movement

Feature 14.10 finalizes the Step 14 persistence and historical movement layer without creating a new authority for world state.

## Ownership

Step 14 records have one authoritative owner:

- `LocationRuntime`: world identity, location records, names, lifecycle, containment, and spatial relationships.
- `EntityLocationRuntime`: exact entity placement, placement intervals, active placement, body/person physical resolution, and placement transactions.
- `InteractionPointRuntime`: interaction points, host assignments, provider assignments, reservations, sessions, and interaction-point transactions.
- `LocationConnectionRuntime`: connection identity, endpoints, open/lock/blockage state, access grants, connection state history, and traversal transactions.
- `LocationRouteRuntime`: route segments, route networks, route history, and route transactions.
- `TravelJourneyRuntime`: journeys, route-plan snapshots, journey steps, progress, lifecycle, replan state, journey history, and journey transactions.
- `TravelConditionRuntime`: travel conditions, hazard exposures, encounter records, and processed trigger state.
- `PoliticalTravelRuntime`: political checkpoints, crossing authorizations, border/jurisdiction crossing records, and political-travel transactions.
- Step 13 remains authoritative for governments, territories, jurisdictions, law, crimes, warrants, and justice state.

Derived state is not persisted as an authority. Occupancy indexes, ancestor/descendant caches, route adjacency, route-search frontiers, current-location summaries, last-known summaries, scheduler jobs, and movement-history timeline entries are rebuilt from source records.

## Persistence Manifest

`Step14PersistenceManifestBuilder` creates a scene-independent manifest from Step 14 runtime save-data snapshots. The manifest records:

- schema version;
- world ID;
- save slot ID;
- authoritative save time;
- participant presence;
- participant schema version;
- participant revision;
- authoritative record count;
- historical record count;
- transaction count;
- required and optional dependencies;
- the Step 14 ownership map;
- validation diagnostics.

Validation rejects or reports:

- missing participants;
- unsupported schema versions;
- world/save identity mismatch;
- duplicate authoritative ownership categories;
- invalid placement intervals;
- overlapping placement intervals for one entity;
- missing location references;
- missing journey references from encounters;
- unavailable route-segment references where historical projection can still preserve the stable edge ID.

The manifest is diagnostic and validation infrastructure. It is not a second save-file system.

## Historical Movement Projection

`MovementHistoryService` is a read-only projection over existing persisted source records. It clones source save data on construction and returns immutable projections.

Supported queries include:

- `BuildTimeline`
- `ResolveExactLocationAt`
- `ResolveHistoricalLocationPath`
- `GetHistoricalOccupancy`
- `GetMovementDistance`
- `GetVisitSummary`
- `ResolveHistoricalWorldContext`
- `ValidateHistory`

Movement history remains separate from Step 8 historical events. Ordinary movement transitions can live only in movement history. Major travel events may reference Step 8 records through source/provenance IDs, but Feature 14.10 does not flood Step 8 with every room transition.

## Historical Semantics

Exact location at time `T` can return:

- exact placement found;
- in transit;
- unplaced;
- no historical record;
- entity not yet created;
- entity already ended;
- invalid history;
- hidden.

When the entity is in transit, the service returns journey ID, step ID, route edge, previous location, next location, completed distance, step distance, progress fraction, lifecycle, and authoritative time. It does not invent an exact room while mid-route.

Historical containment uses containment links active at the requested time. Renamed, reparented, or destroyed locations remain resolvable through stable `LocationId` records.

Political context is projected from recorded political crossings and remains a reference to Step 13 state. Past crossing legality is not recalculated from current law unless a future system explicitly asks for a retrospective hypothetical.

## Visibility

Historical movement projections support:

- development authoritative;
- owner/traveler;
- authorized institutional;
- public.

Public projections omit hidden, secret, restricted, and diagnostic movement records. Hidden movement does not contribute hidden counts to public results.

## Restore Contract

Feature 14.10 assumes source runtimes restore their own records through existing Step 4 persistence participants. After restore:

- source records are authoritative;
- derived indexes are rebuilt by owning runtimes;
- movement history is queried on demand;
- scheduler jobs can be reconciled from journey logical state;
- scene binding remains deferred to Feature 14.11.

Feature 14.10 does not persist transforms, GameObjects, NavMesh state, scene anchors, camera state, or streamed world chunks.

## Feature 14.11 Boundary

Feature 14.11 should bind restored authoritative IDs to Unity presentation:

- `LocationId` to scene anchors;
- `InteractionPointId` to markers;
- `ConnectionId` to doors or gates;
- route segments to visual paths where useful;
- travelers to physical scene objects;
- current journey progress to presentation position.

Scene binding must not modify authoritative history merely because a GameObject appears.
