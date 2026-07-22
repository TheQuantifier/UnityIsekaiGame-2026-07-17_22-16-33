# Biological Resources and Vital Processes

Feature 7.4 adds body-owned vital process state for alpha biological simulation.

## Ownership

`ActorBodyRuntime` owns `VitalProcessRuntime` beside Anatomy and Body Condition. The runtime is keyed by the exact Actor/body ID, not by the player, inventory, or scene object alone. This keeps replacement bodies, species changes, and future multiplayer ownership boundaries explicit.

The runtime depends on:

- `SpeciesDefinition` for profile selection.
- `AnatomySnapshot` for exact body structure and revision checks.
- `BodyConditionSnapshot` for capacity effects from damaged or missing structures.
- `DefinitionRegistry` for canonical resource/profile definitions.

## Definitions

Canonical biological resources are registered in the production content catalog:

- `resource.biology.blood`
- `resource.biology.breath`
- `resource.biology.temperature`
- `resource.biology.nutrition`
- `resource.biology.hydration`
- `resource.biology.sleep-need`
- `resource.biology.fatigue`

Vital process profiles map these resources to Species:

- `vital-profile.human`
- `vital-profile.undead-human`
- `vital-profile.basic-construct`
- `vital-profile.basic-spirit`

Profiles define whether each resource is active, its bounds, initial value, safe/strained/critical thresholds, and deterministic per-hour process rates.

## Runtime Models

The runtime supports three active resource models:

- `DepletingPool`: lower values are worse, used by Blood, Breath, Nutrition, and Hydration.
- `AccumulatingNeed`: higher values are worse, used by Sleep Need and Fatigue.
- `TargetCenteredValue`: both low and high values can be bad, used by Temperature.

Inactive resources remain queryable in snapshots but reject mutations with `InactiveResource`. This is how constructs and spirits can have canonical biological resources without pretending they breathe or bleed.

## Mutation Rules

Preview and execution share the same validation and calculation path. Preview does not mutate values, increment revisions, register transaction IDs, or emit events.

Execution revalidates exact body identity, anatomy revision, condition revision, resource activity, finite amounts, and transaction ID. Duplicate transaction IDs are idempotent and do not apply a second mutation.

## Body Condition Interaction

Feature 7.4 does not implement bleeding, suffocation, poison, disease, hypothermia damage, or hazard loops. It only establishes state and capacity hooks.

Current alpha capacity integration:

- Damaged or failed lungs reduce effective Breath capacity.
- Damaged or failed heart reduces effective Blood capacity.

This links Body Condition to Vital Processes without creating hazard damage or lifecycle transitions yet.

## Persistence

`BodySaveData` schema version 4 includes `VitalProcessSaveData`.

Restore validates:

- saved body ID matches the restored body;
- saved Species matches the restored Species;
- resource IDs are unique and resolve;
- saved values fit authored bounds.

Restore rebuilds Anatomy and Body Condition first, then restores Vital Processes silently. No vital mutation events are emitted during restore.

Older body saves without vital data rebuild fresh vital processes from the current Species profile.

## Deferred Work

The following are intentionally not implemented in Feature 7.4:

- bleeding damage over time;
- suffocation or drowning;
- starvation and dehydration harm;
- sleep deprivation effects;
- fatigue penalties;
- poison and disease progression;
- environmental heat/cold hazard evaluation;
- direct lifecycle state transitions from vital pressure.

Those systems should consume the runtime snapshots and lifecycle pressure flags in later features instead of owning vital state directly.
