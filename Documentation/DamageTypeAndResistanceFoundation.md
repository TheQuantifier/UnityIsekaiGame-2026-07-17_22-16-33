# Damage Type And Resistance Foundation

Feature 3.13 adds typed damage definitions and runtime resistance calculation without replacing the current health, stat, ability, melee, or projectile owners.

## Definitions

`DamageTypeDefinition` is immutable ScriptableObject configuration. It owns a stable `damage.*` ID, display metadata, primary category, tags, optional icon, optional parent damage type, damage family, whether general Defense applies, and minimum-damage policy.

Definitions never store current damage amount, current source, current target, runtime resistance, immunity state, active statuses, or mutable actor state.

Prototype definitions created: `damage.physical`, `damage.physical.slashing`, `damage.physical.blunt`, `damage.magic`, and `damage.magic.arcane`.

Prototype categories created: `category.damage`, `category.damage.physical`, and `category.damage.magical`.

Prototype tags created: `tag.physical` and `tag.arcane`.

## Hierarchy

Each damage type may have one parent. Parent chains are deterministic and validated for self-parenting and circular references.

Resistance lookup uses exact value first, even when the exact value is zero. If no exact value exists, it uses the nearest ancestor with a configured direct resistance. It does not sum every ancestor by default.

## Packets

Runtime damage now travels as a `DamagePacket` with one or more `DamageComponent` entries. Each component has one damage type and amount. Legacy callers that still create `DamageInfo(rawAmount, DamageType)` are adapted into a single legacy component.

`DamageEffectDefinition` can use `typedComponents` for multi-type effects, `typedDamageType` plus `baseAmount` for the common one-type case, or legacy `DamageType` fallback for old serialized content.

## Resistance Model

Resistance is normalized percentage data: `0.00` means no resistance, `0.25` means 25% reduction, `-0.25` means 25% weakness, and `1.00` means full immunity.

Supported values are `-1.00` through `1.00`. Runtime modifiers outside that range are rejected, and effective totals are clamped.

Formula: `typedDamageAfterResistance = typedDamageBeforeResistance * (1 - effectiveResistance)`.

## Calculation Order

The deterministic order is:

1. calculate configured base damage;
2. add source attack power where the effect or attack opts in;
3. create one or more typed components;
4. apply general Defense only when the damage type says Defense applies;
5. apply typed resistance or weakness;
6. apply minimum damage unless immunity reduced the component to zero;
7. apply the summed final damage to health once.

Physical, slashing, and blunt damage apply Defense. Arcane damage does not apply general Defense. Legacy untyped damage keeps the old Defense behavior for compatibility.

## Result Data

`DamageResult` keeps existing summary properties and adds typed detail: typed resistance mitigation, weakness amplification, and per-component `DamageComponentResult` values.

Per-component results include damage type, original amount, Defense mitigation, effective resistance, resistance mitigation or weakness amplification, final component damage, and immunity state.

## Runtime Ownership

`ActorStats` implements `IDamageResistanceReceiver` and owns one `RuntimeResistanceCollection` per actor. This keeps resistance state beside combat stats without moving it into health or status UI.

`RuntimeResistanceCollection` owns base resistance values and exact-source runtime modifiers. Removing one source only removes that source's contributions.

## Integrations

`ActorProfileDefinition` can define optional base resistances. These are copied into an actor's runtime collection during stat initialization and never mutate the profile.

`EquipmentData` can define optional resistance modifiers. `PlayerStats` registers them by equipment slot source identity while equipped and removes only that slot's resistance when equipment changes.

`StatusEffectDefinition` can define optional resistance modifiers. `StatusEffectController` registers them by runtime status application ID. Expiration and removal clean up only that status contribution.

`PrototypeSword` uses `damage.physical.slashing`. Arcane Bolt and Heavy Arcane Bolt damage effects use `damage.magic.arcane`. Enemy melee preserves baseline behavior through the legacy physical fallback when no scene damage type reference is assigned.

## Save Data

Do not serialize calculated effective resistance as authoritative state. Restore owners in this order:

1. resolve actor profile and base resistances;
2. restore equipment;
3. restore statuses;
4. restore standalone persistent resistance modifiers if a future owner exists;
5. recalculate effective resistances;
6. restore vitals and combat state.

## Validation

Definition validation checks damage type IDs, categories, parent references, self-parenting, circular hierarchy, classification references, profile resistance references, equipment resistance references, status resistance references, and damage effect typed references.

Damage packets skip malformed runtime components. Negative damage remains unsupported; healing is modeled elsewhere.

## Content Steps

To add a damage type, create a `DamageTypeDefinition`, use a stable `damage.*` ID, assign category/tags, pick an optional parent, set Defense policy, and register the asset in `PrototypeDefinitionCatalog`.

To add actor resistance, add base resistance to an `ActorProfileDefinition`, or add a resistance modifier to equipment or a status. Use normalized values from `-1` to `1` and let the owning system restore the modifier.

## Known Limits

No critical hits, armor penetration, resistance penetration, reactions, status buildup, material rules, body-part damage, shields, stagger, AI reactions, final balance, VFX, save/load implementation, or multiplayer replication are included.

Feature 3.14 should build on this by choosing the next runtime combat owner, likely weapon/spell authored damage packets or save/load restoration of equipment and status resistance sources.
