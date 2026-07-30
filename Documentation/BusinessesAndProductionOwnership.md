# Feature 11.5 - Businesses and Production Ownership

Feature 11.5 adds the business layer that sits above economy, trade, payroll, item identity, and production. It records which business exists, who owns it, who controls it, what operating resources it references, and how explicit business activity is classified for reporting.

## Runtime Ownership

`BusinessRuntime` owns business records only:

- `BusinessInstanceData`
- ownership and control records
- establishment records
- account and inventory assignments
- stock classifications
- production sponsorship and output-owner policy records
- funding allocations
- revenue, expense, capital contribution, and owner withdrawal records
- accounting periods, profit-and-loss statements, and cash-flow summaries

It does not own money balances, item instances, production jobs, payroll obligations, trade sessions, organizations, land, buildings, tenancy, rent, taxation, or legal authority. Those remain owned by their existing runtimes or later features.

## Definitions

`BusinessDefinition` is the catalog identity for a business type. It declares permitted owner subject kinds, establishment types, default account and inventory purposes, default production output policy, revenue and expense categories, and ownership policy.

Business definition IDs use the `business.` namespace. Business instances reference those definitions through `businessDefinitionId`.

## Ownership And Control

Ownership and control are separate:

- Ownership records describe economic/voting interests held by a person or organization.
- Control records describe delegated authority such as managing inventory, selling stock, spending funds, hiring employees, or approving payroll.

A controller does not become an owner just because they manage a shop. An owner does not automatically become the active controller unless a control record says so.

## Establishments

Establishments represent operating locations such as shops, stalls, workshops, warehouses, or production sites. In 11.5, establishments hold references to a location, account assignments, inventory assignments, production stations, and market instances.

Land, buildings, property title, tenancy, rent, and fixtures are intentionally deferred to 11.6.

## Accounts And Inventory

Business account assignments reference accounts owned by `EconomyRuntime`. Assigning an account to a business does not create money and does not transfer funds.

Business inventory assignments reference inventory IDs. Assigning an inventory to a business does not mutate item ownership or custody in `ItemInstanceIdentityRuntime`.

Stock classifications describe how an item instance or item definition is used by the business: for sale, production input, work in progress, finished goods, tool, consumable, salvage, and related categories.

## Production Ownership

Production sponsorship is separate from production execution:

- `ProductionWorkflowRuntime` owns work orders, jobs, batches, queues, and producer state.
- `BusinessRuntime` records the business sponsor, funding account, input/output inventory references, responsible producer subject, and output owner policy.

Output ownership policy describes who should own resulting goods, but actual item ownership remains an item identity operation.

## Finance And Accounting

Business financial records classify authoritative external events:

- revenue references economy transactions, trade records, item/service IDs, or approved adjustments
- expenses reference economy transactions, payroll, production, item/service IDs, or approved adjustments
- capital contributions and owner withdrawals are separate from operating revenue/expense

Profit-and-loss statements summarize operating performance for a closed accounting period. Cash-flow summaries summarize operating inflows/outflows, payroll outflows, capital inflows, withdrawals, asset purchases, and financing placeholders.

The runtime validates that transaction-backed revenue and expenses match currency and amount. Failed operations return errors before mutating live state.

## Access And Persistence

Business records expose Step 8 information subject references through `BusinessInformationSubject`. `ProjectBusiness` uses `InformationAccessRuntime` to return full, redacted, denied, or privileged-debug views without mutating the authoritative business record.

`BusinessPersistenceParticipant` captures and restores business runtime state as a shared-world participant. Prepare validation rejects missing business definitions, duplicate records, and child records that reference missing businesses before commit. Commit rolls back if restore fails unexpectedly.

## Deferred Scope

Feature 11.5 intentionally does not implement:

- land ownership
- building ownership
- tenancy
- rent
- legal/tax regimes
- loans
- investment markets
- autonomous business AI
- complex accounting standards
- UI dashboards
- NPC hiring/firing workflows

Those systems should reference business records later rather than being owned inside 11.5.
