# Historical Records, Journals, and Codex

Feature 8.9 adds a record and projection layer for player-facing knowledge records. It does not become the authority for facts, beliefs, memories, history, sources, transfers, or information access.

## Ownership Boundaries

- Knowledge, Memory, History, Information Source, Information Transfer, and Information Access runtimes remain the owners of their own records.
- `KnowledgeRecordRuntime` stores explicit record entries such as journals, codex entries, medical notes, investigation notes, and preserved historical records.
- Live projections can be previewed or copied into explicit records, but live projections themselves are not persisted as authoritative state.
- Projection and privileged inspection are non-mutating read-only views.
- Reading a record as a Person is an explicit operation that may create reader-owned source, evidence, and memory effects without mutating authoritative History or forcing truth.

## Reading Effects

`KnowledgeRecordRuntime.ReadRecordAsPerson` is the high-level read operation for gameplay-context record reading.

It performs these steps:

- validates reader and record identity;
- projects the record through `InformationAccessRuntime`;
- rejects denied access before side effects;
- registers the record as the reader's immediate `InformationSource`;
- preserves source lineage by transforming from an existing referenced source when available;
- creates reader-owned `PersonKnowledgeRuntime` evidence when a valid proposition is available or explicitly supplied;
- forms or reinforces a `PersonMemoryRuntime` memory of reading;
- recovers an existing forgotten read-memory when the same record is read again;
- rolls back source, evidence, and memory changes if any required side effect fails.

The read operation deliberately does not compare against authoritative truth unless a future caller supplies an explicit truth-authorization path. Record evidence may increase confidence, preserve uncertainty, preserve correction/dispute metadata in the record, and support or correct a proposition, but it does not automatically make the reader's belief true.

These paths remain separate:

- **Preview/project:** no effects.
- **Privileged inspection:** no effects.
- **Read as Person:** may create source, evidence, and memory effects.
- **Restore:** never creates reading effects.

## Record Definitions

`KnowledgeRecordDefinition` describes allowed record shape:

- category;
- allowed subject types;
- allowed owner kinds;
- default projection kind;
- persistence policy;
- optional access policy;
- indexing, sorting, grouping, discovery, redaction, uncertainty, correction, and revision flags.

Prototype definitions are currently registered through `PrototypeKnowledgeRecordDefinitionFactory`. Test Lab and the prototype persistence service both use this shared source, so records created during prototype automation can be validated during save capture and restore. Catalog-authored definitions remain authoritative: if the prototype catalog later provides the same stable ID, the generated prototype fallback is skipped.

Missing definitions are still a hard validation failure. The fallback exists only to keep prototype-only 8.9 record definitions available to the prototype registry paths that create and persist them; it does not allow records to save without a valid `KnowledgeRecordDefinition`.

Production content should move canonical authored definitions into catalog assets when content authoring begins.

## Supported Prototype Categories

- Journal;
- Historical Record;
- Biography;
- Bestiary;
- Location and Map Discovery;
- Medical and Diagnosis;
- Investigation, Evidence, and Source;
- Organization and Faction;
- Quest Log;
- Custom.

## Access

Records project through `InformationAccessRuntime` when a caller is not privileged. Projection can return:

- full access;
- redacted access;
- partial access;
- denied access with no visible record ID.

Denied projections must not leak hidden record IDs, hidden detail values, provenance chains, or subject existence unless an access policy explicitly permits that.

## Persistence

Only explicit records and collections are persisted by `KnowledgeRecordPersistenceParticipant`.

The participant is player-scoped, optional, and depends on the Step 8 systems only as optional ordering dependencies. Restore uses prepare/commit behavior and rejects corrupt payloads without mutating the live runtime.

Version 1 stores:

- owner ID;
- record revision;
- explicit records;
- collections;
- processed transaction IDs for idempotence.

## Test Lab

The Test Lab `Records 8.9` page can create representative records, validate definitions, verify owner reading effects and denied reads, create corrections and collections, search records deterministically, test live projection boundaries, and validate save/restore.

Automation suite:

`feature.8.9.historical-records-journals-codex`

## Manual Verification

1. Open PrototypeScene and press Tab.
2. Open Test Lab, then Knowledge Step 8, then Records 8.9.
3. Click Validate Defs and confirm Success.
4. Create Journal, History, Biography, Bestiary, Location, Medical, and Investigation records.
5. Click Read Owner and confirm the owner can read a record and receives source/evidence/memory IDs.
6. Click Deny Read and confirm the unauthorized projection does not reveal a visible record ID.
7. Click Correct and confirm the original remains auditable.
8. Click Collection and Search.
9. Click Projection and confirm preview does not mutate Knowledge, Memory, or History revisions.
10. Click Save/Restore.
11. Run 8.9 Auto.
