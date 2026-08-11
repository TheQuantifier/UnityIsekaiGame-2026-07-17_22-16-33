# Feature 15.2 - Quest Availability, Eligibility, Offering, Acceptance, Assignment, and Abandonment

Feature 15.2 adds participant-level quest state on top of the quest identity records from Feature 15.1.

## Ownership

`QuestRuntime` remains the owner of quest identity, definitions, lifecycle, issuer references, recipient metadata, origins, subject links, and visibility metadata.

`QuestParticipationRuntime` owns:

- availability evaluation results;
- eligibility evaluation against caller-supplied facts;
- offer records;
- acceptance and refusal records;
- assignment records;
- abandonment, withdrawal, suspension, resume, and expiration transitions;
- participation transactions and event history.

The participation runtime does not own person, organization, location, knowledge, reputation, profession, legal, or inventory records. Those systems provide facts through `QuestEligibilityContext` and `QuestEligibilityFactSet`.

## Core Distinctions

The implementation keeps these states separate:

- quest exists versus quest is available;
- available versus eligible;
- eligible versus offered;
- offered versus accepted;
- accepted versus assigned;
- abandoned versus failed or completed;
- issuer authority versus recipient consent.

Objective progress and quest completion remain outside Feature 15.2.

## Prototype Policies

`PrototypeQuestDefinitionFactory` now authors representative participation policies for the prototype quest definitions:

- Guild postings are exclusive, consent-based, authority-gated, and require guild counter context.
- Merchant delivery quests are nonexclusive, consent-based, and offered through the merchant counter.
- Civic investigations allow direct institutional assignment and require civic eligibility.
- Hidden dungeon rumors can be offered without prevalidating eligibility and preserve hidden visibility.
- Dynamic bounties are capacity-limited and board-authorized.

## Persistence

`QuestParticipationRuntimePersistenceParticipant` saves participation state under `world.quest-participation`.

It requires the quest identity participant (`world.quests`) to restore first, validates all referenced quest IDs and definitions before commit, and rolls back if a commit unexpectedly fails.

## Validation

Feature coverage includes:

- definition policy validation;
- availability and eligibility separation;
- preview and idempotent offer creation;
- acceptance revalidation and consent;
- exclusive capacity and stale acceptance rejection;
- abandonment capacity release;
- visibility-safe offer and assignment queries;
- save/restore without replaying participation events;
- failed prepare without mutating live runtime state.
