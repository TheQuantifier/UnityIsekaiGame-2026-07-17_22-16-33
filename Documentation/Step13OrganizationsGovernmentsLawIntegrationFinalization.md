# Step 13 Organizations, Governments, Law, and Justice Integration Finalization

Feature 13.12 finalizes Step 13 as an integrated institutional layer. It does not replace the Feature 13.1 through 13.11 runtimes with a central state owner. Instead, it adds a bounded integration facade, validation model, action-gate contract, deterministic projection snapshot, transaction coordinator, tests, and Test Lab automation that make ownership and cross-runtime boundaries explicit.

## Runtime Ownership

Step 13 authoritative ownership remains distributed:

- `OrganizationRuntime`: organization identity, hierarchy, aliases, lifecycle, and visibility.
- `OrganizationMembershipRuntime`: memberships, rank assignments, offices, and office assignments.
- `OrganizationAuthorityRuntime`: institutional permissions, authority grants, delegations, and approvals.
- `OrganizationResourceRuntime`: organization treasuries, accounts, budgets, resource restrictions, custody, and resource metadata.
- `OrganizationDecisionRuntime`: goals, policies, proposals, votes, resolutions, and execution plans.
- `FactionRuntime`: faction identity, affiliation, roles, platforms, influence, splits, and mergers.
- `DiplomacyRuntime`: diplomatic actors, recognition, relations, agreements, breaches, wars, ceasefires, and peace.
- `GovernmentRuntime`: polities, governments, territories, sovereignty, control, administration, seats, and jurisdictions.
- `LegalRuntime`: legal authorities, instruments, provisions, applicability, rights, permits, duties, exemptions, immunity, citizenship, residency, and legal status.
- `CrimeRuntime`: incidents, reports, allegations, suspects, evidence links, investigations, warrants, wanted status, notices, and risk assessment.
- `JusticeRuntime`: courts, arrest/custody records, cases, charges, hearings, rulings, findings, judgments, sentences, remedies, appeals, stays, remands, and clemency.

Adjacent systems stay authoritative for their own records. Step 13 only references or derives from them:

- `EconomyRuntime`: financial transaction history and currency movement.
- `PropertyRuntime`: property title and ownership.
- `BusinessRuntime`: business ownership and operation.
- `ItemInstanceIdentityRuntime`: item instance identity.
- Step 8 information access and history systems: visibility, redaction, knowledge-safe views, and historical events.
- Step 12 social systems: reputation, relationships, influence, emotions, family, and households.

## Integration API

`Step13InstitutionalIntegrationFacade` is the single production-facing integration surface for finalization checks. It provides:

- `OwnershipMap`: the authoritative domain owner table, including derived external handoffs.
- `PersistenceDependencies`: the Step 13 restore-order graph.
- `CreateRuntimeSummaries()`: deterministic runtime readiness/count/revision summaries.
- `ValidateComplete()`: ownership, dependency, scheduler, runtime-readiness, and save-graph validation.
- `CreateReadinessSnapshot()`: immutable combined Step 13 readiness status and fingerprint.
- `EvaluateProtectedAction(...)`: stable institutional action gate evaluation.
- `CreateInstitutionalContextSnapshot(...)`: bounded deterministic projections across Step 13 runtimes.

The facade does not mutate records and does not own Step 13 domain state.

## Protected Action Order

Protected institutional actions are evaluated in a stable gate order:

1. Identity
2. Authority
3. Jurisdiction
4. Legality
5. Domain ownership
6. Consent context
7. Resource availability
8. Explicit world time
9. Prepared

This keeps institutional authority separate from legal permission. A guild officer can have internal authority without legal jurisdiction, and a legal actor can have legal jurisdiction without organization-resource authority.

## Persistence and Restore

The Step 13 persistence dependency graph is explicit and acyclic:

- Organizations restore before memberships.
- Memberships restore before authority.
- Authority restores before resources, decisions, crimes, and justice.
- Resources and decisions restore before factions and diplomacy.
- Governments restore after organization and diplomacy context.
- Laws restore after governments and institutional authority.
- Crimes restore after laws, governments, authority, and diplomacy.
- Justice restores after crimes, laws, governments, organizations, and authority.

The validator checks missing participant keys, duplicate participants, self-dependencies, dependency cycles, runtime presence, runtime save data, and cross-runtime save-graph validity.

## Transactions

`Step13InstitutionalTransactionCoordinator` provides a small, deterministic transaction boundary for cross-runtime workflows. It supports preview, prepare, commit, rollback, post-commit, required/optional participant policy, and duplicate transaction suppression. Failed required commits trigger rollback in reverse participant order.

## Projection and Visibility

Institutional context snapshots are immutable read models. They collect bounded references from the owning runtimes, sort them deterministically, include runtime summaries, carry projection visibility, and fingerprint the result. They do not expose mutable runtime collections and do not create knowledge, memories, history records, or gameplay audits.

Visibility ownership remains with the source runtime and Step 8 access systems. Step 13 projections classify records as public, participant, knowledge-safe, privileged, redacted, concealed, or diagnostic without becoming the owner of privacy policy.

## Validation and Automation

Feature 13.12 adds Edit Mode coverage for:

- complete readiness and ownership graph validation;
- action gate ordering and denial behavior;
- immutable deterministic projections;
- scheduler and dependency validation failures;
- transaction preview, rollback, and idempotence.

It also adds a Test Lab suite:

`feature.13.12.organizations-governments-law-integration-finalization`

The suite validates readiness, action gate ordering, projection snapshots, transaction atomicity, and handoff boundaries through the normal Test Lab automation catalog, so command-line and in-game automation share one source of truth.

## Step 14 and Step 15 Handoff

Step 13 exposes stable references and immutable signals for later travel, geography, quests, narrative, and UI work. It does not implement autonomous political AI, policing AI, legal UI, travel, narrative orchestration, multiplayer permissions, or account-level authorization. Those future systems should call into Step 13 through typed references, action contexts, and projection snapshots rather than duplicating institutional state.
