# Save Slots, Autosave, And Load UI

Feature 4.7 adds the first player-facing persistence workflow on top of the existing `PersistenceService`.

The implementation remains local and synchronous for the prototype, but the UI is only a client of persistence operations. `PersistenceService` still owns slot validation, write/delete/load behavior, backup files, and autosave generation rotation.

## Slot Model

The prototype exposes:

- 5 manual slots: `manual-1` through `manual-5`;
- 3 rotating autosave slots: `autosave-0` through `autosave-2`;
- one internal staging slot: `autosave-staging`.

`PrototypeSaveSlotCatalog` centralizes these IDs and display names. UI and development tools build slot rows from `SaveSlotDescriptor` instead of inventing slot names.

Manual slots can be saved, loaded, validated, backup-loaded, and deleted. Autosave slots can be loaded, validated, and backup-loaded, but cannot be overwritten directly by the player.

## Metadata

The save envelope now provides UI-facing metadata:

- created time;
- last-written time;
- play time seconds;
- scene summary;
- place summary;
- player summary;
- world ID;
- player/account ID;
- schema version and game version.

`PersistenceService.GetSlotMetadata` and `ListSaveSlots` read metadata through validation. Corrupt or missing saves are reported as descriptors with compatibility/status details rather than hidden from the UI.

## Autosave

`AutosaveCoordinator` is attached to `PrototypePersistenceServiceBehaviour`.

Default behavior:

- timer autosave every 5 minutes;
- debounced autosave requests from progression events;
- force autosave from the Save/Load page and Test Lab;
- retry delay after blocked/failed autosave;
- no overlapping save/load operations.

Autosave writes to `autosave-staging` first. If the staging write succeeds, `PersistenceService.RotateAutosaveSlots` moves generations so `autosave-0` is newest, `autosave-1` is previous, and `autosave-2` is oldest. Rotation rewrites each copied envelope to the target stable slot ID.

## Dirty State And Play Time

`GameSaveDirtyTracker` tracks whether meaningful player state has changed since the last successful save/load/autosave.

The prototype marks dirty on inventory, equipment, vitals/resources, status, quest, contract, and place changes. Quest/contract changes also request a debounced autosave because those are progression events.

`PlayTimeTracker` records unscaled local play time and feeds `PersistenceService.PlaytimeSecondsProvider`. Loading a save restores the saved play time value.

## Tab Menu UI

`SaveLoadMenuView` is generated inside the existing unified Tab menu.

The right-side Tab navigation remains unchanged except for the added `Save/Load` page. The page uses two selector rows: one for the save slot and one for the action. The selected slot metadata, compatibility/status, backup presence, dirty state, and current persistence operation state remain visible below the selectors.

Confirmation behavior:

- overwriting an occupied manual slot requires executing Save twice;
- loading while dirty requires executing Load twice;
- loading a backup requires executing Load Backup twice;
- deleting a manual slot requires executing Delete twice.

## Test Lab And Editor Tools

The Test Lab Persistence section now includes save-slot diagnostics, manual slot 1 save, force autosave, short autosave interval, dirty/clean toggles, backup validation, and explicit backup load.

Editor menu commands remain prototype-oriented and now include save-slot listing through the same service metadata. They are development conveniences, not runtime architecture.

## Multiplayer Boundary

The Save/Load page operates on local prototype player saves only.

It does not grant client authority over shared-world or region state. Future multiplayer persistence should remain server-owned and normally automatic. Disconnecting one player should persist that player's `Player`-scoped state while the world continues for other connected players.

Manual local save/load is retained here only as a proof and development workflow.

## Deferred Work

Feature 4.7 does not add:

- asynchronous disk operations;
- cloud saves;
- account authentication;
- world-state persistence for enemies, pickups, doors, containers, or NPC schedules;
- cross-scene loading;
- server persistence;
- recovery flows beyond explicit backup load.
