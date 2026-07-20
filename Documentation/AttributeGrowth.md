# Attribute Growth

Feature 5.2 alpha growth is intentionally simple.

Growth model:

- qualifying actions add constant fractional growth;
- no diminishing returns;
- no anti-grind reduction;
- no daily caps;
- no difficulty or level scaling;
- values can exceed 100;
- action growth is permanent.

`CharacterAttributes.TryRecordTrainingEvent` records immutable growth events. Each event has a stable event ID, category, source system, timestamp, and one or more `RuntimeAttributeSourceContribution` records.

Invalid growth is rejected before any state changes:

- missing source IDs;
- missing or unknown Attribute IDs;
- negative, zero, NaN, or infinite amounts;
- duplicate event IDs.

The Test Lab includes Feature 5.2 controls for strength training, balanced training, setting a removable Strength 100+ development source, clearing development sources, and proving invalid negative growth is rejected.
