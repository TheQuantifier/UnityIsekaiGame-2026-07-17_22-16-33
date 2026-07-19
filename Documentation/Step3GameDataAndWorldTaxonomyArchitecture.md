# Step 3 Game Data And World Taxonomy Architecture

Step 3 establishes the shared static-data foundation for the prototype. It is intentionally a definition and integration layer, not a new gameplay milestone.

## Core Rule

ScriptableObject definitions describe authored game data. Runtime MonoBehaviours, runtime instances, and save-data DTOs own mutable state.

Definitions may contain stable IDs, display metadata, taxonomy, typed references to other definitions, and prototype tuning values. They must not contain current inventory quantities, equipped-slot state, active status applications, quest or contract progress, current health, cooldowns, spawned-scene state, faction reputation, place discovery state, or calculated resistance/stat totals.

## Definition Catalog

`DefinitionCatalog` is the static entry point for authored definitions. `PrototypeDefinitionCatalog` currently gathers prototype items, categories, tags, rarity, quality, condition, abilities, effects, statuses, beings, actor profiles, places, factions, contracts, quests, people, and damage types.

`DefinitionRegistry` is the read-only runtime lookup shape built from a catalog. It resolves stable IDs to `IGameDefinition` instances and reports invalid or duplicate IDs without mutating source assets.

`DefinitionCatalogValidator` is the gate for catalog health. Each definition family can participate through `IDefinitionCatalogValidationParticipant` to validate cross-definition references, hierarchy constraints, duplicate-looking modifiers, and legacy fallbacks.

## Taxonomy

Categories and tags are shared metadata. Categories give one primary tree position inside a domain such as item, ability, place, being, or faction. Tags add flexible labels like `tag.magic`, `tag.arcane`, `tag.guild-related`, or `tag.hostile`.

Taxonomy is descriptive. It should not become a runtime authority for ownership, current membership, objective progress, or combat state. Runtime systems may use categories and tags for filtering, UI display, loot rules, or future generation rules.

## Items And Item Instances

`ItemDefinition` owns stable item identity, display info, classification, rarity, stack policy, use effects, and equipment configuration. The current prototype still keeps legacy item IDs for some early assets, and validation warns rather than fails for those IDs.

`ItemInstance` owns runtime identity for stateful items. `ItemInstanceSaveData` and related inventory/equipment save DTOs preserve instance IDs, definition IDs, quality, condition, and metadata. This lets two swords exist as separate runtime objects even when they share the same item definition.

Inventory and equipment own where an item instance currently lives. The definition does not know whether it is in a bag, equipped, dropped, damaged, or serialized.

## Rarity, Quality, And Condition

`RarityDefinition` is static authored significance for an item definition.

`QualityDefinition` and `ConditionDefinition` are separate from rarity and are intended for generated or runtime item-instance metadata. They exist now so save data and future item generation can reference stable IDs without overloading rarity.

## Abilities, Effects, Statuses, Stats, And Combat

`AbilityDefinition` owns static ability cost, cooldown, targeting, delivery, and effect references. Runtime casters own cooldowns, current resources, projectile instances, and selected loadouts.

`EffectDefinition` assets execute reusable behaviors such as damage, healing, and status application. They validate their referenced definitions through the catalog.

`StatusEffectDefinition` owns static duration, stacking, visible metadata, stat modifiers, resistance modifiers, and optional periodic effects. `StatusEffectController` owns active applications and removes exact-source modifiers when applications expire or are removed.

`ActorStats` owns runtime generic stats and typed resistance state for each actor. Equipment, statuses, profiles, and future systems add modifiers by exact source identity so one source can be removed without erasing another.

`DamageTypeDefinition` owns typed damage identity, parent hierarchy, Defense policy, minimum-damage policy, and display metadata. Damage packets use those definitions at runtime, while health components still own current health and application results.

## Beings, Actor Profiles, Places, And Factions

`BeingDefinition` describes broad creature or person taxonomy. `ActorProfileDefinition` describes reusable base actor stats and optional base resistances for a being. Neither owns the currently spawned actor.

`PlaceDefinition` describes world hierarchy and metadata such as place kind, map label, parent place, governing faction, and discovery placeholders. Scene triggers and reporters own whether a player is currently inside a place.

`FactionDefinition` describes organizations and governments with hierarchy, kind, authority flags, presentation color, and typed place/person references. Memberships, ranks, diplomacy, laws, economy, and reputation remain future runtime systems.

## Contracts And Quests

Contracts and quests are separate runtime systems that share objective and reward definitions. Step 3 adds typed references where practical, such as people, factions, and places, while keeping old strings as serialized fallbacks.

`ContractDefinition` is static job metadata. `ContractInstance` owns progress and reward claim state.

`QuestDefinition` is static narrative/task metadata. `QuestInstance` owns active stage, objective progress, completion, and reward claim state.

## Prototype Integration Points

The prototype scene uses the Step 3 data through these representative paths:

- inventory and equipment display item definition metadata and runtime item instance IDs;
- player equipment contributes stat and resistance modifiers through exact equipment-slot sources;
- abilities spend resources only after target validation succeeds;
- status effects update the Character page status readout from controller events;
- actor profiles initialize reusable base stats and base resistances;
- place triggers report typed reach-location objectives;
- the contract board opens typed contract definitions in the existing modal/input model;
- typed damage and resistance calculations flow through shared damage packets.

## Closeout Status

Step 3 is ready to serve as the static data foundation for Step 4. Remaining prototype warnings are known legacy-content warnings, mostly old item IDs and broad root taxonomy IDs.

Step 4 now begins with `PersistenceService`, a versioned save envelope, explicit participant registration, and prototype-only save/load proof. See `Documentation/PersistenceServiceFoundation.md` and `Documentation/Step4PersistenceRoadmap.md`. Future Step 4 features should add one runtime owner at a time on top of these definitions.
