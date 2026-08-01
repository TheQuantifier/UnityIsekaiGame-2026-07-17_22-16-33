# Step 12.1 Relationship Identity and Records

`RelationshipRuntime` is the authoritative owner for persistent Person-to-Person relationship records.

Relationship definitions describe valid categories, directionality, role types, duplicate-active policy, lifecycle capability, and default access policy references. Relationship records store stable record IDs, two participant Person IDs, role assignments, lifecycle state, start/end world time, source event or record references, access policy ID, tags, and revision.

The runtime supports symmetric, directed, and reciprocal role-distinct definitions. Symmetric relationships canonicalize participant ordering so a friendship between A and B resolves the same way as B and A. Role IDs are reusable role types, not positional slots, so symmetric records can assign the same role type to both endpoints.

Persistence remains strict:

- Missing relationship definitions fail prepare before any live mutation.
- Missing or unknown participants fail validation.
- Duplicate record IDs and disallowed duplicate active relationships fail.
- Invalid roles, self-relationships when prohibited, bad lifecycle state, and invalid time ranges fail.
- Restore rebuilds runtime state only after validation succeeds.

History, memory, knowledge, and access remain owners of their own records. Relationship records may reference historical events or source records, but creating a relationship does not create or replay history events.

Prototype fallback definitions are registered through `PrototypeRelationshipDefinitionFactory` so Test Lab and persistence share one source of prototype relationship definitions while catalog-authored definitions remain authoritative when present.
