# Player Inventory And Equipment Persistence

Feature 4.2 adds the first real player-state participant on top of the Feature 4.1 save service.

For final Step 4 participant inventory, schema policy, and regression coverage, see `Documentation/Step4PersistenceArchitecture.md`, `Documentation/Step4PersistenceSchemaInventory.md`, and `Documentation/Step4PersistenceRegressionChecklist.md`.

## Ownership

Inventory and equipment are `Player`-scoped state. The current prototype owner is `local-player`, inside `local-world`, under `local-account`.

Loading this participant restores only what the player possesses. It does not restore shared-world entities, world pickups, enemies, chests, doors, contracts, quests, statuses, vitals, scene position, or other players.

Future multiplayer persistence remains server-authoritative: clients may request item changes, but the server creates or validates item-instance IDs and persists player inventory/equipment automatically on disconnect, checkpoint, or controlled shutdown.

## Participant

The runtime participant is `PlayerInventoryEquipmentPersistenceParticipant`.

- Key: `player.inventory-equipment`
- Scope: `Player`
- Owner ID: `local-player` in the prototype
- Required: yes
- Participant schema version: `1`
- Load phase: `Inventory`

The participant is combined intentionally. Inventory and equipment stay separate gameplay systems, but their payloads are captured, prepared, and committed as one aggregate so the same stateful item instance cannot appear in both places.

The prototype persistence menu wires the participant to the Prototype Player inventory/equipment components and `PrototypeDefinitionCatalog` during Play Mode.

## Payload Shape

`PlayerInventoryEquipmentSaveData` contains:

```csharp
public int schemaVersion;
public InventorySaveData inventory;
public EquipmentSaveData equipment;
```

Inventory entries reuse `InventorySaveData` / `InventoryEntrySaveData`.

Equipment entries reuse `EquipmentSaveData` / `EquipmentSlotSaveData`.

Definition-only stacks store stable item definition IDs, quantity, slot order, and empty slots. They do not create item-instance IDs during restore.

Stateful entries store `ItemInstanceSaveData`, including definition ID, persistent instance ID, optional quality ID, and optional normalized condition.

The payload never stores ScriptableObject references, live slots, calculated stats, UI state, selected slots, hover state, or open menu state.

## Save Capture

Capture reads the current inventory DTO and equipment DTO on the main thread, wraps them in the aggregate payload, and self-validates before the save file is written.

Failed capture or failed self-validation aborts the save. The previous valid primary save remains untouched because the service still uses the Feature 4.1 atomic write flow.

## Two-Phase Load

Preparation parses and validates the full aggregate payload before live state changes.

Validation includes:

- participant and payload schema version;
- inventory DTO restore against a temporary inventory;
- equipment DTO restore against temporary equipment;
- item definition resolution through `DefinitionRegistry`;
- quality definition resolution through `DefinitionRegistry`;
- condition range validation;
- stack quantity and max-stack validation;
- item-instance policy validation;
- equipment slot compatibility;
- duplicate item-instance IDs inside inventory;
- duplicate item-instance IDs inside equipment;
- duplicate item-instance IDs across inventory and equipment;
- slot capacity and slot-order restoration.

Commit restores the prepared inventory DTO, then restores the prepared equipment DTO. Equipment stat and resistance modifiers rebuild from the equipment change event, using the existing equipment-source identity policy.

An unexpected commit failure attempts a local rollback to pre-load inventory/equipment snapshots and reports a structured participant commit failure. Rollback is a defensive fallback, not a replacement for prepare-time validation.

Feature 4.8 also wraps this participant in service-level transaction handling. The service validates dependencies before capture/load, captures rollback payloads before commit, commits participants in dependency order, and marks the runtime unsafe if rollback cannot restore a coherent state.

## Current Vitals

Feature 4.2 does not persist current health, mana, or stamina.

If restored equipment changes maximum vitals, existing runtime behavior applies: lowering a maximum clamps current value, and raising a maximum does not refill automatically. Feature 4.3 restores current vitals after equipment and save-eligible statuses have rebuilt final max-stat modifiers.

## World Pickup Limitation

World pickups are not persisted by this participant. Player inventory/equipment persistence owns what the player possesses, not whether a world pickup still exists.

During local manual tests, avoid using an old save in a reset world in a way that lets the same nonpersistent world pickup be collected again. Shared-world or persistent-entity state belongs to later Step 4 work.

Feature 4.4 quest/contract persistence depends on inventory restoring first so current-held collect objectives can recalculate from the player's restored inventory.

## Extending Item Save Data

To extend item persistence later:

1. Add fields to `ItemInstanceSaveData` or the relevant inventory/equipment DTO.
2. Bump the participant schema version when old payload interpretation changes.
3. Validate new fields during prepare, not commit.
4. Keep definitions immutable and referenced by stable IDs.
5. Keep ownership explicit: player inventory/equipment is not shared-world state.

## Known Limitations

- No save/load UI beyond development menu commands.
- No autosave.
- No world pickup persistence.
- No current vital persistence.
- No status, quest, contract, position, scene, enemy, chest, door, or shared-world persistence.
- No networking, authentication, cloud saves, or server database integration.
- Recovery UI is development-focused; final player recovery workflows are still deferred.
