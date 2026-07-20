# Character System Overview

Feature 5.6 closes Step 5 by adding one runtime boundary for the character systems built in Features 5.1 through 5.5.

The prototype player now has a `CharacterSystemCoordinator`. It does not duplicate subsystem state. It resolves and coordinates:

- `PlayerIdentityProgression` for account, player, person identity, origin, birth gifts, roles, social statuses, titles, wallet, and overall level;
- `CharacterAttributes` for permanent Base Attribute state;
- `CalculatedStatCollection` for derived numerical stats and source-safe contributions;
- `CharacterResourceCollection` for current Health, Mana, Stamina, and future resources;
- `CharacterSkillCollection` for Skills, hidden learning, XP, grades, mastery, effects, and ability/action unlocks;
- `CharacterTraitCollection` for Traits, lifecycle, discovery, linked grants, capabilities, resistances, immunities, and requirement-facing state;
- `StatusEffectController`, `PlayerEquipment`, and `PlayerInventory` as actor/runtime integration points.

The approved identity hierarchy remains:

`Account -> Player -> Persistent Person -> Current Actor / Body`

The system remains local single-player for the prototype, but the contracts are written so future NPCs, companions, enemies, summons, and multiplayer-controlled characters can use the general character subsystems without requiring a local player menu or account save slot.

The three-layer numerical model is:

1. Base Attributes: permanent character foundation and growth.
2. Calculated Stats: derived values and source-owned contributions.
3. Current Resources: current/max resource state and transactions.

Step 5 is player-focused in presentation, but the runtime core is NPC-ready where practical. Player-only systems are the HUD, Character menu, Test Lab controls, local save-slot ownership, and current origin/birth-gift presentation flow.

