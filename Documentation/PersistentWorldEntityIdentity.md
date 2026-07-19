# Persistent World-Entity Identity

Feature 4.6 adds a stable identity layer for runtime world objects. It is a reference foundation only. It does not persist full enemy state, pickup state, container contents, NPC schedules, door state, region simulation, or shared-world snapshots yet.

For final Step 4 stable-ID audit, ownership boundaries, and limitations, see `Documentation/Step4PersistenceArchitecture.md`, `Documentation/Step4PersistenceOwnershipMatrix.md`, and `Documentation/Step4PersistenceKnownLimitations.md`.

## Purpose

`WorldEntityIdentity` gives scene and runtime objects a durable handle that future persistence systems can use to say "this exact world object" without serializing a Unity object reference.

World entity IDs are separate from:

- definition IDs, such as `item.health-potion` or `being.prototype-enemy`;
- item instance IDs, which identify a specific item in inventory/equipment;
- quest and contract runtime IDs;
- scene keys, such as `scene.prototype`;
- persistence participant keys, such as `player.location`.

A world entity may reference a definition for diagnostics or restoration policy, but the world entity ID is the identity of the in-world object.

## Identity Kinds

`WorldEntityIdentityKind` distinguishes:

- `Authored`: an object placed in a scene with a stable local authored ID.
- `RuntimeSpawned`: an object created during play with a generated runtime GUID ID.
- `RestoredRuntime`: a runtime-created object recreated from a saved world entity ID.
- `Transient`: a temporary object that must not be persisted by reference.

Authored IDs compose as:

`entity.<scene-key>.<local-authored-id>`

Example:

`entity.scene.prototype.enemy.primary`

Runtime IDs compose as:

`entity.<world-id>.runtime.<guid>`

Example:

`entity.local-world.runtime.0f5d3c55c1c64fd19dd59ea3e9a05e78`

## Runtime Registry

`WorldEntityRegistry` is a process-local registry for currently loaded identities. It:

- registers non-transient identities;
- rejects duplicate IDs;
- resolves identities by ID;
- resolves typed components from an identity;
- keeps disabled objects registered;
- unregisters destroyed objects;
- exposes diagnostics for the Test Lab and editor tools.

The registry is not a database and does not make clients authoritative over shared-world state. It is a lookup table for currently loaded Unity objects.

## References

`WorldEntityReference` is a serializable reference DTO with schema version, entity ID, scene key, world ID, identity kind, optional definition ID, and optional expected component type.

References validate before resolving. They reject missing IDs, invalid IDs, unsupported schema versions, and transient entities. If an expected component type is supplied, resolution fails rather than silently returning the wrong kind of object.

## PrototypeScene Assignments

The scene usability setup assigns authored identities to the current prototype objects that are likely to become persistent world actors or interactables:

- Prototype Enemy: `entity.scene.prototype.enemy.primary`
- Prototype Dialogue NPC: `entity.scene.prototype.npc.dialogue.primary`
- Prototype Contract Board: `entity.scene.prototype.contract-board.primary`
- Prototype Quest Investigation Area: `entity.scene.prototype.quest.investigation-area`
- Prototype Delivery Crate: `entity.scene.prototype.delivery.crate.primary`
- Prototype Damage Dummy: `entity.scene.prototype.target.damage-dummy.primary`
- Generated health potion, ore, sword, and helmet pickups under stable pickup IDs.
- Legacy prototype pickup objects when present.

Enemy loot drops and Test Lab spawned loot receive runtime world entity IDs. Transient Test Lab loot proves that temporary objects can exist without registering as persistent references.

## Editor Tools

Editor menu commands:

- `Tools > World Entities > Validate Current Scene`
- `Tools > World Entities > Assign Missing Authored IDs`
- `Tools > World Entities > List Registered Runtime Entities`

Validation checks missing/invalid IDs and duplicate full authored/runtime IDs in the loaded scene. The assignment tool is explicit and editor-only; it does not silently create IDs during runtime save/load.

## Future Persistence Direction

Future world-state participants may store world entity references when they need to restore or update a specific world object. Those participants must still own their data and validation rules.

Examples:

- a pickup participant may save whether an authored pickup was collected;
- a region participant may save which runtime loot entities still exist;
- an actor participant may save server-authoritative enemy state;
- a contract participant may reference a delivery target by world entity ID.

Feature 4.6 does not implement those participant payloads. It only provides stable IDs, references, registration, diagnostics, and scene assignments.

Feature 4.8 recovery and consistency auditing do not make world entities persistent by themselves. They provide transaction, rollback, and audit hooks that future `SharedWorld` or `RegionOrScene` world-entity participants can use once those systems own mutable world state.

## Multiplayer Ownership

World entity identity is compatible with server-authoritative persistence. In multiplayer, clients may display or reference world entity IDs sent by the server, but clients must not become authoritative over `SharedWorld` or `RegionOrScene` state by uploading local world entity saves.

When one player disconnects, the server can persist that player's `Player`-scoped state while registered world entities continue in the active world for other players. Region or shared-world entity state should persist on server checkpoints, region unload, or controlled shutdown.
