# Step 12.4 - Rumors, Gossip, and Social Knowledge Propagation

Feature 12.4 adds an authoritative rumor runtime for transmissible social claims. A rumor is not canonical truth: history still owns what happened, knowledge owns each person's evidence and belief, memory owns recollection, and relationship, attitude, and reputation runtimes remain separate social authorities.

## Runtime Ownership

`RumorRuntime` owns:

- stable root rumor records and derived rumor versions;
- structured rumor claim payloads backed by the existing knowledge proposition model;
- origin metadata, source attribution, disclosure, authenticity, confidence, salience, and memorability;
- transmission records describing speaker, listener, channel, time, outcome, resulting version, evidence, belief, and memory references;
- deterministic propagation order, bounded fan-out, awareness indexes, and propagation metrics;
- save/restore snapshots and idempotent transaction history.

It does not own:

- authoritative historical events;
- person belief or evidence state;
- person memories;
- relationship records;
- interpersonal attitudes;
- reputation or standing changes.

Those systems can be affected only through their existing public operations. Reputation, attitude, or relationship changes from rumors must be implemented as explicit future bridge operations, not as implicit rumor side effects.

## Definitions

Rumor behavior is definition-backed through:

- `RumorDefinition`
- `RumorCommunicationChannelDefinition`
- `PrototypeRumorDefinitionFactory`

Definitions configure categories, disclosure defaults, salience, memorability, transmission difficulty, distortion policy, retransmission, anonymous source handling, source concealment, and validation tags. Definitions are immutable configuration; mutable rumor instances live only in `RumorRuntime`.

Prototype fallback definitions are registered through the shared definition factory path so Test Lab, persistence, and runtime validation see the same definitions.

## Knowledge And Memory Integration

Rumor transmission uses existing knowledge and memory runtimes:

- listener evidence is recorded with `KnowledgeAcquisitionSource.Testimony`;
- belief direction and strength are derived from the transmission outcome;
- listener memory records that the rumor was heard without creating a separate memory system;
- preview validation happens before any mutation, so failed memory or knowledge writes cannot leave partial rumor-side mutations.

A person can hear or remember a rumor without the rumor becoming true. Later evidence can confirm or contradict the same claim through normal knowledge operations.

## Distortion And Propagation

Derived rumor versions preserve:

- the original root rumor ID;
- parent rumor lineage;
- unchanged claim identity unless a future structured claim operation explicitly changes it;
- deterministic distortion operations and confidence adjustments.

Propagation is deterministic and bounded. Listener IDs are de-duplicated, sorted ordinally, and capped by the request limit. This makes command-side automation, Test Lab automation, and persistence replay stable.

## Persistence

`RumorPersistenceParticipant` persists world-scoped rumor state under `world.rumors`. It validates all rumor definitions, channels, person references, claims, root/parent lineage, transmission references, and transaction records before commit. Restore uses prepare/commit rollback semantics so corrupt payloads cannot partially mutate live runtime state.

## Test Lab

Feature 12.4 registers the Test Lab suite:

`feature.12.4.rumors-gossip-social-knowledge-propagation`

The suite covers:

- definition readiness and root identity;
- listener knowledge and memory effects;
- derived version distortion lineage;
- bounded deterministic propagation;
- separation from relationship, attitude, and reputation runtimes;
- persistence and corrupt-restore rejection.

The fixture system includes rumor snapshots and fingerprints, so undeclared rumor mutations are visible to automation ownership checks.
