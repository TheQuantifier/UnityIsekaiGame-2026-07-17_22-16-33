# Game Definition And Stable ID Guidelines

This document defines the Step 3.1 foundation for static game definitions, stable IDs, validation, and lookup.

## Static Definitions

A static game definition describes what something is. It is authored data, usually stored in a ScriptableObject asset.

Examples:

- `ItemDefinition`: item ID, display name, description, stack rules, use effects, equipment configuration.
- `SpellDefinition`: spell ID, display name, mana cost, cooldown, projectile configuration.
- `PersonDefinition`: person ID, display name, title, role metadata.
- `QuestDefinition`: quest ID, title, stages, prerequisites, reward.
- `ContractDefinition`: contract ID, title, objectives, reward.

Runtime state describes the current state of a particular instance.

Examples:

- `InventorySlot`: item definition reference plus current quantity.
- `QuestInstance`: quest definition reference plus current stage, objective state, and reward state.
- `ContractInstance`: contract definition reference plus objective progress and reward state.
- `PersonIdentity`: currently loaded scene presence for a static `PersonDefinition`.

ScriptableObject definitions must not store mutable runtime gameplay state.

## Shared Contract

Step 3.1 uses a small interface:

```csharp
public interface IGameDefinition
{
    string Id { get; }
    string DisplayName { get; }
}
```

No shared ScriptableObject base class was introduced. Existing definition assets use different serialized field names such as `itemId`, `personId`, `questId`, and `contractId`. A base class would require field migration or duplicate serialized identity fields. The interface provides shared lookup behavior without changing asset inheritance or serialized data layout.

Definitions migrated now:

- `ItemDefinition`
- `SpellDefinition`
- `PersonDefinition`
- `QuestDefinition`
- `ContractDefinition`

Definitions deliberately deferred:

- `DialogueNodeDefinition`, because dialogue nodes currently have no stable ID and are graph nodes rather than globally registered world definitions.
- `ContractObjectiveDefinition` subclasses, because they are reusable objective configuration assets but not yet independent world definitions.
- `ContractRewardDefinition` and `QuestStageDefinition`, because they are embedded serializable configuration rather than independent ScriptableObject definitions.

## Stable ID Format

Definition IDs use this accepted format:

- lowercase letters;
- digits;
- periods;
- hyphens;
- underscores;
- no whitespace;
- no leading or trailing whitespace;
- no leading or trailing separator;
- no repeated separator of the same character.

Examples for new content:

- `item.health-potion`
- `spell.arcane-bolt`
- `person.prototype-npc`
- `quest.strange-disturbance`
- `contract.prototype-elimination`

Existing legacy IDs such as `health_potion` and `prototype_npc` remain valid for compatibility, but new content should prefer a namespaced domain prefix with a period.

## ID Immutability

Stable IDs must:

- remain unchanged after saves or other assets reference them;
- not depend on display names;
- not depend on GameObject names;
- not depend on scene paths;
- not be regenerated during ordinary imports;
- be unique in configured definition catalogs.

Renaming an ID is a migration, not a refactor. Future save data should store stable IDs and resolve definitions through catalogs during load.

## Uniqueness Rule

The selected rule is globally unique IDs inside a definition catalog. New IDs should be namespaced by domain, such as `item.` or `quest.`, so generic future systems can resolve them without carrying a separate type key.

The current validator allows legacy non-namespaced IDs with a warning. This keeps existing prototype assets compatible while making the preferred format explicit for future content.

## Catalog And Lookup

Static definition lookup is explicit and asset-driven:

- `DefinitionCatalog` is a ScriptableObject containing authored definition asset references.
- `DefinitionCatalogValidator` validates missing references, incompatible types, invalid IDs, duplicate asset references, and duplicate IDs.
- `DefinitionRegistry` is initialized from a catalog or known definitions and supports safe lookup by ID and by expected definition type.

The lookup system does not use scene searches, `GameObject.Find`, `Resources.FindObjectsOfTypeAll`, or `AssetDatabase` in runtime code.

The prototype catalog lives at:

- `Assets/GameData/Prototype/PrototypeDefinitionCatalog.asset`

It includes representative top-level prototype definitions for items, spells, the prototype NPC, the staged quest, and prototype contracts.

`PersonRegistry` remains separate. It tracks currently loaded `PersonIdentity` scene instances, while the definition catalog tracks static `PersonDefinition` assets.

## Creating A New Definition Type

1. Decide whether the asset is an independently referenceable static definition.
2. Add stable ID and display name fields using names that fit the domain.
3. Implement `IGameDefinition` by mapping `Id` and `DisplayName` to those fields.
4. Keep mutable runtime state in runtime classes or MonoBehaviours, not in the ScriptableObject.
5. Add the asset to an explicit `DefinitionCatalog` when it should be globally resolvable.
6. Run `Tools/Game Data/Validate Definitions`.

Do not add category, rarity, faction, prefab, icon, value, or other domain-specific fields to `IGameDefinition`.

## Creating A New Definition Asset

1. Create the asset through its `CreateAssetMenu`.
2. Assign a stable ID before referencing it elsewhere.
3. Prefer the domain-prefixed format, such as `item.new-example`.
4. Assign a display name for UI.
5. Add the asset to the appropriate catalog.
6. Validate definitions before committing.

## Safe Migration Rules

- Do not rename existing serialized ID fields unless necessary.
- Use `FormerlySerializedAs` if a serialized field must be renamed.
- Do not regenerate IDs.
- Do not change IDs only to match the preferred new format.
- Treat ID renames as content migrations that require reference and save compatibility review.
- Keep old string fallback fields until serialized assets no longer need them.

## Future Save And Load

Save data should store stable definition IDs, not whole ScriptableObject assets.

Future load flow:

1. Read a saved stable ID.
2. Resolve the static definition through the configured catalog/registry.
3. Reconstruct mutable runtime state separately.
4. Report missing IDs clearly instead of silently substituting unrelated content.

## Future Content Versioning

Catalogs currently expose a content version string only as metadata. Future features may add deterministic ordering, catalog hashes, addressable content, mod packages, server-authoritative catalogs, or multiplayer compatibility checks.

Those systems are not implemented in Step 3.1.

## Known Limitations

- Existing item, person, quest, and contract IDs are valid legacy IDs but do not use the preferred domain prefix.
- `SpellDefinition` gained an additive `spellId` field; existing prototype spell assets were assigned IDs.
- There is no global project catalog bootstrap yet. Runtime systems should receive or create registries explicitly from configured catalogs.
- Dialogue nodes, objective definitions, rewards, places, species, factions, tags, rarity, quality, condition, abilities, and effects are outside this feature.
