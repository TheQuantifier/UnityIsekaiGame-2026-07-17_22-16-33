# Feature 15.4 - Quest Completion, Failure, Deadlines, Rewards, and Consequences

Feature 15.4 adds the terminal outcome layer for quests. Quest definitions now author completion policy, failure conditions, deadlines, reward packages, and consequence descriptors. Runtime state remains assignment-centered: objective progress belongs to Feature 15.3, participation belongs to Feature 15.2, and Feature 15.4 records when an assignment reaches a terminal outcome.

## Runtime Ownership

`QuestOutcomeRuntime` owns:

- terminal outcome records for completed, failed, cancelled, and expired assignments
- deadline tracking records derived from accepted assignments
- reward entitlement records created from terminal outcomes
- reward grant records that prove an entitlement was delivered or rejected
- deterministic transaction and event records for idempotence and persistence validation

It does not own money, items, social standing, memberships, credentials, legal state, knowledge, or other downstream systems. Reward delivery is delegated through `IQuestRewardEffectExecutor`, which makes owner-runtime boundaries explicit and keeps quest completion from duplicating state owned elsewhere.

## Completion and Failure

Completion evaluation uses `QuestObjectiveProgressRuntime.SummarizeAssignment` and the authored `QuestCompletionPolicyData`. The runtime supports automatic completion, turn-in requirements, issuer verification, and explicit system completion. Completion requests carry the assignment, requester, issuer/provider context, interaction point, location, time, source event, provenance, and optional revision expectation.

Failures are terminal outcomes too. They can be requested directly or produced by deadline evaluation. A terminal outcome is indexed by assignment, so completion cannot follow failure and failure cannot be applied twice.

## Deadlines

Deadline definitions support absolute world time and assignment-relative windows. `TrackAssignment` materializes deadline records at assignment acceptance time, and `EvaluateDeadlines` processes due records deterministically. Expired deadlines record both the deadline expiration and the resulting terminal outcome, and repeated evaluation at the same time is idempotent.

## Rewards

Rewards are separated into:

- reward definitions authored on the quest definition
- reward entitlements created when an assignment reaches a terminal outcome
- reward grants created when an entitlement is delivered to its owner runtime

Reward categories include currency, items, reputation, relationship, membership/rank, profession or qualification, legal permit/status, knowledge, and custom categories. Unsupported categories fail explicitly instead of being silently treated as granted. Claimable rewards remain pending until claimed; grant-on-completion rewards call the executor immediately.

## Persistence and Visibility

`QuestOutcomePersistenceParticipant` persists outcome state without replaying completion, deadline, or reward effects. Restore validates the graph against quest, participation, objective, and definition runtimes before mutating live state. Invalid payloads are rejected before commit.

Outcome and reward projections use the existing quest visibility model. Hidden rewards can be queried by privileged diagnostics, while ordinary access receives redacted target and source details without leaking hidden identifiers.

## Validation Coverage

Edit Mode coverage validates:

- authored outcome policies, deadlines, rewards, and consequences
- turn-in completion and duplicate protection
- deterministic deadline failure
- reward delegation and idempotent claims
- hidden reward redaction
- persistence round trip and corrupt payload rejection

Test Lab automation mirrors those workflows through the shared automation catalog for both in-game and command-line automation runners.
