# Character Snapshots And Queries

Feature 5.6 adds read-only character access through `CharacterSystemCoordinator` and `CharacterQueryService`.

Snapshot types:

- `CharacterIdentitySnapshot`: account, player, person, actor/body, display name, origin, birth gift, readiness, revision.
- `CharacterProgressionSnapshot`: overall level, learned Skills, visible or development Trait snapshots, roles, social statuses, titles.
- `CharacterNumericalSnapshot`: Base Attributes, Calculated Stats, Current Resources.
- `CharacterSocialSnapshot`: roles, social statuses, titles, wallet balances.
- `CharacterCapabilitySnapshot`: capabilities, resistances, immunities.
- `CharacterFullSnapshot`: composed schema-versioned snapshot for UI, Test Lab, logging, save diagnostics, and future replication.

Snapshots copy runtime collections into read-only arrays/dictionaries. UI should not mutate objects reached through snapshots.

`CharacterSystemCoordinator.Revision` increments after logical character changes and invalidates cached snapshots. The Character menu uses the player snapshot. Test Lab uses the development snapshot so hidden Skills and undiscovered Traits can be inspected.

`CharacterQueryService` supports:

- `GetBaseAttribute`
- `GetCalculatedStat`
- `GetResource`
- `GetSkillGrade`
- `HasTrait`
- `GetCapability`
- `EvaluateRequirement`

Requirement evaluation fails clearly when the character is not `Ready`.

