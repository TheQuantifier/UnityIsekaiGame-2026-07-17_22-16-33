# Taxes, Fees, and Institutional Revenue

Feature 11.8 adds the economic foundation for institutional charges: taxes, tariffs, tolls, license fees, administrative fees, fines, penalties, withholding, remittance, revenue recognition, allocation, refunds, filings, audits, statements, persistence, and access-aware projections.

The feature owns assessment, obligation, collection, payment records, allocation, and institutional revenue bookkeeping. It does not own laws, jurisdictions, governments, crimes, courts, enforcement, seizure, imprisonment, autonomous policy, or reputation effects. Later legal and governance systems must supply explicit authority, violation, permit, judgment, border, route, or institutional-decision references.

## Authority Boundaries

Institutional revenue authority is explicit runtime data. It states which institution may assess, collect, receive remittance, refund, waive, adjust, audit, or allocate revenue for permitted categories, subjects, currencies, and source references. The runtime validates that the authority record exists and permits the requested operation, but it does not determine whether the source law, office, title, court, charter, or external decision is legally valid.

Feature 11.1 remains the authority for account balances, reservations, transfers, refunds, and ledger entries. Feature 11.4 remains payroll authority. Feature 11.5 remains business accounting authority. Feature 11.6 remains property and title authority. Feature 11.7 remains the general contract and obligation authority where reused.

## Revenue Definitions and Accounts

Institutional revenue definitions are stable catalog definitions for sales tax foundations, payroll taxes, import tariffs, tolls, license fees, fines, and future charge families. A definition declares category, institution kind, required authority category, taxable subject kinds, taxable event kinds, currency, tax-base policy, exact rate policy, assessment period kind, withholding support, collection-account purpose, filing requirement, refund policy, and validation rules.

Institutional revenue accounts are account assignments, not balances. They map an institution, account ID, currency, purpose, receiving authority, effective scope, access policy, and provenance to an existing Feature 11.1 account. Purposes include tax collection, payroll contribution collection, customs collection, toll collection, license revenue, fine collection, refund funding, escrow foundation, and revenue distribution.

## Taxable Subjects and Events

Taxable subjects describe roles such as assessed party, economic bearer, payer, withholding agent, remitting party, receiving institution, reporting party, and beneficiary. These roles stay distinct so the system can represent buyer-paid tax, seller collection, employer withholding, institutional remittance, and third-party payments without collapsing them into one actor.

Taxable events are immutable references to authoritative source records such as completed trades, payroll payments, property valuations, item imports, route use, license applications, administrative service requests, and explicit external fine decisions. A taxable event is not an assessment. The same exclusive event cannot be assessed twice under the same policy and period.

## Tax Bases and Exact Rates

Tax-base calculation is deterministic and mutates no state. It can use fixed amounts, transaction gross or net values, item quantities, payroll gross pay, business revenue or profit, property assessed value, route usage count, license duration, administrative-service occurrence, external fine amount, and custom exact quantities.

Rate policies use exact integer and rational arithmetic only. Supported policy shapes include fixed amount, flat proportional, per-unit, progressive bracket, threshold charge, tiered fixed charge, minimum or maximum charge, capped proportional charge, percentage plus fixed amount, value band, quantity band, and custom foundations. Progressive and tiered calculations declare marginal, whole-base, tiered-fixed, threshold-triggered, or custom behavior. Brackets are ordered deterministically and stable IDs are final tie-breakers.

## Adjustments

Exemptions, deductions, credits, waivers, and refunds are separate concepts.

An exemption removes some or all of a subject, event, or base from a charge. A deduction reduces the taxable base. A credit reduces the calculated assessment after the gross charge is determined. A waiver reduces outstanding obligation without recording payment. A refund creates a new compensating Feature 11.1 transaction and preserves the original payment.

## Assessments, Obligations, Payments, and Arrears

Assessment generation validates authority, definition, subject, event, base, rate, period, adjustments, currency, and account assignment. It creates immutable assessment records and, when approved, exact institutional obligations. Calculation does not move money.

Payments use EconomyRuntime transfers. Partial payment reduces outstanding amount and may leave an obligation partially paid or in arrears after due time. Arrears do not automatically cause seizure, legal action, property changes, punishment, reputation changes, or enforcement.

## Withholding and Remittance

Withholding retains money from a payment into a holding account. Remittance transfers withheld money to the institutional account. Withholding is not remittance, withheld money is not business revenue, and remittance above unremitted amount is rejected.

## Charge Families

Transaction taxes preserve original trade prices and market prices. Payroll taxes and contributions integrate with payroll source records without replacing payroll authority. Business taxes use authoritative revenue and profit records without redefining profit. Property taxes use authoritative title and valuation references without altering title. Tariffs require explicit import or export events. Tolls require explicit route or facility-use events. License and permit fee payment does not issue a license. Fine records require explicit external decisions, and fine payment does not determine guilt or enforce punishment.

## Revenue Records and Allocation

Institutional revenue records classify committed payments as institutional revenue. Classification does not move money. Allocation distributes collected revenue by exact units or deterministic policies through EconomyRuntime transfers. Allocations must conserve value and reject duplicate allocation.

## Filings, Audits, Statements, and Receipts

Filings are submitted declarations and are not authoritative truth. Audits compare filings or claims to authoritative records and create findings, but they do not create criminal consequences or automatic punishment. Statements and receipts provide immutable reporting over assessments, payments, withholdings, refunds, penalties, waivers, and revenue records.

## Access and Persistence

Access-aware projections use InformationAccessRuntime to return full, redacted, concealed, or denied assessment views. Public projections hide protected amounts, account IDs, exemptions, credits, filing details, audit details, arrears, fines, and protected source references.

Persistence round-trips authorities, account assignments, taxable events, periods, filings, assessments, obligations, payments, withholding, remittance, revenue records, allocations, penalties, waivers, refunds, audits, and statements. Restore rebuilds indexes and revisions without replaying assessments, collection, remittance, allocation, refunds, penalties, knowledge, records, or history. Corrupt cross-runtime references are rejected before commit.

## Test Lab

The Test Lab suite for Feature 11.8 uses fresh runtime fixture ownership and run-scoped mutable IDs. It covers exact rate policies, duplicate event prevention, representative tariffs, tolls, license fees and fines, collection, allocation, penalties, waivers, refunds, withholding, remittance, filings, audits, redacted projections, persistence restore, and corrupt graph validation.

## Deferred Systems

Deferred systems include laws, jurisdictions, full governments, courts, crimes, appeals, search and seizure, imprisonment, foreclosure, business closure enforcement, license revocation enforcement, border navigation, customs inspections, smuggling, sanctions, autonomous auditors, tax evasion AI, reputation effects, complex real-world tax codes, final UI, networking, and replication.
