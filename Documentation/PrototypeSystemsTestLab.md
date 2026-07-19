# Prototype Systems Test Lab

Maintenance 4.4A adds a centralized development-only test surface for the current prototype systems. The Test Lab is not final gameplay UI. It is a replaceable QA and debugging page inside the existing Tab menu so prototype systems can be exercised without creating one-off scene buttons, modal windows, or temporary Text objects.

## Development Boundary

The runtime Test Lab scripts live under `Assets/Scripts/Development` and are wrapped in `UNITY_EDITOR || DEVELOPMENT_BUILD`.

The Tab menu integration is also guarded by the same compiler symbols. Release builds should not expose the Test Lab page, menu button, service, or helper APIs.

The service uses public runtime APIs where practical. A few runtime owners expose development-only helpers for scoped cleanup:

- `PlayerInventory.DevelopmentClearInventory`
- `PlayerInventory.DevelopmentOccupiedSlotCount`
- `PlayerEquipment.DevelopmentClearEquipment`
- `PlayerQuestLog.DevelopmentClearQuestLog`
- `PlayerContractJournal.DevelopmentClearContractJournal`
- `ContractObjectiveTarget.DevelopmentSetTargetCategory`

These helpers are also editor/development-build only.

## Menu Integration

`InventoryScreenController` creates and configures a `PrototypeTestLabService` when the Tab menu initializes in editor/development builds. `InventoryScreenView` creates a Test Lab navigation button and content root if serialized scene references are not already present.

The page uses the same modal behavior as the rest of the Tab menu:

- no new input action;
- no standalone modal;
- existing right-side navigation remains the menu shell;
- cursor unlock and gameplay input blocking remain owned by `InventoryScreenController`;
- existing Inventory, Character, Spells, and Journal pages keep their behavior.

## Service Architecture

`PrototypeTestLabService` receives a `PrototypeTestLabContext` containing live scene references:

- definition catalog and registry;
- player inventory and equipment;
- player stats, health, mana, stamina, and statuses;
- spell loadout/caster references;
- quest log and contract journal;
- prototype reset controller;
- prototype persistence service;
- player transform;
- enemy health, controller, melee attack, statuses, and transform.

The service records recent operations in a bounded history list, returns `PrototypeTestLabOperation` values for every action, and reports destructive-action confirmation requirements through the same history.

## Controls

The generated Test Lab page includes:

- selector displays for items, statuses, damage types, quests, contracts, places, and people;
- selector display for scene test points;
- cycle buttons for selectors so choices remain clickable in the generated Unity UI;
- quantity and amount inputs;
- inventory grant, stateful grant, remove, fill, and clear actions;
- equipment equip and unequip-all actions;
- player damage, heal, set-health, mana drain, stamina drain, and restore-vitals actions;
- player/enemy status apply and remove actions;
- typed damage against player or enemy;
- enemy defeat and reset actions;
- quest start and talk/reach/defeat signal actions;
- contract accept and clear actions;
- save, load, validate, and delete slot actions;
- teleport to scene test points;
- scenario buttons for clean baseline, combat setup, full inventory, quest midpoint, contract testing, and persistence round trip;
- diagnostics and operation history readouts.

Destructive actions require pressing the same action twice: clear inventory, unequip all, clear quest log, clear contract journal, and delete save.

## Persistence Integration

The Test Lab reuses `PrototypePersistenceServiceBehaviour`. If one is not present during Play Mode, the controller may create a prototype persistence service object and configure it with the same player systems used by the menu. This preserves the Feature 4.1-4.4 local save/load proof while keeping the lab itself non-authoritative over future shared-world state.

Persistence controls are still local prototype controls. They do not imply player authority over shared-world state in future multiplayer architecture.

## Diagnostics

Diagnostics currently report:

- definition registry count;
- missing core references;
- duplicate item instance IDs across inventory and equipment;
- duplicate runtime status application IDs on player and enemy.

The diagnostics are intentionally replaceable. They are meant to catch common prototype setup mistakes, not to replace definition validation, automated tests, or future PlayMode system test suites.

## Manual Testing

Use the Test Lab only in the existing Tab menu:

1. Open `Assets/Scenes/PrototypeScene.unity`.
2. Enter Play Mode.
3. Press Tab.
4. Confirm the existing Inventory, Character, Spells, and Journal pages still open and behave normally.
5. Click `Test Lab`.
6. Confirm the cursor remains visible, movement/look/combat are blocked, and the right-side menu shell remains unchanged.
7. Click `Diagnostics` and confirm the diagnostics text updates.
8. Use `Next Item` to select a useful prototype item. Click `Grant Item` and confirm it appears in Inventory.
9. Click `Grant Stateful` and confirm a stateful item can be inspected with a unique instance ID.
10. Click `Fill Inventory`, then press `Clear Inventory` once and confirm it only asks for confirmation. Press `Clear Inventory` again and confirm inventory clears.
11. Click `Restore Vitals`, then damage/heal/set health and confirm the HUD/menu vitals update without exceeding maximum health.
12. Use `Next Status`, then apply a status to the player. Confirm the Character page status readout updates. Return to Test Lab and remove or clear temporary statuses.
13. Use `Next Damage`, then damage the enemy and reset the enemy. Confirm enemy health/reset behavior still works in the scene.
14. Use `Next Quest`, start a quest, and use Talk/Reach/Defeat report buttons to verify quest progress changes in Journal.
15. Use `Next Contract`, accept a contract, and verify it appears in Journal.
16. Use the persistence scenario, click `Save`, change player state, click `Load`, and confirm saved player state restores.
17. Click `Delete Save` once and confirm it requires confirmation. Click it again only if you want to remove the prototype slot.
18. Close the Tab menu and confirm movement, look, interaction, combat, and normal prototype controls resume.

## Known Limitations

- The UI is generated at runtime and is intentionally plain.
- Selectors are cycled with buttons rather than using authored dropdown prefabs.
- Scenario buttons prepare useful states but do not bypass normal quest/contract objective rules.
- The lab is a prototype accelerator, not a formal end-to-end PlayMode test harness.
