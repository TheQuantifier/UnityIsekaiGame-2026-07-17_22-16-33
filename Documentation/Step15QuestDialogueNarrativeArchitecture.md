# Step 15 - Quests, Dialogue, Narrative, and Events Architecture

Step 15 is finalized as a set of owner runtimes coordinated through explicit typed requests and immutable projections. It does not use scene objects, UI state, or Test Lab fixtures as narrative authority.

## Feature Map

- 15.1: Quest identity, definitions, and runtime quest records.
- 15.2: Quest offers, eligibility, acceptance, assignments, and abandonment.
- 15.3: Quest objectives, conditions, and progress tracking.
- 15.4: Quest completion, failure, deadlines, reward entitlements, and reward claims.
- 15.5: Quest sources, boards, discovery, listings, and presentation-safe availability.
- 15.6: Conversation identity, participants, provider context, and lifecycle.
- 15.7: Dialogue nodes, choices, conditions, flow state, and typed effects.
- 15.8: Narrative events, triggers, conditions, typed actions, signals, and cascades.
- 15.9: Persistent narrative state, variables, transitions, and consequences.
- 15.10: Narrative arcs, stages, dependencies, and quest/state/event orchestration.
- 15.11: Step 15 persistence, historical reconstruction, recovery diagnostics, and scene binding readiness.
- 15.12: Integration finalization, aggregate readiness, immutable narrative context, final validation, and Step 16 authoring contracts.

## Authoritative Ownership

- `QuestRuntime` owns quest existence, quest lifecycle, issuer/recipient/origin references, subject links, and quest history events.
- `QuestParticipationRuntime` owns offers, eligibility result records, acceptance, assignments, capacity/exclusivity, abandonment, and participation events.
- `QuestObjectiveProgressRuntime` owns objective runtime records, objective progress, current-state reconciliation, source-event idempotence, and objective events.
- `QuestOutcomeRuntime` owns terminal outcomes, deadlines, reward entitlements, reward grant records, and outcome events.
- `QuestSourceRuntime` owns quest sources, listings, discovery records, source associations, and source/listing history.
- `ConversationRuntime` owns conversation identity, lifecycle, provider context, participants, and conversation events.
- `DialogueFlowRuntime` owns current dialogue node, visits, choices, local conversation variables, and dialogue flow events.
- `NarrativeEventRuntime` owns narrative event lifecycle, trigger state, signals, typed action execution records, processed trigger keys, and cascade lineage.
- `NarrativeStateRuntime` owns typed persistent narrative variables and immutable transition history.
- `NarrativeArcRuntime` owns arc lifecycle, stage lifecycle, processed signal keys, and arc-to-quest bindings.
- `Step15NarrativeHistoricalService` owns no domain state. It reconstructs derived history from owner payloads.
- `Step15NarrativeIntegrationService` owns no domain state. It validates owner graphs, reports readiness, and builds read-only context projections.
- Scene Binding owns no Step 15 logic. It maps scene objects to stable logical IDs and refreshes presentation from owner runtime snapshots.

## Concept Separation

Quest definitions are not quests. Quests are not offers. Offers are not assignments. Assignments are not objectives. Objective satisfaction is not quest completion. Quest completion is not reward delivery. Reward entitlement is not currency or item ownership. Quest source listings are not assignment authority.

Conversation identity is separate from dialogue flow. Participants are not automatically speakers. Dialogue choices are not effects. Conditions read owner state; effects route explicit requests to owner runtimes.

Narrative events are not Step 8 historical events, not narrative state, and not narrative arcs. Narrative state is not domain state for inventory, permits, reputation, membership, law, or location. Narrative arcs coordinate dependencies and stages but do not own quest, event, or state records.

## Integration Boundaries

Use transactionally coordinated operations where multiple authoritative records must appear together, such as offer acceptance plus assignment creation, objective initialization after assignment acceptance, terminal outcome creation, reward entitlement creation, and required narrative state consequences.

