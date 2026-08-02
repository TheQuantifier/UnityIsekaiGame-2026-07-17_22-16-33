# Feature 13.6 - Faction Identity, Alignment, and Internal Political Dynamics

Feature 13.6 adds an authoritative faction runtime for political, ideological, interest-group, and internal-bloc state.

## Ownership Boundary

`FactionRuntime` owns faction records, names, affiliations, roles, positions, vote recommendations, dispositions, split/merge history, and faction persistence.

It does not own formal organizations, organization memberships, authority grants, treasuries, proposals, votes, resolutions, social records, knowledge records, or access policies. Those remain owned by their original runtimes.

Factions may reference organization state as host context or eligibility input, but faction membership never grants organization membership, office, authority, resources, or votes.

## Runtime Integration

The runtime is configured from:

- `DefinitionRegistry`
- `OrganizationRuntime`
- `OrganizationMembershipRuntime`
- `OrganizationAuthorityRuntime`
- `OrganizationResourceRuntime`
- `OrganizationDecisionRuntime`
- world ID
- known Person IDs

The Test Lab fixture bundle now owns `FactionRuntime` as a first-class runtime area. Snapshot restore, runtime fingerprinting, fixture isolation, and hostless automation validation include faction state.

## Prototype Definitions

`PrototypeFactionDefinitionFactory` provides fallback prototype-only definitions for:

- representative political faction definitions
- affiliation definitions
- internal role definitions
- position/platform definitions
- alignment axis definitions

Production catalog definitions remain authoritative. The fallback factory only fills known prototype gaps and does not bypass validation.

## Persistence

`FactionPersistenceParticipant` persists shared-world faction state under `world.factions`.

Restore validates:

- schema version
- expected world ID
- faction definition references
- host organization references
- affiliation definition references
- known Person references
- role, position, recommendation, disposition, and structural-event references

Failed prepare does not mutate live faction runtime state. Commit uses rollback if a prepared payload fails to restore.

## Visibility

Faction projections support denied, concealed, redacted, full, and development views.

Secret factions are concealed to ordinary requesters. Development or privileged projection may inspect full stored records without creating gameplay knowledge, membership, votes, or social state.

## Automation Coverage

Feature 13.6 automation covers:

- definition and runtime readiness
- faction identity, hosts, rename, and lifecycle transitions
- affiliation eligibility and organization-membership separation
- internal role assignment
- proposal positions and vote recommendations
- vote cohesion reports using organization-owned votes
- influence reports using membership and recommendation signals
- split and merge structural records
- directional faction dispositions
- secret projection boundaries
- save/restore and corrupt-payload rejection

## Deferred

The faction runtime does not implement diplomacy, treaties, wars, government law, autonomous faction strategy, espionage behavior, or final UI policy. Later systems should provide context to `FactionRuntime` or consume its projections rather than moving those responsibilities into factions.
