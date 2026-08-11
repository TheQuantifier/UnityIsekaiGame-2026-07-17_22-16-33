# Feature 15.11 Narrative Persistence, Historical Reconstruction, Recovery, and Scene Integration

Feature 15.11 finalizes Step 15 reliability boundaries. It does not add new quest, dialogue, or narrative mechanics. It adds a shared persistence and historical inspection layer that reads authoritative Step 15 owner records without replaying historical events or treating scene objects as authority.

## Ownership Boundary

Authoritative records stay with their owning runtimes:

- `QuestRuntime` owns quest records, subject links, issuers, recipients, origins, lifecycle, and quest runtime events.
- `QuestParticipationRuntime` owns offers and assignments.
- `QuestObjectiveProgressRuntime` owns objective progress records, objective evidence, and objective progress history.
- `QuestOutcomeRuntime` owns terminal outcomes, deadlines, reward entitlements, and reward grants.
- `QuestSourceRuntime` owns quest sources, listings, discovery records, and source associations.
- `ConversationRuntime` owns conversation identity, participants, subject links, and conversation lifecycle.
- `DialogueFlowRuntime` owns current node, visits, choices, local variables, and flow history.
- `NarrativeEventRuntime` owns narrative event records, signals, action executions, trigger history, and processed trigger keys.
- `NarrativeStateRuntime` owns persistent narrative state records and transition history.
- `NarrativeArcRuntime` owns narrative arcs, stages, bound quest references, processed signal keys, and arc transactions.

`Step15NarrativeHistoricalService` owns no gameplay state. It builds manifests, validates cross-runtime references, rebuilds derived historical indexes on demand, and returns immutable projection models.

Derived indexes and scene bindings are not serialized as alternate authority. They are rebuilt or rebound after restore.

## Restore Phases

The Step 15 manifest records a deterministic restore pipeline:

1. ReadEnvelope
2. ValidateSchema
3. DeserializeCandidate
4. ResolveDefinitions
5. ResolveDependencies
6. PrepareIndexes
7. CrossValidate
8. CommitAuthoritativeState
9. RebuildDerivedState
10. RestoreScheduler
11. RestoreSubscriptions
12. Reconcile
13. ValidateFinalState
14. PublishReady
15. SceneRebind

Restore validation checks schema versions, world scope, duplicate authoritative IDs, and representative cross-runtime references before any restored state is considered ready.

## Historical Reconstruction

Historical queries are projections over owner save data:

- Quest-at-time snapshots include quest lifecycle, offers, assignments, objective state, outcomes, rewards, and active listings.
- Person quest snapshots include pending offers, active assignments, completed and failed quest IDs, and claimable rewards.
- Conversation snapshots include lifecycle, participants, active dialogue node, and latest selected choice.
- Narrative state snapshots return variable values at a requested world time.
- Narrative arc snapshots return lifecycle, active/completed stages, and bound quest IDs.

These projections never replay historical events into live runtimes and never mutate the source snapshot.

## Timeline Queries

The unified timeline merges Step 15 owner histories into deterministic `NarrativeTimelineEntry` records ordered by world time, sequence, category, and stable source reference. Queries support filtering by person, quest, conversation, narrative event, narrative state, narrative arc, location, organization, category, and time window.

Visibility modes are explicit:

- `Development` sees hidden diagnostics.
- `PersonSafe` hides restricted entries and only exposes person-safe history.
- `Institutional` is reserved for broader authorized institutional views.

Hidden entries are filtered before pagination so counts and cursors do not leak hidden IDs to ordinary callers.

## Recovery and Validation

Validation distinguishes:

- Recoverable derived gaps, such as a dialogue flow current node that has no matching visit record.
- Non-recoverable authoritative corruption, such as duplicate authoritative narrative arc IDs.

Recoverable issues are reported for repair/rebuild paths. Authoritative corruption remains a hard validation failure.

## Scene Integration

Scene bindings are presentation-only. Location and interaction point scene objects can display, offer, or route interactions, but they do not own quest, conversation, dialogue, narrative state, or narrative arc data.

After restore, scene presentation must rebind to authoritative runtime records. Missing scene objects can degrade presentation, but they must not change the restored Step 15 state.

## Automation Coverage

The Feature 15.11 automation suite covers:

- Manifest ownership and restore phase declaration.
- Historical reconstruction across quest, conversation, state, and arc records.
- Unified timeline visibility and deterministic pagination.
- Recovery diagnostics for derived gaps and authoritative corruption.

Edit Mode tests also cover immutable snapshot behavior, validation without mutation, hidden timeline filtering, and deterministic manifest fingerprints.
