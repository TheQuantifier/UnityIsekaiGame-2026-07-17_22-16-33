# Test Lab Fixture Ownership

The old automation fixture model has been removed as an accepted pattern. Automated Test Lab scenarios must use `TestLabScenarioContext`, declared isolation mode, required runtime areas, required fixture IDs, scoped mutable IDs, fixture dependency preflight, pre-restore mutation auditing, isolation restore, and post-restore integrity checks.

Manual Test Lab buttons may still operate on the active shared prototype runtime when a developer intentionally clicks actions in sequence. Automation may not rely on manual shared state, previous scenario execution, hidden "last created" IDs, or fixed mutable runtime records.

## Core Rule

Definitions may be globally reusable. Mutable automation fixture instances must have explicit scope, ownership, dependencies, and lifecycle.

Fixed IDs are allowed for definitions, immutable canonical prototype entities, and explicitly reusable fixtures whose semantic identity never changes. Run-scoped IDs are required for memories, historical events, evidence, mutable beliefs, source instances, transfers, access policies, grants, denials, concealments, records, collections, suppressions, corrections, revisions, temporary bodies, and any runtime instance that can change state.

## Isolation Modes

- `FreshRuntime`: default for ordinary feature automation. The runner resets the Test Lab runtime, creates a scenario context, prepares required fixtures, audits mutations before cleanup, and disposes the scope after integrity checks.
- `SnapshotRestore`: reserved for scenarios that must exercise production-wired runtime restore behavior.
- `SharedRuntime`: reserved for named orchestration scopes where sharing is the behavior under test.
- `PersistentFixture`: reserved for deliberate multi-step integration workflows with an owning scope and cleanup boundary.

Do not add legacy, unmanaged, none, or skip-validation modes.

Each scenario must also declare the mutable runtime areas it needs through `RequiredRuntimeAreas`. `FreshRuntime` and `SnapshotRestore` are only valid when the fixture system can isolate every declared area. At present, automatic fresh/snapshot isolation is implemented for the Knowledge/History runtime graph. Scenarios that need Character, Combat, Biology, or Persistence runtime mutation must either use a non-isolated mode with an explicit mutable-state fixture scope or wait for a dedicated runtime fixture provider that can isolate that area.

## Fixture Providers

Fixture providers are grouped by bounded area under `Assets/_Project/Development/TestLab/Automation/Fixtures/`. The central registry resolves providers and validates dependencies, but feature-specific setup belongs in small provider classes.

Each provider must:

- declare a stable fixture ID;
- declare dependencies;
- prepare the fixture through `TestLabScenarioContext`;
- use scoped mutable IDs;
- return a typed handle or a handle payload;
- register owned mutations with the ledger;
- validate semantic equivalence before reusing existing records.

## Semantic Ensure

Reusable fixture creation must compare meaningful signatures, not only stable IDs. A signature should include owner, subject, definition ID, referenced IDs, payload kind, classification, visibility, state, source lineage, and important typed fields.

If an ID exists with a different signature, the ledger records a conflict and postflight fails. Fixtures must not overwrite or normalize conflicting records to satisfy setup.

## Runner Enforcement

Every automation scenario gets:

- a `TestLabScenarioContext`;
- a valid isolation mode;
- declared required runtime areas;
- default runtime and mutable-state fixture requirements;
- dependency preflight;
- an ownership ledger;
- scenario-scoped Knowledge, History, Memory, Source, Transfer, Access, and Record runtimes for automation;
- deterministic runtime fingerprints captured before and after scenario execution;
- a pre-restore mutation audit that catches undeclared record, revision, counter, and serialized-state changes;
- restore/disposal after the mutation audit;
- a post-restore baseline integrity check;
- reset before and after scenario execution;
- guaranteed scope disposal after scenario failure, exception, cleanup failure, mutation-audit failure, restore failure, or integrity failure.

Successful fixture checks are silent. Failures include suite ID, scenario ID, run ID, isolation mode, runtime areas, fixture ID, stable ID, expected signature, actual signature, runtime fingerprint diffs, and shuffle seed when applicable.

Nested automation runs preserve their parent scenario context. This matters for master suites that run child suites: the child scenario receives its own context and runtime bundle, then the parent context is restored after child cleanup.

The enforced lifecycle is:

