# Professional Ranks, Mastery, and Specializations

Feature 10.6 adds professional ranks, specialization progression, mastery recognition, and rank-based permission foundations for Step 10.

## Ownership Boundaries

Professional ranks do not own profession identity, training, professional activities, credentials, skills, knowledge, capabilities, titles, positions, employment, organization membership, or reputation. Those systems remain authoritative for their own records.

Rank evaluation reads:

- `PersonProfessionRuntime` for active, formal, informal, recognized, and specialization relationships.
- `TrainingRuntime` for completed training programs.
- `ProfessionalActivityRuntime` for supervised, independent, teaching, leadership, breadth, depth, quality, and difficulty experience evidence.
- `CredentialRuntime` for active credentials, passed examinations, and credential permission foundations.
- Existing rank and mastery records for prior-rank and non-duplicate requirements.

Evaluation is read-only. It produces a dependency snapshot and deterministic evaluation hash so later promotion can reject stale or unauthorized decisions before mutation.

## Definitions

`ProfessionalRankDefinition` is catalog-authored and immutable at runtime. It declares the profession, optional specialization, rank order, prior-rank requirements, training requirements, credential requirements, experience requirements, examination requirements, authorized promotion authorities, formal/informal track behavior, permission foundations, lifecycle policies, visibility, and persistence validation rules.

`ProfessionalRankLadderDefinition` declares a rank ladder for a profession or specialization. It validates ordered ranks, roots, terminal ranks, lateral ranks, demotion ranks, formal and informal track support, rank skipping policy, unreachable ranks, and cycles. Specialization ladders may depend on external general profession ranks, but ladder topology is computed only from ranks that belong to that ladder.

`ProfessionalMasteryDefinition` declares an authored mastery recognition for a profession or specialization. Mastery requires explicit qualifying evidence such as rank, validated experience, breadth, depth, independent work, teaching or leadership, credentials, examinations, achievements, and authorized recognition. A generic experience threshold alone is not enough to define mastery.

Prototype definitions are registered through `PrototypeProfessionDefinitionFactory` so Test Lab, persistence, catalog validation, and command automation all resolve the same source of truth.

## Runtime Flow

`ProfessionalRankRuntime` supports:

- Advancement evaluation.
- Rank applications, evidence requests, approval, rejection, and withdrawal.
- Authoritative promotion.
- Informal rank recognition.
- Lateral rank changes and privileged corrections.
- Specialization-rank progression.
- Qualifying achievement records.
- Mastery evaluation and recognition.
- Suspension, reinstatement, demotion, revocation, retirement, and disputed-rank state.
- Permission-foundation checks for teaching and supervision.
- Access-aware redacted projections.
- Save and restore validation.

Promotion revalidates the stored advancement snapshot before commit. Unauthorized authorities, stale dependency hashes, invalid rank skipping, missing prerequisites, duplicate active ranks, and invalid lifecycle transitions reject without partial mutation.

## Formal And Informal Rank

Formal and informal ranks are explicit track data. Formal ranks require an active formally recognized profession relationship when configured by the rank definition. Informal recognition can record social or local rank standing without granting formal authority, credentials, titles, employment, or organization membership.

## Specialization And Mastery

Specialization ranks are regular rank records scoped to a specialization. They can depend on general profession ranks while preserving an independent specialization ladder.

Mastery records are separate from rank records. A Person can hold rank without mastery, and mastery requires explicit qualifying achievements or equivalent authored evidence. Mastery recognition does not grant skills, knowledge, capabilities, credentials, titles, positions, employment, or membership.

## Permissions

Rank permission foundations are predicates over active rank records. They can be consumed by later teaching, supervision, or authority systems, but the rank runtime does not perform the downstream action itself.

Suspended, revoked, retired, inactive, and disputed ranks do not satisfy active permission checks unless a later feature explicitly creates a different projection rule.

## Access And History

Rank records, applications, mastery records, and qualifying achievements expose Step 8 information subjects. Callers can request full, redacted, concealed, or denied projections through `InformationAccessRuntime`.

Secret ranks redact protected fields such as rank record IDs, person IDs, profession IDs, specialization IDs, authority IDs, application IDs, evidence IDs, permission foundations, state notes, access policy IDs, and provenance references.

The runtime emits lightweight history hooks for milestone events such as rank application, promotion, demotion, suspension, revocation, retirement, mastery recognition, and correction. Restore paths do not replay hooks.

## Persistence

`ProfessionalRankPersistenceParticipant` persists rank applications, rank records, mastery records, qualifying achievements, and history hook state as a strict player-scoped participant.

Restore validation rejects unsupported schema versions, missing or duplicate IDs, unknown people, missing rank or mastery definitions, invalid prior ranks, invalid application links, invalid mastery evidence, broken achievement references, duplicate active ranks, and invalid lifecycle state before commit. Failed restore leaves the existing rank runtime unchanged.

## Test Lab

Feature 10.6 registers the `feature.10.6.professional-ranks-mastery-specializations` automation suite under Step 10.

It covers definition validation, ladder validation, advancement evaluation, application and promotion flow, stale and unauthorized rejection, specialization progression, mastery recognition, lifecycle permission boundaries, competency non-grants, access-aware redaction, persistence restore, and corrupt restore rejection.

Rank runtime is part of `TestLabRuntimeBundle`, so fresh runtime scenarios and snapshot restore scenarios include rank state in mutation auditing and deterministic fingerprints.

## Deferred

Feature 10.6 does not implement employment contracts, wages, position duties, schedules, labor markets, career transitions, aspirations, full life paths, reputation, final UI, or autonomous NPC rank applications. Later systems should consume rank, mastery, and permission-foundation state instead of being implemented inside ranks.
