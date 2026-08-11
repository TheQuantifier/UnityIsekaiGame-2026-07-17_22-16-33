# Feature 15.3 - Quest Objectives, Conditions, and Progress Tracking

Feature 15.3 adds an objective-progress runtime beneath quest participation. Quest definitions remain authored catalog data, quest assignments remain owned by `QuestParticipationRuntime`, and `QuestObjectiveProgressRuntime` owns per-assignment objective records, progress evidence, runtime objective IDs, and persistence.

## Runtime Boundaries

- `QuestDefinition` now declares objective definitions and objective groups.
- `QuestParticipationRuntime` still owns offers and accepted assignments.
- `QuestObjectiveProgressRuntime` instantiates objective records only for accepted assignments.
- Objective progress never completes, fails, or rewards a quest. Feature 15.4 owns quest completion and outcome transitions.

## Progress Model

Objective definitions support required, optional, and hidden objectives with categories, tags, prerequisites, groups, and stable target references. Runtime records track a stable objective definition ID, a stable runtime objective ID, current value, target value, satisfaction state, evidence, and counted source events.

Supported models include boolean event/state objectives, counters, current and cumulative quantities, unique target counts, and thresholds. Event progress requires committed domain signals with stable source event IDs, so duplicate events are ignored without mutating progress.

## Current State

Current-state objectives reconcile from explicit `QuestObjectiveStateContext` fact sets. The runtime does not scan per frame. Inventory, travel, combat, location, social, or future world systems should publish committed objective signals or call reconciliation at meaningful domain boundaries.

## Visibility

Hidden objectives may progress internally without leaking through ordinary queries. Privileged diagnostic queries can inspect hidden objectives for Test Lab, persistence validation, and development tooling.

## Persistence

`QuestObjectiveProgressPersistenceParticipant` captures objective progress as a dedicated world-scoped participant after quest records and quest participation. Prepare validation rejects invalid world IDs, missing assignments, missing quests, missing objective definitions, invalid objective IDs, and malformed progress before live runtime mutation.

## Prototype Coverage

The prototype quest catalog includes representative objective definitions for guild postings, merchant delivery, civic investigation, hidden rumors, and dynamic bounties. Edit Mode and Test Lab automation cover registration, assignment instantiation, event-driven progression, prerequisite unlocking, duplicate event rejection, current-state reconciliation, hidden visibility, independent per-assignee progress, persistence, and failed-restore safety.
