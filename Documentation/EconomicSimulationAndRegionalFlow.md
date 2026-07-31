# Economic Simulation and Regional Flow

Feature 11.9 adds an aggregate regional-flow runtime for settlement and regional economy simulation. It models commodities as exact integer quantities in regional pools, derives production, consumption, labor pressure, wealth summaries, shortages, and inter-region movement, and publishes optional market observations without taking ownership of market pricing or trade settlement.

## Runtime Boundaries

`RegionalFlowRuntime` owns aggregate regional state:

- economic regions
- commodity pools
- aggregate quantity operations
- unresolved economic cohorts
- production and consumption records
- labor, wealth, shortage, and conservation snapshots
- trade-flow orders and regional cycle records

It does not own exact item instances, money accounts, trade sessions, businesses, payroll, property, or institutional revenue. Those systems remain authoritative and may feed or consume regional-flow records through explicit references.

## Exactness Rules

All regional quantities use integer units and a `CommodityUnit`. The runtime rejects missing commodities, missing pools, invalid units, over-reservation, insufficient available stock, and unsupported operation kinds.

Exact item aggregation is explicit. `AggregateExactItems` records the source event key and rejects the same source event, commodity, and quantity from being counted twice. `Materialize` is explicit as well; the regional pool does not silently create concrete item instances.

## Production And Consumption

Production profiles describe aggregate inputs, outputs, and labor needs. Consumption profiles describe aggregate needs. Execution writes immutable production or consumption records and applies the underlying pool mutations atomically. Injected failures restore the pre-operation snapshot before returning failure.

When a `MarketRuntime` and market instance ID are supplied, production and consumption can publish market supply and demand observations. These are observations only; market price formation remains owned by the market runtime.

## Flows And Conservation

Trade connections define permitted regional lanes and capacities. Flow orders are planned, reserved, departed, and arrived in explicit states. Source reservations, outbound quantities, destination inbound quantities, losses, and arrivals are updated as separate conserved quantities.

`BuildConservationAudit` records regional totals at a simulation boundary and is intended as the central place for future cross-runtime conservation checks.

## Access And Persistence

Regional pools and regions support Step 8 access-aware projections through `InformationAccessRuntime`. Restricted callers receive redacted quantities and provenance while privileged internal callers may inspect full records.

`RegionalFlowPersistenceParticipant` saves and restores world-scoped regional-flow state. Prepare validation checks the graph before commit so corrupt payloads do not partially mutate live simulation state.

## Test Lab

The `feature.11.9.economic-simulation-regional-flow` automation suite runs hostlessly through the shared Test Lab runtime bundle. The suite covers pool exactness, production and consumption, derived labor and shortage snapshots, inter-region flow conservation, cycle rollback, access projections, and persistence rejection.
