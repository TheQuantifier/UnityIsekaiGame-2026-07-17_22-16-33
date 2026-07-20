# Character System Persistence

Feature 5.6 does not replace existing persistence participants with one oversized payload. Each subsystem keeps its own participant and schema.

Current Step 5 participant inventory:

| Participant | Authoritative state |
| --- | --- |
| `player.identity-progression` | account/player/person, origin, birth gift, roles, social statuses, titles, wallet, overall progression records |
| `player.attributes` | Base Attribute permanent sources and growth events |
| `player.skills` | learned Skills, grades, XP, mastery, hidden learning |
| `player.resources` | current resource records, transaction/recovery state |
| `player.traits` | Trait records, lifecycle, discovery, sources, linked grants |
| Inventory/equipment/status participants | actor/body and gameplay integration state that can affect the character |

Dependency order:

1. Definitions and registry.
2. Identity/progression.
3. Base Attributes.
4. Calculated Stats configuration.
5. Skills.
6. Traits.
7. Inventory/equipment/statuses.
8. Current Resources restore and reconciliation.
9. Coordinator full rebuild and Ready event.

Authoritative state is persisted. Derived caches are rebuilt. Development saves may be explicitly invalidated when Step 5 schemas change; permanent legacy aliases should be avoided unless migration has clear value.

Future multiplayer persistence is server-owned. A disconnect normally persists one player's person/actor state while the shared world continues for connected players. Clients may request actions and display snapshots, but they must not authoritatively grant progression, set resources, modify Traits, or submit trusted stat results.

