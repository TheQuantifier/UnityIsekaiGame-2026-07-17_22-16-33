# Profession Identity and Definitions

Feature 10.1 introduces profession identity as a descriptive Person relationship, not a class, skill package, title, role, or organization grant.

## Definitions

`ProfessionDefinition` is the stable catalog identity for a vocation or line of work. It records category, recognition style, related skills and knowledge subjects, allowed specializations, recognized authorities, visibility policy, and validation metadata.

`ProfessionSpecializationDefinition` describes a child focus under one parent profession. Specializations must reference an existing parent profession and can declare related skills, knowledge subjects, capabilities, and production activities as associations only.

The prototype definition factory currently supplies representative fallback definitions for blacksmith, field medic, scout, spy, weaponsmith, and trauma care. These are registered through the shared Test Lab and persistence definition registry path so validation and save capture consume the same source of truth.

## Runtime Ownership

`PersonProfessionRuntime` owns profession relationship records for Persons. A relationship records:

- Person ID and profession ID.
- Relationship state, active flag, primary flag, and practice form.
- Self-declared and formal recognition state.
- Recognition authority and reference IDs.
- Specialization IDs.
- Access policy, provenance, tags, revision, and dispute state.

The runtime supports multiple professions per Person. The first active profession becomes primary by default. Explicit primary changes are atomic and clear the old primary. Active duplicate Person-profession relationships are rejected unless the caller uses the same relationship ID with the same normalized payload, which is treated as idempotent.

## Boundaries

Profession identity does not grant skills, knowledge, capabilities, recipes, titles, ranks, roles, positions, faction membership, organization authority, or information access.

Definition fields such as related skills and related knowledge subjects are references for search, UI, and later progression systems. Separate runtimes must still grant or deny actual competencies.

## Access And History

Profession relationships can produce Step 8 information subject references for definitions, specializations, relationship records, formal recognition, self-declaration, primary state, and active state.

Public projections can be full, redacted, concealed, or denied based on `InformationAccessDecision`. Secret profession relationships redact owner identity, relationship ID, protected recognition data, provenance, and secret tags from ordinary inspection while retaining authoritative internal state.

The runtime emits transient history hook data for profession events. These hooks are not persisted and restore never replays them.

## Persistence

`PersonProfessionPersistenceParticipant` captures profession relationship save data as an identity projection. Prepare validates the complete graph before commit. Restore is all-or-nothing: corrupt payloads are rejected without mutating live profession state.

The prototype persistence service registers the profession participant with the player save graph after player identity and knowledge are available, using the same registry provider as the rest of the prototype persistence system.

## Current Limitations

Profession definitions are prototype-authored through a shared factory until durable Unity assets are promoted for Step 10 content authoring. This is an explicit fallback registration path, not a validation bypass.

Life path progression, profession rank advancement, teaching requirements, legal practice rules, organization membership, job positions, and economic production rights remain later Step 10 systems that should consume profession identity instead of being owned by it.
