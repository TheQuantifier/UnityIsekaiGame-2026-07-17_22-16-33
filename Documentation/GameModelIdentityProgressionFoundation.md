# Game Model Identity And Progression Foundation

Feature 5.1 adds the first player-owned identity/progression model. It is still local and prototype-quality, but it separates account, player, person, and current world-entity identity so later multiplayer persistence does not treat one player's save as ownership of the world.

## Runtime Model

- `PlayerIdentityProgression` owns player-scoped identity and progression state.
- Account ID, player ID, person ID, and current world-entity ID are distinct concepts.
- Origin assignment is once-only per progression reset.
- Starting origin rewards apply once: permanent stat grants, Gold, starting role, social statuses, title, and birth gift assignment.
- Birth gifts support immediate awakening and delayed active-playtime awakening.
- Roles keep lifecycle state and history. Conflicting roles are rejected unless replacement is explicitly accepted.
- Social statuses are non-combat status records with context, such as Government, Organization, Place, or Global.
- Wallet state currently supports `currency.gold`.
- Overall level is derived, not directly authored: 75% activity and 25% persistent stat contribution by default.

Feature 5.4a terminology update: origin and birth-gift permanent stat rewards apply as persistent Base Attribute sources when `CharacterAttributes` is present. Roles and Social Statuses remain active effects, but their legacy modifiers are bridged into Calculated Stat contributions by `ActorStats`.

## Definitions

New definition types:

- `OriginFamilyDefinition`
- `OriginDefinition`
- `BirthGiftDefinition`
- `RoleDefinition`
- `SocialStatusDefinition`
- `TitleDefinition`
- `CurrencyDefinition`
- `OverallLevelConfiguration`

Prototype content includes native-born, summoned-otherworlder, and reincarnated-local families; farmer, merchant, noble, royal-court, church, mage-tower, accidental, commoner, adventurer, and scholar/mage origins; commoner, noble, summoned guest, and outsider roles; core legal/social statuses; Lord/Lady titles; Gold; and six birth gifts.

## Persistence

The required player-scoped participant is:

`player.identity-progression`

Load phase: `IdentityAndProgression`, before inventory, stats/vitals/status, quests/contracts, and location.

Old development saves that do not contain this participant are intentionally incompatible. Migration is not provided because the old saves cannot prove person identity, origin assignment, starting rewards, wallet state, role history, social status history, or birth-gift reward state.

This participant is player-owned only. It does not restore shared-world state, region state, NPCs, enemies, pickups, boards, or other players.

## Test Lab

The existing Tab menu Test Lab now has an `Identity 5.1` section with controls to validate identity IDs, generate origin, prove duplicate origin rejection, reset identity for development, advance or awaken birth gifts, add/replace/suspend/revoke/abandon roles, add/resolve social statuses, add/spend Gold, and record activity/participation samples.

Manual test steps:

1. Open Play Mode in `PrototypeScene`.
2. Press Tab and select `Test Lab`.
3. Select the `Identity 5.1` top Test Lab tab.
4. Press `Validate IDs`; expect success.
5. Press `Generate Origin`; expect origin, birth gift, roles/statuses, wallet, and level summary to update.
6. Press `Duplicate Proof`; expect the second origin assignment to be rejected.
7. Select `role.noble` or another role and press `Add Role`; expect a conflict if another social-stratum role is active.
8. Press `Accept Conflict`; expect the previous role to move to history and the selected role to become active.
9. Press `Add Money`, then `Spend Money`; expect Gold balance to change.
10. Press `Record Success`, `Record Failure`, and `Participation`; expect the overall-level diagnostics to update.
11. Save and load from the Save/Load UI or Test Lab Persistence tab; expect identity/progression state to persist.