```text
scenario execution
scenario cleanup
suite teardown
fixture.audit
fixture.restore
fixture.integrity
reset.cleanup
scope disposal
```

`fixture.audit` runs before restore so undeclared mutations cannot be hidden by cleanup. `SnapshotRestore` captures the mutable runtime bundle before scenario execution, audits live pre-restore diffs, restores the snapshot, and then verifies the restored runtime matches the baseline fingerprint. `SharedRuntime` and `PersistentFixture` use named run-scoped runtime bundles, not the global prototype scene runtime, and those bundles are disposed when the owning automation run ends.

## Runtime Resolution Boundary

Automation code must resolve mutable Step 8 runtime state through the active `TestLabScenarioContext`. The Test Lab service routes Knowledge, History, Memory, Information Source, Information Transfer, Information Access, and Knowledge Record operations through the scenario runtime bundle whenever automation is active.

Manual Test Lab buttons may still use the scene-wired runtime graph. Shared manual helpers that are still public must either:

- delegate to a fixture provider while automation is active; or
- be clearly manual-only and not called by registered automation scenarios.

## Automation Host Boundary

The automation runner is scene-independent. It does not depend on `PrototypeScene`, `PrototypeTestLabService`, `PrototypeTestLabView`, scene object names, or a Prototype player hierarchy. Scene-specific access is supplied through `ITestLabAutomationHost`.

A host must declare:

- stable host ID;
- display name and scene label for diagnostics;
- supported runtime areas;
- supported isolation modes;
- fresh-runtime, snapshot, shared-runtime, and persistent-fixture area coverage;
- definition context;
- persistence, reset, fingerprint, dirty-state, domain-event, visible-UI, and automated-execution features.

The runner resolves a host only when the scenario explicitly requires one. Hostless `FreshRuntime` scenarios use an isolated runtime bundle and a default immutable definition context. `SharedRuntime`, `SnapshotRestore`, `PersistentFixture`, non-isolated runtime areas, or scenarios with a required host ID are validated against the selected host before reset, setup, fixture preflight, or step execution.

Unsupported hosts fail as normal automation failures with diagnostics. They must not throw unhandled exceptions or silently fall back to Prototype.

## Host Registry

`TestLabAutomationHostRegistry` is the deterministic discovery point for scene hosts. It supports explicit registration, unregister, active-host resolution, lookup by stable host ID, duplicate-ID rejection, ambiguity detection, Unity object pruning, and test cleanup.

The additive scene policy is strict: if more than one active host is registered and no scenario specifies a host ID, automation fails with an ambiguity diagnostic. Selection is never based on scene load order.

Static registrations must be cleared by scene host lifecycle methods. Unity-object hosts are pruned if destroyed. Non-Unity adapter hosts, such as the Prototype adapter, must unregister explicitly when their owning menu/service is disabled or disposed.

## Prototype Host Adapter

`PrototypeTestLabAutomationHost` adapts the existing Prototype Test Lab service to the generic host contract. It reports Prototype capabilities, supplies the Prototype definition context, delegates runtime-bundle construction, exposes scene fingerprints, binds the active scenario runtime bundle around scenario execution, and clears run-scoped bundles at the automation run boundary.

Prototype Step 3 through Step 8 automation suites now explicitly require `host.prototype-test-lab`. This is intentional because those suites still call Prototype Test Lab manual-operation methods. The runner itself does not know about the Prototype service; the Prototype-specific suite and adapter layer do.

## Generic Scene Host

`TestLabAutomationHostBehaviour` is a reusable development-only scene component for non-Prototype scenes. It can be configured with a definition catalog and explicit capability flags. It supports isolated `FreshRuntime` and isolated `PersistentFixture` Knowledge/History bundles without requiring Prototype UI. It does not claim shared scene runtime or snapshot support unless the configured fields explicitly say so, and unsupported runtime construction fails with a structured diagnostic.

`Assets/_Project/Scenes/Development/TestLabGenericHostScene.unity` is the first non-Prototype development scene that carries this host. It uses the stable host ID `host.generic-test-lab`, references the prototype definition catalog as its explicit definition context, and intentionally contains no `PrototypeTestLabService` or `PrototypeTestLabView`.

Production gameplay scenes may omit this host entirely. Missing automation support is normal for production scenes and does not require production runtime code changes.

## Non-Prototype Support Modes

Non-Prototype automation currently has three support levels:

