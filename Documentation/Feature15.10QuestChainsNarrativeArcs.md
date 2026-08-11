# Feature 15.10 - Quest Chains, Narrative Arcs, Dependencies, and Reactive Orchestration

Feature 15.10 adds narrative arc orchestration as a coordination layer over existing Step 15 systems.

## Ownership Boundary

`NarrativeArcRuntime` owns only arc records, stage records, dependency resolution state, bound quest references, and arc transaction history.

It does not own quest records, quest outcomes, narrative event records, dialogue state, or persistent narrative variables. Those remain owned by `QuestRuntime`, `QuestOutcomeRuntime`, `NarrativeEventRuntime`, `DialogueFlowRuntime`, and `NarrativeStateRuntime`.

## Definition Model

`NarrativeArcDefinition` describes:

- Arc identity, scope, visibility, repeat policy, and cascade budget.
- Stable stage definitions and terminal-stage rules.
- Entry, completion, skip, and failure dependencies.
- Quest bindings that reference or instantiate quest records through `QuestRuntime`.
- Stage actions delegated through existing narrative action categories.

The validator rejects missing IDs, unsupported action categories, invalid quest bindings, missing local stage references, stage dependency cycles, and cross-arc cycles.

## Runtime Behavior

Arcs start explicitly with `NarrativeArcStartRequest`.

Signals enter through `NarrativeArcSignalRequest` and can represent:

- Explicit arc progression.
- Quest outcomes.
- Narrative state changes.
- Dialogue choices.
- Narrative events.
- Current world conditions.
- Custom prototype signals.

The runtime reevaluates eligible stages deterministically within the authored cascade budget. Dependency-free entry stages activate when an arc starts, while completion, skip, and failure dependencies require a matching signal or owner-runtime query.

Quest bindings are reference-only from the arc perspective. When a stage needs a quest, the arc runtime asks `QuestRuntime` or an injected binding executor to create or reference the quest, then stores only the resulting quest ID.

## Event and State Integration

`NarrativeEventRuntime` can delegate `RequestNarrativeArcProgression` actions through `NarrativeEventRuntimeIntegrations.NarrativeArcSignalExecutor`.

`NarrativeStateRuntime` remains the owner of persistent branching state. Arc dependencies can evaluate narrative state through the state runtime or through explicit narrative-state signals.

## Persistence

`NarrativeArcPersistenceParticipant` stores arc records and transaction history under `world.narrative-arcs`.

Restore validates the full payload before commit, rebuilds indexes atomically, and rolls back if commit fails. Restore does not replay quest binding, event emission, state transitions, or external actions.

## Visibility

Arc snapshots are immutable projections. Hidden, secret, and diagnostic arcs redact scope details and stage data from non-development views. Redacted projections do not expose hidden stage counts.

## Prototype Coverage

Prototype definitions include:

- Adventurers Guild introduction chain.
- Merchant delivery branch.
- Hidden mayor investigation arc.
- Royal succession branch.
- Parallel two-of-three convergence.
- Cross-arc follow-up dependency.

Automation covers definition readiness, state-driven quest binding, quest outcome branching, parallel convergence, narrative-event hooks, redaction, and persistence-safe restore.
