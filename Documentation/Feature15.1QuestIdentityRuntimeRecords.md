# Feature 15.1 - Quest Identity, Definitions, and Runtime Records

Feature 15.1 introduces the authoritative identity layer for quests.

## Ownership

`QuestDefinition` describes authored quest metadata: category, importance, repeatability policy, default visibility, default source channel, supported issuer types, supported recipient scopes, and semantic tags.

`QuestRuntime` owns runtime quest records. Runtime records have stable `QuestId` values distinct from `QuestDefinitionId` values, so one authored definition can safely produce multiple runtime quest records when its policy allows it.

The existing `PlayerQuestLog` remains a compatibility gameplay journal for active player progression. It is not the new authoritative owner of world quest existence.

## Runtime Records

Runtime quest records store:

- world/save scope
- lifecycle state
- issuer reference
- intended recipient reference
- origin location or interaction point reference
- source channel
- subject links using Step 8 information subject references
- visibility
- provenance/source references
- revision

Quests reference other systems by stable IDs only. Person, item, organization, government, location, incident, journey, encounter, dialogue, contract, board, reward, and objective systems remain their own owners.

## Boundaries

Feature 15.1 does not implement objective completion, rewards, quest board selection logic, dialogue branching, AI quest selection, procedural quest generation, UI quest journal redesign, or quest balancing.

Those later systems should create, query, or transition records through `QuestRuntime` instead of mutating quest identity data directly.

## Persistence

`QuestRuntimePersistenceParticipant` captures, prepares, and commits quest runtime state through the shared persistence contract. Prepare validates schema version, world scope, definition references, record identity, event references, and transaction references before commit.

Failed prepares do not mutate the live runtime. Restore commits do not replay creation or lifecycle events.

## Automation

`PrototypeStep15AutomationSuites` registers the Feature 15.1 suite through the central automation catalog. The suite is scene-independent and command-line compatible.

Covered automation scenarios:

- quest definition readiness
- unique and repeatable runtime identities
- issuer, recipient, origin, and subject reference boundaries
- visibility-safe hidden quest queries
- lifecycle revision and idempotence behavior
- persistence and world isolation
