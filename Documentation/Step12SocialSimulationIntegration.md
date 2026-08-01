# Step 12 Social Simulation Integration

Step 12 is finalized as an integration layer over the existing social runtimes. It does not create a new authoritative social data owner. Relationships, attitudes, reputation, rumors, interactions, norms, social networks, decisions, influence, emotions, and family/household records remain owned by their feature runtimes.

## Authority Map

`Step12SocialSimulationFacade` exposes a single authority map for Step 12 domains. Each entry names the feature that owns the domain, the authoritative runtime, whether the domain is derived, and the read-only runtimes that may consume it.

- 12.1 `RelationshipRuntime` owns relationship records.
- 12.2 `InterpersonalAttitudeRuntime` owns directional attitude values.
- 12.3 `ReputationRuntime` owns audience-scoped standing.
- 12.4 `RumorRuntime` owns rumor records and transmissions.
- 12.5 `SocialInteractionRuntime` owns social interaction records, pending responses, and promises.
- 12.6 `SocialNormRuntime` owns norm assessments.
- 12.7 `SocialNetworkRuntime` owns informal groups and derives social graph projections.
- 12.8 `SocialDecisionRuntime` owns decision state and delegates execution.
- 12.9 `SocialInfluenceRuntime` owns influence attempts and decision modifiers.
- 12.10 `SocialEmotionRuntime` owns emotion episodes and moods.
- 12.11 `FamilyRelationshipRuntime` owns households and derives kinship from relationship and attitude records.

## Integration Facade

The facade provides bounded social context snapshots, health snapshots, validation reports, persistence dependency metadata, and Step 13 consequence references. It returns immutable projection records instead of raw runtime state. Callers receive stable record references, visibility classifications, projection state, source runtime summaries, diagnostics, truncation status, and a deterministic fingerprint.

The context snapshot is intentionally bounded. If more records exist than the configured limits, the snapshot is marked truncated and includes diagnostics. This keeps decision, UI, and future simulation callers from accidentally performing unbounded social graph reads.

## Transaction Boundary

`Step12SocialSimulationTransactionCoordinator` defines the cross-runtime transaction contract:

- `Preview` must be non-mutating.
- `Prepare` validates required participants before commit.
- `Commit` applies ordered runtime mutations.
- `Rollback` runs in reverse order after a required commit failure.
- `PostCommit` is best-effort and cannot fail the already committed transaction.
- Duplicate transaction IDs are idempotent after a successful commit.

This coordinator is a contract and guardrail for multi-runtime social flows. Individual runtimes still own their own records and validation.

## Scheduler Guardrails

`Step12SocialSimulationValidator` checks social scheduler safety rules:

- no zero or negative evaluation budget,
- bounded queued consequences,
- bounded recursion depth,
- no system time dependency,
- no immediate recursive dispatch,
- no persistence dependency cycles.

Future autonomous social simulation should feed deterministic world time and ordered evaluation batches through this boundary.

## Persistence Graph

The Step 12 persistence dependency graph is explicit. Social runtime participants are validated in dependency order against the owning runtime save data. The graph also references Step 8 knowledge, memory, history, and access dependencies where social data stores provenance, rumor evidence, memories, or access-aware projections.

Restore must remain prepare-before-commit. Failed validation must not mutate live runtime state or rebuild partial indexes.

## Step 13 Handoff

Step 13 should consume immutable social signals, not social runtime internals. The facade emits `Step12ConsequenceReference` values that carry source feature, source record ID, source transaction ID, destination runtime, destination record ID, operation, world time, revision, visibility, and active state.

Step 13 may use these references for law, factions, group behavior, conflict, or public consequences, but it should not become an owner of Step 12 records.
