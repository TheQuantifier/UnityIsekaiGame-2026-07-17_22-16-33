# Current Resources

Feature 5.4b adds Current Resources as the third numerical layer:

1. Base Attributes are persistent long-term person qualities.
2. Calculated Stats are derived runtime values.
3. Current Resources are mutable current values with maximums derived from Calculated Stats.

The alpha resources are:

- `resource.health`, capped by `calculated-stat.maximum-health`
- `resource.stamina`, capped by `calculated-stat.maximum-stamina`
- `resource.mana`, capped by `calculated-stat.maximum-mana`

`ResourceDefinition` assets describe metadata, initialization, minimum value, maximum stat link, regeneration or degeneration rules, permitted operations, persistence policy, and future authority expectations. They do not store current runtime values.

Runtime state lives in `CharacterResourceCollection`. It owns `RuntimeResourceRecord` entries, applies transactions, emits change/threshold events, reconciles maximum changes, and creates/restores `PlayerResourcesSaveData`.

The existing `PlayerHealth`, `PlayerMana`, and `PlayerStamina` APIs remain the compatibility surface for HUD, spells, abilities, items, and Test Lab controls. When `CharacterResourceCollection` is configured, those wrappers delegate to it. If no resource collection is configured, they retain their legacy local behavior.

Maximum reconciliation is clamp-only for the alpha resources. Increasing a maximum does not refill current value. Decreasing a maximum clamps current value down to the new maximum.
