# Feature 13.5 - Organizational Goals, Policies, Proposals, and Internal Decisions

Feature 13.5 adds the authoritative governance-decision runtime for organizations. It models what an organization is trying to accomplish, which internal policies are active, which proposals are pending, how votes and resolutions are processed, and which approved execution operations are requested after a decision is adopted.

## Runtime Ownership

`OrganizationDecisionRuntime` owns only decision-domain records:

- organization goals and goal progress;
- standing policy records and supersession state;
- proposal, amendment, voter roll, vote, resolution, and execution records;
- decision transaction records and immutable projections.

It does not own organization identity, memberships, authority grants, treasuries, accounts, economy balances, item identity, property, business records, contracts, social state, legal state, NPC strategy, or UI state. Those systems remain authoritative for their records.

## Cross-Runtime Authority

Decision operations consult `OrganizationAuthorityRuntime` for submission, amendment, voting, vote closure, veto, override, emergency, confidential-view, and execution permissions. The decision runtime resolves the institutional process, while Feature 13.3 remains the authority source.

Approved resource operations are delegated to `OrganizationResourceRuntime`. For example, an approved budget proposal records a decision execution, then asks Feature 13.4 to create the budget. Preview and failed execution paths restore both the local decision state and resource state, so decision execution cannot leave partial destination mutations.

## Definitions

Prototype definitions are provided by `PrototypeOrganizationDecisionDefinitionFactory`:

- recruitment and reserve-fund goals;
- confidentiality and budget-limit policies;
- simple majority, secret ballot, and emergency executive procedures;
- adopt-policy, establish-goal, approve-budget, and emergency proposal definitions.

Catalog-authored definitions remain authoritative. Prototype fallback definitions are only a development safety net and should be retired naturally as authored assets are added.

## Persistence

`OrganizationDecisionPersistenceParticipant` stores the shared-world decision graph under `world.organization-decisions`.

Prepare validation rejects corrupt graphs before commit:

- missing organizations;
- missing goal, policy, procedure, proposal, or currency definitions;
- missing voter rolls, proposals, resolutions, or votes;
- unsupported execution operation kinds;
- invalid policy parameters;
- invalid treasury/account/currency scopes when resource state is available.

Restore loads decision records without replaying proposal submission, voting, authority approval consumption, or resource execution. If restore fails, the live runtime is rolled back to its previous state.

## Test Lab

Feature 13.5 registers Test Lab suite `feature.13.5.organizational-goals-policies-decisions`.

The suite covers runtime readiness, goal and policy lifecycle, proposal amendment, voting, tallying, resolution closure, execution preview, resource-backed execution, persistence, redacted projections, and corrupt-save rejection.

`OrganizationDecisions` is a fixture-owned runtime area, so hostless and scene-independent automation runs receive a fresh isolated decision runtime alongside organization identity, memberships, authority, resources, economy, and item identity.

## Boundaries For Later Features

This feature intentionally does not implement factions, diplomacy, government jurisdiction, legislation, crimes, courts, public elections, autonomous organizational strategy, multiplayer permissions, or final UI visibility. Later systems should issue requests to this runtime or consume immutable decision records rather than taking ownership of decision state.
