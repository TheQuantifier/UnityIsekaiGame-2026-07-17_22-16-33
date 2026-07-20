# Character Mutation Boundaries

The coordinator routes and observes state. It is not a replacement owner for every subsystem.

Owning mutation APIs:

- Identity, origin, birth gifts, roles, social statuses, titles, wallet: `PlayerIdentityProgression`.
- Base Attribute permanent and growth state: `CharacterAttributes`.
- Calculated Stat contributions: `CalculatedStatCollection`.
- Current Resources: `CharacterResourceCollection` transactions and reconciliation.
- Skills: `CharacterSkillCollection`.
- Traits and capability grants: `CharacterTraitCollection`.
- Statuses and temporary modifiers: `StatusEffectController`.
- Equipment effects: `PlayerEquipment` and `PlayerStats`.
- Inventory items: `PlayerInventory`.

Prohibited patterns:

- Direct mutation of serialized lists from gameplay scripts.
- Treating cached Calculated Stats as persisted authoritative data.
- Replaying restore data through ordinary grant/spend gameplay events.
- Granting abilities without a source owner.
- Recalculating stats manually in UI.
- Letting UI own or mutate status, resource, Skill, Trait, or identity state.

Development reset semantics:

- Scene reset may reset temporary scene state and position only.
- Resource refill changes current resources only.
- Progression reset is explicit and destructive.
- Character deletion must remove persisted Step 5 player state and must not leave the character `Ready`.

