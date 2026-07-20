# Feature 5.5 Traits, Capabilities, and Requirements

Feature 5.5 adds the first prototype foundation for persistent Traits, aggregated Capabilities, and shared Requirement checks.

## Traits

Traits are persistent character facts that are not Skills, Base Attributes, Calculated Stats, Resources, Roles, Social Statuses, Titles, Conditions, Equipment, or Origin choices. They describe innate, acquired, supernatural, cursed, racial, biological, or narrative character properties.

`TraitDefinition` owns static authored data:

- stable ID, display name, description, classification, tags, and alpha flag;
- polarity and permanence class;
- default lifecycle, discovery, and visibility;
- conflict groups and explicit incompatible Traits;
- Calculated Stat contributions;
- ability/action grant IDs;
- Boolean and numeric Capability grants;
- resistance and immunity grants;
- linked Trait grants;
- optional Skill grants through the existing Skill API;
- optional requirement metadata for future authoring.

Runtime state is owned by `CharacterTraitCollection`. There is one runtime record per Trait per character. A record may have multiple source records, so removing one source does not remove the Trait while another source still owns it.

Trait lifecycle states:

- `Dormant`: owned but inactive; no active stat/capability effects.
- `Active`: active and contributing effects.
- `Suppressed`: still owned but temporarily not contributing effects.
- `Removed`: no longer active in normal gameplay.
- `Historical`: retained for audit/history after explicit replacement.

Discovery states:

- `Undiscovered`: hidden from normal Character UI.
- `Suspected`: visible only when the Trait is not secret.
- `Discovered`: true name may be shown.

Visibility states:

- `Public` and `Known` can show their true name when not undiscovered.
- `Hidden` can show a placeholder while suspected.
- `Secret` is hidden from normal Character UI until discovered.

Conflicts reject by default. Replacement must be explicitly requested and must authorize every replaced Trait ID. Replacement is transactional: if a linked grant, effect rebuild, or replacement step fails, the previous Trait records are restored.

## Capabilities

`CapabilityDefinition` describes reusable Boolean or numeric capabilities such as low-light vision, mana sense, ambidexterity, immunities, and resistances. Capabilities are runtime aggregates, not independent owner state.

Aggregation policies:

- `BooleanAny`: true when any active source grants true.
- `Sum`: numeric contributions are added and clamped to the definition bounds.
- `Highest`: highest numeric contribution wins.
- `PriorityOverride`: highest-priority source wins.
- `Blocker`: blocker sources force the snapshot into a blocked state.

Trait-owned Capability contributions are rebuilt from active Traits. UI and requirement code read snapshots only; they do not mutate Capability state.

## Requirements

`RequirementSetDefinition` contains a root logical group with child requirement nodes. `CapabilityRequirementEvaluator` is a pure read-only evaluator shared by future abilities, actions, dialogue, equipment gating, contracts, and Test Lab diagnostics.

Supported requirement node families include:

- Base Attributes;
- Calculated Stats;
- current, maximum, and normalized Resources;
- Skill grade;
- Trait lifecycle;
- Role, Social Status, Origin, Birth Gift, and Title;
- inventory and equipped item checks;
- ability/action ownership;
- condition/status presence and absence;
- currency balance;
- Boolean and numeric Capabilities.

The evaluator does not spend Resources, grant state, remove state, or alter discovery. It returns node-level internal reasons for Test Lab and player-facing failure reasons based on visibility.

## Persistence

Feature 5.5 adds optional player participant `player.traits`.

- Scope: `Player`
- Owner: local prototype player ID
- Schema: `1`
- Load phase: `Skills`
- Required: `false`

Saved Trait records store stable Trait definition IDs, lifecycle, discovery, visibility override, source records, suppression source records, replacement history, transition history, and linked grant metadata. Definitions remain authoritative for current effects; load restores records, then rebuilds active Trait effects from definitions.

Development saves without `player.traits` are accepted because this is a new optional participant. Saves containing unknown Trait IDs, duplicate Trait records, duplicate source records, invalid lifecycle, or invalid discovery are rejected. No legacy aliases are retained for obsolete Trait IDs.

Future multiplayer persistence remains server-owned. Clients may request Trait-affecting actions, but the server should own final Trait records and automatic persistence. On disconnect, the server should persist the disconnecting player's Trait state while shared-world state and other connected players continue.

## Prototype Content

The prototype catalog now includes alpha Trait content for ambidexterity, frailty, reincarnated memory, beastkin/night vision, brave/cowardly conflict, living/undead conflict, poison immunity/resistance, mana sensitivity, fire resistance, and blessing-style stat bonuses.

The prototype catalog also includes Capability definitions for ambidexterity, low-light vision, mana sense, otherworld knowledge, poison immunity, fire resistance, and poison resistance, plus Requirement definitions for heavy sword technique, fire ritual, and royal audience checks.