- Hostless `FreshRuntime` Knowledge/History: fully scene-independent and uses the runner's explicit default definition context.
- Generic scene-host `FreshRuntime` or `PersistentFixture` Knowledge/History: scene-present, Prototype-free, catalog-backed, and suitable for validating scene portability.
- Prototype adapter host: scene-bound compatibility layer for Step 3 through Step 8 suites that still call Prototype Test Lab operations.

Character, Combat, Biology, Persistence, shared-scene runtime, and full scene snapshot behavior are not claimed by the generic host until dedicated provider interfaces exist for those domains. A non-Prototype scene must fail compatibility for those areas instead of silently routing through Prototype.

## Definition Context

Definition access is represented by `TestLabDefinitionContext`. Catalog-authored definitions remain authoritative. Prototype fallbacks remain explicit and diagnostic. Missing definitions remain hard failures for runtime bundle construction or persistence validation; host registration is not a validation bypass.

Different hosts must not map the same stable definition ID to conflicting definitions. Cross-host persistence tests should only be written when the involved hosts declare compatible definition contexts.

Required definitions are declared on scenarios through `RequiredDefinitionIds`. The runner checks those IDs during compatibility preview before reset, setup, fixture preparation, or step execution. A host or hostless context with missing definitions or catalog validation errors fails as an automation compatibility failure.

Catalog-authored definitions override fallback definitions by construction: a host exposes one concrete definition context, and fallback availability is diagnostic metadata rather than an alternate lookup path. Do not implement silent fallback from one host's context to another host, static editor discovery, or the Prototype service.

## Compatibility Preview

Before executing a selected scenario, suite, quick run, full run, or rerun, the runner previews every selected scenario against the current host and definition context. If any selected scenario is incompatible, the run reports explicit `host.compatibility` failures and executes none of the selected scenario steps.

This all-or-nothing preflight prevents partial suites from mutating runtime state before a later scenario discovers a missing host, unsupported runtime area, missing definition, ambiguous host, or catalog conflict.

## Host Lifecycle During Runs

The runner records the selected host and host registry revision before scenario execution. Before every step, it verifies that the same host instance is still registered at the same registry revision. If a scene unload, additive-load change, duplicate host registration, or manual disposal changes the registry, automation fails with `host.continuity` and does not select a replacement host.

This makes scene transitions and host removal visible as deterministic automation failures instead of allowing the runner to continue against a different scene.

## Scene Compatibility Checklist

1. Add or register one automation host.
2. Assign a stable host ID.
3. Supply a definition catalog or an explicit immutable definition context.
4. Declare supported runtime areas.
5. Declare supported isolation modes.
6. Declare fresh, shared, snapshot, and persistent area coverage separately.
7. Supply reset behavior.
8. Supply snapshot behavior only when complete.
9. Supply persistence only when supported.
10. Validate host configuration before running a suite.
11. Run host compatibility tests.
12. Confirm unregister/cleanup on scene unload.
13. Confirm required definition IDs resolve from the selected host context.
14. Confirm a bad catalog fails compatibility before scenario setup.
15. Confirm loading an additional host scene creates an ambiguity failure unless scenarios specify `RequiredHostId`.
16. Confirm unloading the selected host during a run fails with `host.continuity`.

## Manual Scene-Host Verification

1. Open `PrototypeScene`.
2. Enter Play Mode.
3. Open Test Lab and confirm the automation summary shows `host.prototype-test-lab`.
4. Run a Step 8 `FreshRuntime` suite and confirm it passes while using an isolated Knowledge/History bundle.
5. Run a Step 6 or Step 7 `SharedRuntime` scenario and confirm it uses the Prototype scene state.
6. Run the same suite twice and confirm no fixture or runtime state leaks.
7. Exit Play Mode and confirm no stale Prototype host remains after scene unload.
8. Open a development scene with `TestLabAutomationHostBehaviour` and no `PrototypeTestLabView`.
9. Enter Play Mode and confirm the generic host registers with its configured host ID.
10. Run a compatible hostless or generic-host `FreshRuntime` Knowledge/History scenario through an edit-mode or developer execution path.
11. Attempt a scenario requiring Character, Combat, Biology, or Persistence on a host that does not support that area and confirm it fails before setup.
12. Attempt `SnapshotRestore` on a host that does not declare complete snapshot support and confirm validation fails.
13. Load two host scenes additively and confirm unselected active hosts produce an ambiguity failure.
14. Specify one required host ID and confirm automation uses only that host.
15. Unload the selected scene and confirm the host unregisters.
16. Run normal, reverse, and shuffled ordering from valid hosts and confirm deterministic results.
17. Open `Assets/_Project/Scenes/Development/TestLabGenericHostScene.unity` and confirm the host registers as `host.generic-test-lab`.
18. Confirm the generic host can run a compatible Knowledge/History `FreshRuntime` scenario without Prototype UI.
19. Confirm the same host rejects a Character, Combat, Biology, Persistence, shared-runtime, or snapshot scenario before setup.
20. Load the generic host scene additively with Prototype and confirm unqualified host selection reports ambiguity.

