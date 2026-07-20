# Skill Learning

Feature 5.3 separates unknown Skill learning from learned Skill XP.

## Unknown Versus Learned

Characters do not start with every Skill at level zero. Unknown Skills may accumulate hidden learning progress when the player performs qualifying actions. Hidden progress is not shown in normal gameplay or the Character menu. The Prototype Test Lab can inspect it for development testing.

When hidden progress reaches the authored natural-learning threshold, the Skill is learned at the configured starting grade, normally `F`. The threshold-completing action learns the Skill but does not also award normal Skill XP; XP begins on the next valid learned use.

## Alpha Learning Condition

Each naturally learnable `SkillDefinition` uses one simple qualifying-action counter. A qualifying action is described by authored data instead of a hardcoded switch:

- stable qualifying event/action ID;
- action category;
- optional item category;
- optional item tag;
- optional magic tag;
- optional action tag;
- required count;
- granted starting grade.

Every completed qualifying action adds exactly one hidden learning point. A failed but executed action counts. A blocked input or action that never executes does not count.

## Duplicate Protection

`SkillActionExecutionEvent.EventId` is combined with the Skill ID to form an action-processing key. One event may progress multiple matching Skills, but it can progress each matching Skill only once. This protects against duplicate callbacks, repeated subscriptions, local replay, and restoration-driven events.

## Direct Grants

Origins, birth gifts, quest rewards, trainers, development tools, and future systems grant Skills through the shared `CharacterSkillCollection.GrantSkill` API.

Duplicate grants use one runtime record per Skill:

- unknown Skill: learn at requested starting grade;
- existing lower grade: promote to requested grade;
- same or higher grade: no downgrade and no duplicate;
- existing XP is preserved;
- newly reached cumulative grade packages and ability/action unlocks apply once.
