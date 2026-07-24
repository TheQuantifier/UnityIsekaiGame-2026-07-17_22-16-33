# Secrets, Visibility, and Information Access

Feature 8.8 adds a shared access boundary for knowledge-facing systems without replacing the existing Knowledge, History, Memory, Source, or Transfer runtimes.

## Ownership

`InformationAccessRuntime` owns access policy state, grants, denials, concealments, classification changes, audit records, and persistence data. It does not own facts, beliefs, memories, historical events, information sources, or transfer records. Those systems remain authoritative for their own records and should query access decisions before exposing protected information.

## Subjects

Protected data is addressed through typed `InformationSubjectReferenceData` values. A subject includes a subject type, stable subject ID, optional parent subject, owner person, controlling entity, and tags. This avoids treating every protected record as a plain string and lets future systems distinguish facts, memories, source identity, transfers, life events, diagnoses, previous-body continuity, legal records, organizations, and custom subjects.

## Policies

`InformationAccessPolicyDefinition` is the authored policy definition. Runtime policy data records the concrete protected subject, classification, disclosure policy, resharing policy, source visibility policy, detail visibility policy, audit policy, and default visible/redacted/hidden detail IDs.

Public information can be inspected without becoming automatically known. Private and secret information requires contextual access, explicit grants, or privileged validation/debug/persistence context. Source identity and detail projection are separate decisions from the basic allow/deny result.

## Grants, Denials, and Concealment

Access grants are explicit and scoped to a person, organization, role, title/status, token, public access, or custom grantee. Grants can allow modes such as inspect, share, reshare, reveal source, or reveal details, and they can expose only selected detail IDs.

Denials take precedence over grants. Concealments can hide the existence of a record or specific sensitive details unless an authorized exception token is present.

Revoking a grant affects future access only. It does not erase already formed knowledge, memories, evidence, source records, or transfer records.

## Persistence

`InformationAccessPersistenceParticipant` persists player-scoped access state using the `person.information-access` participant key. Restore uses prepare/commit behavior and validates schema version, owner, policies, grants, denials, and concealments before mutation. Failed restore leaves live access state unchanged.

The participant is optional and has optional dependencies on knowledge, memory, source, and transfer participants. Restore emits no discovery, sharing, reveal, or audit side effects.

## Integration Boundary

Feature 8.8 provides the common policy and decision layer. Existing callers can migrate incrementally by asking `InformationAccessRuntime` for an `InformationAccessDecision` or `RedactedInformationProjection` before exposing data.

`InformationTransferRuntime` now has an optional integration hook on `InformationTransferRequest`. When an access runtime and policy are supplied, sender access is evaluated with `Share` or `Reshare` mode before transfer records are created. Requests that do not provide an access runtime retain their existing Feature 8.7 behavior while callers are migrated.

Non-transfer callers now have access-aware projection adapters:

- `AuthoritativeHistoryRuntime.GetHistoryProjection` and `GetBiographyProjection` protect historical events, life events, and biography views before returning displayable records.
- `PersonMemoryRuntime.GetMemoryProjection` and `GetRecallProjection` separate owner recall state from another requester's authority to inspect stored memory records.
- `PersonKnowledgeRuntime.GetKnowledgeProjection` and `QueryKnowledgeProjections` redact propositions, confidence, evidence, sources, and context without moving belief ownership into the access runtime.
- `InformationSourceRuntime.GetSourceProjection` and `GetSourceChainProjection` protect source identity, original-source visibility, and provenance-chain details.

These adapters resolve the authoritative record from the owning runtime first, construct a typed access subject, evaluate access, and then return a full, redacted, concealed, or denied projection. They do not mutate owning records, create knowledge, alter recall metadata, or record gameplay audits unless a caller explicitly asks for auditing.

Raw query APIs remain available for internal owner operations, persistence, validation, migrations, and privileged debug tooling. Ordinary gameplay-facing code should use the access-aware projections when inspecting protected records owned by another person or system.

Current prototype integration is available through Test Lab section `Access 8.8` and automation suite `feature.8.8.secrets-visibility-information-access`. The `Adapters` action verifies history, biography, memory, knowledge, source, and provenance projections through the shared access runtime.

## Deferred

Role, organization, legal authority, profession, consent, and server-authoritative multiplayer ownership hooks are represented as IDs and context fields only. They are intentionally not implemented as full organization, law, network, authentication, or database systems in this feature.