Use observer-style idempotent reactions where loose coupling is healthier, such as quest board listings reacting to assignments, narrative events observing quest outcomes, and arcs observing quest/state/event signals. Observer integrations must carry stable source IDs and dedupe keys.

## Visibility and Knowledge

Player-safe queries use access-aware projections. Hidden content must not leak IDs, counts, future branches, choices, events, or stages. Development diagnostics may inspect full owner snapshots without creating player knowledge or recall metadata.

Conditions must state which semantic source they inspect:

- authoritative truth;
- person knowledge;
- person belief;
- institutional knowledge;
- historical occurrence.

## Persistence and Restore

Owner runtimes restore their payloads before derived indexes, subscriptions, scheduler state, scene binding refresh, and historical projections are rebuilt. Restore must never replay quest completion, reward grants, dialogue choices, narrative actions, state consequences, or arc activation actions.

`Step15NarrativeHistoricalService` builds the Step 15 manifest and historical timeline from owner payloads. `Step15NarrativeIntegrationService` validates final graph coherence and aggregate readiness from cloned snapshots.

## Aggregate Readiness

`Step15NarrativeIntegrationService.BuildReadiness` reports required owner runtimes, optional scene binding state, derived historical/query services, schema versions, record counts, validation state, and deterministic manifest fingerprints. Core Step 15 readiness does not require a loaded Unity scene, UI, local player, Editor, or Test Lab.

## Unified Narrative Context

`Step15NarrativeIntegrationService.BuildNarrativeContext` returns immutable projection-only data for a person:

- visible quest offers;
- active assignments;
- active objectives;
- turn-in-ready quests;
- claimable rewards;
- available quest sources;
- active conversations;
- current dialogue nodes;
- visible narrative state;
- active arc stages;
- recent visible narrative timeline entries;
- location/institution context.

The context query is bounded and owner-derived. It does not mutate quests, knowledge, history, dialogue, rewards, arcs, or scene bindings.

## Final Validation

The finalizer validates:

- single ownership per Step 15 category;
- required owner payload presence;
- schema and world consistency through the historical service;
- accepted offer to assignment coherence;
- current assignment to objective coherence;
- objective/assignment/quest reference agreement;
- terminal outcome uniqueness;
- completion from satisfied objective state;
- reward entitlement and grant uniqueness;
- source/listing claim references;
- dialogue flow to conversation ownership;
- narrative event cascade budget and typed action safety;
- narrative state domain-duplication warnings;
- arc bound quest references and processed signal uniqueness;
- scene binding presentation-only behavior.

## Prototype Scene Contract

Prototype scene objects should expose stable logical requests only:

Scene/UI input -> stable logical request -> authoritative owner runtime -> committed result -> domain event/snapshot -> presentation refresh.

Quest boards and counters should bind to `QuestSourceId`, `InteractionPointId`, `LocationId`, and provider context. They must not store quest journal state, dialogue state, or story flags.

## Step 16 Authoring Contract

World content can author:

- quests, issuers, recipients, origins, subject links, visibility, repeatability;
- quest boards, guild counters, mayor desks, hidden sources, and publication rules;
- objectives, progress categories, progress sources, hidden objectives, and dependencies;
- completion, failure, deadlines, reward packages, and claim rules;
- conversations, providers, participants, nodes, choices, conditions, and typed effects;
- narrative events, triggers, conditions, typed actions, repeat policies, and cascade limits;
- narrative state variables, branches, transitions, and consequences;
- arcs, stages, quest bindings, dependencies, branch convergence, recovery paths, and merges.

Step 16 should add content framework and authoring scale, not new foundational quest/dialogue/narrative ownership.

## Known Boundaries

NPC autonomous quest selection, final UI, multiplayer authority transport, VR interaction, procedural quest generation, and content authoring tools remain future work. Step 15 exposes clean runtime APIs and projections for those systems to consume later.
