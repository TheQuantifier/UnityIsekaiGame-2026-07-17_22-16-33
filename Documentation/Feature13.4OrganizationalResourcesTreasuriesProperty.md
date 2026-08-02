# Feature 13.4: Organizational Resources, Treasuries, and Property

## Ownership model

`OrganizationResourceRuntime` owns institutional resource metadata: treasury and account identity, restrictions, budgets, reservations, inventory associations, custody, property and business associations, revenue-routing instructions, dissolution resource plans, and coordination transaction records.

It does not own the underlying assets or financial truth:

- `EconomyRuntime` owns currency accounts, exact balances, reservations, transactions, and balanced ledger entries.
- Step 9 item runtimes own item identity, state, composition, quality, durability, and inventory placement.
- `PropertyRuntime` owns property records and ownership interests.
- `BusinessRuntime` owns business identity, ownership, and operating state.
- `ContractEconomyRuntime` owns contracts, obligations, loans, and receivables.
- `PayrollRuntime` owns compensation calculations, payroll obligations, pay runs, statements, and wage debt.
- `OrganizationRuntime` owns organization identity, hierarchy, and lifecycle.
- `OrganizationAuthorityRuntime` owns permissions, effective authority, approvals, and authorization audits.

An organization may own an asset, operate it, control its institutional use, or assign custody. These are separate facts. A custody record never changes Step 9 ownership, and a property or business association cannot fabricate Step 11 ownership.

## Financial coordination

Organization account records reference stable Economy account IDs. Deposits, withdrawals, transfers, freezes, closures, reservations, and revenue routing execute through `EconomyRuntime`. Organization metadata stores the institutional purpose and authorization context while Economy remains the source of balance and ledger truth.

Balances expose distinct values for total, available, restricted, reserved, encumbered, and frozen funds. Budgets do not create currency. Restrictions do not move currency. Reservations are explicit claims backed by the Economy reservation system.

Revenue routing rules are deterministic instructions over revenue records owned by Step 11 systems. Applying a route performs ordinary Economy transfers atomically. Rules cannot allocate more than gross revenue and do not create a parallel revenue ledger.

## Authority and atomicity

Every mutation checks Feature 13.3 authority before financial execution. Financial validation remains separate from authorization: permission cannot make an overdraw, currency mismatch, invalid restriction, or exceeded hard budget valid.

Joint-approval actions use the same stable operation ID as the resource request. Approval consumption, Economy mutation, and organization metadata mutation are coordinated with rollback checkpoints. A downstream failure restores Economy, authority approvals, and resource metadata.

Stable transaction IDs are mandatory. Replaying the original transaction is idempotent. A different transaction ID cannot masquerade as the creation of an already-existing stable record.

## Lifecycle and dissolution

Ordinary resource mutation is allowed only while the owning organization is active. Frozen and closed account states mirror their Economy account state. Dormant, dissolved, and archived organizations cannot perform ordinary resource operations.

Dissolution uses an explicit pre-dissolution resource plan. A plan identifies accounts to freeze, obligations to preserve, and explicit asset instructions. Execution freezes the selected accounts and preserves unresolved assets. It never guesses beneficiaries, successors, valuations, or transfer destinations. The plan and all resource records remain persistable after the organization lifecycle becomes dissolved or archived.

## Branches and consolidated views

Branch treasury and account metadata retain explicit branch organization IDs. Direct account queries remain separate by owning organization and treasury. `GetConsolidatedView` traverses the authoritative Organization hierarchy and returns immutable component balances plus exact currency totals; it does not merge or mutate child accounts.

## Liabilities and valuation

`QueryLiabilities` creates immutable read projections from Contract and Payroll owners. Contract obligations, loans, payroll obligations, and wage debt are not copied into resource state. Wage debt that represents an existing payroll obligation is not double counted.

`GetKnownValuation` reports cash plus known receivables minus known liabilities per currency. Items, inventories, properties, and businesses without an authoritative valuation source are returned as unvalued asset IDs. The runtime never fabricates prices or combines currencies.

## Events, queries, and visibility

Immutable post-commit events are published only after a successful commit. Preview, rejected, and duplicate operations publish nothing. Subscriber failures are bounded diagnostics and cannot roll back an already-committed mutation.

Queries return cloned records or immutable snapshots with ordinal deterministic ordering. Account projections support full, redacted, concealed, and denied access without mutating resource or knowledge state. Step 8 remains the owner of information-access policy; this runtime only applies the supplied projection decision.

## Persistence

`OrganizationResourcePersistenceParticipant` restores after Organization, Organization Authority, and Economy, with Item, Property, Business, Contract, and Payroll participants as optional graph dependencies. Prepare validates the entire resource graph before commit. Restore replaces metadata without replaying monetary transactions. Failed prepare and failed commit leave live state unchanged.

Persisted state includes treasuries, accounts, restrictions, budgets, reservations, inventory associations, property associations, business associations, custody, revenue-routing rules, dissolution plans, transaction identity, world identity, and revision state.

## Test Lab

Feature suite `feature.13.4.organizational-resources-treasuries-property` defines fifteen fresh-runtime scenarios from the shared automation catalog:

1. Runtime readiness
2. Treasury creation
3. Basic transfer
4. Authority denial
5. Restricted funds
6. Reservation
7. Joint approval
8. Inventory and custody
9. Property associations
10. Business and revenue routing
11. Payroll funding
12. Branch finances
13. Dissolution boundary
14. Reconciliation
15. Persistence validation

The same definitions run through the in-game Test Lab and command-side runner.

## Deferred boundaries

Feature 13.4 does not implement governance policy, political budgeting, legal seizure, inheritance selection, autonomous liquidation, market appraisal, faction strategy, or multiplayer replication. Feature 13.5 and later systems may issue authorized requests or consume immutable events, but they must not take ownership of this runtime's records or the underlying Step 9/11 state.
