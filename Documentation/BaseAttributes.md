# Base Attributes

Feature 5.4a defines Base Attributes as the permanent numerical foundation of a person or player character.

## Rules

- Base Attributes start from authored foundation values, currently `1.0` for alpha prototype attributes.
- Runtime storage keeps fractional precision.
- Standard display uses floor, so `1.75` displays as `1`.
- Base Attributes do not have current or maximum variants.
- Base Attributes are persisted through player/person-owned records: permanent source contributions and growth events.
- Only permanent adjustments and authored growth events mutate Base Attributes.
- Temporary statuses, equipment, Skills, Roles, Social Status, conditions, and scene effects do not mutate Base Attributes.

## Alpha Definitions

- `attribute.strength`
- `attribute.agility`
- `attribute.endurance`
- `attribute.vitality`
- `attribute.intellect`
- `attribute.willpower`
- `attribute.perception`
- `attribute.charisma`
- `attribute.mana-capacity`

## Implementation Notes

The Unity asset and serialized type names still use `AttributeDefinition`, `CharacterAttributes`, and `PlayerAttributesSaveData`. They are intentionally retained to avoid broad Unity serialization churn. New UI, documentation, and diagnostics should use Base Attribute terminology.

`CharacterAttributes` rebuilds full values from authored definitions, permanent source contributions, and growth events. It does not keep temporary modifier state.

## Display

The Character menu displays floor values for compact readability. The Test Lab diagnostic page displays full fractional value, display floor, foundation value, permanent source total, and growth total.
