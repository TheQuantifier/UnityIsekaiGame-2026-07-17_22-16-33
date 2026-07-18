# Object And Item Taxonomy

This document describes the Step 3.3 taxonomy foundation for physical and inventory-representable objects.

## Object Versus Item

An object definition describes a type of thing that can exist in the world or be referenced by world systems.

An item definition describes an object that can participate in inventory-related systems.

Runtime state remains separate:

- Static definition: `item.health-potion`
- Runtime inventory state: `item.health-potion x 4`
- Static definition: `item.prototype-sword`
- Runtime equipment state: one equipped sword reference

Step 3.3 does not add a generalized runtime item-instance system. Current inventory and equipment state still reference `ItemDefinition` assets directly. Future per-instance data such as condition, quality, enchantments, ownership, or unique identity should be added as runtime state around stable definition IDs.

## Selected Architecture

The implementation uses a mixed, capability-driven model:

- `CategoryDefinition` assets describe taxonomy.
- `TagDefinition` assets describe flexible traits.
- `ItemDefinition` remains the concrete inventory item definition.
- Small interfaces expose read-only object and item capabilities.
- Existing item-use and equipment data remain the behavioral authority.

There is no large `WorldObjectDefinition` base class and no item-type enum.

## Object And Item Interfaces

`IObjectDefinition` represents common object metadata:

```csharp
public interface IObjectDefinition : IGameDefinition, ICategorizableDefinition, ITaggedDefinition
{
    string Description { get; }
    Sprite Icon { get; }
}
```

`IInventoryItemDefinition` represents stackable inventory objects:

```csharp
public interface IInventoryItemDefinition : IObjectDefinition
{
    bool Stackable { get; }
    int MaximumStackSize { get; }
}
```

`IUsableItemDefinition` and `IEquippableItemDefinition` expose current behavior flags without depending on category checks:

```csharp
public interface IUsableItemDefinition
{
    bool IsUsable { get; }
    int UseEffectCount { get; }
    bool HasMissingUseEffect { get; }
}

public interface IEquippableItemDefinition
{
    bool IsEquippable { get; }
}
```

`ItemDefinition` implements these interfaces. Runtime systems can keep using the existing `ItemDefinition` API.

## Item Category Hierarchy

The initial item taxonomy is:

```text
item
  item.equipment
    item.weapon
    item.armor
    item.accessory
  item.consumable
  item.material
  item.ingredient
  item.tool
  item.trade-good
  item.key
  item.quest-item
  item.book
  item.miscellaneous
```

These categories are enough to classify current prototype items and establish extension points for future content. They are not a final item library.

## Category Versus Capability

Categories describe what an item is.

Capabilities describe what an item can do.

Examples:

- Category: `item.weapon`
- Capability: equippable through `EquipmentData`
- Category: `item.consumable`
- Capability: usable through configured `ItemUseEffect` assets
- Category: `item.material`
- Capability: stackable inventory item with no use or equip behavior

Gameplay must not infer equip/use behavior only from category. Category metadata helps validation and future filtering, but current behavior remains data-driven by stack settings, item-use effects, and equipment data.

## Current Capabilities

Current item capabilities are:

- inventory item: can be stored in `PlayerInventory`;
- stackable item: uses `stackable` and `maximumStackSize`;
- usable item: has configured `ItemUseEffect` assets;
- equippable item: has `EquipmentData` with `Equippable`;
- world-pickup item: represented by `WorldItemPickup`;
- lootable item: referenced by `LootEntry`.

World pickup and loot capability are currently relationships from scene objects or loot tables to an `ItemDefinition`; they are not fields on every item definition.

## Stackability Rules

Stackability is static item-definition data:

- stackable items use `MaximumStackSize`;
- non-stackable items report a maximum stack size of 1;
- invalid stack sizes are clamped in `ItemDefinition.OnValidate`;
- inventory stack identity still uses the item definition reference;
- categories and tags do not affect stack equality.

No unique-item stack behavior exists yet.

## Equipment Integration

Prototype equipment items are classified as:

- Prototype Sword: `item.weapon`, `tag.weapon`, `tag.prototype`
- Prototype Helmet: `item.armor`, `tag.armor`, `tag.prototype`

Both categories inherit from `item.equipment`.

Equipment behavior still depends on `EquipmentData`, `EquipmentSlotType`, stat modifiers, and melee weapon data. Equip slots are not inferred from category.

