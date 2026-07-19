# Generic Actor Stats And Combat Integration

Feature 3.9 generalizes the runtime stat and status foundation so combat-capable actors can share the same stat receiver model.

## Architecture

`ActorStats` is the generic runtime stat component. It implements `IActorStats` and `IRuntimeStatReceiver`, owns one `RuntimeStatCollection`, and exposes calculated max health, max stamina, max mana, attack power, defense, and movement speed as read-only values.

Base values are serialized actor configuration. Active modifiers are runtime state and are registered by exact `StatModifierSource`. Removing one source only removes that source's modifiers.

`PlayerStats` now inherits from `ActorStats` and remains the player-specific adapter for equipment modifiers. Existing player callers still use `MaximumHealth`, `MaximumStamina`, `MaximumMana`, `AttackPower`, `Defense`, and `StatsChanged`.

The prototype enemy now has `ActorStats` and `StatusEffectController`, so it can receive status-owned runtime modifiers without depending on player-only classes.

## Health And Vitals

Player health remains player-specific because it owns defeated input blocking. Enemy health remains enemy-specific because it owns enemy defeat presentation hooks. Both read defense through shared combat stat lookup.

Enemy maximum health now comes from `ActorStats` when present, with the old serialized `maximumHealth` retained as a compatibility fallback. Max-health decreases clamp current health; max-health increases do not refill current health automatically except during explicit reset.

Player stamina and mana continue to use the player stat adapter and remain separate from enemy health.

## Damage Pipeline

Combat callers calculate pre-mitigation damage at execution time:

`preMitigation = configured base damage + source attack power` when `AttackPowerScalingPolicy.AddSourceAttackPower` is selected.

`DamageEffectDefinition` defaults to `IgnoreSourceAttackPower`, so Arcane Bolt and Heavy Arcane Bolt keep their configured damage values until a future magic-power stat exists.

Targets apply shared mitigation:

`finalDamage = max(1, preMitigation - defense)` for valid positive hits.

Zero defense preserves incoming damage. Defense cannot produce negative damage, and valid hits have an explicit one-point minimum.

`DamageResult` now carries structured calculation values: pre-mitigation amount, defense, mitigated amount, applied amount, remaining health, defeated state, and message.

## Integration Points

Player melee uses the shared attack-power calculation with `AddSourceAttackPower`.

Enemy melee uses the shared attack-power calculation with `AddSourceAttackPower`; the prototype enemy keeps base attack damage at 12 and base attack power at 0 to preserve baseline behavior.

Spell projectile delivery remains unchanged. Ability projectile payloads use `DamageEffectDefinition`, which ignores attack power by default and still applies target defense.

Status effects apply modifiers through `StatusEffectController` into the actor's `IRuntimeStatReceiver`. Prototype Might modifies attack power. Prototype Weakened modifies defense and can now affect enemy damage intake.

Equipment modifiers are still owned by `PlayerStats` using equipment-slot source identities. Status modifiers use status application IDs, so equipment removal does not remove status modifiers and status expiration does not remove equipment modifiers.

## Prototype Wiring

The Prototype Player keeps `PlayerStats` for compatibility and status/equipment integration.

The Prototype Enemy has:

- `ActorStats`: 65 max health, 0 attack power, 1 defense, 1.8 movement speed placeholder;
- `EnemyHealth`: reads max health and defense from `ActorStats`;
- `EnemyMeleeAttack`: adds actor attack power to configured base damage;
- `StatusEffectController`: owns enemy runtime statuses;
- `PrototypeStatusEffectInteractable`: applies Prototype Weakened to the enemy through the existing interaction system.

No new input action or permanent debug UI was added.

## Reset Behavior

Prototype reset clears temporary player and enemy statuses before restoring vitals. Equipment remains equipped. Status-owned modifiers are removed by status source identity; equipment modifiers are rebuilt by equipment slot identity.

Repeated reset should leave baseline stats stable and should not duplicate modifiers.

## Save And Restore Ordering

Future save/load should restore in this order:

1. actor base configuration;
2. equipment state;
3. persistent statuses;
4. authoritative runtime modifier owners rebuild modifiers;
5. calculated stats recalculate;
6. current vitals clamp to the restored maximums.

Calculated final stat values should not be serialized as authoritative state. Raw runtime modifiers should only be saved when no authoritative owner, such as equipment or status, can rebuild them.

## Validation Rules

Current definition validation still covers stat modifiers and status references. Feature 3.9 adds runtime tests for invalid damage, duplicate modifier sources, defense mitigation, source attack-power scaling, enemy status compatibility, and max-health clamping.

Scene-level actor wiring is validated through Unity import and manual prototype checks rather than mutating assets automatically.

## Known Limitations

Movement speed is configured on `ActorStats` but not yet wired into the player motor or enemy controller. Being definitions, species, levels, skills, damage types, resistances, critical hits, block/dodge/parry, AI reactions, full save/load orchestration, and multiplayer replication remain future work.
