# Feature 5.4a Persistence and Migration

Feature 5.4a does not add a new persistence participant and does not bump `player.attributes`.

## Why No Schema Bump

The existing `player.attributes` payload already persists the authoritative Base Attribute state:

- permanent source contributions;
- training/growth events;
- player ID and person ID.

Calculated Stats are derived caches and are not saved. The new purpose/resource metadata is definition data and validation data, not player runtime state.

## Compatibility

Existing Feature 5.2 and 5.3 development saves should remain compatible as long as their definition IDs are canonical and their `player.attributes` schema is version 1.

Pre-5.2 saves remain intentionally rejected because they do not contain Base Attribute source records or growth history.

## Retained Legacy Names

Serialized types retain legacy names:

- `AttributeDefinition`
- `CharacterAttributes`
- `RuntimeAttributeValueRecord`
- `PlayerAttributesSaveData`
- `PlayerAttributesPersistenceParticipant`

These names are retained to avoid unnecessary Unity serialization and save DTO churn. Documentation, diagnostics, Character menu text, and Test Lab labels use Base Attribute terminology.

## Manual Save Hygiene

Before final manual testing for this feature, delete stale local test saves if they were created before Feature 5.2 or before the Step 4 canonical-ID cleanup. Current Feature 5.2/5.3 saves can be used to verify that Base Attribute terminology and Calculated Stat resource metadata do not break load.
