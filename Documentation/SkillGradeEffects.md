# Skill Grade Effects

Skill effects are cumulative. A Skill granted or promoted to a higher grade receives every package from `F` through the reached grade.

Example:

- `F`: +2 Physical Power
- `E`: +3 Physical Power
- `D`: +2 Accuracy

At `D`, the active Skill contribution includes both Physical Power packages and the Accuracy package. Higher packages do not replace lower packages.

## Calculated Stat Contributions

Skill effects integrate with the Feature 5.2 `CalculatedStatCollection`. Each grade package owns source-safe contributions using source category `Skill` and a stable source identity:

`<skill-id>.grade.<grade>`

For example:

- `skill.swordsmanship.grade.f`
- `skill.swordsmanship.grade.e`

Rebuilding Skill effects removes existing Skill-grade sources for learned Skills, reapplies reached packages, and does not duplicate contributions.

## Ability And Action Unlocks

`SkillDefinition` structurally supports multiple ability/action unlocks at both the Skill level and grade-package level. Alpha content normally uses one unlock where useful, but the runtime supports more than one.

An unlock contains:

- ability reference or future action ID;
- required Skill grade;
- source identity;
- alpha availability flag;
- future metadata.

Lower-grade unlocks remain available after promotion. A higher starting grade unlocks every eligible lower-grade ability/action immediately. Duplicate unlock IDs are tracked on the runtime Skill record and are not added twice.

## Boundaries

Skills may contribute to Calculated Stats, unlock abilities/actions, unlock capability IDs for future requirement checks, and satisfy future interactions. Skills must not permanently modify Base Attributes, rewrite identity/origin state, or grant Skill XP bonuses.
