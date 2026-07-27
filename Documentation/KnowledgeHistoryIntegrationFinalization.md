# Feature 8.10 - Knowledge and History Integration Finalization

Feature 8.10 adds an integration facade for Step 8 without replacing the systems that already own data.

## Ownership

- `PersonKnowledgeRuntime` owns facts, evidence, beliefs, misconceptions, and stale belief state.
- `AuthoritativeHistoryRuntime` owns world history, life events, body transitions, and biography projections.
- `PersonMemoryRuntime` owns person memories, recall, suppression, alteration, degradation, and memory save data.
- `InformationSourceRuntime` owns source identity, lineage, transformations, and reliability assessments.
- `InformationTransferRuntime` owns sharing, teaching, transfer audit records, and multi-recipient transfer transactions.
- `InformationAccessRuntime` owns access policy, grants, denials, concealment, redaction, discovery, and audit state.
- `KnowledgeRecordRuntime` owns explicit journals, records, codex entries, corrections, collections, and record-reading side effects.
- `KnowledgeHistoryFacade` owns no records. It coordinates readiness, validation, request routing, and transaction diagnostics.

## Facade

`UnityIsekaiGame.Knowledge.Integration.KnowledgeHistoryFacade` is the stable high-level Step 8 entry point.

It exposes:

- readiness snapshots across definitions, knowledge, history, memory, sources, transfers, access, and records;
- save-payload validation using each owning runtime's strict validator;
- cross-runtime relationship validation for memories, records, source lineage, transfers, and access-policy references;
- request wrappers for observation, history recording, memory formation, transfer execution, access evaluation, record creation, and record reading;
- transaction diagnostics with operation kind, failure stage, participating subsystems, transaction ID, and rollback indicators;
- persistence participant inventory and dependency documentation;
- definition fallback diagnostics for known prototype definition providers.

The facade intentionally does not bypass access rules or return raw private records to ordinary callers.

## Persistence Graph

Step 8 participants remain independently owned:

- `person.knowledge`
- `person.memory`
- `person.information-sources`
- `person.information-transfers`
- `person.information-access`
- `person.knowledge-records`
- `world.authoritative-history`

The required dependency is:

- `person.memory -> world.authoritative-history`

Optional dependencies preserve partial save compatibility where the owning runtime can validate safe degraded operation:

- knowledge may reference body state;
- sources may reference knowledge;
- transfers may reference knowledge, memory, and sources;
- access may reference knowledge, memory, sources, and transfers;
- records may reference knowledge, history, memory, sources, transfers, and access.

Restore remains prepare/commit driven through the persistence service. Step 8 restore should validate all referenced definitions and reject corrupt payloads without partially mutating live runtime state.

Feature 8.10 treats this graph as executable integration data, not prose only. The Test Lab hardening actions validate that participants are present, dependency order is stable, payloads are prepared before commit, failed restores preserve live state, and restored state rebuilds indexes without replaying gameplay events.

## Definition Fallbacks

Catalog-authored definitions are authoritative. Prototype-only fallback definitions are allowed only for known development records and should come from a shared provider, not from persistence validation bypasses.

Feature 8.9 established `PrototypeKnowledgeRecordDefinitionFactory`. Feature 8.10 exposes fallback diagnostics so Test Lab and persistence can verify they are resolving the same stable definition IDs.

Fallback registration is safe for known prototype definitions. Validation bypass is not safe and should not be used.

## Access-Aware Projections

Normal projection callers should resolve authoritative records from the owning runtime, construct an information subject, ask `InformationAccessRuntime` for a decision, and return full, partial, redacted, concealed, or denied projections.

Privileged persistence, validation, and development inspection may use raw snapshots only when they are explicitly privileged and do not create knowledge, recall metadata, transfer records, or gameplay audit side effects.

The intended ordinary access paths are:

- `AuthoritativeHistoryRuntime.QueryHistoryProjectionsByPerson`
- `AuthoritativeHistoryRuntime.GetBiographyProjection`
- `PersonMemoryRuntime.GetMemoryProjection`
- `PersonKnowledgeRuntime.GetKnowledgeProjection`
- `InformationSourceRuntime.GetSourceProjection`
- `InformationSourceRuntime.GetSourceChainProjection`
- `KnowledgeRecordRuntime.ProjectRecord`
- `KnowledgeRecordRuntime.Search`
- `KnowledgeRecordRuntime.ReadRecordAsPerson`

