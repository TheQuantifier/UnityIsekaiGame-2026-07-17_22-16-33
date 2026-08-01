# Feature 13.1 - Organization Identity and Records

Feature 13.1 establishes organizations as persistent world entities without taking ownership of memberships, ranks, offices, laws, government behavior, faction power, or resources.

## Ownership

`OrganizationRuntime` is the authoritative owner of organization identity records. It owns stable organization IDs, lifecycle state, current and historical names, public aliases, parent/branch/affiliate/successor links, headquarters references, operating-area references, visibility flags, provenance references, and persistence state.

Other systems may reference organization IDs, but they should not duplicate organization identity state. Profession positions, economy accounts, social reputation, historical events, and future faction/government systems should treat organization IDs as references into this runtime.

## Definitions

`OrganizationDefinition` defines broad organization types and capabilities:

- category, such as guild, company, institution, religious order, civic body, household, or secret society
- supported visibility classes
- allowed aliases, branches, affiliates, headquarters, operating areas, dissolution, and successors
- optional social norm references and tags

Prototype definitions are supplied by `PrototypeOrganizationDefinitionFactory`. Catalog-authored definitions remain authoritative when present, and the prototype factory only fills missing prototype definitions so Test Lab, persistence, and runtime validation resolve the same IDs.

## Runtime Behavior

The runtime provides transactional operations for:

- creating organizations
- renaming organizations while preserving former official names
- lifecycle transitions
- setting headquarters and operating areas
- linking parent, branch, affiliate, predecessor, and successor records

Operations are idempotent by transaction ID. Preview operations restore the previous state after producing a result.

Validation is strict. Save/restore and live mutation reject missing definitions, duplicate stable IDs, missing official names, invalid lifecycle states, invalid known person/place references, unsupported definition capabilities, multiple active official names, self-links, and hierarchy cycles before commit.

## Access Boundary

Organizations expose Step 8 subject references through `InformationSubjectType.Organization`. Projections can return full, redacted, concealed, or denied results according to the organization visibility and privileged access context.

Normal projection reads do not mutate knowledge, memory, history, social state, or organization records.

## Persistence

`OrganizationPersistenceParticipant` stores organizations in the shared world scope under `world.organizations`. It validates payloads during prepare and rolls back on failed commits. Organization persistence is optional for legacy saves, but saved organization payloads must pass full graph validation.

## Test Lab

Feature 13.1 automation is registered under `feature.13.1.organization-identity-records`. The suite uses fixture-owned fresh runtimes and is command-line compatible because it does not require scene objects.

Covered behaviors include definition readiness, seeded prototype organizations, create/rename/lifecycle idempotence, hierarchy link cycle rejection, visibility projections, save/restore, and corrupt payload rejection.
