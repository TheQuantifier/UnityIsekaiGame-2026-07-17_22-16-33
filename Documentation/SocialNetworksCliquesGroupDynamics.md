# Feature 12.7 - Social Networks, Cliques, and Group Dynamics

Feature 12.7 adds read-only social graph projections and persistent informal social group records.

## Ownership Boundary

`SocialNetworkRuntime` does not own relationships, attitudes, reputation, rumors, interactions, or norms. Those systems remain authoritative for their own records. The network runtime reads their immutable save/projection data to build graph snapshots and owns only:

- informal social group records
- informal group membership records
- group lifecycle and role mutations
- deterministic graph projection cache entries
- processed social-network mutation transaction IDs

Graph analysis never creates groups implicitly. Creating a group from a clique/community candidate is an explicit preview/execute mutation.

## Graph Projections

`SocialGraphProjectionDefinition` defines which edge sources are included, how direction and weight should be interpreted, and the bounded limits for traversal and analysis. Prototype definitions include:

- relationship network
- mutual trust network
- composite social network
- rumor reach network

Supported edge semantics are explicit:

- objective relationships
- directed attitudes
- mutual attitudes
- recent interactions
- rumor transmissions
- shared informal group membership
- reserved custom registered projections

`SocialGraphSnapshot` returns immutable node and edge snapshots with deterministic incoming/outgoing indexes. Request-level edge-kind and minimum-weight filters are included in cache keys so narrower queries cannot accidentally reuse broader graph projections.

## Informal Groups

`InformalSocialGroupDefinition` models non-institutional social groups such as friend circles, adventuring parties, households, and court circles. These are intentionally separate from organizations, factions, legal authority, and role/title systems.

Group mutations support preview, execute, duplicate transaction handling, membership role changes, membership end, dissolution, validation, and persistence. Role and leadership rules come from definitions.

## Persistence

`SocialNetworkPersistenceParticipant` saves and restores only group-owned state. It validates definitions, known Person IDs, membership references, active membership uniqueness, and schema version before commit. Invalid payloads are rejected without mutating the live runtime.

## Test Lab

Feature 12.7 is registered as:

`feature.12.7.social-networks-cliques-group-dynamics`

The automation suite covers:

- definition readiness and non-mutating previews
- graph edge semantics and source-runtime projection
- neighbor, mutual connection, path, metrics, clique, and community analysis
- informal group lifecycle, role validation, idempotence, and metrics
- persistence round trip and corrupt payload rejection

The suite is hostless and uses fresh isolated runtimes, so both the in-game Test Lab runner and command-line automation runner execute the same catalog definitions.