Raw snapshots and raw query methods remain valid for owner-internal logic, persistence prepare/commit, definition validation, deterministic tests, and explicitly privileged development simulation. They should not be used as ordinary gameplay privacy paths. Existing Test Lab/debug projections that impersonate normal gameplay access should route through access-aware projections; intentionally privileged diagnostics must label themselves as privileged and avoid creating knowledge, memory, transfer, or audit side effects unless the action is explicitly testing those effects.

Known remaining legacy access boundaries are not separate privacy systems: they are direct owner APIs used by persistence, validation, and tests. No remaining ordinary Step 8 caller should rely on UI-side filtering after receiving raw private data.

## Transaction Semantics

Cross-runtime operations should be explicit about:

- the transaction ID;
- participating subsystems;
- the failure stage;
- whether rollback was attempted;
- whether rollback succeeded;
- the underlying owner result.

`InformationTransferRuntime` remains whole-transfer atomic for multi-recipient transfers: if a recipient mutation fails, previous recipient effects are rolled back by the transfer runtime.

`KnowledgeRecordRuntime.ReadRecordAsPerson` owns its source, evidence, and memory side effects and rolls them back if any linked mutation fails.

## Step 9 Contracts

Feature 8.10 adds minimal request and interface contracts for future item, recipe, production, teaching, and provenance knowledge:

- `IItemKnowledgeService`
- `IRecipeKnowledgeService`
- `IProductionDiscoveryService`
- `IItemIdentificationKnowledgeSink`
- `ICraftingHistoryRecorder`
- `IRecipeTeachingService`
- `IProvenanceRecordService`

These contracts do not implement Step 9 runtime behavior. They reserve the boundary so future item and crafting systems can supply information subjects, provenance, and discovery requests to Step 8 without taking ownership of Step 8 records.

## Test Lab

The Test Lab now includes a Step 8 `Integration 8.10` page with:

- readiness validation;
- integrated state validation;
- definition fallback diagnostics;
- persistence graph validation;
- representative discovery, history-memory, record-reading, and access projection flows;
- Step 9 contract preview;
- 8.10 automation suite execution.

The feature suite `feature.8.10.knowledge-history-integration` tests the 8.10 facade and representative workflows. Step 8 no longer registers a separate master suite; use the ordinary Step 8 feature suites when you want full Step 8 coverage.

## Performance and Save Size

Feature 8.10 does not persist derived projections. Readiness snapshots, cross-runtime validation summaries, access projections, and Step 9 contract previews are derived at runtime and are intentionally excluded from save payloads.

The current prototype validation uses bounded in-memory snapshots and deterministic ID sets. This is acceptable for the prototype data size. If Step 8 data grows substantially, the likely optimization points are cached cross-runtime lookup indexes, paged record search, capped validation diagnostics, and incremental dirty-participant validation. Those optimizations should not change ownership: each runtime still owns its own records and the facade remains a coordinator.

Save-size growth is expected to come from authoritative records, evidence, memory details, source lineage, transfer audit records, access audits, and history events. The integration layer should keep compatibility by validating references and preserving stable IDs, not by duplicating owner data in a combined Step 8 blob.

## Manual Validation

1. Open PrototypeScene and press Tab.
2. Open Test Lab.
3. In the Step 8 dropdown, select `Integration 8.10`.
4. Run `Prepare` to ensure prototype record fixtures have their backing history, evidence, and source entries.
5. Run `Readiness`, `Validate`, `Fallbacks`, and `Save Graph`.
6. Run `Discovery`, `Event Memory`, `Record Read`, and `Access`.
7. Run `Step 9 API`.
8. Run `Run 8.10 Auto`.
9. Run all Step 8 automation and confirm no failures.
10. Run definition validation and confirm no new Step 8 errors or warnings.

## Limitations

- The facade does not implement NPC disclosure decisions, espionage, legal permission systems, organization authority, or final UI visibility.
- Step 9 contracts are intentionally non-authoritative placeholders until item, recipe, production, and crafting systems are implemented.
- Existing privileged debug projections remain available for development and persistence, but ordinary gameplay-facing callers should use access-aware projection paths.
- Implementation, documentation, and automated-test authoring are complete for the current pass. Final closure remains conditional on successful Unity Edit Mode execution, Feature 8.10 automation, ordinary Step 8 feature automation, definition validation, and manual Test Lab verification in Unity.
