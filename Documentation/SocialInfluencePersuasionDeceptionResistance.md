# Feature 12.9 - Social Influence, Persuasion, Deception, and Resistance

Feature 12.9 adds the authoritative runtime for social influence attempts. It models persuasion, deception, resistance, compliance pressure, and temporary decision modifiers without taking ownership away from earlier systems.

## Authority Boundaries

- `SocialInfluenceRuntime` owns influence attempt records, processed transactions, method cooldowns, deception/detection outcomes, and influence-created decision modifiers.
- `PersonKnowledgeRuntime` remains the owner of evidence and beliefs. Influence can submit testimony evidence, but it does not store beliefs as its own state.
- `SocialInteractionRuntime` remains the owner of promises, accepted requests, and other social interaction records. Influence can request an interaction when compliance succeeds.
- `SocialDecisionRuntime` remains the owner of NPC action selection. Influence only exposes bounded score modifiers through `ISocialDecisionModifierSource`.
- `InterpersonalAttitudeRuntime` remains the owner of trust, hostility, affection, respect, and loyalty values. Influence can submit source-owned attitude mutations when deception is detected.

## Runtime Behavior

Influence attempts are definition-backed by `SocialInfluenceMethodDefinition`. Prototype fallback definitions are supplied by `PrototypeSocialInfluenceDefinitionFactory` and are registered through the same catalog/fallback pipeline as the rest of the prototype social stack.

Each request records:

- Speaker, target, witnesses, method, intent, subject, and optional claim.
- Evidence references and argument payloads.
- Truth status, speaker belief state, deception mode, honesty classification, and detection outcome.
- Belief outcome and compliance outcome as separate concepts.
- Deterministic influence score, resistance score, margin, and margin class.
- Optional knowledge evidence ID, social interaction record ID, and decision modifier ID.

Preview requests perform the same deterministic calculation without mutating runtime state, knowledge, interactions, attitudes, cooldowns, or decision modifiers.

## Determinism

The runtime derives attempt IDs and random-like rolls from SHA-256 stable tokens. It does not use system time, frame time, object instance IDs, or process-randomized string hashing. The same request, seed, and runtime state produce the same preview and execution outcome.

## Persistence

`SocialInfluencePersistenceParticipant` stores world-scoped influence state under `world.social-influence`. Prepare validation rejects unsupported schema versions, duplicate attempt/modifier IDs, unknown people, and missing influence method definitions before any live runtime mutation. Commit uses rollback if restore unexpectedly fails after prepare.

## Test Lab

The `feature.12.9.social-influence-persuasion-deception` automation suite verifies:

- Influence definitions resolve and preview without mutation.
- Belief influence writes through the knowledge runtime.
- Compliance outcomes write through the social interaction runtime.
- Detected deception mutates trust and hostility through attitude runtime.
- Decision modifiers affect Step 12.8 scoring without decision ownership moving to influence.
- Save/restore preserves state and rejects corrupt payloads.

## Future Integration Rules

Do not bypass `SocialInfluenceRuntime` for persuasion/deception mechanics in gameplay code. Callers should submit an influence request and consume the returned projection. Do not write direct belief, attitude, or decision state as a substitute for an influence attempt when the player or NPC is trying to persuade, intimidate, mislead, confess, deny, reassure, or influence another actor.
