# Item Durability, Wear, Repair, Breakage, and Salvage

Feature 9.4 introduces `ItemDurabilityRuntime` as the authoritative owner of physical item condition.

## Ownership Boundary

`ItemInstanceIdentityRuntime` still stores the legacy `ItemConditionStateData` field for compatibility, migration, and coarse projections. It is no longer the owner of physical condition. New gameplay systems should read or mutate durability through `ItemDurabilityRuntime`.

The item runtime ownership model is:

- `ItemInstanceIdentityRuntime`: stable identity, ownership, custody, location, and lifecycle.
- `ItemCompositionRuntime`: materials, components, and physical structure.
- `ItemQualityAffixRuntime`: workmanship, quality, defects, rarity, affixes, and stat modifiers.
- `ItemDurabilityRuntime`: current/max/original durability, wear, damage channels, repair history, breakage, salvage, and functional contribution.

## Migration

When a durability record is missing, `EnsureDefaultDurability` creates one from the current item instance, composition, and quality state. If an older identity condition exists, its normalized value and condition category seed the new durability record. The identity record is not mutated during this migration.

## Gameplay Effects

Damage and wear reduce current durability. Permanent damage also lowers maximum durability relative to original maximum durability. Repair restores current durability but may add permanent capacity loss depending on repair quality.

Functional state is derived from item and component durability:

- fully functional items contribute normally;
- impaired and partially disabled items can be scaled by consumers;
- broken or destroyed items contribute no equipment stat modifiers;
- salvage marks the durability record as salvaged and can optionally destroy the identity.

## Components

Durability can be tracked at the item level and at composition component level. Component IDs are validated against the item composition when composition data is available. Critical or essential broken components can make the whole item broken.

## Persistence

`ItemDurabilityPersistenceParticipant` persists durability as shared world state after identity, composition, and quality records are available. Restore validates item references, component references, duplicate durability records, and schema version before committing. Failed restore uses runtime rollback.

## Access Projection

Durability projections expose Step 8 information subjects. Redacted projections can hide repair history, structural weakness, hidden damage, maintenance provenance, and salvage yields while still returning a stable projection object.

## Test Lab

Feature 9.4 automation runs in the item runtime fixture bundle. The fixture snapshots and restores item identity, composition, quality, and durability together so durability mutations do not leak between scenarios.

## Current Limitations

This feature intentionally does not implement full repair stations, tool quality requirements, crafting queues, merchant pricing, or final UI. Those systems should call the durability runtime rather than owning durability state themselves.
