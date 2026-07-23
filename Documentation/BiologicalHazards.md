# Feature 7.5 - Biological Hazards

Biological hazards are body-owned runtime state layered on top of Feature 7.4 vital processes. They do not replace vital resources, body condition, Step 6 damage, or lifecycle logic.

## Ownership

The ownership path is:

`Person -> current Actor/body -> ActorBodyRuntime -> AnatomyRuntime -> BodyConditionRuntime -> VitalProcessRuntime -> BiologicalHazardRuntime`

Hazards belong to the exact Actor/body. A Person can receive a new body later without inheriting the old body's active hazard instances.

## Canonical Hazards

The alpha catalog registers:

- `hazard.biology.bleeding`
- `hazard.biology.suffocation`
- `hazard.biology.overheating`
- `hazard.biology.hypothermia`
- `hazard.biology.starvation`
- `hazard.biology.dehydration`
- `hazard.biology.extreme-fatigue`
- `hazard.biology.sleep-deprivation`
- `hazard.environment.exposure`

Environmental exposure definitions map world/exposure causes to hazard definitions:

- `exposure.environment.breathable-air-unavailable`
- `exposure.environment.heat`
- `exposure.environment.cold`
- `exposure.environment.general`

## Sources and Stacking

Each active hazard has stable source contribution IDs. Removing one source removes only that source; other sources for the same hazard remain active.

Supported stacking policies are:

- `Independent`
- `MergeSources`
- `AdditiveRate`
- `StrongestSource`
- `MaximumSeverity`
- `RefreshDuration`
- `ReplaceSameSource`
- `NonStacking`

Severity values are:

- `Trace`
- `Minor`
- `Moderate`
- `Serious`
- `Severe`
- `Critical`
- `Catastrophic`

## Vital Process Integration

Hazard ticks use `VitalProcessRuntime.PreviewMutation` and `VitalProcessRuntime.ApplyMutation`. The hazard runtime never changes vital resource values directly.

Preview ticks do not mutate hazards, vital resources, revisions, committed transaction IDs, or emit hazard/vital execution events.

Execution ticks commit once through the existing vital mutation API and use transaction IDs derived from the hazard tick transaction.

## Step 6 Boundary

Step 6 remains the owner of Health damage. Biological hazards can produce `BiologicalHazardDamagePlan` entries for future Step 6 execution, but this runtime does not directly mutate Health.

## Lifecycle Boundary

Lifecycle state remains authoritative outside the hazard runtime. Hazards can report lifecycle evaluation requests in tick consequences; they do not directly set Defeated, Unconscious, Dead, or Recovered states.

## Persistence

`BodySaveData` is schema version 5 and includes `BiologicalHazardSaveData`.

Restore validates the exact Actor/body ID, restores hazards after vitals, preserves active hazard sources, and suppresses hazard activation/tick events during restore.

Older body save schemas rebuild an empty hazard runtime from the restored body/vital state.

## Deferred

The following are intentionally deferred:

- biological resistances and vulnerabilities;
- disease, infection, poison, and toxin content;
- natural healing and treatment gameplay;
- food, drink, and sleep interactions;
- construct repair and non-biological equivalents;
- atmosphere, weather, underwater, and shelter simulation;
- AI responses;
- production UI;
- transformation/corpse/decomposition logic;
- networking/server authority.
