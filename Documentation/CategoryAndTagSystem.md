# Category And Tag System

This document describes the Step 3.2 classification foundation.

## Responsibilities

Categories and tags are static game data used to classify other static definitions.

Categories are structured primary classifications. They answer what broad kind of thing a definition is.

Examples:

- `item`
- `item.weapon`
- `item.armor`
- `item.consumable`
- `ability`
- `ability.spell`
- `person`

Tags are flexible traits or descriptors. They answer what qualities, capabilities, or group labels a definition has.

Examples:

- `tag.healing`
- `tag.weapon`
- `tag.magic`
- `tag.projectile`
- `tag.quest-giver`
- `tag.prototype`

Categories and tags are metadata only in Step 3.2. Inventory, equipment, spellcasting, quests, contracts, dialogue, and reset behavior do not depend on them yet.

## Static Definitions

Categories and tags are registered static definitions:

- `CategoryDefinition : ScriptableObject, IGameDefinition`
- `TagDefinition : ScriptableObject, IGameDefinition`

They are included in the same `DefinitionCatalog` as items, spells, people, quests, and contracts. This preserves global stable-ID validation and shared lookup through `DefinitionRegistry`.

`IGameDefinition` remains minimal and still exposes only:

```csharp
string Id { get; }
string DisplayName { get; }
```

Category and tag fields are not added to `IGameDefinition`.

## Domain Model

Categories and tags use a lightweight `CategoryDomain` enum:

- `General`
- `Object`
- `Item`
- `Ability`
- `Being`
- `Person`
- `Place`
- `Faction`
- `Quest`
- `Contract`
- `Profession`

Domains guide validation and editor organization. They are not intended to block future cross-domain systems. A `General` tag can be assigned broadly. Category/domain mismatches are validation errors when an opted-in definition declares an expected domain and uses a non-general category from another domain.

## Classification Interfaces

Definitions opt in through small interfaces:

```csharp
public interface ICategorizableDefinition
{
    CategoryDefinition PrimaryCategory { get; }
    CategoryDomain ClassificationDomain { get; }
}

public interface ITaggedDefinition
{
    IReadOnlyList<TagDefinition> Tags { get; }
}
```

Legacy raw-string tag fields can be surfaced to validation through:

```csharp
public interface ILegacyStringTaggedDefinition
{
    IReadOnlyList<string> LegacyTags { get; }
    string LegacyTagLabel { get; }
}
```

`PersonDefinition` currently uses this to preserve its serialized `roleTags` field while warning that new content should use typed `TagDefinition` references.

## Category Hierarchy

Each `CategoryDefinition` may reference one optional parent category.

Rules:

- category IDs are authoritative;
- display names are not used for lookup;
- parent references are optional;
- a category cannot parent itself;
- circular parent chains are validation errors;
- traversal stops safely if a cycle is encountered;
- parent categories should be present in the same configured catalog.

The current prototype hierarchy is intentionally small:

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
ability
  ability.spell
person
quest
contract
```

`Documentation/ObjectAndItemTaxonomy.md` is authoritative for the item branch of the category tree.

## Stable IDs

Categories use domain-shaped IDs such as:

- `item`
- `item.weapon`
- `ability.spell`
- `person`

Tags use the `tag.` namespace:

- `tag.healing`
- `tag.weapon`
- `tag.prototype`

Existing Step 2 prototype definition IDs are still retained for compatibility. New content should prefer namespaced, lowercase, stable IDs.

## Creating A Category

1. Create a `CategoryDefinition` asset.
2. Assign a stable ID.
3. Assign a display name.
4. Choose a `CategoryDomain`.
5. Assign a parent category only when hierarchy is useful.
6. Add the category asset to the configured `DefinitionCatalog`.
7. Run `Tools/Game Data/Validate Definitions`.

## Creating A Tag

1. Create a `TagDefinition` asset.
2. Assign a stable ID with the `tag.` prefix.
3. Assign a display name.
4. Choose `General` unless the tag is specific to one domain.
5. Add the tag asset to the configured `DefinitionCatalog`.
6. Run `Tools/Game Data/Validate Definitions`.

## Assigning Classification

Step 3.2 migrates only a small set of prototype definitions:

- Health Potion: `item.consumable`, `tag.healing`, `tag.prototype`
- Prototype Sword: `item.weapon`, `tag.weapon`, `tag.prototype`
- Prototype Helmet: `item.armor`, `tag.armor`, `tag.prototype`
- Arcane Bolt and Heavy Arcane Bolt: `ability.spell`, `tag.magic`, `tag.projectile`, `tag.prototype`
- Prototype NPC: `person`, `tag.quest-giver`, `tag.prototype`

Quest and contract definitions are not assigned categories yet. Their root categories exist in the catalog for lookup and future migration.

## Runtime Queries

Use `ClassificationUtility` for shared queries:

```csharp
ClassificationUtility.HasTag(itemDefinition, "tag.healing");
ClassificationUtility.IsInCategory(itemDefinition, "item.weapon");
ClassificationUtility.IsInCategory(itemDefinition, "item");
ClassificationUtility.GetAncestors(category);
```

Comparisons use stable IDs where possible rather than relying only on asset reference identity.

## Validation

`Tools/Game Data/Validate Definitions` now checks:

- stable-ID rules;
- global duplicate IDs;
- duplicate catalog references;
- null catalog references;
- category self-parenting;
- circular category ancestry;
- parent categories missing from the catalog;
- assigned categories missing from the catalog;
- assigned tags missing from the catalog;
- duplicate tag references on a definition;
- incompatible category domains;
- tag domain mismatch warnings;
- legacy raw-string role tags.

The validator reports errors, warnings, and informational messages. It does not mutate assets.

## Legacy Migration

`PersonDefinition.roleTags` remains serialized and readable for compatibility. It is not discarded or converted automatically.

New person content should use typed `TagDefinition` references instead of adding new raw role strings.

## Known Limitations

- Tags are flat; no tag hierarchy is implemented.
- Only one primary category is supported per opted-in definition.
- Secondary categories are deferred until a concrete use case appears.
- No UI filtering, loot filtering, shop filtering, AI filtering, or gameplay rules use categories or tags yet.
- Category root IDs such as `item` intentionally represent taxonomy roots and may still trigger generic no-domain-prefix warnings from the shared stable-ID validator.
- The prototype catalog remains explicit and manually curated.

## Future Use

Later features can use this foundation for:

- object and item taxonomy;
- rarity, quality, and condition metadata;
- abilities and effects;
- beings and species;
- places and location hierarchy;
- factions and organizations;
- damage and resistance groups;
- shops, loot, contracts, AI context, and persistence validation.
