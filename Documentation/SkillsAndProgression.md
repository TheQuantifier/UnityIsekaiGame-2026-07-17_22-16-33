# Skills And Progression

Feature 5.3 introduces the alpha Skill system. In this project, Skills and Proficiencies are the same underlying model: a learned, player-owned runtime package that can improve derived capability through Calculated Stats, unlock abilities/actions, and progress independently through use.

Skills are not Base Attributes, Professions, Roles, Offices, Titles, Ranks, guild levels, or overall character level. Base Attributes remain the persistent long-term person-stat model refined in Feature 5.4a. Skills may contribute to Calculated Stats, but they do not permanently mutate Base Attributes or directly modify other Skills.

## Grades

Skills use the ordered `SkillGrade` enum:

- `F`
- `E`
- `D`
- `C`
- `B`
- `A`
- `AA`
- `AAA`

`AAA` means mastery. Mastered Skills remain learned and active, keep their cumulative grade effects, keep unlocked abilities/actions, and receive no further XP in the alpha rules.

## XP

Only learned Skills gain normal Skill XP. Each valid learned Skill use grants exactly 1 XP through the action-event pipeline. Failed but executed uses count; blocked actions do not. XP is independent per Skill, does not share across Skills, and has no alpha modifiers from Base Attributes, equipment, Roles, teachers, difficulty, rested state, daily caps, or anti-grind rules.

Each `SkillDefinition` authors its own XP thresholds for `F` through `AA` transitions. Excess XP carries across promotions, and administrative Test Lab grants can cross multiple grades in one operation. XP is capped at mastery by setting current XP to zero at `AAA`.

## Alpha Prototype Skills

The prototype catalog currently includes:

- `skill.swordsmanship`
- `skill.unarmed-combat`
- `skill.arcane-magic`
- `skill.healing-magic`
- `skill.appraisal`
- `skill.trading`
- `skill.smithing`

These are representative alpha definitions, not a final Skill list.
