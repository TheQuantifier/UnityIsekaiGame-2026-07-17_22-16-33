# Calculated Stats

Feature 5.2 introduces Calculated Stats as cached derived runtime values. They are rebuilt from Attributes plus active source-safe contributions.

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

`CalculatedStatCollection` caches results and emits `CalculatedStatsChanged` after dependencies update. It does not persist calculated values. Saves persist Attributes and active systems persist their own state, then calculated stats are rebuilt after load.

Compatibility path:

- old `StatType.MaximumHealth`, `MaximumStamina`, `MaximumMana`, `AttackPower`, `Defense`, and `MovementSpeed` map to calculated-stat IDs;
- `ActorStats` reads calculated values when configured;
- old `RuntimeStatModifier` callers are bridged into calculated-stat contributions by source category;
- future systems should register `RuntimeCalculatedStatContribution` directly instead of treating legacy base stats as authoritative.

Feature 5.3 Skill grade packages register direct `RuntimeCalculatedStatContribution` entries with source category `Skill`. Each reached grade uses a distinct source ID such as `skill.swordsmanship.grade.f`, so cumulative Skill effects can rebuild without duplicating or replacing lower-grade packages.
