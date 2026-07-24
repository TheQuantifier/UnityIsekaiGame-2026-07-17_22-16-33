# Character History, Memory, and Historical Timelines

Feature 8.3 adds the first production foundation for objective historical records and Person-owned memories.

It does not replace current authoritative gameplay state. Body, biology, disease, injury, location, faction, relationship, and quest systems remain the owners of current state. History records transitions and facts about what happened.

## Core Separation

```text
Current authoritative state
  Owned by gameplay runtimes such as ActorBodyRuntime and future location/faction systems.

Authoritative history
  Canonical records of what happened in the simulated world.

Person Knowledge
  What a Person currently knows or believes, owned by PersonKnowledgeRuntime.

Person Memory
  A retained recollection or learned representation of a past event, owned by PersonMemoryRuntime.
```

The human player seeing UI or a debug panel does not automatically create Person Knowledge or memory. Memory formation is explicit.

## Authoritative Events

`AuthoritativeHistoryRuntime` stores one canonical event record per stable event ID. Events are indexed into Person, body, location, organization, and tag timelines without duplicating mutable event data.

Event definitions are authored as `HistoricalEventDefinition` assets with `history-event.*` IDs. The prototype defines representative event types:

- `history-event.person-participation`
- `history-event.hidden-witnessed-event`
- `history-event.body-transition`
- `history-event.correction`
- `history-event.diagnosis`

Each event record includes stable IDs, occurrence and recorded world time, deterministic sequence, Persons, bodies, location, organization, tags, source system, provenance, visibility, correction links, correlation ID, and a typed payload.

## Deterministic Ordering

Timeline ordering is:

1. occurrence world time;
2. stable sequence number;
3. stable event ID.

Queries do not rely on dictionary order, Unity object order, frame timing, hash values, or system clock time.

## Corrections

Corrections do not silently rewrite previous records. A correction is recorded as a new event with `supersedesEventId`. The original record remains queryable and is marked `Superseded`; accepted-version queries follow the correction chain.

Corrections to authoritative history do not automatically rewrite Person beliefs. Beliefs must be revised through normal Knowledge evidence.

## Person Memories

`PersonMemoryRuntime` owns structured memory records for a specific persistent Person. A memory can reference a historical event, belief, evidence IDs, source type, formation time, remembered event time, last recall time, confidence, clarity, salience, visibility, identity/body-at-time, and correction links.

Feature 8.4 extends these memory records with deterministic recall requests/results, accessibility states, structured detail availability, suppression records, reinforcement/degradation metadata, and auditable revision history. Authoritative history remains unchanged when a memory is forgotten, suppressed, altered, or recovered.

Memory states include accessible, inaccessible, uncertain, disputed, corrected, and forgotten. Forgetting a memory changes Person memory accessibility; it does not delete authoritative history.

## Knowledge Integration

When requested, forming a memory can create ordinary `PersonKnowledgeRuntime` evidence for `fact.event.occurred`. The memory runtime does not own confidence math or belief revision; it delegates to the existing Knowledge runtime.

Historical evidence uses normal provenance values such as direct observation, testimony, written source, personal experience, scripted revelation, and development fixture.

## Body Continuity

The history runtime can record body occupation ranges for a persistent Person. A body transition event can close the old occupation and open a new one. The Person remains the identity owner; body IDs are contextual references.

Body continuity is not automatically public. A privileged authoritative query can see it, while normal Person-contextual queries require visibility or Person-owned memory.

## Privacy

Authoritative history may contain hidden, private, diagnostic-only, or secret records. `QueryPersonAccessible` returns only public/personally observable events, private participant events, or events accessible through the Person's memories. Debug and simulation systems may use privileged queries deliberately.

Player-facing journals and later NPC systems should use Person-contextual queries, not unrestricted authoritative snapshots.

## Persistence

`AuthoritativeHistoryPersistenceParticipant` persists shared-world history under `world.authoritative-history`.

`PersonMemoryPersistenceParticipant` persists Person-owned memory under `person.memory`.

Restore validates payloads, rebuilds indexes, preserves stable IDs and ordering, and suppresses normal runtime events. It does not replay discoveries, memory formation, or Knowledge mutation.

## Validation

Runtime and save validation reject missing definitions, unknown Persons or bodies when known-ID providers are supplied, duplicate event or memory IDs, invalid time ranges, invalid correction targets, circular correction chains, missing event references, and malformed body occupation ranges.

## Test Lab

The Test Lab adds `History 8.3` under `Knowledge Step 8`.

Controls demonstrate:

- authoritative event recording;
- hidden events;
- uninformed Person privacy checks;
- witness memories;
- testimony;
- incorrect historical beliefs;
- authoritative correction;
- belief revision through evidence;
- forgetting memory while preserving authoritative history;
- body transition and previous-body memory;
- view comparison;
- save/restore.

## Deferred

Feature 8.3 does not implement final journal UI, codex UI, dialogue, rumor propagation, reputation, relationship simulation, quest generation, political history, procedural biographies, networking, server replication, full reincarnation gameplay, or automatic recording of everything rendered to the human player.
