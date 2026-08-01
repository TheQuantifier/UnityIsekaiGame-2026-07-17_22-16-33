# Feature 13.3 - Organizational Roles, Permissions, and Institutional Authority

Feature 13.3 adds the authority layer for organizations. It answers whether a Person may perform an institutional action on behalf of an organization without taking ownership of organization identity, membership, ranks, or offices.

## Ownership Boundaries

- `OrganizationRuntime` remains the source of truth for organization records, lifecycle, hierarchy, visibility, and parent/branch links.
- `OrganizationMembershipRuntime` remains the source of truth for memberships, rank assignments, offices, and office assignments.
- `OrganizationAuthorityRuntime` derives effective authority from those owning runtimes plus explicit authority grants, delegations, approvals, and audits.
- Feature 13.3 does not implement real resources, treasuries, legal authority, voting, networking, or final UI.

## Definition Types

- `OrganizationPermissionDefinition` defines a stable permission such as inviting members, appointing officers, issuing orders, viewing restricted records, or delegating permission.
- `InstitutionalActionDefinition` maps an action to required permission IDs, permission-combination policy, scope type, approval count, and audit expectations.
- `OrganizationAuthorityRoleDefinition` groups permission grants and explicit denials into an institutional role.
- `OrganizationAuthorityBindingDefinition` binds a role to an existing membership type, rank, office definition, office assignment kind, or explicit branch relationship.

Catalog-authored definitions are authoritative. Prototype fallback definitions are registered only for known prototype IDs so validation remains strict.

## Runtime Behavior

- Effective authority is resolved deterministically from active memberships, active rank assignments, active office assignments, active direct grants, and active delegated grants.
- Parent organizations do not automatically grant branch authority. Branch authority requires an explicit branch-scoped binding or grant.
- Delegation validates source authority, permission delegability, redelegation rules, scope narrowing, expiration, and cycles.
- Joint approvals are explicit records. Authorization can require a configured approval count and can consume approvals as part of the decision.
- Authorization is preview-safe and does not mutate unless approval consumption or audit recording is explicitly requested.
- Snapshots and projections clone internal data so callers cannot mutate live authority state.

## Persistence

`OrganizationAuthorityPersistenceParticipant` persists authority grants, approvals, audits, and transaction history after organization and membership participants. Prepare validation rejects corrupt schema, missing definition references, missing person or organization references, duplicate IDs, invalid lifecycles, and malformed scopes before commit. Restore rolls back on failure.

## Test Lab

Suite ID:

`feature.13.3.organizational-roles-permissions-authority`

Coverage includes:

- Definition readiness and deterministic empty authority queries.
- Authority derived from membership, rank, and office bindings.
- Scoped direct grants, expiration, idempotence, and delegation.
- Explicit branch authority boundaries.
- Joint approvals, consumption, and audit records.
- Save, restore, and corrupt payload rejection without mutating live runtime state.

## Design Rule

Capability, permission, authority, role, office, rank, membership, ownership, and governance are intentionally separate concepts. A Person can have the capability to do something without institutional permission, can hold an office without unrelated permissions, and can be authorized for one action without owning the underlying organization or record.
