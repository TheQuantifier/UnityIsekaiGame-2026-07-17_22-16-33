# Feature 8.7 - Information Sharing and Teaching

Feature 8.7 adds a production runtime for deliberate information transfer between Persons. It connects existing Person Knowledge, Memory, History, and Information Source systems without making transfer state authoritative for truth.

## Ownership

`InformationTransferRuntime` owns transfer audit records and processed transfer transactions. It does not own objective truth, authored facts, source reliability, belief revision, memory state, skills, capabilities, relationships, dialogue, quest progress, or reputation.

The transfer runtime calls:

- `PersonKnowledgeRuntime` to preview or record recipient evidence;
- `PersonMemoryRuntime` to form communication or teaching memories;
- `InformationSourceRuntime` to register transfer sources, transformations, source chains, and reliability-adjusted evidence strength.

## Transfer Definitions

`InformationTransferDefinition` is the authored policy for a transfer method. Definitions use canonical IDs such as `information-transfer.direct-testimony` and declare:

- transfer mode;
- supported Knowledge domains;
- allowed source categories;
- recall requirements;
- summary, translation, and demonstration support;
- default fidelity, completeness, and evidence strength;
- evidence and memory policies.

Prototype Test Lab definitions are transient development fixtures. Current production persistence records transfer definition IDs only when a definition is explicitly supplied.

## Content

Transfer content is structured through `TransferContentItemData`. A content item can reference a proposition, belief, evidence, memory, historical event, life event, source identity, diagnosis, concept, procedure, warning, or custom payload.

Content records separate the claim from transfer imperfections:

- deliberate falsehood;
- omitted details;
- distortion;
- summary or translation loss;
- intended understanding;
- claimed certainty and source IDs.

## Sender Boundaries

When content is not flagged as authorized falsehood, the sender must have accessible Knowledge for the proposition if a sender Knowledge runtime is supplied. Recall-required transfers use `PersonMemoryRuntime.Recall` in preview mode with metadata mutation disabled, so blocked or forgotten memories prevent the transfer without reinforcing or changing the sender memory.

Execution intentionally does not mutate sender recall metadata. A transfer records that recall was required and whether recall succeeded, but it does not increment recall count, update last-recall time, or reinforce the sender memory. Future narrative systems can add explicit "recall as action" behavior separately if needed.

## Recipient Effects

Recipient Knowledge and Memory runtimes are optional. This lets the system audit transfers involving unloaded or offline Persons. When runtimes are supplied, execution can create recipient evidence and communication memory. Preview uses the same path but does not mutate recipient Knowledge, Memory, Sources, transfer revisions, or processed transactions.

Multi-recipient execution is batch atomic. If any recipient fails during source creation, Knowledge application, or memory formation, all dependent runtime mutations from the transfer are rolled back:

- no transfer audit record is committed;
- no transfer source-chain node remains;
- no recipient evidence or belief mutation remains;
- no recipient memory remains;
- recipient and source indexes return to their pre-transfer state.

This is intentionally stricter than per-recipient success because transfer records are currently authored as one logical communication act. Later dialogue systems can layer separate per-recipient transfer requests when partial delivery is desired.

## Source Lineage and Confidence

Transfers can create a new information source or transform an existing source. Source chains preserve immediate and original source identity. Recipient confidence is based on raw transfer strength, source reliability, and understanding state. A copied, summarized, translated, anonymous, or hidden-source transfer remains distinguishable from firsthand observation.

Reshares transform the immediate source and remain dependent on the original source. A resharer is not treated as an independent original witness unless a separate firsthand source is registered. Distortion, omission, summary, and translation flags remain attached to the reshare record.

## Teaching

Teaching transfers can communicate concepts, procedure references, demonstrations, and guided-practice style payloads. They intentionally do not grant skills, capabilities, traits, or proficiency ranks. Future learning systems can consume transfer records and memories as inputs.

Understanding and acceptance are separate. A recipient may receive and remember a communication while only partially understanding it, misunderstanding it, treating it as low-confidence evidence, or learning source identity without accepting the proposition.

## Clarifications, Corrections, and Retractions

Clarifications use `parentTransferId` and create a new auditable transfer. Corrections use `correctionOfTransferId` and create new recipient-owned evidence; they do not delete the original transfer, original memory, or old evidence. Retractions use `retractionOfTransferId` and are represented as distinct audit records. Runtime and save validation reject self-references and circular transfer chains.

## Persistence

`InformationTransferPersistenceParticipant` persists Person-scoped transfer audit data under `person.information-transfers`. Restore validates the payload and rebuilds audit state without replaying evidence, memory formation, source registration, or gameplay events.

Production transfers should use persistent registered `InformationTransferDefinition` assets and store their stable definition IDs. Test Lab-only fixture transfers validate transient development definitions separately and omit those transient IDs from audit records so restore does not depend on non-catalog fixtures. Runtime behavior after restore depends on saved audit fields, not on re-executing the transfer definition.

## Test Lab

The Test Lab adds `Sharing 8.7` under `Knowledge Step 8`. It includes actions for true fact sharing, false belief sharing, recall-required sharing, suppressed-memory rejection, source lineage, inherited confidence, teaching, demonstration, resharing, omissions, corrections, privacy scopes, and save/restore.

## Deferred

The feature does not implement dialogue UI, NPC AI, social reputation, language fluency progression, pedagogy skill advancement, multiplayer authority, networking, or automatic quest/relationship consequences.
