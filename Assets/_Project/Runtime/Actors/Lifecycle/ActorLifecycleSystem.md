# Actor Lifecycle System

Feature 6.3 introduces reusable Actor/body lifecycle state for Health reaching zero.

## Scope

Lifecycle state belongs to the current Actor/body, not the Person identity. A saved player lifecycle payload records player ID, person ID, actor ID, policy ID, and lifecycle state, but it does not duplicate current Health. Health remains owned by `player.resources`.

## States

- `Active`: the actor can act normally.
- `Defeated`: the actor has been defeated but has not moved into unconsciousness or death.
- `Unconscious`: default living-being zero-Health result. No automatic death occurs.
- `Dead`: the body is dead and cannot act until revived.

`Recovered` and `Revived` are transition outcomes that return a body to `Active`; they are not stored states.

## Policies

`DefeatPolicyDefinition` controls the zero-Health outcome:

- `BecomeUnconscious`
- `DieImmediately`
- `RemainDefeated`
- `IgnoreDefeat`

If no policy asset is assigned, `ActorLifecycleController` uses a local default equivalent to `defeat-policy.living-standard`: zero Health transitions to `Unconscious`, death is not automatic, and recovery/revival restore at least 1 Health.

The prototype catalog includes:

- `defeat-policy.living-standard`
- `defeat-policy.immediate-death`
- `defeat-policy.nonliving-ignore`

## Capabilities and Requirements

Lifecycle checks use policy capability keys where available:

- `can.become-unconscious`
- `can.die`
- `can.recover`
- `can.be-revived`
- `immunity.death`

Missing lifecycle capability contributions are treated as allowed for the prototype so actors do not require new Traits just to participate. Blocked or explicitly false contributions reject the transition. Optional recovery, death, and revival `RequirementSetDefinition` references are evaluated through the existing character requirement system.

## Transactions

Health mutation duplicate protection remains owned by `CharacterResourceCollection` through `ResourceChangeRequest.EventId`. Lifecycle keeps a small transition ID cache only for non-resource state transitions and for resource-triggered lifecycle replay prevention.

Preview APIs do not mutate resources, change lifecycle state, emit execution events, dirty persistence, or consume transaction IDs. Execution revalidates target actor ID and runtime state before committing.

## Health-Zero Flow

`ActorLifecycleController` listens to `CharacterResourceCollection.ResourceChanged`. A committed Health event with `BecameEmpty`, not marked duplicate/restoration/migration, triggers defeat resolution. Preview damage, duplicate damage, failed damage, and already-zero Health do not trigger lifecycle transitions.

## Persistence

`PlayerActorLifecyclePersistenceParticipant` saves only lifecycle state and loads after `player.resources`. Restore validates player owner and actor/body ID, then applies lifecycle state without replaying lifecycle events. The post-load coherence check rejects invalid pairings such as `Active` with zero Health or `Dead` with positive Health.

## Test Lab

The existing Tab menu Test Lab includes a `Lifecycle 6.3` section with preview and execute controls for:

- zero-Health damage against player or enemy;
- explicit defeat;
- recovery;
- death;
- revival;
- duplicate transaction reuse.

These controls are development-only and do not add production input actions.

## Caller Audit

Migrated/updated production callers:

- `PlayerMeleeCombat` blocks attacks when an `ActorLifecycleController` says the player is not `Active`.
- `EnemyMeleeAttack` blocks attacks when the enemy is not `Active`.
- `PlayerSpellcaster` blocks casts when the caster is not `Active`.
- `DamageHealingService` remains the authoritative high-level Health damage/healing entry point and now naturally drives Health-zero lifecycle through `CharacterResourceCollection` events.

Remaining direct paths:

- `PlayerHealth` and `EnemyHealth` still implement legacy `IDamageable` for older prototype interactions and presentation. They remain because the project still has legacy `PlayerHealth`/`EnemyHealth` UI, enemy controller, and old projectile paths. They should be retired once all prototype combat targets are backed by current resources and `DamageHealingService`.
- `DamageEffectDefinition` still supports the legacy `IDamageable` fallback for compatibility when no current-resource target exists.
