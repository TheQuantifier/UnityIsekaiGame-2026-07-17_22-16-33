# Experimentation And Discovery

Feature 9.9 adds a production experimentation layer for controlled tests, reverse engineering, failed attempts, accidental outcomes, recipe inference, substitutions, reproducibility, and discovery claims.

Experimentation does not own authoritative recipes, item state, production jobs, history, or knowledge. It records experiment-specific meaning and references those owning runtimes by stable ID.

## Ownership

`ExperimentDefinition` is the authored catalog contract for a kind of test. It defines supported targets, variables, required controls, observation requirements, production requirements, and confirmation policy.

`ExperimentationRuntime` owns:

- hypotheses and typed claims;
- exact experiment plans;
- runs and trials;
- measurements;
- links to crafting operations and production jobs;
- evidence IDs produced through Step 8 knowledge;
- inferences;
- discovery claims and reviews;
- registration proposals;
- experiment logs;
- experimentation persistence.

It does not mutate `RecipeDefinition` or `ProductionChainDefinition`. A confirmed discovery may create a `RecipeRegistrationProposalData`, but authoritative catalog registration is left to a later authored workflow.

## Evidence Boundary

An experiment result is not automatically truth. The flow remains explicit:

1. a run or trial occurs;
2. a Person observes or interprets it;
3. `PersonKnowledgeRuntime` records evidence;
4. a hypothesis gains support or contradiction;
5. an inference may be recorded;
6. a discovery claim may be reviewed;
7. a recipe registration proposal may be submitted with explicit authorization.

This preserves the difference between production outcome, observation, evidence, belief, hypothesis support, discovery, and authority.

## Persistence

Experimentation saves as `world.experimentation-discovery`. Restore validates references before commit and rolls back on failure. Snapshot-restore Test Lab runs include experimentation in their mutation fingerprint.

## Access

Experiment runs can be projected through `InformationAccessRuntime`. Full access exposes trials, evidence, outputs, and provenance. Redacted access preserves the existence and safe summary of the run while hiding sensitive trial and evidence detail.

## Feature 9.10 Contracts

Feature 9.10 should consume these contracts:

- `ExperimentationRuntime.CreateSaveData()` and `RestoreFromSaveData(...)` for integration audit.
- `ExperimentationRuntime.ProjectRun(...)` for access-safe UI/debug views.
- `RecipeRegistrationProposalData` as the bridge from confirmed discovery to authoritative authored recipe registration.
- Stable links to crafting operation IDs and production job IDs instead of copied records.
- Explicit Step 8 evidence IDs for Person-relative learning.

Feature 9.10 should verify integration completeness, but it should not convert experimentation into the owner of recipes, production workflows, or knowledge records.
