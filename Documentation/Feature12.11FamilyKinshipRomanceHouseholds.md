# Feature 12.11 - Family, Kinship, Romance, and Household Relationships

Feature 12.11 adds the first family and household layer for Step 12 social simulation.

## Authority Boundaries

- `RelationshipRuntime` remains the owner of direct person-to-person relationship records.
- `FamilyRelationshipRuntime` derives kinship from relationship records and owns only household records, household memberships, and family/romance transaction idempotence.
- `InterpersonalAttitudeRuntime` owns subjective attitude values. Romantic attraction is an attitude dimension, not a relationship or consent record.
- `SocialInteractionRuntime` remains the place where consent-bearing interactions can be represented. 12.11 only consumes explicit consent references or explicit scripted/player consent flags.
- Step 11 economy/property systems remain authoritative for property and financial ownership. Households may reference a residence or property ID but do not own the property record.

## Parentage

Prototype relationship definitions now cover biological parent-child, adoptive parent-child, legal guardian-dependent, and foster guardian-dependent records. Parentage creation delegates to `RelationshipRuntime` after validating:

- Known Person IDs.
- No self-parentage.
- No ancestry cycle.
- Definition availability.
- No unsafe mutation on failure.

Biological and adoptive parentage can coexist. Adoption does not erase biological lineage, and guardianship remains separate from parentage.

## Kinship

Kinship is derived on demand from active relationship records. Queries are deterministic, bounded by `KinshipTraversalLimits`, and return immutable path/tree snapshots. Hidden parentage is excluded from non-privileged views, while privileged diagnostic views can include it.

Supported derived classifications include parent, child, sibling, ancestor, descendant, cousin, spouse/partner, former partner, and basic in-law classifications.

## Romance

Romantic eligibility is policy-driven through `RomanticEligibilityPolicyDefinition`. The prototype strict policy requires adult participants, explicit consent, no prohibited close kinship, no guardian-dependent conflict, and no active exclusive partner conflict.

Romantic attraction is stored as `attitude.romantic-attraction` in `InterpersonalAttitudeRuntime`. It is directional, ranges from 0 to 100, and cannot substitute for consent or relationship state.

Romantic lifecycle transitions create or end records in `RelationshipRuntime`. 12.11 tracks transition transaction IDs for idempotence but does not own the relationship record.

## Households

Households are persistent social living arrangements, not ownership groups. `FamilyRelationshipRuntime` owns:

- Household identity and lifecycle.
- Household memberships and roles.
- Split, merge, dissolve, residence-reference, and membership-role changes.
- Persistence prepare/commit/rollback validation.

Households can reference places or property records by ID while leaving those records under their owning systems.

## Persistence And Test Lab

`FamilyRelationshipPersistenceParticipant` saves and restores household state with strict prepare-time validation. Test Lab fresh runtimes now include `FamilyRelationshipRuntime` in snapshot restore and fingerprint auditing so automation can detect undeclared family/household mutations.

Prototype definition fallbacks are centralized through `PrototypeFamilyRelationshipDefinitionFactory`, which composes relationship and attitude fallbacks before adding romance policy and household definitions. Catalog-authored definitions still take precedence.
