# Feature 12.8 - Social Decision-Making and Relationship-Driven NPC Behavior

Feature 12.8 adds a deterministic social decision layer for NPCs. It evaluates relationship, attitude, reputation, group, pending-request, and authored-context signals to select a social intention and candidate action.

The runtime does not own interaction execution. `SocialDecisionRuntime` owns decision profiles, intentions, consideration scoring, selected candidates, cooldowns, recent decision history, and persistence. `SocialInteractionRuntime` remains the authority for executing social interactions and applying consequences.

## Runtime Ownership

- `SocialDecisionRuntime` evaluates and records decision state.
- `SocialDecisionProfileDefinition` controls enabled intentions, scoring limits, cadence, and default execution mode.
- `SocialIntentionDefinition` maps high-level goals to eligible `SocialInteractionDefinition` records or explicit no-action selections.
- `SocialConsiderationDefinition` defines bounded score inputs and response curves.
- `SocialDecisionPersistenceParticipant` validates and restores decision state without mutating live runtime state during prepare.

## Integration Boundaries

The runtime reads from these existing systems:

- Relationships from Feature 12.1.
- Interpersonal attitudes from Feature 12.2.
- Reputation from Feature 12.3.
- Rumors from Feature 12.4 as an optional dependency signal.
- Social interactions from Feature 12.5 for preview and execution.
- Social norms from Feature 12.6 as an optional dependency signal.
- Social networks and groups from Feature 12.7.

Decision evaluation can run in evaluate-only mode without creating state. Committed decisions update only decision state and, when configured for submission, delegate the selected interaction to Feature 12.5.

## Test Lab Coverage

The Feature 12.8 Test Lab suite verifies:

- Definitions resolve and validate.
- Evaluate-only decisions are non-mutating.
- Candidate selection is deterministic for equivalent inputs.
- Missing targets produce explicit no-action.
- Submitted decisions create interaction records through `SocialInteractionRuntime`.
- Persistence round trips restore state and reject corrupt payloads before commit.
