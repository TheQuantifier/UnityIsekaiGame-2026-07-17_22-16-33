# Career History and Transitions

Feature 10.8 adds a career-history layer for Step 10 profession systems. The layer records career episodes, transition records, milestones, and deterministic timeline projections while keeping the existing profession, rank, credential, training, activity, position, employment, knowledge, and record runtimes as the owners of their own data.

## Ownership

Career history owns only career narrative structure:

- Career episodes such as employment periods, profession practice periods, retirement periods, and gaps.
- Career transitions such as promotion, demotion, transfer, resignation, dismissal, retirement, return, career change, achievements, and setbacks.
- Career milestones that point back to authoritative source records.
- Timeline queries and access-aware career projections.

It does not duplicate authoritative state from upstream systems. A career transition stores stable source references to real profession relationships, rank records, credential records, training enrollments, professional activities, position instances, and employment records. Validation fails when those source records are missing.

## Episodes

`CareerEpisodeData` represents a bounded or open-ended career period for a Person. Episodes can be primary, secondary, concurrent, secret, disputed, exclusive, or a career gap. Episodes carry source references and transition IDs, and snapshots returned from the runtime are cloned so callers cannot mutate authoritative state.

## Transitions

`CareerTransitionDefinition` is the authored catalog definition for transition rules. Each definition declares allowed source and destination episode states, required source record types, authority requirements, access policy defaults, and whether secrecy is allowed.

`CareerTransitionRecordData` records the actual transition event. It includes source and destination episode IDs, previous and new rank/employment/position references, authority, access policy, source record references, and dependency revisions used to reject stale previews.

## Timelines

`CareerHistoryRuntime.BuildTimeline` returns a deterministic `CareerTimelineSnapshot` sorted by world time and stable IDs. The snapshot exposes active, primary, concurrent, gap, and retirement views without granting write access to the runtime.

## Access

Career history creates Step 8 information subjects for episodes, transitions, timelines, gaps, retirement, career changes, promotion, transfer, resignation, dismissal, achievements, and setbacks. Public projections redact protected fields from secret career episodes and confidential transitions, while privileged projections can inspect the full records.

## Persistence

`CareerHistoryPersistenceParticipant` persists the runtime as a separate player-scoped participant after upstream profession and employment participants. Save preparation validates schema version, IDs, referenced definitions, source records, person IDs, organization IDs, primary-career uniqueness, and transition graph integrity. Restore is atomic: corrupt payloads are rejected before commit, and restore clears hooks so load does not replay career-history events.

## Requirement Adapters

`CareerHistoryRequirementAdapters` provides read-only helpers for future requirements:

- `HasPreviousProfession`
- `HasPreviousEmployment`
- `HasPriorSupervisoryExperience`
- `HasNoProhibitedDismissal`
- `IsRetired`
- `HasCareerTransition`

These adapters query career history without mutating upstream profession systems.

## Test Lab

Feature 10.8 adds a `feature.10.8.career-history-transitions` automation suite. It runs through the shared Test Lab fixture system and covers definitions, episodes, authoritative transitions, concurrent and primary careers, gaps, access redaction, retirement, return, career change, persistence, corrupt restore rejection, and restore replay prevention.

## Boundaries

Deferred systems include final UI presentation, NPC career decision-making, full legal/contract policy rules, economy compensation details, and procedural career simulation. Those systems should consume career-history projections and requirement adapters rather than becoming part of this runtime.
