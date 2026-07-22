# Body Condition, Injury, and Structural Damage

Feature 7.3 adds the first localized body-condition runtime. The system is owned by `ActorBodyRuntime` through `BodyConditionRuntime`, which is built from the current exact Actor/body ID and the ready `AnatomyRuntime` snapshot.

## Runtime Ownership

`BodyConditionRuntime` is not a global service and is not player-owned. It belongs to one exact Actor/body and stores:

- per-anatomy-node structure condition;
- active and resolved injury records;
- a condition revision;
- committed localized-damage transaction IDs.

The runtime is rebuilt when Species/Anatomy changes, restored from `BodySaveData`, and queried through immutable snapshots. Replacement bodies do not inherit condition state unless explicitly restored from compatible save data for the same Actor/body.

## Definitions

`InjuryTypeDefinition` is the authored definition for a category of injury, such as `injury.fracture`, `injury.laceration`, or `injury.incorporeal-disruption`. It can restrict compatible anatomy categories and carries future metadata flags for systems that are deliberately deferred.

`StructuralFailurePolicyDefinition` is a small authored policy marker for future lifecycle integration. Feature 7.3 records disabled/destroyed/vital-failure policy definitions, but does not yet make vital organ failure kill or defeat an Actor.

## Damage Flow

Localized structural damage uses `LocalizedStructuralDamageRequest`.

Preview:

- validates body, anatomy, node, injury definition, compatibility, and expected revisions;
- projects integrity, functional, structural, and presence outcomes;
- does not mutate structures or injuries;
- does not change revisions;
- does not emit condition events;
- does not consume transaction IDs.

Execution:

- revalidates the same request;
- applies one integrity mutation;
- records one injury record;
- records the transaction ID for idempotency;
- raises a condition event unless restoring.

Duplicate transaction IDs return a duplicate success result and do not apply additional structural damage.

## Persistence

`BodySaveData` schema version 3 now contains `BodyConditionSaveData`. Restore validates that:

- the saved body matches the current exact Actor/body;
- all saved structure and injury node IDs resolve in the restored Anatomy;
- saved injury definition IDs resolve in the catalog;
- structure integrity values are coherent.

Restore rebuilds healthy Anatomy-owned structure records first, overlays saved condition data, restores committed transaction IDs, and suppresses condition events.

## Deferred Systems

The following remain explicitly out of scope for 7.3:

- blood, bleeding, breath, suffocation, temperature, hunger, thirst, sleep;
- disease, poison, infection;
- pain, treatment, natural healing, repair, prosthetics;
- transformations and corpses;
- body visuals, hitboxes, ragdolls, and gore;
- networking/server authority.

Feature 7.4 and later can consume the condition snapshot and injury records without replacing the 7.3 data model.
