# Place Definition And Location Hierarchy

Feature 3.11 adds static place identity and hierarchy foundations without turning Unity scenes or triggers into authoritative location data.

## Responsibilities

`PlaceDefinition` is the static identity and metadata for a place. It owns stable ID, display name, description, category, tags, icon, optional parent place, `PlaceKind`, optional scene key, provisional map label/position metadata, optional default governing faction, controlling-faction placeholder, danger metadata, and discovery metadata.

`PlaceIdentity` is an optional runtime scene presence for a loaded place. It references a `PlaceDefinition` and exposes place ID/display metadata and optional bounds. It does not register globally and does not own permanent place state.

`CurrentPlaceTracker` is the runtime current-place stack used by player-location persistence. It tracks active triggers, chooses the deepest place in the hierarchy, and supports restore-time refresh without turning place definitions into mutable state.

Unity scenes remain technical content containers. A scene can contain multiple places, and a place can later be represented across more than one scene. The scene key on `PlaceDefinition` is optional metadata, not the stable identity.

Location triggers report a `PlaceDefinition` stable ID or legacy location string. They do not define places, discovery, quest state, or permanent world state.

## Place Kind And Categories

`PlaceKind` was added as compact structural metadata:

- `World`
- `Nation`
- `Region`
- `Settlement`
- `District`
- `Building`
- `Interior`
- `Dungeon`
- `Wilderness`
- `Route`
- `PointOfInterest`

Category definitions remain the flexible taxonomy and ancestry model. `PlaceKind` is used for validation and simple hierarchy queries, while category membership supports broader filtering. If they differ, validation warns rather than blocking unusual fantasy content.

Prototype place categories use the `category.place.*` namespace to avoid collisions with globally unique `place.*` definition IDs:

- `category.place`
- `category.place.world`
- `category.place.nation`
- `category.place.region`
- `category.place.settlement`
- `category.place.wilderness`
- `category.place.point-of-interest`
- `category.place.building`

Prototype tags added:

- `tag.settlement`
- `tag.safe-zone`
- `tag.dangerous`
- `tag.guild-related`
- `tag.outdoor`

Existing `tag.prototype` is reused.

## Hierarchy Rules

Each `PlaceDefinition` can have one optional parent `PlaceDefinition`. The first implementation models primary physical or organizational containment:

- nation belongs to world;
- region belongs to nation;
- settlement belongs to region;
- wilderness belongs to region;
- point of interest belongs to wilderness;
- building placeholder belongs to settlement.

Validation rejects self-parenting, missing catalog parents, and circular parent chains. Some unusual relationships, such as a nation inside a building, are warnings instead of errors so later fantasy-specific edge cases remain possible.

Administrative, political, legal, and economic relationships are not modeled through parent hierarchy. `DefaultGoverningFaction` can point at a static `FactionDefinition` as authored metadata, but current political control, taxation, routes, supply links, jurisdiction changes, or market affiliation belong in future runtime systems.

## Hierarchy Helpers

`PlaceHierarchyUtility` provides:

- stable-ID comparison;
- descendant checks;
- contains-or-is checks;
- ancestor traversal;
- nearest ancestor by `PlaceKind`;
- nearest ancestor by category;
- containing settlement, region, and nation helpers;
- cycle detection;
- readable hierarchy paths.

These helpers operate on already-resolved definitions and do not scan `AssetDatabase` at runtime.

## Runtime Decisions

`PlaceIdentity` was added, but no `PlaceRegistry` was introduced. The current feature has no concrete runtime need for loaded-place lookup, and static lookup stays in `DefinitionCatalog`.

Feature 4.5 introduces `CurrentPlaceTracker` for the player. It is still lightweight and scene-local; it does not own discovery, control, schedules, or permanent world state.

## Reach Location Integration

`ReachLocationObjectiveDefinition` now has an optional typed `targetPlace`. `TargetLocationId` resolves to `targetPlace.Id` when assigned, otherwise it falls back to the legacy `locationId` string.

`QuestReachLocationReporter` now has an optional typed `targetPlace`. It reports the typed place ID when assigned and falls back to the legacy `locationId` string. This preserves current serialized content while making new content use `PlaceDefinition`.

The existing `A Strange Disturbance` reach objective and scene trigger now reference `place.poi.disturbance-site`. The old `prototype_disturbance_site` string remains as fallback compatibility data.

Quest progress still occurs only while the objective instance is active, because the runtime instance subscribes to `QuestObjectiveSignalBus.ReachedLocation` only during activation.

## Contract And Person Integration

Contracts were not broadly refactored. Future contract origin/destination/requester/target-area fields should use `PlaceDefinition` references or stable place IDs while preserving existing delivery destination behavior.

`PersonDefinition` now has optional `homePlace`. This is static identity metadata only. Current runtime location, schedules, movement, homes, workplaces, and residence simulation remain future work. `settlementIdPlaceholder` remains for compatibility.

Being and actor profile definitions do not own location state. A being may later have habitat metadata, but `ActorProfileDefinition` remains base actor configuration, not where an actor lives.

## Prototype Places

The prototype catalog includes:

- `place.world.prototype`
- `place.nation.prototype`
- `place.region.prototype`
- `place.settlement.prototype-town`
- `place.wilderness.prototype-outskirts`
- `place.poi.disturbance-site`
- `place.building.prototype-guild-board-area`

`PrototypeScene` is referenced only as optional scene metadata on places that currently appear there.

## Save-Data Preparation

`PlaceReferenceSaveData` stores a stable `placeId`.

`ActorLocationSaveData` stores:

- `currentPlaceId`
- `sceneKey`
- `position`
- `rotation`

These DTOs do not serialize `PlaceDefinition` assets directly and do not write files.

Future restoration order is represented by `PlaceRestoreOrder`:

1. Resolve `PlaceDefinition`.
2. Load or confirm the required scene.
3. Locate a spawn or saved position.
4. Restore actor position.
5. Restore current-place tracking.
6. Restore place-related runtime state.
7. Notify quests, schedules, and UI after restoration completes.

## Validation

Definition validation covers:

- stable ID and duplicate ID checks through the shared catalog validator;
- category and tag references through shared classification validation;
- missing parent references;
- self-parenting;
- circular hierarchy;
- place kind/category mismatch warnings;
- suspicious hierarchy warnings;
- malformed scene keys;
- non-finite map metadata;
- person home-place references missing from catalog.

## Creating Places

To create a new place:

1. Create `Unity Isekai Game/Places/Place`.
2. Assign a globally unique `place.*` ID.
3. Assign `PlaceKind`.
4. Assign a `category.place.*` category.
5. Add only useful tags.
6. Set parent place when it has a primary container.
7. Register it in the active `DefinitionCatalog`.

To create a hierarchy:

1. Create the highest container first.
2. Assign each child one parent.
3. Validate definitions.
4. Fix missing parent references or cycles before using the hierarchy in quests.

## Known Limitations

This feature does not implement map UI, fast travel, scene loading, additive streaming, procedural generation, final nations or lore, political control, place ownership, territory, economy, taxes, weather, schedules, homes/workplaces, travel routes, discovery UI, fog of war, housing, full save/load, or multiplayer replication.
