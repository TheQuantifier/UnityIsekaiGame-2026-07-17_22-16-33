# Education, Training, and Apprenticeship

Feature 10.3 adds the education layer for professions without making education the owner of skills, profession identity, titles, ranks, employment, or knowledge truth.

`TrainingProgramDefinition` describes a formal or informal program such as an apprenticeship, academy course, safety course, tutoring program, drill, residency, or certification preparation path. Programs reference professions, specializations, entry paths, curricula, instructor requirements, learner capacity rules, workplace or station needs, access policy, and completion requirements.

`TrainingCurriculumDefinition` owns the authored structure of a program. It contains modules, lessons, and practical assignments. Validation rejects duplicate IDs, missing module references, missing transfer definitions, lesson transfer-mode mismatches, and dependency cycles.

## Runtime Ownership

`TrainingRuntime` owns training records:

- enrollments and apprenticeship state
- instructor, mentor, and supervisor assignments
- learning sessions and attendance records
- practical assignment records
- supervised work records
- progress snapshots and deterministic progress tokens

`PersonProfessionRuntime` still owns profession relationships. Training completion does not create, recognize, activate, or specialize a profession relationship unless a later system explicitly requests that through the profession-entry flow.

`InformationTransferRuntime` still owns teaching/disclosure execution. Training sessions can route teaching through Step 8 transfer requests, but attendance alone never creates knowledge or memory.

## Progress Model

Progress is evaluated from runtime records and curriculum definitions. The result contains completed requirements, remaining requirements, failed requirements, blockers, a percentage, and a revision-backed token.

Authoritative progress can include hidden requirements. Perceived progress hides hidden modules from ordinary learner-facing views. Completion requires a current authoritative token so stale evaluations cannot complete a program after intervening changes.

## Persistence

`TrainingPersistenceParticipant` captures training state as a player-scoped progression participant. Restore validates the entire graph before commit and rolls back on failure. Restore does not replay training history hooks.

Missing training payloads restore as an empty training state for older saves. Invalid payloads remain hard failures rather than silent validation bypasses.

## Access And Records

Training records expose Step 8 information subject references and access-aware projections. Public or learner-facing projections can redact protected details such as person IDs, instructor IDs, hidden requirements, progress tokens, and provenance.

History hooks are emitted for meaningful training lifecycle events, but authoritative history remains owned by the history runtime.

## Boundaries

Training does not grant:

- skills or attributes
- recipes or crafting knowledge
- profession identity or recognition
- titles, ranks, roles, jobs, offices, or faction status
- legal permissions or organization authority
- information access grants

Those systems can later consume training outputs as evidence or eligibility inputs, but training is not their owner.

## Prototype Definitions

Prototype fallback definitions are centralized in `PrototypeProfessionDefinitionFactory`. Catalog-authored definitions remain authoritative. The fallback path is definition registration for known prototype content, not a validation bypass.

The prototype blacksmith apprenticeship demonstrates:

- a visible safety module
- a visible practical module
- a hidden assessment module
- formal lesson, demonstration, and guided-practice transfer definitions
- supervisor-required crafting practice
- completion blocked until hidden requirements are satisfied

