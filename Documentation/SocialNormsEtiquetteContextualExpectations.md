# Social Norms, Etiquette, and Contextual Expectations

Feature 12.6 adds definition-backed social norm assessment. A social norm is an authored expectation about conduct in a context: greeting a host, avoiding public insults, respecting witnesses, honoring promises, or excusing a breach because of emergency or unfamiliar custom.

## Runtime Ownership

`SocialNormRuntime` owns only social norm assessment records and transaction dedupe state. It does not own relationships, attitudes, reputation, rumors, promises, memories, or history.

The runtime may read or delegate to:

- `RelationshipRuntime` for contextual relationship conditions.
- `InterpersonalAttitudeRuntime` for consequence commits.
- `ReputationRuntime` for public or audience-scoped consequence commits.
- `RumorRuntime` and `SocialInteractionRuntime` for references and promise state.

This keeps norms as the expectation and assessment layer, while existing Step 12 runtimes remain the owners of their records.

## Definitions

`SocialNormDefinition` is immutable authored data. It declares:

- Category, scope, conduct strength, priority, specificity, and severity.
- Required target, witness, public, interaction, promise, place, audience, relationship, visibility, channel, role, and tag conditions.
- Actor-knowledge and contextual exceptions such as ignorance, emergency, custom conflict, or privilege.
- Conflict override targets and deterministic precedence.
- Consequence definitions targeting attitudes, reputation, references, interactions, or promises.

Prototype fallback definitions are supplied by `PrototypeSocialNormDefinitionFactory`. Catalog-authored definitions take precedence, so fallback definitions can be retired naturally as real assets are authored.

## Evaluation

`Preview` performs a full read-only assessment and returns candidate records without mutating runtime state or downstream runtimes.

`Execute` validates the request, resolves applicable definitions, applies exceptions, resolves conflicts, plans consequences, commits required consequences atomically, records stable assessment snapshots, and stores processed transaction IDs for idempotence.

Duplicate transaction IDs return the original committed assessment set without applying a second mutation.

## Persistence

`SocialNormPersistenceParticipant` captures and restores only social norm assessment and dedupe state. Prepare validates:

- Schema version.
- Assessment IDs and transaction IDs.
- Referenced norm definitions.
- Known actors, targets, witnesses, and observers.
- Processed transaction references.

Commit restores through the runtime and rolls back if an unexpected commit failure occurs.

## Test Lab

The Test Lab fixture bundle now includes `SocialNormRuntime` in fresh, shared, persistent, snapshot, restore, and fingerprint flows. This allows command-side and in-game automation to share the same scenario definitions and mutation ownership checks.

The automation suite is:

`feature.12.6.social-norms-etiquette-contextual-expectations`

It covers readiness, non-mutating previews, public/private context, actor knowledge, exceptions, observer interpretation, conflict resolution, promise breach assessment, persistence, and idempotence.

## Boundaries

Feature 12.6 intentionally does not implement law, courts, government authority, autonomous NPC planning, dialogue UI, or full culture simulation. Those systems can later provide context to social norms or consume norm assessments, but they should not be implemented inside `SocialNormRuntime`.
