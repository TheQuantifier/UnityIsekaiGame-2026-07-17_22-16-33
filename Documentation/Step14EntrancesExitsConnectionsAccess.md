# Step 14.5 Entrances, Exits, Connections, and Access

Feature 14.5 adds the authoritative local connection layer for world locations. A connection is a scene-independent runtime record that says two known locations can be traversed through an entrance, exit, doorway, passage, cell door, dungeon entrance, or similar access point.

## Authority Boundaries

`LocationConnectionRuntime` owns:

- Stable connection records and endpoint records.
- Directionality, lifecycle, open state, lock state, blockage state, and visibility.
- Connection access policy references and explicit access grants.
- Traversal preview and execution.
- Connection persistence validation and rollback-safe restore.

It does not own:

- Location identity, hierarchy, or spatial adjacency. Those remain in `LocationRuntime`.
- Entity placement authority. Traversal delegates final movement to `EntityLocationRuntime`.
- Interaction point ownership. Connections may reference interaction points, but `InteractionPointRuntime` owns those records.
- Organization, office, rank, property, legal, warrant, custody, key, or credential ownership. Connections consume those facts through `LocationConnectionAccessContextData`.
- Scene GameObjects, door animation, pathfinding, NPC route search, or multiplayer account permissions.

This keeps Step 14.5 as the local access/traversal authority without duplicating other domain runtimes.

## Runtime Model

Connections are stable records identified by `location-connection.*` IDs. Each connection has:

- A definition ID from `LocationConnectionDefinition`.
- Source and destination location IDs.
- Two endpoint records.
- Directionality.
- Lifecycle, open, lock, and blockage state.
- Visibility and scene binding metadata.
- Access policy definition IDs.
- Optional interaction point IDs.
- Transaction history and mutation history.

Definitions describe allowed endpoint categories and supported mechanics. Runtime records hold mutable world state.

## Access Evaluation

`EvaluateAccess` combines:

- Connection direction.
- Lifecycle availability.
- Open/closed state.
- Lock and key state.
- Blockage state.
- Access policies.
- Active explicit access grants.

Policies consume context values such as membership IDs, rank IDs, office IDs, authority IDs, employment IDs, property IDs, permit IDs, warrant IDs, custody role IDs, key IDs, and credential IDs. These are references supplied by the caller or a future facade. The connection runtime never creates or mutates those external records.

## Traversal

Traversal is atomic from the caller's point of view:

1. Resolve the actor's current exact placement from `EntityLocationRuntime`.
2. Confirm the actor is at the connection origin.
3. Evaluate access.
4. Preview without mutation when requested.
5. Execute relocation through `EntityLocationRuntime`.
6. Roll back entity placement if relocation fails.
7. Record traversal history only after successful movement.

Spatial adjacency does not imply traversal. A location may be near another location without having a traversable connection.

## Persistence

`LocationConnectionPersistenceParticipant` captures only connection runtime data. Restore validation rejects missing connection definitions, missing endpoint locations, missing access policies, missing referenced interaction points, malformed endpoint records, invalid grants, and world mismatches before commit.

Restore is rollback-safe. If commit fails after validation, the previous live connection runtime state is restored.

## Prototype Coverage

`PrototypeLocationConnectionDefinitionFactory` provides prototype-only fallback definitions and seeded records for the current prototype world:

- Public building entrances.
- Doorways and lockable doors.
- Restricted offices.
- Guild storage.
- Prison cell doors.
- Dungeon entrances.
- One-way drops.
- Hidden passages.
- Representative access policies for public, membership, rank, office, authority, employment, ownership, legal permit, warrant, custody, key, and explicit whitelist access.

These fallbacks are a shared prototype definition source, not a validation bypass. Catalog-authored definitions can replace them later without changing runtime ownership.
