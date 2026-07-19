# Step 4 Persistence Schema Inventory

Step 4 uses one save envelope schema plus independent participant schemas. Do not treat participant payload versions as one global version.

## Save Envelope

- Type: `GameSaveEnvelope`
- Current version: `PersistenceService.CurrentSchemaVersion == 1`
- Required fields: format identifier, schema version, game version, save ID, slot ID, display name, timestamps, world ID, player ID, account ID, participant records, content checksum.
- Optional/diagnostic fields: play time, scene summary, place summary, player summary, transaction ID, parent transaction ID, save revision, completed-write marker.
- Compatibility: versions less than `1` or greater than current are rejected.
- Migration: extension point exists through `ISaveMigration`; no automatic envelope migration is currently enabled.
- Stable-ID dependencies: slot ID, participant keys, world ID, player ID, account ID.

## Slot Metadata

- Types: `SaveSlotMetadata`, `SaveSlotDescriptor`
- Version source: envelope schema plus descriptor fields.
- Required fields: slot ID, existence/validity state, schema version, compatibility/status, message.
- Optional/diagnostic fields: display name, timestamps, play time, scene/place/player summaries, transaction ID, save revision, backup availability.
- Compatibility: corrupt, missing, future-version, or incomplete slots remain listable with status instead of hidden.
- Migration: descriptors are rebuilt from save metadata on demand.
- Stable-ID dependencies: manual and autosave slot IDs.

## Prototype Development State

- Participant: `prototype.state`
- Payload: `PrototypePersistenceStateSaveData`
- Current version: `1`
- Required fields: schema version, test value, note, flag.
- Compatibility: unsupported participant payload versions are rejected during prepare.
- Migration: none.
- Stable-ID dependencies: participant key and local player owner ID.

## Inventory/Equipment

- Participant: `player.inventory-equipment`
- Payload: `PlayerInventoryEquipmentSaveData`
- Current version: `1`
- Required fields: schema version, inventory DTO, equipment DTO.
- Optional fields: item quality ID and condition where item metadata exists.
- Compatibility: unsupported participant payload versions are rejected.
- Migration: none.
- Stable-ID dependencies: item definition IDs, item instance GUIDs, quality IDs, equipment slot IDs.

## Stats/Vitals/Statuses

- Participant: `player.stats-vitals-status`
- Payload: `PlayerStatsVitalsStatusSaveData`
- Current version: `1`
- Required fields: schema version, current health, current mana, current stamina.
- Optional fields: actor profile ID, status list.
- Compatibility: unsupported participant payload versions are rejected; defeated saves are rejected.
- Migration: none.
- Stable-ID dependencies: actor profile ID, status definition IDs, status application IDs, source IDs.

## Quest/Contract

- Participant: `player.quests-contracts`
- Payload: `PlayerQuestContractSaveData`
- Current version: `2`
- Required fields: schema version, quest list, contract list.
- Quest fields: quest definition ID, runtime instance ID, state, current stage ID, diagnostic stage index, active objective progress.
- Contract fields: contract definition ID, runtime instance ID, state, objective progress.
- Compatibility: schema version `1` is rejected because it used order-derived objective keys; future versions are rejected.
- Migration: none for version `1`; future migrations must prove stage/objective identity before restoring progress.
- Stable-ID dependencies: quest IDs, contract IDs, quest runtime IDs, contract runtime IDs, quest stage IDs, quest objective IDs, contract objective IDs.

## Location

- Participant: `player.location`
- Payload: `PlayerLocationSaveData`
- Current version: `1`
- Required fields: schema version, scene key, position, rotation, location mode.
- Optional/diagnostic fields: place ID, spawn point ID, saved UTC time, diagnostic scene name/path/build index.
- Compatibility: unsupported participant payload versions are rejected; cross-scene restoration is rejected clearly.
- Migration: none.
- Stable-ID dependencies: scene key, place ID, spawn point ID.

## World Entity Reference

- Type: `WorldEntityReference`
- Current version: `1`
- Required fields: schema version, entity ID, identity kind.
- Optional/diagnostic fields: scene key, world ID, definition ID, expected entity type.
- Compatibility: unsupported reference versions and transient references are rejected.
- Migration: none.
- Stable-ID dependencies: world entity ID, scene key, world ID, definition ID.

## Transaction Marker

- Fields: transaction ID, parent transaction ID, save revision, completed-write marker.
- Current version: envelope version `1`.
- Required for correctness: none; these are diagnostic transaction fields.
- Compatibility: old Step 4 saves without transaction metadata remain readable because checksum ignores these fields.
- Migration: not required.

## Autosave Metadata

- Slot IDs: `autosave-0`, `autosave-1`, `autosave-2`, `autosave-staging`.
- Current version: envelope version `1`.
- Required fields: same as envelope metadata after rotation.
- Compatibility: each generation validates independently.
- Migration: none.
- Stable-ID dependencies: autosave slot ID and generation order.

## Recovery And Quarantine

- Recovery report type: `SaveRecoveryScanReport`
- Candidate type: `SaveRecoveryCandidate`
- Quarantine files: `<slot>.json.quarantine.<utc>.json`
- Current version: diagnostic only.
- Compatibility: corrupt files are not loaded; valid backups/autosaves remain candidates.
- Migration: none.
- Stable-ID dependencies: slot ID and file naming convention.