## Master Suite Structure

Step 8 master automation is split conceptually into:

- isolated feature validation, where Feature 8.1 through 8.9 suites run through their own scenario scopes;
- deliberate integration hardening, where Feature 8.10 workflows use an explicit persistent fixture scope.

Feature suites must not share runtime state accidentally.

The Step 8 master orchestration scenario uses `FreshRuntime`; it only invokes child suites and does not own their mutable state. The final hardening scenario uses `PersistentFixture` because it intentionally validates long-lived cross-runtime integration state.

## Implemented Fixture Providers

- `Core/TestLabCoreFixtureProviders`: baseline runtime and mutable state scope fixtures required by every scenario.
- `History/TestLabHistoryFixtureProviders`: scenario-owned hidden historical event plus witness memory fixture. It returns a `HiddenHistoryFixtureHandle` containing the event ID, memory ID, owner Person ID, and source ID.

The legacy `FormWitnessHistoryMemory` Test Lab helper remains available for manual interactive use, but when automation is active it delegates to `TestLabHistoryFixtureProviders.WitnessMemoryFixtureId` through the scenario fixture registry.

## Prohibited Automation Patterns

Do not add automation scenarios that depend on:

- hidden prior scenario setup;
- mutable fixed IDs;
- manual Test Lab helper state;
- static "last created" IDs;
- blind create calls that overwrite existing records;
- global runtime records reused between isolated tests;
- helper methods that silently create missing dependencies without fixture ownership.

Manual-only helpers should be named and kept clearly separate from automation fixture APIs.

## Fingerprint Auditing

The runtime bundle fingerprint covers the Step 8 Knowledge/History graph by combining record counts, revisions, sequence counters, and deterministic serialized-state hashes for Knowledge, History, Memory, Information Source, Information Transfer, Information Access, and Knowledge Record runtimes.

The fingerprint is intentionally independent of provider ledger reporting. If scenario code mutates a runtime directly and never registers a fixture mutation, the pre-restore audit still sees the runtime diff and fails the scenario.

The fingerprint is not a replacement for fixture ownership. A provider must still declare what it creates or mutates through the ledger so intended changes can be distinguished from leaks.

## Current Limits

The current hard enforcement has two layers:

- Knowledge/History runtime graph: true fresh and snapshot runtime bundles are implemented.
- Scene-bound Step 3 through Step 7 domains: Character, Combat, Biology, and Persistence are fingerprinted through the Test Lab service so hidden mutations are visible to the framework, but they are not yet true in-memory fresh runtime bundles.

Because of that, Step 3 through Step 7 scenarios are temporarily declared as `SharedRuntime` with explicit runtime areas. This is an honest migration state, not the final target. `TestLabAutomationValidation.BuildMigrationInventory` reports the current counts and fails any unapproved shared-runtime suite.

The temporary shared-runtime allowlist may only contain known Step 3 through Step 7 feature suites. New feature automation should not be added to that allowlist. To remove an entry, add real fixture providers and fresh/snapshot runtime support for the area, then convert that suite back to `FreshRuntime`.

## Future Feature Checklist

1. Add a feature-scoped fixture provider.
2. Declare provider dependencies.
3. Use scoped mutable IDs from `TestLabScenarioContext`.
4. Return typed handles or handle payloads.
5. Select an isolation mode, using `FreshRuntime` unless sharing is under test.
6. Register the scenario with required fixture IDs.
7. Add ownership expectations to the ledger.
8. Add postflight leak/conflict coverage.
9. Verify normal, reverse, seeded shuffled, and repeated execution where practical.
