# Prototype Scene and Menu Usability

Maintenance 4.4B is a prototype usability and presentation pass for `Assets/Scenes/PrototypeScene.unity`. It keeps the scene as a systems laboratory rather than a final town, dungeon, or art pass.

## Scene Zone Layout

The intended scene organization is:

- `Central Hub`: player spawn, open movement space, and sightlines to every test zone.
- `Inventory and Items`: health potion and iron ore pickup groups for stacking, full-inventory, collect, delivery, and item-use testing.
- `Equipment`: repeated sword and helmet pickups for stateful instance identity testing.
- `Combat`: prototype enemy, damage dummy, range space, and reset-friendly enemy positioning.
- `Magic and Statuses`: Prototype Might and Prototype Weakened interactables plus Test Lab pathways for resistance and weakness checks.
- `Dialogue and Quests`: Prototype NPC and a readable route toward the investigation area.
- `Contracts`: Prototype Contract Board and a distinct delivery target.
- `Investigation Area`: visible trigger boundary tied to the typed `PlaceDefinition`.
- `Persistence / Test Lab`: persistence and development shortcut context, with matching Test Lab teleport points.

## Scene Hierarchy

The maintenance setup organizes scene objects under these parents where possible:

- `Prototype Scene/Environment`
- `Prototype Scene/Player`
- `Prototype Scene/Items`
- `Prototype Scene/Actors`
- `Prototype Scene/Quest and Contract`
- `Prototype Scene/Test Interactables`
- `Prototype Scene/UI`
- `Prototype Scene/Test Infrastructure`

Generated zone floors, signs, pickups, status applicators, delivery target, damage dummy, and test points are kept under `4.4B Generated Usability Layout` or the relevant functional parent. Re-running the setup replaces generated objects by stable names.

## Pickup Groups

The prototype item area should include:

- several single Health Potion pickups;
- one Health Potion bundle;
- one larger potion crate for full-inventory pressure;
- small Prototype Iron Ore pickups;
- one larger Prototype Iron Ore stack;
- two Prototype Sword pickups;
- two Prototype Helmet pickups.

The repeated sword and helmet pickups use the same static `ItemDefinition` assets. Runtime instance identity still comes from the existing inventory item-instance path.

## Combat, Loot, and Status Areas

The combat zone keeps the existing Prototype Enemy as the primary active enemy and adds a simple damage dummy if scene setup is applied. The existing `EnemyLootDrop` and `PrototypeEnemyLootTable` remain the loot test path.

The status area keeps in-world interaction for Prototype Might and Prototype Weakened while the Test Lab provides faster direct status application/removal. Resistance and weakness testing remains Test Lab driven unless future actor profiles need dedicated physical targets.

## Quest and Contract Areas

The Prototype NPC remains tied to its existing person, dialogue, and quest references. The investigation trigger remains the typed place-based quest reporter and should be visually obvious but non-final.

The contract board remains a contract-board interaction, not a top-level Tab page. The delivery target is visually separated from the board and uses `prototype_delivery_crate`.

## Menu Layout Standards

The shared Tab menu remains the owner of cursor unlock and gameplay input blocking.

Navigation order is:

- Character
- Inventory
- Spells
- Journal
- Test Lab, only in editor/development builds

Navigation buttons use consistent preferred heights and centered labels. Content pages should use scrollable areas when long data is present and must not own modal state independently.

## ScrollRect Conventions

Prototype scroll areas should:

- use a masked viewport;
- keep content anchored for vertical growth;
- avoid layout cycles from conflicting `ContentSizeFitter` and layout groups;
- keep non-interactive labels from blocking raycasts;
- make mouse-wheel scrolling work where the Unity UI event system supports it.

## Target Resolutions

Manual layout checks should cover:

- 1280x720;
- 1920x1080;
- 2560x1440;
- 3440x1440 ultrawide.

The current prototype still uses Unity UI `Text` rather than a TextMeshPro migration. A future final-art/UI pass can migrate text rendering once a production UI direction exists.

## Test Lab Integration

The Test Lab now exposes scene `PrototypeTestPoint` objects as a selector and a `Teleport` action. Scene setup creates points for:

- `test-point.spawn`
- `test-point.items`
- `test-point.equipment`
- `test-point.combat`
- `test-point.magic-status`
- `test-point.npc-quest`
- `test-point.contract-board`
- `test-point.investigation-area`

## Adding New Test Objects

When adding new prototype test objects:

- put them in the matching scene zone;
- use typed definitions and public runtime APIs;
- keep labels descriptive and prototype-only;
- do not duplicate static definitions only to create runtime variety;
- keep colliders spaced so interaction prompts target one object at a time;
- add a Test Lab shortcut only when it exercises an existing runtime pathway.

## Known Limitations

- This is not a final art pass.
- The prototype UI still uses Unity UI `Text`.
- The Test Lab UI is generated and plain.
- The scene setup utility requires the Unity editor to have exclusive access to the project before it can write scene changes.
