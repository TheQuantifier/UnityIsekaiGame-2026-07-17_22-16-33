# Feature 15.8 Narrative and World Events

Feature 15.8 adds a typed narrative event orchestration layer for authored world events, quest hooks, dialogue consequences, discovery beats, and cross-system signals.

`NarrativeEventRuntime` owns only narrative orchestration records:

- `NarrativeEventDefinitionId` identifies authored event definitions.
- `NarrativeEventId` identifies scoped runtime event records.
- `NarrativeActionExecutionId` identifies each attempted typed action.
- `NarrativeSignalRecordData` records explicit, stable narrative signals.

It does not own quests, conversations, knowledge, history, social state, organizations, law, travel, or locations. Actions that affect those domains are delegated through `NarrativeEventRuntimeIntegrations` or through the authoritative owning runtime. Required action failures fail the narrative event; optional actions are recorded as skipped or failed without bypassing owner validation.

## Boundaries

- Triggers are data categories, not arbitrary script callbacks.
- Conditions distinguish authoritative truth, actor knowledge, beliefs, quest state, dialogue state, location state, social state, legal state, and other owner-runtime state.
- Actions are typed and auditable. `Custom` actions are rejected by definition validation.
- Restore validates the full graph before commit and does not replay narrative actions.
- Hidden narrative events redact trigger, condition, and action details outside development projections.
- Duplicate trigger occurrences are ignored through stable occurrence keys.
- Cascades are explicit narrative signals and are bounded by authored cascade depth.

## Prototype Coverage

The prototype definition factory registers representative events for:

- dungeon-entry quest unlocks,
- quest-completion follow-up events,
- dialogue-choice signals,
- knowledge-unlocked conversations,
- hidden faction offers,
- delayed publication,
- cascade signal handling.

The Test Lab suite `feature.15.8.narrative-world-events-triggers-conditions-actions` verifies definition readiness, scoped trigger idempotence, cross-runtime delegation, hidden projections, required action failure behavior, cascades, and persistence rejection before live mutation.
