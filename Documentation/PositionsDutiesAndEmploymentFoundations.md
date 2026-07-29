# Positions, Duties, and Employment Foundations

Feature 10.7 connects a Person to exact organizational positions, duties, employment state, and position-derived authority. It intentionally keeps profession identity, rank, credential, title, capability, knowledge, and compensation in their owning systems.

## Profession Versus Position

A profession is a field of practice, such as blacksmithing. A position is an organizational job or office, such as Royal Forge Senior Smith. A Person may be a blacksmith without being employed, and a Person may hold an administrative position unrelated to their primary profession.

## Position Definitions and Instances

`PositionDefinition` is catalog-authored and immutable. It describes reusable requirements, duties, authorities, capacity, vacancy policy, classification, access policy, and Step 11 compensation references.

`PositionInstanceData` is runtime-owned. It represents one exact position in one organization, tracks vacancy and holder state, reporting links, provenance, and revision. Position instances belong to organizations, not to the Person occupying them.

## Duties

`DutyDefinition` describes a typed responsibility for a position. `DutyAssignmentData` is runtime-owned and links an active employment record to a duty definition. Duty completion must reference authoritative activity evidence when the duty requires evidence.

Feature 10.7 does not implement daily scheduling, attendance, or autonomous task execution.

## Eligibility

`PositionEmploymentRuntime.EvaluateEligibility` is read-only. It evaluates profession, specialization, rank, credential, training, experience, capacity, duplicate holder, and compatible-employment requirements against the owning runtimes. Authoritative and perceived eligibility are separate snapshots so hidden requirements can be redacted without mutating employment state.

## Applications, Offers, and Appointments

Applications and offers are persistent request records. Appointment revalidates the submitted eligibility snapshot, checks authority, checks capacity, creates employment, updates the position holder list, and commits atomically. Stale eligibility, missing authority, bad capacity, and invalid references are rejected before mutation.

## Multiple Positions

A Person may hold multiple compatible positions. Shared positions can have multiple holders. Exclusive full-time positions block other exclusive full-time appointments. The same exact position cannot be assigned twice to the same Person.

## Authority Grants

Authority is resolved from active employment and the active position definition. Suspended, resigned, dismissed, retired, contract-ended, or former employment does not provide active authority. Authority is not copied into unrelated systems.

## Reporting Relationships

Positions can report to exact supervisor positions. Reporting validation rejects self-supervision and cycles. The foundation is intentionally narrow and does not attempt to become a full organization-chart editor.

## Vacancies and Staffing

Vacant, partially filled, filled, and closed positions remain queryable. Closing a staffed position requires explicit holder transition policy. Vacant positions remain valid organization records.

## Lifecycle

The runtime supports suspension, reinstatement, resignation, dismissal, contract end, retirement, transfer foundations, and position closure. Historical employment records remain queryable and duties are preserved as history.

## Boundaries

Employment does not grant profession, skill, knowledge, capability, credential, rank, title, or compensation. Profession identity does not guarantee employment. Compensation policy, payment schedule, wage, benefit, cost center, contract, and commission fields are references only; Step 11 owns economic implementation.

## Knowledge, Access, and History

Positions, vacancies, employments, applications, offers, appointments, duties, reporting relationships, lifecycle events, and disputes expose Step 8 information subjects. Public projections redact secret or confidential details. History hooks are emitted only after successful runtime commits and are not replayed during restore.

## Persistence

`PositionEmploymentRuntimeSaveData` persists position instances, applications, employment records, duties, reporting links, lifecycle state, authority references, capacity, access policies, provenance, and revisions. Definitions remain catalog-authored. Restore validates references before commit, rebuilds indexes, preserves revisions, does not replay effects, and leaves live state unchanged after corrupt restore.

## Validation

Catalog validation covers stable ID namespace, missing profession, specialization, rank, credential, training, duty, invalid organization type, invalid authority grant, invalid capacity, and invalid version. Runtime validation covers unknown person, unknown organization, unknown position definition, bad capacity, duplicate IDs, missing supervisors, reporting cycles, invalid applications, stale eligibility, missing duty definitions, invalid duty-position pairs, and invalid evidence.

## Test Lab

Feature 10.7 automation is registered under `feature.10.7.positions-duties-employment-foundations`. It uses fixture-owned runtime bundles, run-scoped IDs, fresh runtime isolation, shared definition catalog lookup, and command-line automation support.

## Deferred Features

Wages, salaries, payroll, benefits, taxes, labor-market search, job advertisements, negotiation, full work schedules, attendance, autonomous NPC duty execution, final employment UI, and deeper career transitions remain deferred to Feature 10.8, Feature 10.9, Feature 10.10, Step 11, or later systems.
