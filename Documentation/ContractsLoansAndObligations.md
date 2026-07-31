# Contracts, Loans, and Obligations

Feature 11.7 adds `ContractEconomyRuntime` as the economic agreement runtime that extends the earlier quest contract foundation without replacing it.

`ContractDefinition` and `PlayerContractJournal` remain the quest-board layer from Feature 3.9. `ContractEconomyRuntime` owns economic proposals, accepted contracts, amendments, obligations, performance evidence, payment allocations, credit agreements, loans, installments, and collateral designations.

## Ownership Boundaries

The contract runtime owns:

- Proposal lifecycle: draft, offered, accepted, rejected, withdrawn, expired, activated.
- Contract instance lifecycle and versioned amendments.
- Obligation records and performance evidence.
- Payment allocation records that reference Economy transactions.
- Loan state, exact interest accrual, repayment schedules, delinquency/default/cure/restructure state.
- Collateral designations as references to assets owned by their source runtimes.

The contract runtime does not own:

- Account balances, reservations, or ledger entries. Those stay in `EconomyRuntime`.
- Payroll calculations or wage obligations. Those stay in `PayrollRuntime`.
- Property titles, possession, tenancy, access, or rent ownership. Those stay in `PropertyRuntime`.
- Item ownership and durability. Those stay in Step 9 item runtimes.
- Legal enforcement, taxes, NPC decision-making, or final UI visibility.

## Money and Interest

Authoritative money values use integer units through `MoneyAmount`.

Interest and percentages use `ContractRationalData`:

- `numerator`
- `denominator`
- `rounding`

The runtime calculates interest with integer arithmetic. It does not use floating point for authoritative principal, interest, penalties, repayment allocation, or collateral value.

## Cross-Runtime Atomicity

Operations that move money snapshot both runtimes before execution:

- `ContractEconomyRuntime`
- `EconomyRuntime`

If a later contract operation fails after an Economy transfer, both snapshots are restored. This keeps contract state and ledger state in sync.

## Persistence

`ContractEconomyPersistenceParticipant` stores world contract state under:

`world.contracts`

Prepare validation rejects broken graphs before commit, including missing contracts, proposals, obligations, loans, installments, and collateral references. Commit restores through `ContractEconomyRuntime.RestoreFromSaveData` and rolls back if an unexpected restore failure occurs.

## Test Lab

Feature 11.7 automation runs through the shared Test Lab automation catalog:

`feature.11.7.contracts-loans-obligations`

The scenarios use fresh runtime bundles and do not depend on scene objects. This keeps command-side and in-game automation aligned.
