# Feature 13.8 - Governments, Territories, Sovereignty, and Jurisdictions

Feature 13.8 adds an authoritative political geography runtime without taking ownership from organizations, factions, diplomacy, property, places, history, or information access.

## Ownership boundaries

- `GovernmentRuntime` owns polity, government, political territory, territorial claim, control, administration, government seat, sovereignty claim, jurisdiction, and political transition records.
- `OrganizationRuntime` continues to own institutional identities. Governments reference governing organizations and never duplicate their membership, rank, office, authority, decision, or resource state.
- `DiplomacyRuntime` continues to own diplomatic recognition, agreements, and war. Political records retain stable source references only.
- `PropertyRuntime` continues to own legal and economic property. Territorial transfer changes political control and administration without transferring property ownership.
- Place definitions remain the spatial identity source. Political territories group places rather than replacing them.

## Determinism and persistence

All mutating operations use stable transaction IDs, immutable result projections, explicit world time, deterministic ordering, and prepare-before-commit persistence. Jurisdiction resolution filters by lifecycle and effective time, then applies scope specificity, priority, effective time, and stable ID ordering. Corrupt graphs, missing definitions, invalid references, duplicate identities, and hierarchy cycles are rejected before live state changes.

Polity names are append-only historical records: renaming never changes polity identity. Institution roles and territory-place memberships have explicit effective and end times. Polities, governments, territories, claims, and jurisdictions transition through lifecycle commands rather than deletion. Boundary change, succession, collapse, split, merger, secession, occupation, and transfer use persisted transition plans so later systems can distinguish a proposal from an executed transition.

Territorial transfer prevalidates its complete generated graph and commits control, administration, territory projection, transition, and transaction state in one runtime revision. Existing control and administration records become historical; property ownership remains untouched. Successful executions publish one post-commit event, while previews, duplicates, failed validation, and restore publish none.

Queries return cloned, stably ordered snapshots for each record category and indexed views by polity, territory, or government. Callers cannot mutate authoritative state through a returned record or array.

## Integration boundaries

- Government institution links reference Feature 13.1 organizations; offices, memberships, ranks, and permissions remain owned by Features 13.2 and 13.3.
- Government decisions and treasury/property references remain owned by Features 13.4 and 13.5.
- Faction caucuses remain Feature 13.6 state. Governments may be contested without turning factions into governments.
- Recognition, agreements, rivalries, and wars remain Feature 13.7 state.
- Historical events, knowledge, memory, rumor, and access-aware redaction remain owned by Steps 8 and 12; Feature 13.8 stores stable provenance and visibility metadata only.
- Law, citizenship, crime, courts, world topology, and travel remain deferred to Feature 13.9 and Step 14.

The Test Lab fixture bundle creates, snapshots, restores, fingerprints, and disposes government state in the same dependency order used by runtime persistence. This keeps in-scene and command-line automation on one source of truth.
