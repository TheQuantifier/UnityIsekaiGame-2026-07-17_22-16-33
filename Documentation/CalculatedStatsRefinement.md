# Calculated Stats Refinement

Feature 5.4a refines Calculated Stats as derived, non-authoritative values.

## Rules

- Calculated Stats rebuild from Base Attributes and active source-owned contributions.
- Calculated Stats are not saved as authoritative player state.
- Source systems own their own state and re-register contributions after load.
- Calculated Stats may represent resource maximums, but do not own current resource values.
- Health, Mana, and Stamina current values remain specialized runtime values until a later resource feature.

## Purpose Metadata

`CalculatedStatDefinition` now has a `CalculatedStatPurpose`:

- `General`
- `ResourceMaximum`
- `Combat`
- `Defense`
- `Utility`
- `Movement`
- `Capacity`
- `Accuracy`
- `Support`

`ResourceMaximum` stats must declare a linked future resource ID. The reserved alpha links are:

- `calculated-stat.maximum-health` -> `resource.health`
- `calculated-stat.maximum-stamina` -> `resource.stamina`
- `calculated-stat.maximum-mana` -> `resource.mana`

Definition validation rejects missing resource links, unsupported resource IDs, duplicate resource maximum mappings, and mappings that do not use the expected alpha maximum stat.

## Compatibility

`ActorStats` still exposes legacy properties such as `MaximumHealth`, `AttackPower`, and `Defense`. When a `CalculatedStatCollection` is configured, those properties read from Calculated Stats through `StatTypeCalculatedStatBridge`.

Legacy `RuntimeStatModifier` callers are bridged into calculated-stat contributions where practical. New systems should register `RuntimeCalculatedStatContribution` directly.
