# Feature 5.5 Persistence and Migration

`player.traits` is a new optional player participant at schema version 1.

Feature 5.5 does not invalidate Feature 5.4b development saves. Saves without `player.traits` load with an empty Trait collection and can be resaved with the new participant after testing.

Saves that include `player.traits` must pass strict validation:

- schema version must be `1`;
- player ID must match the expected owner when provided;
- every Trait record must reference a known `TraitDefinition`;
- no duplicate Trait records are allowed;
- lifecycle and discovery enum values must be valid;
- every source record must have a valid source category and source ID;
- duplicate source keys within a Trait are rejected.

Unknown Trait IDs are rejected rather than silently ignored or mapped. This project is still pre-alpha, so obsolete Trait IDs should normally break development saves until an intentional migration is written and tested.

Trait effects are not persisted as authoritative Calculated Stat or Capability values. On load, Trait records restore first, then active Trait effects are rebuilt from current definitions. Current Resources reconcile through the existing Feature 5.4b resource system after stat changes.

Future multiplayer persistence should be server-authoritative and automatic. A client should not be authoritative over Trait records, shared-world state, or replacement history. On disconnect, the server should persist the disconnecting player's `player.traits` state while the shared world and other connected players continue.
