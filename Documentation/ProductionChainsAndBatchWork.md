# Production Chains and Batch Work

Feature 9.8 adds persistent production workflow foundations on top of the existing Step 9 item, production requirement, recipe, and crafting execution systems.

## Core Boundaries

A recipe describes one atomic production operation. A crafting operation is one Feature 9.7 execution attempt. A production chain is a versioned definition of dependent stages. A production job is one persistent runtime execution of a recipe or chain. A work order is an instruction to produce something; it does not own recipe truth, inventory, or output state.

Reservation is not consumption. Station occupancy is not ownership. Batch identity does not replace item identity. Lot identity groups traceable materials or outputs, but exact item instances remain authoritative in the item identity runtime.

## Runtime Ownership

`ProductionWorkflowRuntime` owns work orders, production jobs, stage progress, queues, batches, lots, intermediates, station occupancies, worker assignments, output collection state, provenance summaries, and workflow events.

It does not own item records, composition, quality, affixes, durability, inventory, persons, recipe definitions, station definitions, or production requirement definitions. Those remain in their existing authorities and are referenced by stable IDs.

## Chains and Stages

`ProductionChainDefinition` is a catalog-authored immutable definition. It contains chain versions and stage definitions. Active jobs retain the exact chain version they were created with, so definition updates do not silently alter in-progress work.

Validation rejects missing versions, circular version lineage, duplicate stage IDs, missing dependencies, stage graph cycles, invalid repeat counts, missing recipe references, and missing production requirement references.

## Jobs, Queues, Batches, and Lots

Jobs have stable IDs and reference work orders, recipe or chain IDs, exact versions, batches, lots, intermediates, workers, occupancies, and output items.

Queues are deterministic. The default policy is priority then stable job ID. Manual ordering is explicit.

Batches group outputs produced by a job. Lots track material or output lineage, including split and merge parent/child relationships. Lot merge requires compatible definition/material IDs and units.

## Time and Progress

Production progress uses explicit world-time evaluation. Jobs do not progress through wall-clock time, frame time, or a global per-frame loop. Re-evaluating the same time boundary is idempotent and does not change runtime revision.

Paused, blocked, interrupted, cancelled, failed, and completed jobs do not accrue progress. Stage completion is a separate commit boundary and can reuse Feature 9.7 crafting execution to create output item graphs.

## Failure Boundaries

Job creation and stage transitions are atomic. Failed creation leaves no job, batch, queue entry, reservation, occupancy, or event. Stage-start failures roll back occupancy and workflow state. Stage completion failures roll back workflow-owned state and rely on Feature 9.7 rollback for item-side crafting effects.

Cancellation distinguishes uncommitted progress from completed intermediates and outputs. Already completed stages are not restarted. Remaining active occupancies are released.

## Persistence

`ProductionWorkflowPersistenceParticipant` persists runtime-owned production state and validates the graph before commit. It depends on item identity, production requirements, and crafting execution. It stores references to external authorities rather than duplicating their state.

Restore preserves job IDs, exact definition versions, stage progress, batches, lots, intermediates, queues, assignments, occupancies, output collection state, runtime revisions, and event sequence. Restore does not replay production, consume inputs, apply wear, spend resources, recreate outputs, or emit events.

## Access and Knowledge

Production jobs can be projected as Step 8 information subjects. Privileged/internal projections expose full workflow fields. Public projections redact stages, reservation IDs, worker assignment IDs, hidden material details, and provenance-sensitive fields.

## Deferred Work

Feature 9.8 does not implement recipe experimentation, recipe discovery, broad NPC economic simulation, market supply and demand, final workshop UI, autonomous business management, networking, or Step 9 finalization. Feature 9.9 should build experimentation/discovery on these contracts. Feature 9.10 should audit integration, performance, migrations, UI-facing projections, and final Step 9 readiness.
