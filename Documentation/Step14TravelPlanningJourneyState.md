# Step 14.7 Travel Planning and Journey State

Feature 14.7 adds an authoritative `TravelJourneyRuntime` for logical travel plans and journey lifecycle state.

The runtime owns journey records, journey step progress, lifecycle history, deterministic world-time advancement, active ordinary journey constraints, projection-safe visibility, and journey persistence validation. It does not own exact physical placement. Exact placement remains under `EntityLocationRuntime` from Feature 14.3.

## Ownership Boundaries

- `LocationRouteRuntime` owns route segments, travel networks, and route plans.
- `TravelJourneyRuntime` accepts a route plan and records a journey against it.
- `LocationConnectionRuntime` owns local connection traversal. Journey advancement delegates local connection steps to `LocationConnectionRuntime.Traverse`.
- `EntityLocationRuntime` owns exact placement. Long route segments keep the traveler at the last completed exact location until the segment completes.
- Scene objects, NavMesh, animations, weather, and NPC destination choice are intentionally outside this feature.

## Journey Lifecycle

A normal journey moves through:

`Ready -> Active -> Completed`

It can also be:

- `Paused`
- `Blocked`
- `Cancelled`
- `Replanning`
- `Failed`
- `Historical`

Only one active ordinary journey may exist for a traveler at a time. Administrative or scripted future categories can coexist when explicitly authored that way.

## Movement Model

Long route-segment progress is evaluated against authoritative world time, not Unity frame time.

For long segments:

- partial progress updates the journey record only;
- physical exact placement remains at the previous exact location;
- `GetPhysicalContext` exposes in-transit state, previous location, next location, current step, and progress fraction;
- completion relocates through `EntityLocationRuntime`.

For local connection steps:

- advancement calls `LocationConnectionRuntime.Traverse`;
- access, lock, blockage, direction, and traversal history remain connection-runtime authority.

## Persistence

`TravelJourneyPersistenceParticipant` saves and restores journey graphs after:

- locations;
- entity placements;
- connections;
- route graphs.

Prepare validation rejects corrupt payloads before commit when journeys reference missing locations, route segments, connections, steps, or duplicate active ordinary journeys.

## Test Lab

Automation suite:

`feature.14.7.travel-planning-journey-state`

It covers:

- journey creation from accepted route plans;
- no teleport on journey start;
- deterministic world-time progress;
- local connection traversal;
- pause, resume, and cancel lifecycle;
- blocked-route detection and replanning;
- in-transit and redacted projections;
- persistence validation;
- fixture snapshot rollback.
