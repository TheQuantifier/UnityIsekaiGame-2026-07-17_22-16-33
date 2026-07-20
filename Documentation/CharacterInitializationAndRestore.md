# Character Initialization And Restore

`CharacterSystemCoordinator.InitializeFromRegistry` is the prototype initialization entry point. It resolves subsystem components, configures definition-backed systems, subscribes to events, performs a full rebuild, and exposes `Ready` only after derived state is coherent.

Readiness states:

- `Uninitialized`: no registry/configuration yet.
- `DefinitionsReady`: registry has been accepted.
- `IdentityReady`: identity/runtime references have been bound.
- `Restoring`: persisted state is being restored.
- `Rebuilding`: derived effects, stats, capabilities, and resources are being reconciled.
- `Ready`: gameplay/UI may query snapshots.
- `Failed`: initialization failed and should not be treated as an empty valid character.
- `Disposed`: component unsubscribed during disable/unload.

New-character bootstrap order:

1. Create account/player/person/actor IDs.
2. Assign origin once.
3. Assign birth gifts once.
4. Initialize Base Attributes.
5. Apply origin and birth-gift permanent sources.
6. Grant starting roles, social statuses, titles, wallet, Skills, and Traits.
7. Apply linked Trait grants and source-safe effects.
8. Recalculate stats.
9. Initialize and reconcile resources.
10. Save the deterministic result.

Existing-character restore order:

1. Restore authoritative persisted state.
2. Avoid rerolling origin/gifts or replaying grants as gameplay events.
3. Rebuild Skill effects.
4. Rebuild Trait effects and capabilities.
5. Rebuild equipment/status contributions.
6. Recalculate Calculated Stats.
7. Restore and reconcile Current Resources without refilling.
8. Emit one coherent ready refresh.

Full rebuild order in Feature 5.6:

1. Skill effects.
2. Trait effects and capabilities.
3. Calculated Stats.
4. Resource maximum reconciliation.
5. Snapshot invalidation and one revision increment.

