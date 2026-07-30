# Markets And Price Formation

Feature 11.2 adds a world-scoped market runtime for deterministic reference prices and merchant quote formation.

## Ownership

`MarketRuntime` owns market instances, supply observations, demand observations, scarcity snapshots, reference price history, merchant quote records, and transaction observations.

It does not own item instances, inventories, accounts, currency balances, or transaction execution. Those remain owned by the item identity and economy runtimes. Market records may reference those systems, but price formation and quote creation never mutate them.

## Definitions

`MarketDefinition` describes a market category, scope, currency, supported traded subject kinds, update policy, price policy, merchant margin policy, and access policy.

`MarketSubjectDefinition` describes something that can be priced: an item definition, material definition, item category, service foundation, labor foundation, property foundation, production input/output, or custom subject. It stores the standard unit, baseline price, bounds, currency, and regional or rarity modifiers.

## Observations

Supply and demand are explicit observations. Supply records include a source key so the same source cannot be counted twice. Expired observations remain in history but are ignored by current scarcity calculations.

Transaction observations can summarize committed economy transactions for later price analysis. They are projections of transaction facts, not a trade execution path.

## Price Formation

Reference prices are deterministic integer values in currency units. The runtime uses basis-point math for scarcity, regional, rarity, quality, durability, and margin adjustments.

When supply or demand data is missing, the runtime can produce a fixed fallback price from the subject baseline. Missing data can also be rejected when a caller requires market-derived data.

Market updates are explicit world-time boundary operations. Running the same update for the same market, subject, and world time is idempotent and does not append duplicate price history.

## Merchant Quotes

Merchant quotes are immutable records derived from a current reference price, direction, margin policy, quantity, optional item state, and expiration.

Sell quotes apply merchant markup. Buy quotes apply merchant discount. Item quality and durability can adjust the quote. Hidden item factors, such as maker marks, are only included when the caller is privileged.

Quotes do not transfer items or money. Trade execution must later validate the quote against current market and price revisions before using it.

## Persistence

`MarketPersistenceParticipant` saves and restores market state as a shared world participant. Prepare validation checks the market graph before commit, including definitions, observations, prices, current price references, quotes, and transaction observations. Commit is rollback-safe.

## Access

Market prices and quotes expose Step 8 information subjects. Access-aware projections can return full, redacted, concealed, or denied market records without mutating market state.
