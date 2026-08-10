# Step 14.3 - Entity Location and Occupancy

Feature 14.3 adds the authoritative logical placement runtime for physical entities. `EntityLocationRuntime` owns exact world/location occupancy for supported entity references and derives higher-level occupancy through the Step 14 location hierarchy.

## Ownership Boundary

`LocationRuntime` remains the owner of location identity, lifecycle, containment, and spatial relationships. `EntityLocationRuntime` stores only entity placements inside those locations:

- one active exact placement per ordinary physical entity
- historical placements for relocation and last-known queries
- transaction records for idempotent placement operations
- person-to-active-body bindings for physical person resolution
- capacity/type rules as lightweight validation, not access, routing, or movement logic

It does not own doors, locks, routes, pathfinding, scene transforms, UI visibility, interaction points, or multiplayer authority.

## Entity References

Placements use `EntityLocationReferenceData`:

- `entityType`
- `entityId`
- `worldId`

Supported occupant types are `Person`, `Body`, `ItemInstance`, `WorldEntity`, `Container`, `Actor`, and `Custom`. Stable keys are deterministic and include entity type, world, and ID.

## Exact Placement and Derived Occupancy

Every active placement has one exact `exactLocationId`. Ancestor occupancy is never persisted. Recursive occupancy is derived by asking `LocationRuntime` for descendants and then reading direct placements at each descendant.

This keeps containment changes and placement changes independent:

- moving a room in the hierarchy changes recursive occupancy results without rewriting entity placements
- relocating an entity ends the prior active placement and creates a new active placement
- unplacing an entity ends the placement and leaves a last-known historical record

## Person and Body Semantics

Bodies are physical occupants. A `Person` can resolve physically through its active body binding when the Person does not have a direct active placement. This prevents contradictory Person/body placements while still letting gameplay ask "where is this Person?"

Direct Person placement is available for exceptional cases, but normal physical placement should target the active body.

## Inventory Boundary

Inventory-held item instances are not allowed to also have active exact world placements. `MarkInventoryHeld` makes this explicit so persistence and runtime validation can reject duplicated inventory/world ownership before mutation.

This is a boundary rule only. Inventory systems still own inventory contents and custody details.

## Location Lifecycle and Capacity

New ordinary placements require an active location. Closed locations may retain existing occupants, but they reject new placements. Removed, destroyed, historical, proposed, or unknown locations cannot retain active placements in saves.

Capacity rules can restrict:

- direct occupant count
- allowed occupant entity types

Capacity is direct-only. Recursive or legal access capacity remains future work.

## Persistence

`EntityLocationPersistenceParticipant` captures and restores entity location state after `LocationPersistenceParticipant`. Prepare validates the whole graph before commit:

- schema version
- expected world
- referenced locations
- known entity references when supplied
- no more than one active exact placement per entity
- inventory/world exclusion
- placement lifecycle and timing
- transaction references
- capacity rule references
- person/body binding references

Commit uses a rollback snapshot if restore fails after prepare.

## Test Lab

`feature.14.3.entity-location-occupancy` covers:

- seeded prototype placement readiness
- single active exact placement rejection
- Person-through-body resolution
- direct and recursive occupancy
- relocation history and hierarchy transition diffs
- unplacement and last-known placement
- lifecycle rejection
- capacity/type validation
- inventory/world exclusion
- persistence round trip
- corrupt restore rejection
- fixture snapshot restoration

The Test Lab fixture bundle now includes `EntityLocationRuntime` so automation scenarios can reset and fingerprint entity placement state consistently with other core runtimes.
