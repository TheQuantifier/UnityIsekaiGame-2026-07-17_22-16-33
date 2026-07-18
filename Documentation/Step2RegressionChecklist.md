# Step 2 Regression Checklist

Use this checklist after changing core systems. Start from `Assets/Scenes/PrototypeScene.unity` with a clean Console unless a test says otherwise.

## Movement And Input

- Enter Play Mode.
- Move with WASD/left stick and confirm the player moves relative to camera direction.
- Move the mouse/right stick and confirm camera look works.
- Jump and confirm gravity pulls the player back to the ground.
- Hold sprint while moving and confirm stamina drains.
- Hold sprint while standing still and confirm stamina does not drain.
- Drain stamina to zero and confirm sprint stops until stamina recovers above the restart threshold.
- Open the Tab menu and confirm movement, look, attack, spellcast, and interaction inputs are blocked.
- Close the menu and confirm movement/look/control are restored.

## Interaction And Pickups

- Look at each pickup and confirm the interaction prompt appears.
- Pick up Health Potions and confirm inventory quantity increases.
- Pick up sword and helmet and confirm each appears in inventory.
- Fill or nearly fill inventory, then try a pickup that cannot fully fit; confirm partial/full-inventory messaging and no item loss.
- Confirm interacting while the menu or dialogue is open does nothing.

## Inventory, Item Use, And Equipment

- Open Tab and view all inventory slots.
- Select an empty slot and press Use; confirm no item is consumed.
- Damage the player, select Health Potion, and Use it; confirm health increases and exactly one potion is removed.
- Use Health Potion at full health; confirm use fails and quantity does not change.
- Equip the sword and confirm it moves from inventory to Main Hand.
- Equip the helmet and confirm it moves to the correct equipment slot.
- Swap/unequip equipment and confirm no duplicate items are created and inventory capacity is respected.
- Confirm Health/Stamina/Mana HUD values update when equipment changes maximum vitals.

## Combat And Enemy

- Attack the enemy with melee and confirm enemy health decreases.
- Let the enemy detect and pursue the player.
- Confirm the enemy stops close enough to attack and damages the player on cooldown.
- Defeat the enemy and confirm defeat HUD/status appears.
- Confirm the enemy does not fire duplicate defeat credit from one defeat.

## Loot

- Defeat the enemy and confirm loot drops spawn as world pickups.
- Pick up dropped loot and confirm inventory updates.
- Reset and defeat the enemy again; confirm loot behavior matches the current prototype reset contract.
- Confirm picked-up loot does not duplicate inventory contents unexpectedly.

## Magic And Spell Loadout

- Confirm spell quick slots are visible.
- Cast Arcane Bolt and confirm mana is spent, cooldown starts, and the projectile travels forward.
- Hit the enemy with a spell and confirm enemy health decreases.
- Attempt to cast during cooldown and confirm it fails cleanly.
- Assign or change a spell through the Spells menu page.
- Confirm quick-slot selection changes the active spell.
- Confirm spellcasting is blocked while menu/dialogue is open.

## Dialogue

- Look at `Prototype Dialogue NPC`; confirm the prompt uses the NPC person display name.
- Start dialogue and confirm movement/look/interactions are blocked.
- Advance text with Enter/Space.
- Select a choice with mouse or gamepad/keyboard UI navigation.
- Reach the final dialogue screen and confirm it stays open until closed.
- Close dialogue and confirm gameplay input returns.

## Contracts

- Accept available prototype contracts if the scene provides the current contract source.
- Progress collect, delivery, and defeat objectives.
- Confirm contract state updates in the Journal.
- Deliver required items and confirm inventory decreases by the required amount.
- Complete a contract and claim reward.
- Try claiming again and confirm duplicate rewards are blocked.
- Abandon an active contract and confirm objectives stop progressing.

## Quests

- Talk to `Prototype Dialogue NPC` and confirm `A Strange Disturbance` starts.
- Open Tab, go to Journal, and confirm the quest appears separately from contracts.
- Confirm giver text displays `Prototype NPC, Quest Giver`.
- Walk through the invisible disturbance trigger area and confirm the reach-location stage advances.
- Defeat the prototype enemy and confirm the defeat stage advances.
- Return to the NPC and confirm the quest completes.
- Claim quest reward and confirm Health Potions are granted once.
- Try claiming again and confirm duplicate rewards are blocked.
- Abandon an active quest in a fresh run and confirm objectives stop progressing.

## Person Identity

- Select `Prototype Dialogue NPC` and confirm it has `PersonIdentity` assigned to `PrototypeNpcPerson`.
- During Play Mode, disable and re-enable the NPC and confirm no stale registry entry remains.
- Duplicate the NPC temporarily during Play Mode and confirm a duplicate active person ID warning appears.
- Delete the duplicate and confirm the original NPC still starts dialogue and quest reporting.

## Reset And Defeat

- Let the player be defeated; confirm gameplay control freezes and the defeated reset message appears.
- Press R and confirm health, stamina, mana, control, and player position reset.
- Confirm the enemy returns to its start position with full health and can attack/be defeated again.
- Confirm inventory, equipment, active/completed quests, active/completed contracts, and reward claim state are preserved.
- Repeat reset several times with menu/dialogue open and confirm no duplicate messages, duplicated loot, or broken input state.

## Final Sanity

- Open and close Inventory, Character, Spells, and Journal pages repeatedly.
- Confirm only one Tab-menu page is visible at a time.
- Confirm cursor lock/visibility is restored after closing UI.
- Stop Play Mode and confirm no project assets are modified.
- Check Console for compile errors, missing references, null references, duplicate identity warnings from the normal scene, or repeated event spam.
