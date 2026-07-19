# Ability And Effect Definition Foundation

Feature 3.7 adds a shared static data language for actions and results without replacing the current spell, item, melee, or equipment systems.

## Responsibilities

`AbilityDefinition` describes an action that can be performed. It owns stable ID, display text, category, tags, resource costs, cooldown duration, targeting mode, delivery mode, projectile delivery configuration, and ordered effect references.

`EffectDefinition` describes a result caused by an ability, item, projectile, melee attack, environment object, or future system. Effects execute through explicit contexts and return structured results.

Definitions are static ScriptableObject configuration. Runtime state such as cooldowns, resource values, active projectiles, selected targets, item instances, and execution history lives outside definition assets.

## Independent Effects

The prototype uses independent reusable `EffectDefinition` assets for shared behavior such as arcane damage and health restoration. This keeps spells and consumable items able to reference the same result vocabulary through the definition catalog.

Private embedded effect configuration may still be useful later for one-off ability tuning, but this feature registers reusable effects as first-class definitions because they are now referenced by both abilities and adapters.

## Costs

`AbilityResourceCost` supports Mana, Stamina, and prepared Health costs. Mana and Stamina are active. Health costs are reserved.

Cost execution is two-stage:

1. Validate all costs and reject duplicate resource types.
2. Commit costs only after ability, range, cooldown, delivery, and immediate-effect validation succeeds.

No effect or projectile is executed if cost validation fails. The current `PlayerMana` and `PlayerStamina` APIs are reused.

## Cooldowns

`AbilityCooldownTracker` owns runtime cooldown state by stable ability ID. `AbilityDefinition` stores only static cooldown duration.

Cooldown starts after costs commit and execution begins. For projectile abilities, that point is projectile spawn. Failed target validation, insufficient resources, invalid delivery, and active cooldown do not start cooldown.

Future cooldown groups, global cooldowns, and shared category cooldowns are intentionally out of scope.

## Targeting And Delivery

Targeting answers what is selected. The initial modes are `Self`, `DirectTarget`, and `Direction`.

Delivery answers how effects reach the target. The initial modes are `Immediate` and `Projectile`.

Current Arcane Bolt and Heavy Arcane Bolt use direction targeting plus projectile delivery. Health restoration uses self/direct target semantics through the item-use adapter.

## Contexts And Results

`AbilityExecutionContext` carries source, target, delivery origin, source and target positions, direction, gameplay-blocked state, optional source item definition, optional source item instance, magnitude multiplier, and a projectile-spawn callback.

`EffectExecutionContext` is narrower and carries the resolved source, target, positions, direction, ability, optional item source, and multiplier.

`AbilityExecutionResult` and `EffectExecutionResult` use status enums plus messages. Runtime logic should branch on statuses, not message strings.

## Effect Sequencing

Effects execute in serialized order. Missing effects or failed validation stop execution. There is no rollback of already-applied irreversible effects. Optional effects and branching graphs are deferred.

## Current Integrations

`SpellDefinition` now has an optional `AbilityDefinition` reference. When present, the ability data is authoritative for mana, cooldown, delivery, and effects. Legacy spell fields remain as serialized compatibility fallback fields for existing assets and UI.

`SpellProjectile` can carry an ability payload and execute its configured effects once on impact. Legacy hard-coded projectile damage remains as a fallback for spells without an ability reference.

`RestoreHealthItemUseEffect` can reference `RestoreVitalEffectDefinition`. The health potion keeps its existing item-use flow, including full-health failure and no consumption on failed use.

Melee combat is not rewritten. The new `DamageEffectDefinition` accepts the same `IDamageable` contract used by melee, so melee can migrate later.

Equipment stat modifiers remain persistent equipped-state calculations. Feature 3.8 adds status effects as the preferred bridge for time-bound modifiers. Ability effects can now apply a `StatusEffectDefinition` through `ApplyStatusEffectDefinition` so modifier lifetime is owned by a runtime status controller.

Ability contexts can carry optional `ItemDefinition` and `ItemInstance` sources for future item-instance-aware attacks, wands, or potion instances.

## Prototype Definitions

Current prototype definitions:

- `ability.arcane-bolt`
- `ability.heavy-arcane-bolt`
- `ability.restore-health`
- `effect.arcane-damage`
- `effect.heavy-arcane-damage`
- `effect.restore-health`
- `effect.apply-prototype-might`
- `effect.apply-prototype-weakened`

## Creating A New Ability

1. Create an `AbilityDefinition`.
2. Assign a globally unique `ability.` ID.
3. Assign an ability category and any tags.
4. Configure costs, cooldown, targeting, and delivery.
5. Add ordered effect references.
6. Add reusable effects and the ability to the definition catalog.
7. Run definition validation.

## Creating A New Effect

1. Create a concrete `EffectDefinition`.
2. Assign a globally unique `effect.` ID.
3. Assign category and tags.
4. Configure magnitude and target requirements.
5. Add the effect to the definition catalog when it is independently reusable.
6. Add tests for validation and execution behavior.

## Validation

Definition validation checks ability/effect IDs through existing catalog rules, missing categories, missing effects, effects not registered in the catalog, duplicate resource types, invalid costs, invalid delivery configuration, and spell/ability legacy field conflicts.

## Known Limitations

There is no status-effect runtime, damage-type resistance system, area targeting, homing, chaining, skill progression, ability levels, animation events, save files, multiplayer replication, or final VFX/UI. Health costs are prepared but not active.

Future extensions can add status effects, profession abilities, social actions, world-state effects, AI ability selection, save/load snapshots, and multiplayer-authoritative execution.
