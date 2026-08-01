# Step 12.2 - Interpersonal Attitudes and Relationship Values

`InterpersonalAttitudeRuntime` owns subjective, directional Person-to-Person attitude values. These values are separate from Feature 12.1 relationship records: a relationship describes an objective social record, while an attitude describes one Person's internal stance toward another Person.

## Runtime Ownership

- `RelationshipRuntime` remains the owner of relationship identity, roles, lifecycle, and relationship history references.
- `InterpersonalAttitudeRuntime` owns ordered observer-to-subject attitude records.
- `InterpersonalAttitudePersistenceParticipant` saves and restores attitude state without asking relationship records to replay mutations.
- Test Lab fresh-runtime and scene-runtime paths both receive attitude support through `TestLabRuntimeBundle`.

## Dimensions

The prototype fallback factory supplies six canonical attitude dimensions:

- `attitude.trust`
- `attitude.affection`
- `attitude.respect`
- `attitude.fear`
- `attitude.loyalty`
- `attitude.hostility`

Definitions declare range, neutral value, negative-value policy, semantic category, and tags. Catalog-authored definitions take precedence; fallback definitions only fill missing prototype definitions.

## Value Model

Each ordered pair has at most one attitude record:

```text
observer Person -> subject Person
```

Effective value is deterministic:

```text
authored neutral or baseline + ordered source contributions, clamped to the dimension range
```

Neutral reads do not create records. This keeps ordinary queries cheap and prevents read-only systems from accidentally mutating social state.

## Source Contributions

Contributions are keyed by source ID and may reference:

- a relationship record,
- a historical event,
- dialogue,
- quest logic,
- scripted logic,
- Test Lab fixtures.

Contributions are owned by the attitude record, not by the source system. Removing or ending a relationship does not erase an attitude value unless a later attitude mutation explicitly removes that source contribution.

## Safety Rules

- Every mutation requires a transaction ID.
- Duplicate transaction IDs are idempotent.
- Preview mutations restore the prior runtime snapshot before returning.
- Failed mutations restore the prior runtime snapshot before returning.
- Save validation rejects unknown people, unknown dimensions, duplicate ordered pairs, duplicate source IDs, invalid schema versions, and persisted baseline values outside the authored range.
- Snapshots and save data are cloned on read.

## Test Lab

Feature 12.2 automation is registered under:

```text
feature.12.2.interpersonal-attitudes-relationship-values
```

The suite validates:

- canonical dimensions and neutral reads,
- directed values that do not mirror automatically,
- source contribution clamping,
- preview and duplicate transaction behavior,
- relationship independence,
- save, restore, and corrupt payload rejection.

The suite is hostless and command-line compatible because it uses the shared Test Lab runtime bundle rather than scene-only objects.
