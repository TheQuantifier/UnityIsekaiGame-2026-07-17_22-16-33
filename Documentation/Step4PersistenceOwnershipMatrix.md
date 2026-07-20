# Step 4 Persistence Ownership Matrix

This matrix separates static data, player runtime state, account state, shared-world state, region/scene state, and session-only state. The local prototype uses one local player and one local world, but the architecture does not treat one player's save as ownership of the entire world.

| State | Classification | Persisted Now | Future Authority | Notes |
| --- | --- | --- | --- | --- |
| Item definitions | Static definition | Yes by ID reference | Server/client content validation | ScriptableObjects are not mutable save state. |
| Inventory contents | Player runtime | Yes | Server for multiplayer | `player.inventory-equipment`. |
| Equipment | Player runtime | Yes | Server for multiplayer | Restores exact stateful item instances and modifiers rebuild. |
| Player vitals | Player runtime | Yes | Server for multiplayer | Health, mana, stamina restore after modifiers. |
| Player statuses | Player runtime | Yes | Server for multiplayer | Save-eligible statuses only. |
| Player quests | Player runtime | Yes | Server for multiplayer | Personal quest log. |
| Accepted personal contracts | Player runtime | Yes | Server for multiplayer | Shared boards are not persisted. |
| Player position | Player runtime | Yes, same-scene | Server for multiplayer | Cross-scene restore deferred. |
| Player current place | Player runtime | Yes | Server for multiplayer | Place ID plus tracker refresh. |
| Player identity/progression | Player runtime | Yes | Server for multiplayer | `player.identity-progression`; account/player/person/world-entity IDs stay distinct and shared-world state is not restored by this participant. |
| Player Skills and hidden learning progress | Player runtime | Yes | Server for multiplayer | `player.skills`; clients may display Skill state but future multiplayer servers must validate action execution, XP, promotion, and unlocks. |
| Save-slot metadata | Session/local storage metadata | Yes | Server/account service later | Local UI descriptor, not gameplay state. |
| Play time | Player/account diagnostic | Yes | Server/account service later | Prototype local play time only. |
| Autosave state | Local player save storage | Yes | Server/account service later | Three local generations plus staging slot. |
| Authored world-entity identity | Region/scene identity | Yes as scene identity, not mutable state | Server/region authority | Scene IDs assigned and registered. |
| Runtime-spawned world-entity identity | Region/scene identity | Proof only | Server/region authority | Test Lab recreation proof only. |
| Pickups | Region/scene state | Deferred | Server/region authority | Collected/uncollected state is not persisted. |
| Enemies | Region/scene state | Deferred | Server/region authority | Health, death, position, AI state are not persisted. |
| NPC runtime state | Region/scene/shared-world | Deferred | Server/region authority | Schedules and conversation state deferred. |
| Doors | Region/scene state | Deferred | Server/region authority | Open/locked state deferred. |
| Containers | Region/scene state | Deferred | Server/region authority | Contents and looting state deferred. |
| Faction state | Shared-world/account depending on design | Deferred | Server authority | Definitions exist; runtime reputation not persisted. |
| Settlement economy | Shared-world | Deferred | Server authority | Not implemented. |
| Shared guild boards | Shared-world | Deferred | Server authority | Accepted personal contracts persist, board inventory does not. |
| World events | Shared-world | Deferred | Server authority | Not implemented. |
| Input/menu transient state | Session-only | No | Client session | Menus close during load; not saved. |
| Combat cooldown/projectiles | Session-only | No | Server in multiplayer | Not saved in Step 4. |
| Transaction diagnostics | Local operation metadata | Yes in envelope/result | Server later | Not gameplay authority. |

## Current Boundary

Current local saves restore the local player's state and same-scene location. They do not roll back shared-world state, other players, region simulation, world pickups, enemy state, NPCs, settlement economy, faction state, or guild boards.

## Future Server Model

In multiplayer, the server should persist one disconnecting player's `Player`-scoped participants while connected players continue in the current shared world. Shared-world and region/scene participants should persist on server checkpoint, region unload, simulation event, or controlled shutdown. Clients can request actions, but cannot become authoritative over shared-world persistence by uploading local saves.