## Consumable Integration

Health Potion is classified as:

- `item.consumable`
- `tag.healing`
- `tag.prototype`

Consumable behavior still depends on configured `ItemUseEffect` assets. Full-health failure, successful consumption, and inventory removal after successful use remain unchanged.

## Material, Tool, And Trade-Good Extensions

Step 3.3 adds `Prototype Iron Ore` as a small proof item:

- ID: `item.prototype-iron-ore`
- Category: `item.material`
- Tags: `tag.material`, `tag.prototype`
- Stackable: yes
- Use effects: none
- Equipment: not equippable

It is included in the definition catalog but not placed in the prototype scene. This proves that the taxonomy supports collectible-style items that are neither usable nor equippable without adding scene clutter.

Future material, ingredient, tool, and trade-good systems should build on category and capability metadata without adding crafting, harvesting, shop, or economy behavior prematurely.

## Key And Quest Item Extensions

The taxonomy includes `item.key` and `item.quest-item`, but no locking or quest-inventory behavior exists yet.

Future systems should consider:

- whether items can be dropped;
- whether items can be sold;
- quest ownership;
- unique identity;
- removal rules;
- save/load handling.

Avoid generic booleans like `isWeapon`, `isArmor`, or `isQuestItem` when categories and capabilities already express the concept.

## Visual References

Current `ItemDefinition` assets expose an optional inventory `Icon`.

Future object visuals may need separate references:

- inventory icon;
- pickup prefab;
- equipped prefab;
- placed-world prefab.

Step 3.3 does not add new visual fields because the current prototype does not need them. One prefab should not be assumed to work for every visual purpose.

## Definition Catalog Usage

Object and item definitions use the shared `DefinitionCatalog`.

Rules:

- registered item definitions implement `IGameDefinition`;
- new item IDs should be namespaced;
- existing legacy IDs remain compatible;
- duplicate IDs are detected globally;
- category and tag references must be present in the configured catalog;
- lookup by `ItemDefinition` or `IInventoryItemDefinition` uses the shared `DefinitionRegistry`;
- `PlayerInventory` remains runtime state, not a definition registry.

## Validation Rules

Definition validation now checks item taxonomy rules:

- missing primary item category is an error;
- assigned item category must be in the item domain;
- duplicate and null tags are reported by the classification validator;
- stackable items must report a maximum stack size of at least 1;
- non-stackable items should behave as size 1 stacks;
- equipment-categorized items without equip capability are warnings;
- equippable items outside the equipment/weapon/armor hierarchy are warnings;
- usable items with missing use effects are errors;
- usable items outside `item.consumable` are warnings;
- consumable-categorized items without use effects are warnings;
- category and tag references must be included in the catalog.

The validator reports but does not mutate assets.

## Creating A New Item

1. Create an `ItemDefinition` asset.
2. Assign a stable ID. New IDs should use a namespaced format such as `item.example`.
3. Set display name and description.
4. Assign one primary item category.
5. Assign typed tags as needed.
6. Configure stackability and maximum stack size.
7. Add item-use effects only if the item is usable.
8. Configure equipment data only if the item is equippable.
9. Add the item and any new categories/tags to the definition catalog.
10. Run `Tools/Game Data/Validate Definitions`.

## Safe Migration Rules

- Do not rename existing item IDs without an explicit migration plan.
- Do not convert category metadata into gameplay authority.
- Do not remove existing serialized fields casually.
- Use additive fields and interfaces where possible.
- Keep prototype category/tag assignments metadata-only until a later feature needs behavior.

## Known Limitations

- No rarity, quality, condition, durability, value, weight, ownership, or per-instance item data exists yet.
- No crafting, harvesting, procedural loot generation, shop filtering, or economy behavior exists yet.
- No item filtering UI exists yet.
- `Prototype Iron Ore` is catalog content only; it is not spawned in the prototype scene.
- Legacy item IDs such as `health_potion` remain accepted with warnings.

## Future Extensions

Feature 3.4 can layer rarity, quality, and condition foundations on top of this taxonomy.

Later systems can add:

- durability and condition;
- crafting materials and recipes;
- resource gathering;
- procedural loot rules;
- vendor pricing and shops;
- ownership and theft rules;
- item persistence using stable definition IDs;
- multiplayer content-version validation.
