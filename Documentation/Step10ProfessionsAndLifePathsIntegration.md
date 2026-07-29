# Step 10 Professions and Life Paths Integration

Step 10 defines profession identity, professional entry, training, activity evidence, credentials, ranks, employment, career history, and life-path identity as separate authoritative runtimes. Feature 10.10 does not add another authority. It adds an integration validator and Test Lab consolidation layer that verifies those runtimes remain connected by explicit references, deterministic persistence, and access-aware projections.

## Authority Model

Each Step 10 domain has one owning runtime:

- `profession.relationship`: `PersonProfessionRuntime`
- `profession.entry`: `ProfessionEntryRuntime`
- `training.enrollment`: `TrainingRuntime`
- `professional.activity`: `ProfessionalActivityRuntime`
- `credential.record`: `CredentialRuntime`
- `professional.rank`: `ProfessionalRankRuntime`
- `position.employment`: `PositionEmploymentRuntime`
- `career.history`: `CareerHistoryRuntime`
- `life.path`: `LifePathRuntime`

Other runtimes may reference those records, but they must not recreate or mutate them as local shadows. Requirement adapters and projections consume snapshots or stable record IDs from the owning runtime.

## Graph Validation

`Step10IntegrationValidator` validates a `Step10IntegrationRuntimeSnapshot` built from the nine Step 10 save payloads. The validator checks:

- catalog availability for representative Step 10 definitions;
- save schema versions before restore;
- stable IDs and duplicate runtime indexes;
- known Person, organization, and authority references;
- definition references;
- profession-specialization parentage;
- cross-runtime links from entries, training, activity evidence, credentials, ranks, employment, duties, career episodes, goals, and identities;
- lifecycle conflicts such as multiple primary professions, active employment missing from its position, active career episodes based on ended employment, completed goals with no authoritative completed source, and reporting cycles;
- deterministic canonical fingerprints.

The validator is intentionally strict about hard corruption and missing authority. Access-sensitive records are expected to be exposed through the runtime projection APIs rather than raw gameplay callers.

## Persistence and Migration

Step 10 persistence order is acyclic:

1. `PersonProfessionRuntime`
2. `ProfessionEntryRuntime`
3. `TrainingRuntime`
4. `ProfessionalActivityRuntime`
5. `CredentialRuntime`
6. `ProfessionalRankRuntime`
7. `PositionEmploymentRuntime`
8. `CareerHistoryRuntime`
9. `LifePathRuntime`

Restore should validate schema and cross-runtime dependencies before replacing live state. Unsupported schema versions or missing references must fail before commit. Restore replay must not duplicate hooks, history, profession links, career episodes, goals, or identity records.

## Determinism

The integration validator creates a canonical fingerprint from sorted runtime records. Record insertion order must not affect the fingerprint. This fingerprint is for diagnostics and Test Lab verification; it is not a gameplay ID and should not be used as a replacement for runtime-owned stable IDs.

## Test Lab

Feature 10.10 registers `feature.10.10.profession-life-path-integration-finalization` through the Step 10 automation provider. The suite validates:

- readiness and definition graph validation;
- persistence dependency, schema, and fingerprint behavior;
- an integrated profession/credential/rank/employment/career/life-path scenario;
- larger activity fixtures, immutable snapshots, and access-aware projections.

The command-side automation runner and in-game Test Lab runner both consume the same suite catalog.

## Deferred Systems

Step 10 intentionally does not implement final NPC career decision-making, legal institutions, economy-wide job markets, organization governance, full UI presentation, multiplayer/account permissions, or final balance tuning. Those systems should provide context to Step 10 runtimes rather than taking ownership of Step 10 records.
