# Character History and Life Events

Feature 8.5 adds structured life-event metadata on top of `AuthoritativeHistoryRuntime`.

Life events are not a replacement history system. A life event is still a canonical historical event with a stable event ID, definition ID, timestamp, visibility, correction chain, persistence record, and query behavior. The additional life-event fields classify the event for biography, timeline, participant-role, relationship, and sequence queries.

## Runtime Ownership

`AuthoritativeHistoryRuntime` owns life events because they are historical records. `PersonMemoryRuntime` may remember them, and `PersonKnowledgeRuntime` may know or believe claims about them, but neither memory nor knowledge becomes authoritative for whether the event occurred.

Current character state systems remain separate. Recording a life event such as a title grant, injury, diagnosis, death, or body transition does not directly grant the title, apply an injury, diagnose a condition, kill a body, or replace a body. Those current-state systems may emit or consume life events later through explicit integration points.

## Definitions

`HistoricalEventDefinition` now supports optional life-event metadata:

- life-event category;
- payload kind;
- required and optional participant roles;
- default significance;
- default biography relevance;
- default public-record relevance;
- visibility/correction expectations.

Definitions remain ordinary history definitions. They are not registered in a second catalog or parallel taxonomy.

## Records

`HistoricalEventRecordData` stores life-event data additively:

- `isLifeEvent`;
- category, significance, biography relevance, public-record relevance, and outcome;
- structured participants with life-event roles;
- typed payload data;
- related role, title, social-status, condition, injury, disease, treatment, combat, quest, legal, relationship, and item IDs;
- causal or sequence relationships;
- optional sequence membership.

These fields are projections for queries and biography construction. The canonical event record remains the source of truth.

## Relationships And Sequences

Life events can reference related events through authored relationship types such as cause, consequence, correction, continuation, resolution, or part-of. Relationship validation rejects missing targets and prevents cycles when the relationship requires an acyclic chain.

Sequences group related life events, such as battle -> injury -> diagnosis -> recovery. Sequence order is stable and queryable, but it does not replace event timestamps or canonical history ordering.

## Biography Views

Biography queries can produce different views from the same authoritative events:

- public biography;
- privileged authoritative biography;
- person-known biography;
- person-remembered biography;
- major milestones.

Public biography excludes hidden/private events unless their public-record relevance allows inclusion. Known and remembered views require the corresponding knowledge or memory boundary instead of leaking privileged truth.

## Persistence

Life-event data is persisted through the existing history save data. The schema is additive: ordinary historical events restore as before, and life-event fields restore only when present.

Restore validates:

- known event definitions;
- required participants;
- valid relationship targets;
- sequence membership;
- correction references;
- person/body references where known.

Restore runs silently and does not replay life-event recording, memory formation, knowledge mutation, or current-state transitions.

## Test Lab

The Test Lab adds `Life Events 8.5` under `Knowledge Step 8`.

It demonstrates:

- life-event definition validation;
- birth/creation, discovery, role, title, affiliation, combat, injury, diagnosis, recovery, crime, ownership, death, return, and body-transition recording;
- sequence creation;
- cause/consequence linking;
- presumed-death correction;
- public, authoritative, known, remembered, timeline, and milestone views;
- save/restore validation.

Automation suite `feature.8.5.character-history-life-events` covers the same foundation paths.
