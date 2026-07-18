# Item Instance And Serialization Foundation

This document describes the Step 3.5 foundation for individual item identity and future item serialization.

## Definition Versus Instance Identity

`ItemDefinition` describes what kind of item something is. It owns static configuration such as ID, display name, category, rarity, stack rules, equipment data, and use effects.

`ItemInstance` describes one particular occurrence of an inventory item definition. It can carry:

- a definition reference;
- an optional persistent runtime instance ID;
- optional `ItemInstanceMetadata` for quality and condition.

Definition IDs and instance IDs are intentionally separate. Static definitions remain registered in `DefinitionCatalog`; runtime item instances are not catalog definitions and are not registered globally.

## When An Item Needs An Instance

The selected policy is:

- `DefinitionOnly`: interchangeable items do not need persistent instance IDs.
- `OptionalInstance`: the definition can be used as a stack item today, but generated or stateful copies may receive identity later.
- `AlwaysInstanced`: future stateful or unique items should have persistent identity.

Health Potion and Prototype Iron Ore remain definition-only. Prototype equipment can stay definition-based today while future generated equipment can use optional or always-instanced identity.

## Runtime Model

`ItemInstance` is runtime state. It does not depend on UI, scene objects, or `UnityEditor`.

Creation paths:

- `ItemInstanceFactory.CreateDefinitionOnly(definition)`
- `ItemInstanceFactory.CreateStateful(definition, metadata, instanceId)`
- `ItemInstance.CreateDefinitionOnly(definition)`
- `ItemInstance.CreateStateful(definition, metadata, instanceId)`

Factory methods return explicit creation results. Constructors fail clearly for invalid definitions or malformed IDs.

Mutable runtime changes happen through explicit methods:

- `AssignMetadata`
- `SetQuality`
- `ClearQuality`
- `SetCondition`
- `ClearCondition`
- `EnsurePersistentIdentity`

These methods replace copied metadata. They do not mutate shared ScriptableObject definitions.

## Instance-ID Policy

Persistent instance IDs use canonical GUID strings.

Rules:

- definition-only stacks do not require a persistent ID;
- stateful or unique items should have a persistent ID;
- IDs are generated once during explicit creation, not during property access;
- copied items use `CloneWithNewIdentity` unless restoring the same saved object;
- restoration preserves saved IDs;
- malformed saved IDs fail restoration.

Tests may supply deterministic GUID strings.

## Runtime Metadata Ownership

`ItemInstanceMetadata` stores optional quality and optional normalized condition.

It supports copying and safe mutation through value-returning helpers. Absence of metadata remains distinct from default quality or excellent condition.

Future fields such as enchantments, owner IDs, custom names, creator IDs, or history are deliberately not present yet.

## Serialization Data Shape

`ItemInstanceSaveData` is plain serializable data:

```csharp
public string definitionId;
public string instanceId;
public bool hasQuality;
public string qualityId;
public bool hasCondition;
public float conditionNormalized;
```

The save shape stores stable IDs rather than ScriptableObject references. Explicit presence flags distinguish missing metadata from default values and avoid nullable fields for Unity serialization compatibility.

Step 3.5 does not write files to disk or serialize full inventory/equipment state.

## Restoration Flow

`ItemInstanceSerializationUtility.Restore(saveData, registry)`:

1. validates save data presence;
2. validates definition ID;
3. validates instance ID format;
4. resolves the item definition through `DefinitionRegistry`;
5. rejects definitions that are not inventory item definitions;
6. resolves quality by stable ID when present;
7. validates condition values are within `0..1`;
8. creates an `ItemInstance` preserving identity and copied metadata.

Restoration returns `ItemInstanceRestoreResult` and does not produce partially valid item instances on failure.

Failure statuses distinguish missing save data, missing definition ID, missing item definition, wrong definition type, invalid instance ID, missing quality definition, and invalid condition value.

## Stack Compatibility

Current `PlayerInventory` behavior is unchanged and remains definition-reference based.

`ItemInstanceStackingPolicy` documents future instance-aware rules:

- same item definition required;
- item definition must be stackable;
- persistent IDs do not stack;
- stateful instances that require identity do not stack;
- differing quality does not stack;
- differing condition does not stack;
- definition-only instances of the same stackable definition can stack.

This policy is not wired into production inventory yet.

## Future Equipment Migration

Future equipment migration should preserve the same item instance:

```text
inventory instance
-> equip same instance
-> retain instance ID and metadata
-> unequip same instance back into inventory
```

Equipment must not destroy and recreate item identity once stateful items affect gameplay.

## Future Inventory, Loot, And Reward Migration

Pickups, loot, quest rewards, and contract rewards still grant definition plus quantity.

Future extensions can add generated item instances beside the existing definition path. They should not randomize quality or condition until generation rules and save/load ownership are explicit.

## Validation Rules

Static definition validation warns when:

- always-instanced items are stackable;
- optional or always-instanced items use stack sizes greater than one.

Runtime validation is explicit and opt-in:

- malformed instance IDs are reported;
- duplicate runtime instance IDs are detected in a supplied collection;
- restoration reports missing or wrong definitions and invalid metadata IDs.

Ordinary definition validation does not scan live scenes for runtime item instances.

## Known Limitations

Step 3.5 does not implement disk save/load, inventory serialization, equipment serialization, inventory migration to item instances, durability loss, repairs, quality stat modifiers, procedural generation, enchantments, ownership, trading, shops, item dropping, custom names, item history, or multiplayer replication.

## Save/Load Extension Path

Recommended next steps:

1. Decide inventory save entry shape for definition-only stacks versus stateful instances.
2. Add equipment save entries that preserve item instance IDs.
3. Introduce world/container ownership of item instances.
4. Add versioned save payloads and missing-definition reporting.
5. Only then make condition, quality, enchantments, or ownership affect gameplay.
