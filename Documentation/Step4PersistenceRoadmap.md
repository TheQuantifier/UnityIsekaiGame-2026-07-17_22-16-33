# Step 4 Persistence Roadmap

Step 4 builds persistence incrementally on top of the Step 3 data and taxonomy foundation.

## Sequence

1. Feature 4.1: Save-File and Persistence Service Foundation
2. Feature 4.2: Player Inventory and Equipment Persistence
3. Feature 4.3: Player Stats, Vitals, and Status Persistence
4. Feature 4.4: Quest and Contract Persistence
5. Feature 4.5: Player Position, Scene, and Place Persistence
6. Feature 4.6: Persistent World-Entity Identity
7. Feature 4.7: Save Slots, Autosave, and Load UI
8. Feature 4.8: Persistence Integration and Recovery
9. Feature 4.9: Step 4 Closeout

## Direction

Each feature should add one runtime owner or closely related group of owners to the persistence service.

Preserve these constraints:

- static definitions resolve through stable IDs;
- ScriptableObject assets are never mutable save state;
- save DTOs are plain serializable data;
- participants validate before mutation;
- calculated values rebuild from authoritative state;
- equipment and status modifiers are saved only through their owning systems;
- direct scene object references are not serialized;
- save/load results remain structured.
- player-scoped state and shared-world state remain separable.
- clients are never authoritative over shared-world persistence.

## Multiplayer Ownership Direction

Feature 4.1 is still local and single-player, but every participant now declares a persistence scope and may carry an owner ID for player-scoped data.

Future multiplayer persistence should be server-owned and normally automatic. When one player disconnects, the server should persist that player's `Player`-scoped participants while the `SharedWorld` and active `RegionOrScene` state continue for other connected players. Reconnecting should restore the player into the current world, not roll the world back to the player's disconnect point.

Shared-world state should be saved by server checkpoints, region unloads, authoritative simulation events, or controlled shutdown. Client save/load UI should not upload or restore shared-world state directly.

Future offline-world progression can be modeled through server simulation, scheduled catch-up, or explicit region progression rules. It should not be hidden inside local client save files.

## Feature 4.2 Recommendation

Feature 4.2 should add inventory and equipment participants first. Those participants should be `Player` scoped with an owner ID. Existing inventory and equipment save DTOs already support validate-before-replace restore through `DefinitionRegistry`, making them the best next integration target.
