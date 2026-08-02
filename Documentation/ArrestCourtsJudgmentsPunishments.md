# Feature 13.11 - Arrest, Detention, Courts, Judgments, and Punishments

Feature 13.11 adds the justice-process layer on top of Step 13 governments, laws, authority, diplomacy, and crime records. The owning runtime is `JusticeRuntime`; it stores court, arrest, custody, case, charge, plea, hearing, evidence-ruling, finding, judgment, sentence, remedy, appeal, and clemency records.

## Runtime Ownership

`JusticeRuntime` is the authoritative owner for justice-process records. It does not own crimes, laws, governments, organizations, or authority grants. Those systems remain separate dependencies and are consulted by ID during validation.

The runtime is configured with:

- `DefinitionRegistry`
- `GovernmentRuntime`
- `LegalRuntime`
- `OrganizationRuntime`
- `OrganizationAuthorityRuntime`
- `CrimeRuntime`
- world ID, known persons, and known places

All public snapshots are cloned before returning so callers cannot mutate live state.

## Definitions

`PrototypeJusticeDefinitionFactory` registers prototype-only definitions for:

- justice institutions
- general, appellate, and military courts
- warrant arrest, voluntary surrender, caught-in-act arrest, and military apprehension
- criminal and military charges
- initial hearings, evidence hearings, and trials
- fines, restitution, imprisonment, and probation
- release and property-return remedies
- judgment and sentence appeals
- pardons and commutations

The prototype definitions are added through the shared registry fallback path used by Test Lab and persistence, not by bypassing validation.

## Legal Process

The runtime supports:

- deterministic court selection by jurisdiction and requested court
- arrest only from an active legal basis such as a warrant
- custody creation, transfer, timed review/release metadata, and release orders
- case filing with explicit parties and incident references
- charge filing from crime potential-offense records
- pleas without implied judgment
- hearings with scheduled/opened/closed lifecycle
- evidence submission and admissibility rulings
- findings and judgment outcomes per charge
- sentence components, execution, remedies, appeals, stays, and clemency

The current custody transfer model mutates the custody holder and lifecycle state on the existing custody record. That preserves the custody record identity for Feature 13.11; a future richer facility chain can add transfer history records without changing the custody identity contract.

## Access And Persistence

Justice projections follow the Step 13 political visibility pattern:

- public records project fully
- restricted or confidential records project with redacted details
- secret, hidden, or development-only records are concealed
- privileged callers receive full cloned records

`JusticePersistenceParticipant` captures and restores identity-preserving save data. Prepare validation rejects corrupt graphs before commit and leaves the live runtime unchanged. Restore validates dependencies against the current registry, government, law, organization, authority, crime, world, person, and place context.

## Test Lab

The suite `feature.13.11.arrest-courts-judgments-punishments` covers:

- runtime and definition readiness
- court registration and selection
- warrant arrest, custody transfer, and release
- case, charge, plea, and hearing boundaries
- evidence ruling, finding, and judgment
- sentences, remedies, appeals, and clemency without rewriting judgments
- redacted projections and persistence rejection

The fixture system now includes `TestLabRuntimeArea.Justice`, so command-side and scene-hosted automation resolve the same justice runtime from the same fixture graph.

## Validation

Feature-specific validation passed:

- Edit Mode: 6 passed, 0 failed, 0 skipped
- Automation: 7 passed, 0 failed, 0 error, 0 skipped

Full validation passed after integration:

- Edit Mode: 1056 passed, 0 failed, 0 skipped
- Automation: 619 passed, 0 failed, 0 error, 0 skipped
