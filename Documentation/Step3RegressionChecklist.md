# Step 3 Regression Checklist

Use this checklist before merging Step 3 data or taxonomy changes.

## Automated Checks

- `git status --short`
- `git diff --check`
- `dotnet build Assembly-CSharp.csproj`
- `dotnet build UnityIsekaiGame.EditModeTests.csproj`
- Unity batchmode import/compile
- Unity EditMode tests, with a generated test result XML before claiming pass/fail counts
- `Tools > Game Data > Validate Definitions`
- duplicate `.meta` GUID scan
- missing asset reference scan
- missing script scan

## Definition Catalog

- `PrototypeDefinitionCatalog` loads.
- Catalog validation has zero errors.
- Warnings are reviewed and are only accepted legacy-content warnings.
- Registry resolves representative IDs from every Step 3 family:
  - items
  - categories
  - tags
  - rarity
  - quality
  - condition
  - abilities
  - effects
  - statuses
  - beings
  - actor profiles
  - places
  - factions
  - contracts
  - quests
  - people
  - damage types
- No duplicate stable IDs exist in the catalog.

## Manual Prototype Regression

1. Open `Assets/Scenes/PrototypeScene.unity`.
2. Enter Play Mode.
3. Move, look, jump, sprint, and verify normal controls.
4. Pick up the potion, sword, helmet, and ore pickups.
5. Open the Tab menu.
6. Inventory page: hover items and confirm descriptions appear only while hovered.
7. Inventory page: click an item and confirm item type/info and instance ID display for instanced items.
8. Character page: confirm health, stamina, and mana remain in the HUD, while Attack, Defense, equipment, and Status Effects appear in the menu layout without overlap.
9. Equip the sword and helmet. Confirm stats update and the item instance identity is preserved when moving between inventory and equipment.
10. Use a health potion below maximum health and confirm it restores health without exceeding the current maximum.
11. Cast Arcane Bolt and Heavy Arcane Bolt at the prototype enemy. Confirm mana, cooldown, projectile, hit, and damage behavior still work.
12. Apply Prototype Might and Prototype Weakened where available. Confirm active statuses appear in the Character page, show stack count only above one, show remaining time for timed effects, and disappear immediately when expired or removed.
13. Fight the prototype enemy with melee. Confirm damage, enemy health, defeat, and loot behavior still work.
14. Walk into the visible investigation area. Confirm the on-screen test message appears and the reach-location quest objective completes.
15. Talk to the prototype NPC. Confirm dialogue and quest behavior still work.
16. Use the contract board. Confirm it opens as a menu, unlocks the cursor, blocks movement/look/combat, and lets contracts be clicked/selected.
17. Accept and progress a prototype contract. Confirm contract journal behavior and reward claiming still work.
18. Use the reset control. Confirm health, stamina, mana, enemy state, menus, input locks, statuses, and cooldowns reset as expected while inventory, equipment, quest state, and contract state remain where designed.

## Content Authoring Checks

- New definition IDs use namespace prefixes such as `item.`, `ability.`, `status.`, `place.`, `faction.`, `being.`, `actor-profile.`, and `damage.`.
- New authored definitions are added to the active catalog.
- New cross-definition references are typed references where supported, not only legacy strings.
- Runtime state is added to runtime owners or save-data DTOs, not to ScriptableObject definitions.
- New equipment/status/profile resistance modifiers use supported values from `-1` to `1`.
- New status modifiers and resistance modifiers have source ownership that can be removed independently.
- New Step 4 content does not backfill Step 3 definitions by mutating runtime state into static assets.
