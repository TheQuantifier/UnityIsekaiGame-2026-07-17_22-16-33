# Step 7 Body and Biology Integration

Feature 7.10 finalizes Step 7 by giving future systems one stable production boundary for body and biology state.

## Public Runtime Boundary

`BodyBiologyFacade` is the high-level read and simulation entry point for body biology. It wraps an existing `ActorBodyRuntime`; it does not own body state and does not replace subsystem runtimes.

Stable public calls:

- `CaptureSnapshot()` returns one immutable aggregate view.
- `Validate()` checks readiness, ownership, and revision coherence.
- `PreviewAdvance(request)` evaluates the integrated biological tick without mutation.
- `Advance(request)` commits through existing owner APIs in deterministic order.

Systems that need body facts should prefer the facade or its immutable snapshot instead of directly querying every subsystem.

## Ownership Matrix

- `ActorBodyRuntime` owns exact Actor/body identity, Person/body association, Species, body form, classification, body revision, and lifecycle of Step 7 subsystem runtimes.
- `AnatomyRuntime` owns stable anatomy nodes, hierarchy, body regions, body parts, organs, and structural presence.
- `BodyConditionRuntime` owns localized structural damage, injuries, integrity, structural state, functional state, severing, and missing structures.
- `VitalProcessRuntime` owns biological resources and vital resource mutation.
- `BiologicalHazardRuntime` owns active biological hazards, hazard sources, suppression, and hazard ticking.
- `BiologicalCompatibilityRuntime` owns compatibility rule contributions and interaction evaluation.
- `BiologicalRecoveryRuntime` owns recovery processes, rest context, rate modifiers, and repair/recovery ticking.
- `BodyTransformationRuntime` owns transformation plans, temporary transformation state, reassociation plans, and transformation transaction replay safety.
- `BiologicalConditionRuntime` owns diseases, poisons, symptoms, treatments, immunity memory, and biological condition consequence planning.

No Step 7 facade method mutates subsystem collections directly.

## Aggregate Snapshot

`BodyBiologySnapshot` combines:

- `BodySnapshot`;
- `BiologicalConditionRuntimeSnapshot`;
- `BodyTransformationSnapshot`;
- `BodyBiologyRevisionSet`.

Snapshot capture retries if revisions change while reading. If the revisions remain unstable, the snapshot is returned as incoherent with diagnostics rather than pretending it is safe.

## Deterministic Simulation Order

Integrated advance uses this order:

1. Biological Condition consequences.
2. Biological Hazards.
3. Vital Processes.
4. Biological Recovery.

This order is intentional. Conditions can create pressure, hazards apply active environmental/biological effects, vitals normalize current physiological resources, and recovery resolves after current damage and pressure have been considered.

## Preview Contract

`PreviewAdvance` calls existing preview APIs and must not:

- mutate subsystem state;
- consume duplicate transaction IDs;
- mark save data dirty;
- emit gameplay execution events;
- change revisions.

## Commit Contract

`Advance` commits exactly through existing owner APIs. It uses deterministic subtransaction IDs:

- `.conditions`
- `.hazards`
- `.vitals`
- `.recovery`

Duplicate protection remains owned by the existing subsystem transaction stores.

The aggregate commit is ordered-coherent, not fully atomic. If a later subsystem rejects a request, the result reports the failed step and current snapshot. It does not claim prepare-all/commit-all rollback for subsystems that do not expose that transaction protocol yet.

## Persistence and Restore

Step 7 persistence remains owned by `ActorBodyRuntime` and its subsystem save data. Restore continues to validate exact body, Person, Species, anatomy, condition, vitals, hazards, compatibility, biological conditions, recovery, and transformation through the existing body save contract.

The facade does not serialize independent state. Its snapshots are read models only.

Minimum supported Step 7 body schema is the current `BodySaveData` schema produced after Feature 7.9. Earlier pre-alpha saves may be invalidated rather than silently migrated when stable body ownership cannot be proven.

## Compatibility Enforcement

Feature 7.6 compatibility remains authoritative for:

- biological condition exposure;
- localized injury compatibility;
- hazards and recovery where authored;
- transformation compatibility and suppression.

Feature 7.10 does not bypass compatibility. It only coordinates systems that already enforce it.

## ID Contracts

- Definition IDs remain canonical catalog IDs, such as `capability.can.die`.
- Runtime capability keys remain runtime keys, such as `can.die`.
- Contribution entry IDs identify the owning contribution source and are used for source-safe removal.
- Runtime instance IDs remain runtime instance IDs and are not definition IDs.

## Event Behavior

Preview emits no owner execution events. Restore should replay no biological simulation events. Commit events remain owned by the subsystem that performed the mutation; the facade only returns aggregate diagnostics.

## Long-Interval Policy

The facade passes elapsed game seconds consistently to all participating subsystems. It does not add hidden clamping. Any future long-offline progression cap should be introduced as an authored policy and applied before calling `PreviewAdvance` or `Advance`.

## Step 8 Contracts

Knowledge, discovery, and character-history systems should consume:

- `BodyBiologyFacade.CaptureSnapshot()` for current body facts;
- stable definition IDs for authored facts;
- runtime capability keys only when evaluating runtime behavior;
- immutable snapshots for history records.

Step 8 systems should not mutate Step 7 subsystem state directly.

## Manual Test Plan

1. Open `PrototypeScene`.
2. Enter Play Mode.
3. Open the Tab menu, then Test Lab.
4. In `Body Step 7`, open `Biology Integration 7.10`.
5. Click `Human`, then `Inspect`, then `Validate`.
6. Click `Preview Tick`; confirm the result is preview and no revision-changing failure appears.
7. Click `Fever`, then `Advance`; confirm the operation succeeds.
8. Click `Duplicate`; confirm duplicate protection succeeds.
9. Open Automation, select `Feature 7.10 Biological Integration`, run the suite, and confirm all scenarios pass.
