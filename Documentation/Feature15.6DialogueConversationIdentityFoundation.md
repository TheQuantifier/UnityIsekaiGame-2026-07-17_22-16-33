# Feature 15.6 - Dialogue and Conversation Identity Foundation

Feature 15.6 establishes conversation identity and runtime records without implementing authored dialogue lines, branching choice graphs, or UI conversation flow.

## Ownership Boundary

`ConversationRuntime` owns conversation records, participant roles, lifecycle, source references, projections, idempotence, and persistence. It does not own quests, quest sources, locations, interaction points, organizations, offices, social relationships, knowledge, history, or access policies.

Those systems remain authoritative. Conversations store stable references to them so later dialogue, social, quest, and UI features can project a conversation safely without duplicating state.

## Runtime Shape

The runtime supports:

- `ConversationDefinition` catalog definitions.
- `ConversationRecordData` runtime records.
- `ConversationParticipantRecordData` role records for multi-person conversations.
- `ConversationSubjectLinkData` references to quest, location, organization, social, and knowledge subjects.
- Public, participant, controlling-entity, and privileged projections.
- Transaction idempotence and revision checks.
- Lifecycle transitions from active/proposed states into terminal history states.
- Deterministic indexes by participant, location, interaction point, quest, quest source, quest listing, organization, and office.
- Persistence capture, prepare, commit, rollback, and corrupt-payload rejection.

## Prototype Definitions

Prototype-only definitions live in `PrototypeConversationDefinitionFactory`. Catalog-authored definitions take precedence when they exist.

Representative prototype definitions cover:

- Adventurer Guild Counter.
- Merchant Guild Counter.
- Mayor Desk.
- Guild Head Office.
- Records Desk.
- Prisoner Interview.
- Private Audience.
- Group Briefing.
- Missing Provider Diagnostic.

## Validation

The feature includes Edit Mode tests for definition registration, reference-only context, immutable snapshots, privacy projections, provider/location rejection, overlap/revision rejection, and persistence safety.

It also adds Test Lab automation suite `feature.15.6.dialogue-conversation-identity-foundation`.
