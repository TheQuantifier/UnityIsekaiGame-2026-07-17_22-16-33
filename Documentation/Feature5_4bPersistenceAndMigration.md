# Feature 5.4b Persistence and Migration

Feature 5.4b adds optional player participant `player.resources`.

- Scope: `Player`
- Owner: local prototype player ID
- Schema: `1`
- Load phase: `Vitals`
- Required: `false`

The participant saves `PlayerResourcesSaveData`, including resource definition IDs, current values, last known maximums, lifetime totals, initialization data, and processed event IDs.

`player.stats-vitals-status` remains required at schema version 1 for compatibility and continues to own statuses plus legacy vitals fields. New saves include both participants. During load, the legacy participant can restore old current Health/Mana/Stamina values, then `player.resources` restores the new resource records when present.

Development saves without `player.resources` are still accepted through the legacy vitals path. For final Feature 5.4b testing, recreate local development saves after validating definitions so new saves contain `player.resources`.

Calculated Stats are still rebuilt and are not saved as authoritative values. Resource maximums are derived from rebuilt Calculated Stats during resource restore and reconciliation.

Future multiplayer persistence remains server-owned. Clients may request resource-affecting actions, but clients should not become authoritative over shared-world or player resource state. On disconnect, the future server should persist that player's resource records while the shared world continues for remaining players.
