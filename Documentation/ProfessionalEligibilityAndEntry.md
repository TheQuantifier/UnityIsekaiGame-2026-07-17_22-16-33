# Professional Eligibility and Entry

Feature 10.2 adds the first profession-entry layer on top of Step 10.1 profession identity.

The system intentionally separates these contracts:

- `ProfessionEntryPathDefinition` describes an authored way into a profession or specialization.
- `ProfessionEligibilityResult` is a non-mutating answer to “may this Person enter this path now?”
- `ProfessionEntryRequestData` is the persisted formal application or recognition request.
- `PersonProfessionRuntime` remains the owner of actual profession relationships.
- Recognition marks a profession relationship as formally recognized, but does not grant skill, knowledge, credentials, rank, title, employment, organization membership, or competence.

## Entry Paths

Entry paths are catalog definitions with stable IDs under `profession-entry.*`. They reference one `ProfessionDefinition`, optionally one `ProfessionSpecializationDefinition`, and may use the existing shared `RequirementSetDefinition` framework.

They also carry profession-specific requirements that do not yet have global owning systems, such as authority IDs, organization keys, access keys, reentry policy, active-profession conflicts, and specialization parent rules. Those are evaluated as stable context keys, not as trusted arbitrary booleans.

Prototype fallback definitions live in `PrototypeProfessionDefinitionFactory`, which is now the shared source for prototype professions, specializations, access policies, and entry paths.

## Eligibility

`ProfessionEntryRuntime.Evaluate` is read-only. It returns copied requirement results, failure reasons, conflicts, and a runtime token. The token captures profession-state revision plus the normalized context hash so a later mutation can reject stale evaluations.

Authoritative and perceived eligibility remain separate by flag. The current runtime supports perceived results without mutating owner runtimes; future perception systems can feed redacted or incomplete context into the same evaluator.

## Informal Entry

Informal self-declaration commits through `PersonProfessionRuntime.AddRelationship`.

It can create practicing, self-declared, secret, disputed, restricted, or illegal profession relationships depending on the entry path and profession definition. It does not grant recognition, credentials, ranks, titles, employment, skills, knowledge, traits, capabilities, or organization membership.

## Formal Requests

Formal entry is a persisted request lifecycle:

- `Submitted`
- `UnderReview`
- `Approved`
- `Rejected`
- `Withdrawn`
- `Expired`
- `Cancelled`
- `Invalid`

Submission validates eligibility and stores the normalized evaluation context needed for later approval revalidation. Approval rejects stale tokens before committing the recognized profession relationship.

## Specialization And Reentry

Specialization paths require the parent profession to be active unless the path explicitly disables that rule. Reentry paths accept inactive, former, abandoned, or retired relationships according to the path policy. Revoked and suspended relationships require explicit compatible policies.

## Persistence

`ProfessionEntryPersistenceParticipant` saves only formal-entry request state. It depends on person identity and profession relationships. Restore validates all entry path references, applicant IDs, request state, authority compatibility, and approved relationship links before commit. Restore does not replay history hooks.

Older saves naturally load with no profession-entry participant state.

## Access

Entry paths, eligibility results, formal requests, recognition decisions, and reentry events expose Step 8 subject references through `ProfessionEntryInformationSubject`. Request projections can return full, redacted, concealed, or denied views without returning raw request data first.

## Deferred

Feature 10.2 does not implement exams, full training programs, credentials, competency awards, employment, ranks, salaries, autonomous NPC career decisions, legal authority systems, or organization-membership ownership.
