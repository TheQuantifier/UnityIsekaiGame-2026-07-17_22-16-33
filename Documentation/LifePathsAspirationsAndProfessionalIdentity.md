# Life Paths, Aspirations, and Professional Identity

Feature 10.9 models the personal direction layer that sits above Step 10 profession systems. It records what a Person wants, what goals they are pursuing, how they understand their professional identity, and which achievements or setbacks matter to that life path.

## Ownership

Life-path state references authoritative systems instead of replacing them:

- Profession relationships remain owned by `PersonProfessionRuntime`.
- Training remains owned by `TrainingRuntime`.
- Activities and experience remain owned by `ProfessionalActivityRuntime`.
- Credentials remain owned by `CredentialRuntime`.
- Ranks remain owned by `ProfessionalRankRuntime`.
- Positions and employment remain owned by `PositionEmploymentRuntime`.
- Career episodes and transitions remain owned by `CareerHistoryRuntime`.

`LifePathRuntime` owns only life-path records, aspirations, goals, professional identity records, identity conflicts, and life-path achievement or setback references.

## Life-Path Records

A life-path record identifies a Person's broad active, paused, completed, abandoned, or retired trajectory. It may include formative references such as origin, upbringing, education, mentor, culture, social class, traumatic event, inspiration, obligation, or custom foundation data.

Formative references are stable pointers. They do not create world history, knowledge, or profession state by themselves.

## Aspirations

`AspirationDefinition` is catalog-authored and immutable. A runtime `PersonAspirationData` links a Person to one definition and records target profession, specialization, rank, credential, position, organization, item, activity, or custom target references.

Aspiration lifecycle states are explicit. Aspirations can be planned, active, paused, fulfilled, abandoned, replaced, conflicted, secret, dormant, or custom. Terminal aspirations do not silently become active again.

## Goals

`LifeGoalDefinition` is catalog-authored and immutable. A runtime `PersonGoalData` tracks one Person's pursuit of a defined target. Goals can depend on other goals, list alternatives, carry conflict tags, and track perceived progress separately from authoritative completion.

Goal evaluation reads authoritative systems through adapters. For example, a credential goal checks `CredentialRuntime`; a rank goal checks `ProfessionalRankRuntime`; an activity goal checks `ProfessionalActivityRuntime`.

Goal completion requires a current progress evaluation token. If a source runtime changes between evaluation and completion, completion is rejected as stale.

## Authoritative And Perceived Progress

Authoritative progress is derived from source runtimes. Perceived progress is the Person-relative self-understanding stored on the goal. A goal can be perceived as satisfied while authoritative requirements are still incomplete.

This distinction keeps personal belief and motivation from mutating profession, rank, credential, training, position, or activity state.

## Professional Identity

Professional identity records describe how a Person sees or presents their vocation. Identity may align with, conflict with, or remain separate from actual profession relationships.

A secret or self-perceived identity does not grant a profession relationship. Public projections redact secret identities and conflicts unless the caller is privileged or the subject Person.

## Conflicts

Identity conflicts and goal conflicts are explicit records. They can reference identities, aspirations, and goals without mutating those records directly. Conflict resolution is a separate mutation with its own revision.

## Achievements And Setbacks

Life-path achievements and setbacks reference source records such as profession relationships, training enrollments, professional activities, credentials, ranks, employment, career episodes, transitions, knowledge records, historical events, items, or custom foundations.

Exclusive references prevent double-counting the same source milestone.

## Access And Knowledge

Life-path records expose Step 8 subject references for life paths, aspirations, goals, goal progress, identities, conflicts, achievements, setbacks, origins, formative references, and professional self-concept.

Access-aware projections return full, redacted, concealed, or denied snapshots without creating knowledge, memories, history, or gameplay audits.

## Persistence

`LifePathPersistenceParticipant` persists runtime-owned life-path data with schema validation. Restore validates all known Person references, catalog definition references, internal links, and source references before commit.

Failed restore leaves live runtime state unchanged. Restore also clears transient history hooks so persistence does not replay life-path events.

## Validation

Validation rejects:

- Unknown Persons.
- Unknown aspiration or goal definitions.
- Missing parent aspiration, dependency, alternative, identity, goal, or life-path references.
- Duplicate stable IDs.
- Invalid source references.
- Completed goals with remaining requirements.
- Multiple active primary professional identities for one Person.
- Terminal aspiration or goal lifecycle reversals.
- Stale progress completion.

## Test Lab

Feature 10.9 automation covers definition validation, life-path record creation, aspiration and goal creation, authoritative progress, stale progress rejection, professional identity redaction, lifecycle conflict handling, achievements and setbacks, persistence, and corrupt restore behavior.

The automation uses fixture-owned fresh runtimes and run-scoped IDs so repeated, shuffled, command-line, and in-game runs do not leak state.

## Deferred Features

Feature 10.9 does not implement autonomous NPC planning, motivation AI, satisfaction, regret, social reputation, personality simulation, wages, labor markets, family destiny generation, or final UI. Later systems may consume life-path data as input, but those systems remain separate authorities.
