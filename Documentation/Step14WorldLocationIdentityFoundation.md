# Step 14.1 - World and Location Identity Foundation

Feature 14.1 introduces the authoritative runtime identity layer for world locations.

## Ownership Boundary

`PlaceDefinition` remains authored design metadata for older place/catalog systems. `LocationDefinition` defines what kind of runtime location may exist. `LocationRuntime` owns actual runtime `LocationRecordData` instances.

Runtime location identity must not come from scene object names, transforms, hierarchy paths, terrain tiles, or marker GameObjects. Scene data may only be stored as optional binding keys.

## Core Model

Runtime locations contain:

- Stable `locationId`
- `locationDefinitionId`
- `worldId` from the existing persistence world identity
- Official, common, alias, and historical names
- Lifecycle state
- Semantic tags
- Property, organization, government, and territory references
- Optional prototype/scene binding key
- Visibility, provenance, and revision metadata

External systems remain authoritative for their own records. A location may reference a property, organization, government, or territory, but it does not own or duplicate those records.

## Mutation Rules

All writes go through explicit request/result APIs:

- `CreateLocation`
- `RenameLocation`
- `TransitionLifecycle`
- `RestoreFromSaveData`

Preview requests return projected results without mutation. Duplicate transaction IDs are idempotent. Stale expected revisions are rejected without partial mutation.

## Persistence

`LocationPersistenceParticipant` captures `world.locations` as shared-world data. Payload preparation validates schema, world identity, definitions, lifecycle state, name records, association support, and optional known external reference sets before commit.

Failed prepare does not alter live runtime state. Failed commit attempts rollback.

## Test Lab

The Step 14 provider registers:

`feature.14.1.world-location-identity-foundation`

The suite covers readiness, definition-vs-instance separation, stable identity, lifecycle, names/tags, external associations, scene independence, Step 8 subject references, preview/idempotence, revision safety, persistence validation, and fixture snapshot restore.

The suite runs as a hostless fresh runtime scenario through the same Test Lab runtime bundle used by command-line and in-game automation.

## Prototype Scene Guidance

No scene objects are required for 14.1. When prototype scene markers are useful later, bind them by stable location binding key rather than using the GameObject as identity.
