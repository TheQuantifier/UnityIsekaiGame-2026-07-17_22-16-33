# Qualifications, Credentials, and Certification

Feature 10.5 adds a credential layer for qualifications, applications, examinations, issuing authorities, authoritative credential records, and credential lifecycle state.

## Ownership Boundaries

Credentials do not own profession identity, training, experience, skills, knowledge, capabilities, rank, title, employment, or organization membership. Those systems remain authoritative for their own records.

Credential qualification evaluation reads:

- `PersonProfessionRuntime` for active and recognized profession relationships.
- `TrainingRuntime` for completed programs.
- `ProfessionalActivityRuntime` for validated professional experience.
- `CredentialRuntime` for passed examinations and existing credential state.

Evaluation is read-only. It produces a snapshot with dependency revisions so later approval or issuance can reject stale decisions.

## Definitions

`CredentialDefinition` is catalog-authored and immutable at runtime. It declares the profession or specialization relationship, required training, required professional experience, required examinations, authorized issuers, granted permission identifiers, lifecycle policies, visibility, and persistence validation rules.

`CredentialExaminationDefinition` is catalog-authored and immutable at runtime. It declares the credential definitions it can satisfy, assessment category, passing score, attempt limits, evaluator authorities, and any knowledge, skill, capability, or practical activity references needed by the assessment.

Prototype definitions are registered through `PrototypeProfessionDefinitionFactory` so Test Lab, persistence, and catalog validation all resolve the same source of truth.

## Runtime Flow

`CredentialRuntime` supports:

- Qualification evaluation.
- Application submission, evidence requests, approval, rejection, and withdrawal.
- Examination attempt recording.
- Credential issuance.
- Expiration, renewal, suspension, reinstatement, revocation, surrender, replacement, dispute, and forged-claim recording.
- Permission checks against active authoritative credentials.
- Access-aware redacted projections.
- Save and restore validation.

Applications and issuance revalidate current qualification snapshots. If the underlying profession, training, activity, or credential revision changed, approval and issuance reject stale snapshots instead of committing partial state.

## Authenticity

Forged documents and false credential claims are represented as `ForgedClaimFoundation` records with `ForgedClaim` authenticity. They never become authoritative credentials, never satisfy active permission checks, and are validated separately from real issued records.

## Persistence

Credential persistence is handled by `CredentialPersistenceParticipant`.

Restore validation rejects:

- Unsupported schema versions.
- Missing or duplicate application, examination attempt, or credential IDs.
- Unknown people or issuers.
- Missing credential or examination definitions.
- Forged credentials masquerading as authoritative active records.
- Credentials missing required application references.
- Missing linked applications or examination attempts.
- Duplicate registration numbers where uniqueness is required.

Restore is atomic. Failed restore leaves the existing credential runtime unchanged and does not replay credential history hooks.

## Test Lab

The Feature 10.5 suite is `feature.10.5.qualifications-credentials-certification`.

It covers definition validation, qualification evaluation, applications, examinations, issuance, stale qualification rejection, unauthorized issuers, forged claims, lifecycle permission behavior, redacted projection, and persistence restore rejection.

Credential runtime is part of `TestLabRuntimeBundle`, so fresh runtime scenarios and snapshot restore scenarios include credentials in mutation auditing and deterministic fingerprints.

## Deferred

Feature 10.5 does not implement professional ranks, mastery levels, employment, organizational offices, final title authority, promotion pipelines, NPC career decisions, multiplayer account permissions, or legal court systems. Those later systems should consume credential state and permission checks instead of being implemented inside credentials.
