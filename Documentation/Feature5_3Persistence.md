# Feature 5.3 Persistence

Feature 5.3 adds a player-scoped persistence participant for Skills.

## Participant

- Key: `player.skills`
- Scope: `Player`
- Schema: `PlayerSkillsSaveData.CurrentSchemaVersion == 1`
- Load phase: `PersistenceLoadPhase.Skills`
- Dependencies: `player.identity-progression`, `player.attributes`

The participant owns learned Skill records, hidden learning progress, and consumed action-event keys. It does not authoritatively persist Calculated Stat contributions, ability ownership, cached mastery flags, next-threshold values, or UI state. Those are rebuilt from Skill state and definitions after load.

## Payload

Hidden progress records persist:

- SkillDefinition ID;
- current hidden count;
- required count snapshot;
- qualifying event ID;
- first/latest progress timestamps and playtime;
- source system metadata.

Learned Skill records persist:

- SkillDefinition ID;
- current grade;
- current XP;
- lifetime XP;
- lifetime valid uses;
- acquisition source and reason;
- acquisition timestamp/playtime;
- starting grade;
- last-use timestamp/playtime;
- promotion history;
- rebuilt grade/unlock state for diagnostics.

## Restore

Prepare validates schema, owner identity, definition resolution, duplicate learned records, duplicate hidden records, learned-versus-hidden conflicts, grade validity, XP counters, mastery XP, unknown definitions, and unprocessed XP at non-mastered grades.

Commit restores hidden progress and learned Skills, rebuilds cumulative grade effects, rebuilds ability/action unlocks, recalculates affected Calculated Stats through the existing stat collection, and emits a coherent refresh. Repeated load is designed to avoid duplicate Skills, hidden progress, stat contributions, ability unlocks, and action-event processing.

## Save Compatibility

Feature 5.3 intentionally adds a required `player.skills` participant for development saves. Pre-5.3 local saves do not contain hidden learning progress, learned Skills, consumed action-event keys, or deterministic Skill grant history. Because the project is pre-alpha, old development saves should be recreated after validating definitions instead of silently inventing Skill state.

Future migration may bootstrap empty Skill state only when persisted identity/origin/gift state proves that no Skill grants need to be replayed. It must never reroll origin or gift selection and must never silently grant random Skills.

## Multiplayer Boundary

Current implementation is local and single-player, but the model is player-owned and avoids static global Skill state. In multiplayer, the server must validate qualifying action execution, hidden progress, direct grants, XP awards, grade promotion, mastery, calculated-stat contributions, and ability unlocks. Clients may display Skill state and request actions, but must not authoritatively award progress or upload Skill state as shared-world truth.
