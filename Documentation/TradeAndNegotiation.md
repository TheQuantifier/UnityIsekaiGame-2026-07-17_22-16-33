# Trade and Negotiation

Feature 11.3 introduces `TradeRuntime` as the coordinator for negotiated exchange. It does not own money, item instances, market prices, or information access records. Those remain owned by their existing runtimes.

## Runtime Boundary

- `EconomyRuntime` owns accounts, reservations, transfers, refunds, and monetary ledger entries.
- `ItemInstanceIdentityRuntime` owns item identity, ownership, custody, stack quantity, and item lifecycle.
- `MarketRuntime` owns market prices and merchant quotes.
- `InformationAccessRuntime` owns inspection and redaction decisions.
- `TradeRuntime` owns sessions, offers, counteroffers, trade reservations, valuations, execution records, and receipts.

Trade execution validates all backing runtimes before mutation, reserves assets when requested, and snapshots economy and item state before executing. If execution fails after any backing mutation, those backing runtimes are restored before the failure is returned.

## Sessions and Offers

A `TradeSessionData` identifies the trade policy, participants, market context, active offer, accepted offer, and negotiation round. A `TradeOfferData` contains one or more bundles. Each bundle describes which participant contributes assets and which participant receives them.

Supported prototype asset kinds:

- Money
- Item instance
- Stack quantity
- Physical currency item
- Multiple item instances

Offer lifecycle is explicit: submitted, superseded, accepted, rejected, withdrawn, expired, completed, or failed. Counteroffers supersede the active parent offer instead of editing it in place.

## Reservations

Money reservations are delegated to `EconomyRuntime`. Item reservations are tracked by `TradeRuntime` because item identity does not reserve future ownership by itself.

Reservation IDs are written back to the stored offer assets. This is important: release and execution consume the same reservation identity created during reservation, rather than recalculating or silently relying on offer entry IDs.

## Execution

Accepted offers can execute only when:

- The session is accepted and pending execution.
- Money accounts still exist, use the expected currency, and have available funds or active reservations.
- Item instances still exist, match the expected item definition/revision when supplied, and are not consumed or destroyed.
- Referenced merchant quotes remain valid at execution time.
- No active item reservation from another offer conflicts.

Execution emits:

- Economy transaction IDs for monetary movements.
- Item transfer references for item movements.
- A trade record preserving the accepted offer graph.
- A receipt summarizing the exchanged assets.

## Projections

Trade sessions and offers expose access-aware projections. Normal callers should request projections through `TradeRuntime` instead of reading raw records when privacy matters. Redacted offer projections hide asset and valuation details while preserving a stable subject reference for Step 8 access checks.

## Persistence

`TradePersistenceParticipant` persists world-level trade state under `world.trades`. It is optional and loads after economy and markets. Save validation rejects broken graphs before commit, including offers that reference missing sessions or sessions that reference missing trade policies.

## Test Lab

The Test Lab runtime bundle now includes `TradeRuntime` in the economy runtime area. Fresh runtime, snapshot restore, mutation fingerprinting, in-game automation, and command automation all use the same trade runtime source of truth.
