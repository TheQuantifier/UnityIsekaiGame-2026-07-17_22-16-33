# Recipes and Crafting Knowledge

Feature 9.6 introduces authored recipe definitions and person-relative recipe knowledge. A recipe is authoritative production knowledge: it describes versions, variants, inputs, outputs, byproducts, procedure steps, batch scaling, transfer mappings, and policy references for expected composition, quality, affixes, and durability.

Recipe resolution is intentionally read-only by default. Previewing a recipe creates an immutable resolved snapshot and can ask the existing Production Requirement runtime for an exact requirement plan. Preview does not reserve inputs, consume materials, create outputs, apply tool wear, or progress a crafting job. Reservation is explicit and delegates to the existing production reservation system.

Recipe truth and Person knowledge are separate. `RecipeDefinition` stores the real recipe. `RecipeKnowledgeRuntime` stores what a person knows about it, including partial, incorrect, outdated, source-backed, belief-backed, memory-backed, and record-backed knowledge. Projections can return full or redacted recipe views without mutating the authoritative recipe.

## Runtime Boundaries

- `RecipeDefinition` is the authored catalog identity.
- `RecipeRuntime` resolves versions, variants, batch scaling, procedure order, and production requirement plans.
- `RecipeKnowledgeRuntime` stores person-relative recipe knowledge.
- `RecipeKnowledgePersistenceParticipant` saves and restores recipe knowledge with strict prepare-before-commit validation.
- `ProductionRequirementRuntime` remains responsible for exact input allocation, tool/station checks, reservation conflicts, stale plan checks, and dependency tracking.
- Item identity, material composition, quality/affix, and durability systems remain the owners of their respective item state.

## Deferred

Feature 9.6 does not execute crafting, consume input stacks, create item outputs, mutate durability, run production timers, manage queues, or provide final UI. Those belong to later crafting execution features.
