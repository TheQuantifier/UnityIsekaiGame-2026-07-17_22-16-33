# Feature 11.1 - Currency and Economic Transactions

Currency and economic transactions are modeled as authoritative runtime state, not as direct inventory counters or floating point values.

Core contracts:
- `CurrencyDefinition` identifies an authored currency and optional physical currency item conversion rule.
- `MoneyAmount` stores exact minor-unit amounts as `long` values.
- `EconomyRuntime` owns accounts, reservations, fixed price snapshots, committed transactions, idempotency records, and immutable ledger entries.
- Item-based money conversion goes through `ItemInstanceIdentityRuntime`; physical currency is an item instance, not a second item model.
- Persistence captures account, reservation, transaction, ledger, price snapshot, and idempotency state as a validated graph and rejects invalid payloads before commit.
- Access-aware projections use `InformationAccessRuntime` to expose full or redacted account views without mutating balances or ledgers.

Deferred systems:
- Dynamic markets, exchange rates, bargaining, wages, loans, property ownership, taxation, auction logic, and autonomous trade behavior remain outside this feature.
