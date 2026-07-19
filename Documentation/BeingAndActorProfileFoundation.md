# Being And Actor Profile Foundation

Feature 3.10 adds static actor taxonomy without merging runtime actor state into definition assets.

## Responsibilities

`BeingDefinition` describes a reusable being type or broad classification, such as `being.person` or `being.prototype-enemy`. It owns static metadata only: stable ID, display name, description, category, tags, icon, optional parent being, intelligence classification, social capability, locomotion flags, nature flags, and an optional default actor profile reference.

`ActorProfileDefinition` describes reusable base actor configuration. It owns stable ID, display name, description, a required `BeingDefinition`, category/tags, base values for maximum health, stamina, mana, attack power, defense, movement speed, and optional base typed resistances.

`PersonDefinition` remains individual identity data. It may reference a `BeingDefinition` and optionally an `ActorProfileDefinition`, but it does not own current health, statuses, inventory, equipment, AI state, or scene presence.

`ActorStats` owns runtime base values, runtime modifiers, calculated values, and the actor's runtime typed resistance collection. Equipment and statuses continue to apply stat modifiers through `RuntimeStatCollection` and resistance modifiers through `RuntimeResistanceCollection`.

Location remains separate from being and actor profile data. A being type may later gain habitat metadata, but `ActorProfileDefinition` does not describe where an actor lives or currently is. Static home/workplace information belongs on person/place-facing systems, and current runtime location belongs in future runtime location state.

Faction affiliation remains separate too. Static affiliation defaults live on `PersonDefinition` and `FactionDefinition`; current memberships, ranks, reputation, and legal standing belong to future runtime faction-state systems.

No generic `ActorIdentity` component was introduced. The least disruptive model is:

- `ActorStats` owns the optional `ActorProfileDefinition` reference for actors with runtime stats.
- `PersonIdentity` continues to represent loaded scene presence for named people.
- `PersonRegistry` remains separate from definition lookup and no runtime actor registry was added.

## Profile Precedence And Migration

Actor stat initialization uses this precedence:

1. Assigned `ActorProfileDefinition`, when present and valid.
2. Legacy serialized `ActorStats` fields as fallback.

The legacy fields remain serialized for compatibility with old scenes, prefabs, and tests. They are fallback-only when a valid profile is assigned. `ActorStats.HasProfileLegacyConflict` exposes whether assigned profile values differ from fallback fields so later editor tooling can warn during migration.

Profiles are not mutated in Play Mode. Runtime modifiers remain owned by `RuntimeStatCollection`, and repeated initialization returns `AlreadyInitialized` without clearing modifiers.

## Prototype Configuration

Added prototype categories:

- `being-category`
- `being-category.person`
- `being-category.monster`

Added prototype tags:

- `tag.humanoid`
- `tag.intelligent`
- `tag.social`
- `tag.hostile`
- `tag.can-use-equipment`
- `tag.can-use-magic`

Added prototype beings:

- `being.person`
- `being.prototype-enemy`

Added prototype profiles:

- `actor-profile.player-prototype`: 100 health, 100 stamina, 100 mana, 5 attack, 0 defense, 0 movement metadata.
- `actor-profile.enemy-prototype`: 65 health, 0 stamina, 0 mana, 0 attack, 1 defense, 1.8 movement metadata.
- `actor-profile.civilian-basic`: optional noncombat person baseline.

The prototype player and prototype enemy in `PrototypeScene` now reference these profiles while preserving their legacy fallback values. `PrototypeNpcPerson` references `being.person` and does not require an actor profile.

Feature 4.6 gives the authored Prototype Enemy and Prototype Dialogue NPC separate `WorldEntityIdentity` values. `BeingDefinition`, `ActorProfileDefinition`, and `PersonDefinition` describe static content; world entity IDs identify specific scene/runtime objects that may later carry mutable world state.

## Catalog And Validation

`BeingDefinition` and `ActorProfileDefinition` implement `IGameDefinition` and are registered in `PrototypeDefinitionCatalog`.

Validation catches:

- invalid or duplicate stable IDs through the shared catalog validator;
- missing display names through existing shared checks;
- category/tag references through existing classification validation;
- missing or non-catalog being/profile references;
- circular being parent hierarchies;
- contradictory no-intelligence/social metadata;
- missing profile `BeingDefinition`;
- negative or non-finite actor profile base values.

## Save-Data Preparation

`ActorSaveData` stores stable ID strings instead of asset references:

- `actorProfileId`
- `beingDefinitionId`
- `personDefinitionId`
- current health/stamina/mana placeholders

Recommended future restoration order is represented by `ActorSaveRestoreOrder`:

1. Resolve `BeingDefinition`.
2. Resolve `ActorProfileDefinition`.
3. Initialize `ActorStats` base values.
4. Restore inventory and equipment.
5. Restore statuses.
6. Rebuild runtime modifiers.
7. Restore current vitals.
8. Clamp vitals to final maximums.

Calculated final stats should not be saved as authoritative data.

## Creating Content

To create a new being definition:

1. Create `Unity Isekai Game/Beings/Being`.
2. Assign a globally unique `being.*` ID.
3. Assign a broad `being-category` category and a small set of useful tags.
4. Fill static intelligence, social, locomotion, and nature metadata.
5. Register it in the active `DefinitionCatalog`.

To create a new actor profile:

1. Create `Unity Isekai Game/Beings/Actor Profile`.
2. Assign a globally unique `actor-profile.*` ID.
3. Reference the intended `BeingDefinition`.
4. Enter base stat values only.
5. Register it in the active `DefinitionCatalog`.
6. Assign it to an `ActorStats` or `PlayerStats` component.

## Known Limitations

This feature does not implement full species biology, character creation, body types, visuals, professions, skills, levels, factions, relationships, schedules, AI decisions, damage types, resistances, procedural actors, full save/load, or multiplayer replication. Locomotion and social metadata are classification only and do not drive movement, AI, dialogue, or combat behavior yet.

World entity identity does not change that boundary. It provides actor object handles for future persistence, but actor mutable state still needs explicit server/world/player-owned participants.
