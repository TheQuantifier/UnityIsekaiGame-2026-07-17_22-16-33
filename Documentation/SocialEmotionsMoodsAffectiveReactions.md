# Social Emotions, Moods, and Affective Reactions

Feature 12.10 adds an authoritative social emotion runtime for transient emotions, longer-lived mood state, and affective decision modifiers.

## Ownership Boundary

`SocialEmotionRuntime` owns only affective state:

- emotion episodes
- mood dimension state
- emotion provenance and appraisal references
- emotion-derived social decision modifiers
- emotion save and restore validation

It does not own relationship records, attitude values, memory, knowledge, social interactions, influence attempts, reputation, social networks, or final NPC decision state. Those systems remain authoritative and may provide causes or context for appraisal.

## Definitions

The prototype fallback factory registers canonical alpha emotions, mood dimensions, and appraisal rules when authored catalog assets do not already exist. Authored catalog definitions still take precedence.

Canonical prototype emotions include joy, sadness, anger, fear, relief, gratitude, guilt, shame, pride, anxiety, disgust, envy, resentment, hope, and disappointment.

Mood dimensions include valence, arousal, anxiety, social openness, aggression, and morale.

## Runtime Behavior

Emotion creation supports preview and execute paths. Preview returns the episode and mood projection that would be produced without mutating runtime state. Execute records a stable episode, updates mood state, and creates a decision modifier when the emotion or appraisal rule defines one.

Transactions are idempotent. A duplicate transaction returns the original episode result without a second mutation.

Intensity, duration, decay, stacking, and mood updates are deterministic and based only on request data, definitions, and world time. The runtime does not use frame time or system time.

## Appraisal

Appraisal rules map cause categories, believed truth state, detection outcome, responsibility, and tags to an emotion definition. This keeps affective reactions belief-relative without requiring the emotion runtime to own knowledge or influence records.

For example, an accepted threat can create fear because the character believes the threat, while detected deception can create anger because the character appraises another person as responsible.

## Projections

Emotion projections are explicit:

- owner or privileged access receives the full episode snapshot
- concealed or internal emotions are hidden from other requesters
- observable emotions may be redacted for non-owner requesters

Projection does not mutate emotion state and does not create knowledge, memory, social interaction, or influence records.

## Persistence

`SocialEmotionPersistenceParticipant` saves and restores world social-emotion state. Prepare validates schema version, known persons, duplicate episode and mood identities, and referenced emotion, mood, and appraisal definitions before commit.

Restore is transactional: invalid payloads are rejected before live runtime mutation.

## Integration

`SocialDecisionModifierSourceCollection` composes influence-derived and emotion-derived score modifiers for `SocialDecisionRuntime`. This lets emotions influence decisions without making the emotion runtime responsible for decision ownership.

Prototype persistence, Test Lab fixture ownership, runtime snapshots, fingerprints, and automation bindings include social emotions so command-side and in-game automation run against the same runtime graph.
