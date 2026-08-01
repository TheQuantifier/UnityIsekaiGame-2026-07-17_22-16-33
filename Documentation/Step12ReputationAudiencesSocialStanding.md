# Step 12.3 - Reputation, Audiences, and Social Standing

`ReputationRuntime` owns shared audience-level reputation for a Person. It is separate from relationship records, interpersonal attitudes, legal/social statuses, and historical truth.

## Runtime Ownership

- `RelationshipRuntime` owns objective relationship records.
- `InterpersonalAttitudeRuntime` owns one Person's directional attitude toward another Person.
- `ReputationRuntime` owns aggregated or publicly attributed standing for a subject Person inside an audience.
- `ReputationPersistenceParticipant` saves world-scoped reputation state through the existing prepare/commit/rollback persistence pipeline.

The runtime stores stable reputation-record IDs, subject Person IDs, audience definition IDs, dimension values, source-owned contributions, revisions, and processed transaction IDs. It does not store scene objects, actor/body references, display names, or local-player-only state.

## Audiences

Audience definitions are `ReputationAudienceDefinition` assets or prototype fallbacks. An audience represents a social population or institutional viewpoint, not an individual observer.

Prototype audiences include:

- `reputation.audience.global-public`
- `reputation.audience.place.prototype-town`
- `reputation.audience.organization.adventurers-guild`
- `reputation.audience.organization.adventurers-guild.veterans`
- `reputation.audience.jurisdiction.prototype-kingdom`
- `reputation.audience.custom.hidden-investigators`

Audience-specific reputation does not automatically copy to another audience. Hierarchy-aware reads are explicit and deterministic; direct values take precedence over inherited parent values.

## Dimensions

Canonical dimensions are:

- `reputation.renown`
- `reputation.esteem`
- `reputation.notoriety`
- `reputation.credibility`
- `reputation.perceived-danger`
- `reputation.honor`

Renown is recognition, not approval. A subject can have high renown and negative esteem. Notoriety is not legal guilt. Perceived danger is not actual combat power. Credibility is not interpersonal trust.

## Value Model

For each subject Person and audience, there is at most one active reputation record. Effective values are deterministic:

```text
baseline or neutral value + ordered active source contributions, clamped to the dimension range
```

Neutral reads do not create records. Preview mutations roll back immediately and do not reserve transaction IDs. Executed transaction IDs are idempotent.

## Source Contributions

Source contributions are owned by stable source IDs. A source can be added, replaced, or removed without deleting unrelated sources. Contributions store category, authenticity classification, optional historical-event ID, optional supporting reference ID, timestamp, and amount.

Authenticity classifications include verified, alleged, disputed, fabricated, propaganda, outdated, and unknown. The runtime preserves these classifications but does not decide factual truth.

## Requirements

`ReputationThresholdRequest` evaluates equality, inequality, ordered comparisons, inclusive ranges, inherited or direct-only lookup, and optional minimum renown. Requirement evaluation is read-only and does not create records.

## Persistence

Reputation persists as shared-world simulation state. Prepare validates schema, record IDs, known subjects, audience definitions, dimensions, duplicate active subject-audience records, contribution ownership, and audience hierarchy. Commit restores from the prepared payload; rollback restores the previous live runtime if commit fails.

## Test Lab

Feature 12.3 automation is registered as:

```text
feature.12.3.reputation-audiences-social-standing
```

The suite covers readiness, record identity, dimension independence, audience independence and hierarchy, source contribution idempotence and dispute metadata, requirement evaluation, separation from relationships and attitudes, and persistence validation.

## Deferred Scope

This feature intentionally does not implement gossip propagation, rumor diffusion, individual recognition, automatic attitude conversion, legal guilt, law enforcement reactions, dialogue branching, pricing effects, UI discovery, aliases, disguise, or multiplayer replication. Those systems should later consume or contribute to `ReputationRuntime` through explicit APIs.
