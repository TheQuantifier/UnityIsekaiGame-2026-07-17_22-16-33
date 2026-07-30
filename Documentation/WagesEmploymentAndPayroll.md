# Wages, Employment, and Payroll

Feature 11.4 adds payroll as a world economy subsystem that connects Step 10 employment records to Step 11 currency accounts without making either runtime own the other.

## Ownership Boundaries

`PositionEmploymentRuntime` remains the authority for who is employed, by whom, and under which position. `PayrollRuntime` references those employment records when activating compensation agreements and when validating work evidence. It does not create employment.

`EconomyRuntime` remains the authority for accounts, reservations, transfers, refunds, and ledger conservation. Payroll creates obligations and requests reservations or transfers through the economy runtime. It does not mutate account balances directly.

`PayrollRuntime` owns:

- compensation agreements;
- work schedules;
- work sessions and timesheets;
- pay periods;
- gross/net calculations;
- payroll obligations;
- payroll runs;
- pay statements;
- wage debt;
- corrections and overpayment records.

## Exact Arithmetic

Authoritative monetary values use integer currency units. Compensation rates and deduction ratios are represented as integer units or rational numerator/denominator pairs. Payroll uses decimal calculation only as an intermediate for exact rounding into integer currency units. Floats and doubles are not used for authoritative money, hours, rates, or percentages.

World time fields keep the project’s existing double-based time convention.

## Payroll Flow

1. Activate a `CompensationAgreementData` against an active employment record.
2. Record work sessions from explicit work evidence.
3. Submit and approve timesheets.
4. Create a pay period.
5. Calculate gross pay, adjustments, reimbursements, and deductions.
6. Create a payroll obligation.
7. Create and execute a payroll run through the economy runtime.
8. Publish immutable pay statements and retain any wage debt.

Calculations are pure until committed as payroll records. Obligations are separate from payments, so an unpaid or partially paid wage can exist without pretending money moved.

## Atomic Execution

Payroll execution snapshots both payroll and economy state before mutating either runtime. If a transfer, reservation, account check, or injected failure fails, both runtimes restore their prior snapshots.

All-or-nothing runs require enough available funds for the full obligation total. Partial payroll runs reserve the available amount, execute what can be paid, and record the remaining wage debt.

## Access and Persistence

Pay statements expose access-aware projections. Public projections redact sensitive amount fields; employee, employer, payroll authority, and privileged views may receive full data.

`PayrollPersistenceParticipant` captures, validates, prepares, and commits payroll save data through the existing persistence pipeline. Restore validation is strict: missing compensation definitions, deduction definitions, accounts, calculations, or obligations reject the payload before live state is committed.

## Test Lab

The Test Lab suite is:

`feature.11.4.wages-employment-payroll`

It covers:

- agreement, schedule, and work-evidence validation;
- exact gross/net/deduction calculations;
- reservation, rollback, partial payment, and wage debt;
- persistence, redacted projections, corrections, and overpayments.
