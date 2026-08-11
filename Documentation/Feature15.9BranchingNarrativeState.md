# Feature 15.9 - Branching Narrative State, Persistent Variables, and Consequences

Feature 15.9 adds a durable narrative-state owner runtime for authored world and person story variables. It is designed to sit above quests, dialogue, and narrative events without turning those systems into duplicate flag stores.

## Ownership

`NarrativeStateRuntime` owns narrative state records, typed variable values, transition history, transaction idempotence, and persistence. Other systems may ask for conditions or request transitions, but they do not write state directly.

Prototype definitions are supplied by `PrototypeNarrativeStateDefinitionFactory` and registered into the Test Lab and prototype definition registry. Catalog-authored definitions remain authoritative; prototype fallback definitions exist only so validation, automation, and persistence share one source while authored assets are still being built.

## Definitions

`NarrativeStateDefinition` describes:

- Stable narrative state definition IDs.
- State scope such as World or Person.
- Typed variable definitions.
- Allowed state-token values and terminal values.
- Exclusive branch groups.
- Authored transition definitions.
- Optional transition consequences using the Feature 15.8 narrative action model.

The supported variable kinds are Boolean, Integer, StateToken, StableSubjectReference, OptionalStableSubjectReference, and SmallCounter.

## Runtime Behavior

Transitions use `NarrativeStateTransitionRequest` and support preview, revision checks, idempotent transaction IDs, and atomic consequence preparation. A transition is committed only after source values, mutability, conditions, exclusivity, and consequences validate.

State records are sparse: defaults are projected from definitions until a transition materializes state for a scope. Historical transition records preserve old and new values so systems can query what a branch value was at a past world time.

## Integrations

Feature 15.9 integrates with:

- Narrative events through `RequestNarrativeStateTransition` actions and `NarrativeState` conditions.
- Dialogue through `DialogueConditionKind.NarrativeState` and `DialogueEffectKind.RequestNarrativeStateTransition`.
- Quest eligibility through `QuestEligibilityRequirementKind.NarrativeState`.
- Persistence through `NarrativeStatePersistenceParticipant`.

These integrations route through the narrative-state owner runtime and do not duplicate long-lived story flags inside quests, dialogue flows, or event records.

## Visibility

Hidden, secret, and diagnostic narrative states redact variable values from ordinary projections while development views preserve full data for Test Lab and debugging. Hidden projection checks do not create knowledge, events, or gameplay side effects.

## Persistence

Narrative state saves include materialized states and transition history only. Restore validates schema, world ownership, definition references, variable definitions, variable value types, and transition graph references before committing. Restore does not replay transitions or consequences.

## Prototype Coverage

The prototype suite covers:

- Definition registration and validation.
- Exclusive person branch transitions.
- Merged and terminal world branches.
- Historical value queries.
- Hidden projection redaction.
- Quest, dialogue, and narrative condition adapters.
- Narrative-event-driven state transition requests.
- Persistence restore without consequence replay.
- Corrupt restore rejection before live mutation.
