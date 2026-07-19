# Player Stats, Vitals, And Status Persistence

Feature 4.3 adds player-scoped persistence for current vitals and save-eligible active statuses.

## Ownership

The participant is `Player` scoped and owned by the current local prototype player ID.

- Key: `player.stats-vitals-status`
- Scope: `Player`
- Owner ID: `local-player`
- Schema version: `1`
- Load phase: `Statuses`

The participant restores only personal player state. It does not restore quests, contracts, player position, scenes, world pickups, enemies, containers, or shared-world state.

In future multiplayer, the authoritative server should capture and restore this participant for one disconnecting/reconnecting player while the shared world continues for other connected players. Client save files must not become authoritative over shared-world or other-player state.

## Load Ordering

`player.stats-vitals-status` loads after `player.inventory-equipment`.

The intended order is:

1. inventory and equipment restore;
2. equipment modifiers rebuild through equipment events;
3. statuses restore;
4. status stat and resistance modifiers rebuild;
5. current Health, Mana, and Stamina restore;
6. current vitals clamp against the final calculated maximums;
7. HUD and Character-page listeners refresh from normal runtime events.

## Payload

`PlayerStatsVitalsStatusSaveData` contains:

```csharp
public int schemaVersion;
public string actorProfileId;
public int currentHealth;
public float currentMana;
public float currentStamina;
public List<StatusEffectSaveData> statuses;
```

The payload does not save calculated Attack, Defense, maximum Health/Mana/Stamina, equipment modifiers, status modifier lists, UI state, cooldown UI, active projectiles, input state, or transient regeneration timers.

## Actor Profile

The current prototype treats the player actor profile as scene configuration. The participant stores `actorProfileId` only as validation metadata when a profile exists.

Load resolves the ID through `DefinitionRegistry` and rejects a save whose actor profile conflicts with the current player configuration. It does not swap profiles at runtime.

## Vitals

The participant persists exact current Health, Mana, and Stamina.

Saved values must be finite and non-negative. Health must be above zero because defeated prototype saves are not supported.

Vitals restore after statuses rebuild final modifiers. Restore clamps each value to the final maximum and does not refill resources when a maximum increases.

Stamina and mana restore reset transient regeneration delay state. Stamina also clears stale sprint-frame state and recomputes exhaustion from the restored current value.

## Defeated State

Manual save while defeated is rejected.

This avoids loading into a state where input remains permanently blocked or reset is required immediately after load. Future checkpoint or death systems may adopt a different recovery policy.

## Status Policy

`StatusEffectDefinition` now has `StatusPersistencePolicy`:

- `SaveRemainingDuration`: save active non-instant statuses with their remaining duration.
- `DoNotSave`: exclude the status from save capture.
- `PersistentUntilRemoved`: save active non-instant statuses and restore them as persistent state.
- `ExpireOnLoad`: exclude for this local prototype.

Default authored status behavior is `SaveRemainingDuration`, so existing timed prototype statuses can participate without asset rewrites.

Instant statuses are not saved. Expired or removed statuses are not saved.

Feature 4.3 does not apply offline elapsed time. Timed statuses restore with exactly the saved remaining duration. Future server-time persistence can choose pause-while-offline, continue-by-timestamp, expire-on-logout, or persistent-only policies explicitly.

## Status Identity

Status save entries preserve definition ID, application ID, source ID, duration model, remaining duration, elapsed duration, stack count, and persistence policy.

Application IDs are preserved and duplicate IDs are rejected during prepare. Missing source objects are represented by the restored target object as a local fallback source; source IDs remain stable strings and do not rely on GameObject names.

Status-owned stat and resistance modifiers are not serialized as raw modifier lists. They rebuild from the restored status definition, application ID, source ID, and stack count.

## Two-Phase Restore

Preparation validates participant and payload schema version, runtime references, actor profile metadata, finite current vital values, non-defeated health, status definition IDs, status persistence policy, duration model, remaining duration, stack counts, duplicate application IDs, and temporary status restoration.

No live player state changes during prepare.

Commit snapshots current statuses and vitals for defensive rollback, clears current statuses, restores prepared statuses, lets status restore rebuild modifiers, restores Health/Mana/Stamina, clamps vitals against final maximums, and reports success or attempts rollback on an unexpected commit failure.

Repeated loads clear existing restored statuses before applying the saved set, so status entries and modifiers do not duplicate.

Feature 4.4 quest/contract persistence loads after this participant so Journal state is restored after player inventory, equipment, vitals, and statuses are coherent.

## Standalone Modifiers

Feature 4.3 does not save raw runtime stat or resistance modifiers.

Current player modifiers have authoritative owners: actor profile/base stats, equipment, and statuses. Future standalone permanent effects should receive their own explicit owner and persistence policy before being added to save data.

## Known Limitations

- No quest or contract persistence.
- No player position, scene, or place persistence.
- No world pickup, enemy, chest, door, or shared-world persistence.
- No autosave or final save/load UI.
- No offline timestamp progression.
- No networking, authentication, cloud saves, or server database integration.
