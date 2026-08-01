# Step 12.5 Social Interactions And Relationship Evolution

Feature 12.5 adds an authoritative `SocialInteractionRuntime` for transactional social actions between people. The runtime owns interaction records, pending responses, promise records, processed transaction IDs, and cooldown state. It does not own relationship, attitude, reputation, rumor, knowledge, memory, or history records.

Social interaction definitions are immutable content records under the `social-interaction.` namespace. Prototype fallbacks are supplied by `PrototypeSocialInteractionDefinitionFactory` and are registered through the same definition-registry path used by Test Lab and persistence. Catalog-authored definitions remain authoritative when present.

## Runtime Boundaries

`SocialInteractionRuntime` resolves one request into a deterministic record and consequence plan. Consequences are then delegated to the owning runtime:

- interpersonal effects go to `InterpersonalAttitudeRuntime`;
- relationship creation goes to `RelationshipRuntime`;
- public or witnessed standing changes go to `ReputationRuntime`;
- shared information goes to `RumorRuntime`;
- memory and history consequences are stored as stable references only;
- promises are stored locally as interaction-owned future obligations.

Preview uses the same validation and planning path as execution but does not mutate runtime or delegated state. Execution is idempotent by transaction ID. Duplicate requests return the original record without replaying delegated consequences.

## Persistence

`SocialInteractionPersistenceParticipant` saves only the social interaction runtime graph:

- interaction records;
- pending interactions;
- promises;
- processed transaction ledger;
- cooldown entries.

Restore validates schema version, referenced interaction definitions, required participants, known people, pending-record links, promise-record links, transaction-record links, and cooldown-record links before commit. Invalid payloads are rejected without mutating live runtime state.

## Test Lab

Feature 12.5 is registered as `feature.12.5.social-interactions-relationship-evolution`. Automation covers:

- definition readiness and preview without mutation;
- attitude-producing social interactions;
- pending response and promise creation;
- public and witnessed reputation effects;
- rumor delegation through shared information;
- persistence round trip and rejected corrupt restore.

The command-side and in-game automation runners use the same automation catalog, runtime bundle, fixture ownership, and reset path.
