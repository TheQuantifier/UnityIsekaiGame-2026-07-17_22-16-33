# Feature 15.7 Dialogue Nodes, Conditions, Choices, and Conversation Flow

Feature 15.7 adds an authored dialogue graph layer on top of the Step 15.6 `ConversationRuntime`.

The ownership boundary is:

- `ConversationRuntime` owns conversation identity, participants, subject links, locations, and lifecycle.
- `DialogueFlowRuntime` owns current dialogue graph state, current node, visits, selected choices, local dialogue variables, and flow persistence.
- Quest, social, organization, item, legal, location, knowledge, and history systems remain owners of their records. Dialogue effects route through `IDialogueEffectExecutor` when they need those systems.

## Runtime

Dialogue graphs are authored as `DialogueGraphDefinition` assets or prototype fallback definitions. A graph declares:

- Stable graph, node, choice, transition, condition, and effect IDs.
- A conversation definition ID from 15.6.
- A canonical entry node and optional fallback node.
- Nodes with speaker/listener selectors, text, choices, entry conditions, entry effects, and automatic transitions.
- Choice conditions, hidden/unavailable behavior, repeat policy, target nodes, and delegated effects.

`DialogueFlowRuntime` starts a flow for an existing conversation, evaluates graph conditions from a `DialogueConditionContext`, enters nodes, evaluates visible choices, records deterministic choice selection history, and persists/restores flow state without replaying effects.

## Conditions

Conditions use `DialogueConditionKind` and `DialogueConditionEvaluationMode` to keep authoritative truth, speaker knowledge, listener knowledge, and conversation-local state distinct. The initial implementation uses `QuestEligibilityFactSet` as the shared prototype fact adapter for cross-system facts such as organization membership, rank, authority, reputation, relationships, item state, permits, legal status, and known subjects.

## Effects

Local effects can set dialogue flags and counters directly because they are owned by the dialogue flow. Cross-system effects are represented by stable `DialogueEffectData` and delegated to `IDialogueEffectExecutor`. Required delegated effects fail atomically when no executor is available; optional delegated effects are recorded as intentionally unsupported rather than mutating another runtime.

## Persistence

`DialogueFlowPersistenceParticipant` captures only dialogue-flow state and depends on `ConversationPersistenceParticipant`. Prepare validation rejects missing conversations, missing graph definitions, wrong worlds, unsupported schema versions, duplicate flow IDs, and invalid current nodes before commit. Failed prepare does not mutate the live runtime.

## Prototype Coverage

Prototype graph fallbacks currently cover:

- Adventurer Guild counter.
- Merchant Guild counter.
- Mayor desk.
- Guild Head office.
- Records desk.
- Prisoner interview.

These definitions are shared by Edit Mode tests, Test Lab in-game automation, command-side automation, and development registry validation.
