# Feature 13.2 - Organization Memberships, Ranks, and Offices

Feature 13.2 adds persistent formal affiliation records to the organization system:

- Membership records identify that a Person is formally affiliated with an Organization.
- Rank assignments describe ordered standing inside an organization-specific rank track.
- Office records describe institutional positions that may be vacant, acting, or jointly held.

The implementation deliberately does not grant permissions, governance authority, legal power, resource control, or AI decision behavior. Those later systems should consume membership, rank, and office records as inputs.

## Runtime Ownership

`OrganizationMembershipRuntime` is the authoritative owner for membership, rank, office, and office-assignment records. It depends on `OrganizationRuntime` for organization identity and lifecycle, but it does not duplicate organization records.

The runtime enforces:

- Stable record IDs and deterministic query ordering.
- Transaction idempotence for membership, rank, office, and office-assignment operations.
- Explicit consent boundaries for invitations.
- Branch membership dependencies on parent memberships.
- Rank-track compatibility with membership definitions.
- Office eligibility, vacancy, acting-holder, joint-holder, capacity, and rank requirements.
- Safe ending policies for memberships with active rank or office assignments.
- Immutable snapshots and access-aware membership projections.

## Definitions

The following definition types were added:

- `OrganizationMembershipDefinition`
- `OrganizationRankTrackDefinition`
- `OrganizationRankDefinition`
- `OrganizationOfficeDefinition`

Prototype fallback definitions live in `PrototypeOrganizationMembershipDefinitionFactory`. Catalog-authored definitions remain authoritative; fallback registration only fills known prototype gaps and never bypasses validation.

## Persistence

`OrganizationMembershipPersistenceParticipant` stores the shared-world membership graph under `world.organization-memberships`.

Prepare validation rejects:

- Unsupported schema versions.
- Duplicate record IDs.
- Missing membership, rank, office, organization, or person references.
- Corrupt cross-record references.

Commit restores through `OrganizationMembershipRuntime.RestoreFromSaveData` and rolls back if a prepared payload still fails at commit time.

## Test Lab

The suite `feature.13.2.organization-memberships-ranks-offices` validates:

- Prototype definitions and runtime readiness.
- Application, invitation, and explicit acceptance behavior.
- Branch membership dependency rules.
- Rank ordering and deterministic replacement.
- Office assignment capacity, acting holder, and joint holder behavior.
- Ending policies with active rank and office assignments.
- Projection privacy and corrupt persistence rejection.

The Test Lab fixture system exposes `TestLabRuntimeArea.OrganizationMemberships`, so scene-independent command automation and in-game automation use the same runtime construction path.

## Boundaries

Feature 13.2 does not implement:

- Organization authority or permissions.
- Governance decisions.
- Resource ownership.
- Legal authority.
- Employment state ownership.
- Social relationship creation.
- NPC organization behavior.

Those systems should reference membership, rank, and office projections instead of owning or mutating these records directly.
