# Feature 15.5 - Quest Sources, Boards, Discovery, and Availability Presentation

Feature 15.5 adds source-owned quest publication records without moving ownership of quests, participation, objectives, or outcomes.

## Runtime Boundary

- `QuestSourceRuntime` owns quest source instances, listing instances, discovery records, source associations, source events, and source transactions.
- `QuestRuntime` remains the owner of quest identity, definitions, metadata, lifecycle, and quest visibility.
- `QuestParticipationRuntime` remains the owner of availability, eligibility, offers, assignments, acceptance, capacity, and abandonment.
- Source browse and inspect operations return projections. They do not create offers, assignments, quest facts, or UI state.
- Acceptance through a source delegates to participation and then updates only the listing presentation state.

## Definitions

`QuestSourceDefinition` describes how a source behaves:

- source category, visibility, discovery, and listing discovery policy
- publication capacity, duplicate policy, expiration policy, accepted-listing display, and repeatable-listing display
- quest category, tag, issuer, and repeatability filters
- provider requirements and publication authority requirements
- source roles such as listing, offer, acceptance, turn-in, reward claim, and information unlock

Prototype source definitions are supplied by `PrototypeQuestSourceDefinitionFactory` and are added through the shared quest definition fallback path. Authored catalog definitions still take precedence.

## Prototype Sources

The prototype set includes:

- Adventurer Guild Quest Board
- Adventurer Guild Counter
- Merchant Guild Counter
- Mayor Office Desk
- Hidden Faction Rumor Source
- Empty Quest Archive

These cover public, local, restricted, government, hidden, and empty-source behavior.

## Persistence

`QuestSourcePersistenceParticipant` persists the source graph under `world.quest-sources`.

Prepare validates:

- schema and world scope
- source definitions
- listing source and quest references
- duplicate IDs
- discovery, association, event, and transaction references

Commit restores from prepared data and rolls back on restore failure.

## Automation

The Feature 15.5 Test Lab suite covers:

- definition readiness
- empty source scene binding
- publication authority and browse discovery
- acceptance delegation and claimed listing projection
- expiration, save/restore, and corrupt payload rejection

The suite is registered in the shared automation catalog so command-side and in-game automation use the same scenario definitions.
