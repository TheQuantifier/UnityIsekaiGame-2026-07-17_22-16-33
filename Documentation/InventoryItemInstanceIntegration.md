# Inventory Item Instance Integration

Feature 3.6 migrates inventory and equipment runtime slots so they can hold either definition stacks or exact `ItemInstance` objects.

Definition-only stack APIs remain for ordinary stackable content. `PlayerInventory.AddItemOrInstances` grants always-instanced and optional non-stackable items as individual instances, preserving unique instance IDs for items such as prototype swords.

Inventory and equipment save DTOs record either definition stacks or stateful instances. Restore validates all entries before replacing live state.

Feature 3.7 ability execution contexts can optionally carry a source `ItemDefinition` and `ItemInstance`, but abilities do not mutate quality, condition, or item identity yet.
