# Materials and Item Composition

Feature 9.2 adds an item composition layer on top of Feature 9.1 item identity.

## Ownership Boundary

`ItemInstanceIdentityRuntime` remains the authority for item existence, stable instance IDs, lifecycle, location, ownership, custody, condition, quality, and provenance.

`ItemCompositionRuntime` owns what an item instance is made of:

- material entries such as iron blade, leather grip, oil coating, or potion contents;
- component entries such as blade, hilt, removable gem, abstract vial, or tracked child item;
- graph validation for component parentage and tracked embedded item references;
- derived physical summaries such as known mass and weighted durability;
- composition-aware stack equivalence.

Composition records reference `itemInstanceId`. They do not replace item identity records and do not create item existence on their own.

When item creation requires a composition, callers should use `ItemCompositionCoordinator.CreateItem`. The coordinator prepares item identity and composition in working save graphs, validates both, and commits them together. If default or explicit composition creation fails, the item identity record is not left behind.

## Authored Definitions

`MaterialDefinition` describes reusable material facts:

- canonical material ID;
- material category;
- stable material tags;
- role support such as structure, coating, or binding;
- physical property profile;
- optional composite constituents.

`MaterialCompatibilityRuleDefinition` describes deterministic interactions between material entries. Rules are ordered by descending priority, then canonical rule ID, so repeated evaluation with the same inputs returns the same result.

`ItemDefinition` may declare a default composition template. Existing items with no template stay valid. When a template is authored, catalog validation checks it through the same composition validation path used at runtime.

Default templates carry a `templateVersionId`. That version is copied onto newly initialized item compositions so later edits to the item definition do not silently rewrite already-created item instances.

## Runtime Validation

Composition validation is strict:

- every composition references an existing item instance when an item runtime is supplied;
- every material entry references a known `MaterialDefinition` when a registry is supplied;
- material entry IDs and component entry IDs must be unique inside the composition;
- quantities must be positive and use a concrete unit;
- count quantities must be whole numbers;
- percent and ratio entries may describe partial compositions, but cannot total above 100% / 1.0;
- volume-based mass projection requires material density and never silently converts without it;
- component parent cycles are rejected;
- tracked component items cannot be the parent item, missing, destroyed, consumed, or embedded twice;
- tracked component items must be reserved by item identity for the parent item and component entry before the composition can reference them.

Persistence prepares and validates the whole save payload before commit. A failed prepare or restore leaves the live composition runtime unchanged.

## Tracked Component Location

An embedded tracked item is still owned by `ItemInstanceIdentityRuntime`. Composition only references it. Before a gemstone, removable blade, vial, or other tracked child can appear inside a parent composition, item identity must place the child in a `ProductionReserved` component location with:

- `containerId` equal to the parent item instance ID;
- `transitId` equal to the component entry ID.

That prevents one physical child item from simultaneously being in an inventory, equipped, placed in the world, or embedded in another parent. Use `ItemCompositionCoordinator.AttachTrackedComponent` and `DetachTrackedComponentToInventory` for atomic identity/composition updates.

## Mass Authority

Composition-derived mass is a projection. It is useful for inspection, future crafting, and future physics/capacity work, but Feature 9.2 does not silently replace existing gameplay weight, inventory capacity, equipment requirements, or Rigidbody mass.

The default authority is `AuthoredDefinition`. A complete composition can opt into `CompositionAuthoritative` through explicit policy, but current systems should treat that as a future integration signal rather than automatic capacity logic.

## Durability Boundary

Material physical properties include durability-like values as intrinsic material behavior: durability potential, toughness, structural endurance, and wear resistance. They are not the item's current durability.

Feature 9.4 remains responsible for actual item durability, degradation, breakage, repair, and wear state.

## Access And Projection

Composition exposes Step 8 information subjects through `ItemCompositionInformationSubject`.

Access-aware projections return full, redacted, concealed, or denied composition details without mutating the authoritative record. Redaction hides protected fields such as purity, hidden tracked components, recipe/provenance details, revision history, and access policy IDs.

Normal item gameplay can still use privileged/internal snapshots. Gameplay or UI code that represents another person's inspection should use projections.

## Stack Rules

Definition and identity equivalence are not enough for stateful composition-aware items. Two item instances can share a stack only when their canonical composition signatures match: same source definition, template version, completeness, material entries, unit-normalized quantities, purity/form, component paths, tracked component IDs, provenance-relevant fields, hidden/access-sensitive state, and component structure.

This prevents visually identical but materially different items from silently stacking.

## Deferred Work

Feature 9.2 intentionally does not implement crafting, durability damage, repair, attunement, economy pricing, inscriptions, containers, wielding rules, or final UI inspection. Later Step 9 features should consume the composition runtime instead of storing parallel material state.
