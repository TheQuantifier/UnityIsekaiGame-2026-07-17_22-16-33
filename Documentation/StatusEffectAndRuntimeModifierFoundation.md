# Status Effect And Runtime Modifier Foundation

Feature 3.8 adds the first runtime layer for actor status effects and stat modifiers.

## Definition Versus Runtime State

`StatusEffectDefinition` is static ScriptableObject data. It owns the stable status ID, display text, category, tags, disposition, duration model, stacking policy, refresh policy, visibility flag, ordered stat modifier definitions, and optional typed resistance modifier definitions. It never stores the current target, remaining duration, current stack count, source object, or subscriptions.

`RuntimeStatusEffect` is one active application on one actor. It owns the application ID, source identity, target reference, remaining duration, elapsed duration, stack count, and removed/expired state.

Feature 4.3 adds `StatusPersistencePolicy` on `StatusEffectDefinition`. Save-eligible non-instant statuses preserve definition ID, application ID, source ID, remaining duration, elapsed duration, and stack count. Status-owned stat and resistance modifiers are rebuilt from the restored status rather than serialized directly.

## Modifiers And Stats

`StatModifierDefinition` describes a modifier target and operation. `RuntimeStatModifier` is one active contribution with an exact `StatModifierSource`.

The first stat identifier model is the focused `StatType` enum: maximum health, maximum stamina, maximum mana, attack power, defense, and movement speed. `StatDefinitionUtility` maps these to stable IDs such as `stat.attack-power`, leaving a path to later stat-definition assets without forcing a risky migration now.

`RuntimeStatCollection` calculates values deterministically:

1. base value;
2. summed `FlatAdd`;
3. summed `PercentAdd`;
4. multiplied `Multiplicative`.

Final values are clamped to zero or above. Base values are never permanently changed by modifiers.

## Source Identity

Each runtime modifier has a source type and exact source ID. Status modifiers use the runtime status application ID. Equipment modifiers use slot source IDs such as `equipment.slot.MainHand`.

Removing one status removes only modifiers from that status application. Removing equipment from one slot removes only that slot's contributions. Feature 3.13 applies the same source-identity rule to typed resistance modifiers.

## Status Owner

`StatusEffectController` is the per-actor owner. It can be attached to players, enemies, or other actors. It applies statuses, enforces stacking and refresh policies, updates timed expiration, removes by application ID, definition ID, or source ID, raises runtime events, creates save-data entries, and clears temporary statuses for prototype reset.

It does not depend on player-only components. Stat-modifying statuses require a target with `IRuntimeStatReceiver`; `ActorStats` is the generic receiver used by enemies and future non-player actors.

## Duration And Stacking

Implemented duration models:

- `Instant`: applies immediately and leaves no active runtime status.
- `Timed`: remains active for a configured duration and expires automatically.
- `Persistent`: remains until explicitly removed.

Prepared enum values include permanent, until rest, until death, while in area, while equipped, and conditional, but those policies are not fully implemented yet.

Implemented stacking policies:

- `RejectDuplicate`: second application fails.
- `RefreshDuration`: keeps the existing application and resets duration to full by default.
- `ReplaceExisting`: removes the old application and creates a new one.
- `AddStack`: increases stack count up to `MaximumStacks`; stack-scaled modifiers are rebuilt.
- `IndependentInstances`: each application is separate.

## Equipment And Vitals

`ActorStats` owns the shared runtime stat collection. `PlayerStats` derives from it while preserving its public properties: `MaximumHealth`, `MaximumStamina`, `MaximumMana`, `AttackPower`, and `Defense`.

Equipment remains the authority for equipped items. On equipment changes, `PlayerStats` removes slot-scoped equipment modifiers and re-adds the current slot contributions. Status modifiers remain active during equipment swaps.

Health, mana, and stamina continue to listen to `PlayerStats.StatsChanged`. Increasing a maximum does not refill the resource, decreasing a maximum clamps current value through the existing vital components, and prototype reset clears temporary statuses before restoring vitals.

## Ability Integration

`ApplyStatusEffectDefinition` applies a referenced `StatusEffectDefinition` through the Feature 3.7 effect pipeline.

`StatModifierEffectDefinition` can now apply direct runtime stat modifiers to a target with `IRuntimeStatReceiver`. Status-owned modifiers are preferred for timed effects because ownership and removal are unambiguous.

Recommended flow:

`AbilityDefinition -> ApplyStatusEffectDefinition -> StatusEffectController -> RuntimeStatModifier`

For resistance:

`AbilityDefinition -> ApplyStatusEffectDefinition -> StatusEffectController -> RuntimeResistanceModifier`

## Prototype Content

Added prototype statuses:

- `status.prototype-might`: beneficial timed status, refreshes duration, adds attack power.
- `status.prototype-weakened`: harmful timed status, replaces existing application, lowers defense.

Added prototype abilities:

- `ability.prototype-might`
- `ability.prototype-weaken`

Added `PrototypeStatusEffectInteractable` for low-risk manual application through the existing interaction system.

## UI And Reset

`StatusEffectReadoutView` is a minimal Character-page readout inside the existing Tab menu. It subscribes to `StatusEffectController` events and displays visible active status names, stack count, remaining duration for timed statuses, and persistent status labels. It only reads runtime status state; `StatusEffectController` remains the owner for application, removal, expiration, and modifier updates.

Normal prototype reset clears temporary timed statuses on the player and enemy before restoring vitals. Persistent statuses are preserved unless explicitly cleared.

## Save Data And Restoration

`StatusEffectSaveData` stores stable definition ID, application ID, source ID, remaining duration, elapsed duration, stack count, duration model, and a tick-progress placeholder.

`StatusEffectRestoreUtility.Restore` validates save entries first, rejects duplicate application IDs, resolves definitions through `DefinitionRegistry`, then commits. If an application fails during commit, already-applied restored statuses are removed.

## Validation

Definition validation covers shared stable-ID rules plus status-specific rules: missing category, timed status without positive duration, invalid stack policy and maximum stack combinations, null or invalid modifier definitions, duplicate-looking modifiers as warnings, invalid periodic setup, missing status references from apply-status effects, and status references not registered in the catalog.

## Creating A New Status

1. Create a `StatusEffectDefinition`.
2. Assign a globally namespaced ID like `status.prototype-focus`.
3. Assign a status category and tags.
4. Choose duration, stacking, and refresh policies.
5. Add `StatModifierDefinition` entries if the status changes stats.
6. Register the asset in the active `DefinitionCatalog`.
7. Apply it through `ApplyStatusEffectDefinition` or `StatusEffectController`.

## Known Limitations

Periodic effects are represented in definitions but not executed yet. There is no resistance, cleanse, immunity, injury, disease, hunger, temperature, aura, AI reaction, full save/load orchestration, or multiplayer replication. `StatType` is intentionally small and can later become asset-driven if the stat taxonomy grows.
