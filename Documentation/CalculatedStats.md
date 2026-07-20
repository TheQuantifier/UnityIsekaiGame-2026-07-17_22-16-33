# Calculated Stats

Feature 5.4a refines Calculated Stats as cached derived runtime values. They are rebuilt from Base Attributes plus active source-owned contributions.

Alpha Calculated Stats:

- `calculated-stat.physical-power`
- `calculated-stat.magical-power`
- `calculated-stat.healing-power`
- `calculated-stat.support-power`
- `calculated-stat.physical-defense`
- `calculated-stat.magical-defense`
- `calculated-stat.maximum-health`
- `calculated-stat.maximum-stamina`
- `calculated-stat.maximum-mana`
- `calculated-stat.movement-speed`
- `calculated-stat.carrying-capacity`
- `calculated-stat.accuracy`
- `calculated-stat.evasion`

Evaluation order:

1. weighted Attribute terms;
2. positive flat contributions;
3. negative flat contributions;
4. positive percentage contributions;
5. negative percentage contributions;
6. positive multipliers;
7. reducing multipliers;
8. clamp minimum to zero;
9. round once to nearest whole.

`CalculatedStatCollection` caches results and emits `CalculatedStatsChanged` after dependencies update. It does not persist calculated values. Saves persist Base Attributes and active systems persist their own state, then calculated stats are rebuilt after load.

Each `CalculatedStatDefinition` now declares a `CalculatedStatPurpose`. The alpha purposes are:

- `General`
- `ResourceMaximum`
- `Combat`
- `Defense`
- `Utility`
- `Movement`
- `Capacity`
- `Accuracy`
- `Support`

Resource maximum stats must also declare a linked resource ID:

- `calculated-stat.maximum-health` -> `resource.health`
- `calculated-stat.maximum-stamina` -> `resource.stamina`
- `calculated-stat.maximum-mana` -> `resource.mana`

Feature 5.4b adds `ResourceDefinition` assets for these IDs and `CharacterResourceCollection` owns their current runtime values. The existing Health, Stamina, and Mana wrappers remain as compatibility APIs.

Compatibility path:

- old `StatType.MaximumHealth`, `MaximumStamina`, `MaximumMana`, `AttackPower`, `Defense`, and `MovementSpeed` map to calculated-stat IDs;
- `ActorStats` reads calculated values when configured;
- old `RuntimeStatModifier` callers are bridged into calculated-stat contributions by source category;
- future systems should register `RuntimeCalculatedStatContribution` directly instead of treating legacy base stats as authoritative.

Feature 5.3 Skill grade packages register direct `RuntimeCalculatedStatContribution` entries with source category `Skill`. Each reached grade uses a distinct source ID such as `skill.swordsmanship.grade.f`, so cumulative Skill effects can rebuild without duplicating or replacing lower-grade packages.

See also:

- `Documentation/CalculatedStatsRefinement.md`
- `Documentation/CharacterNumericalModel.md`
- `Documentation/CurrentResources.md`
