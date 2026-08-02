# Feature 13.9: Laws, Rights, Permissions, and Citizenship

## Overview

Feature 13.9 adds an authoritative, world-scoped legal runtime. It owns legal instruments, versioned provisions, individualized legal entitlements, Person legal statuses, legal transition plans, deterministic applicability evaluation, transaction identities, and their persistence graph. It does not own governments, organizations, institutional authority, diplomatic agreements, property, contracts, social norms, residence, or Person identity.

The runtime is deterministic, independent of scenes and frame time, previewable before mutation, idempotent by transaction ID, resettable, disposable, and suitable for future server authority.

## Concept Boundaries

- **Organizational policy** remains owned by Feature 13.5. A policy gains legal force only through an explicit legal instrument.
- **Social norms** remain owned by Feature 12.6. Social disapproval and illegality are independent outcomes.
- **Institutional permission** remains owned by Feature 13.3. It answers whether an actor may act for an institution.
- **Legal permission** answers whether applicable law permits an action. It does not grant institutional authority or physical capability.
- **Capability** answers whether an actor can perform an action. It does not make the action authorized or legal.
- **Rights** protect or permit conduct; **duties** require conduct; **prohibitions** forbid conduct; **exemptions** remove an otherwise applicable rule; **immunities** limit legal consequence or process without erasing the underlying rule.
- **Crime and courts** are deferred. A prohibition is not a crime occurrence, and the runtime records no suspect, warrant, charge, judgment, or punishment.

`LegalRuntime.EvaluateAuthorizedAction` returns institutional authorization and legal applicability as separate results plus a combined `Allowed` value. Neither subsystem duplicates the other's state.

## Definitions and Identity

`LegalAuthorityDefinition`, `LegalInstrumentDefinition`, `LegalProvisionDefinition`, `LegalStatusDefinition`, and `CitizenshipDefinition` are immutable catalog definitions. `PrototypeLegalDefinitionFactory` supplies missing prototype definitions without replacing catalog-authored definitions.

Runtime records use stable IDs. Instrument IDs do not change when an instrument is published, activated, suspended, restored, repealed, or superseded. Provision IDs remain stable while `LegalProvisionVersionData` preserves chronological amendment versions. Citizenship and other statuses have independent stable status IDs and preserve their lifecycle history.

## Instruments and Provisions

Enactment validates the government, government level, jurisdiction ownership, authority definition, institutional authority grant, source resolution, treaty source when applicable, publication timing, effective timing, emergency duration, provision definitions, and provision IDs before committing any record. Preview performs the same validation without mutation.

Publication is explicit through `PublishInstrument`. Lifecycle transitions support suspension, restoration, expiration, repeal, supersession, and historical retention. `TransitionProvision` permits partial suspension, repeal, or supersession without deleting the containing instrument. Amendments close the prior version and append a later version; historical evaluation selects the version active at the requested simulation time.

Legal effects are structured as rights, permissions, duties, prohibitions, exemptions, immunities, eligibility, status grants or restrictions, property restrictions, and contract capacity. Applicability fields cover Person, organization, territory, place, property, office, profession, legal status, activity, and subject matter. These references constrain law; they do not take ownership of the referenced system.

## Applicability and Conflicts

`LegalApplicabilityRequest` contains the subject and authoritative world time. Evaluation is read-only. Applicable provisions are ordered by instrument precedence, scope specificity, effective time, and stable provision ID. Individual entitlements are then incorporated without rewriting general law.

Higher precedence resolves conflicts first. Specific rules sort ahead of general rules at the same tier. An instrument configured with `Unresolved` returns `Conflict` when equal-tier applicable effects oppose one another. The runtime never silently converts an unresolved conflict into permission.

Current and historical queries use the same evaluator. Scheduled law can be evaluated at a historical time after its effective boundary even before a scheduler materializes its current lifecycle. Repealed and superseded records remain queryable.

## Rights, Permits, Duties, Exemptions, and Immunities

Individual entitlements can target a Person or organization and can be scoped to an action, territory, or property. Their own effective time, expiration, visibility, provenance, and lifecycle are persisted. `TransitionEntitlement` supports suspension, restoration, expiration, revocation, and historical retention. Duties identify legal requirements but do not duplicate Step 11 obligation, payment, contract, or tax-calculation records.

## Citizenship and Legal Status

Legal-status records distinguish citizen, subject, national, permanent resident, temporary resident, protected Person, stateless Person, and foreign visitor. Citizenship validates a definition-backed acquisition route and consent policy. Birth and succession can follow authored non-consensual rules; grant and restoration routes enforce consent when required. Multiple citizenship follows both the status and citizenship definitions.

Citizenship is separate from current residence, physical presence, organization membership, and nationality categories. A recognizing government may be active, provisional, in exile, or an occupation administration according to the government runtime. Renunciation requires Person consent. Revocation, loss, dispute, restoration, and supersession require explicit lifecycle operations and retain the original status record.

## Political and Economic Integration

Government and jurisdiction identity come from Feature 13.8. Organization authority grants come from Feature 13.3; decision provenance comes from Feature 13.5. Treaty obligations have no domestic legal effect until an explicit treaty-implementation instrument references an existing Feature 13.7 agreement. Government succession, territorial transfer, dissolution, occupation, and treaty implementation are represented as explicit legal transition plans and never mutate government ownership implicitly.

Property, business, contract, profession, military, and religious IDs can scope provisions and entitlements. Their owning runtimes remain authoritative. Tax calculation, collection, border processing, visas, travel enforcement, crime, and courts are deferred.

## Visibility and Knowledge

Authoritative applicability does not depend on whether a requester knows a law. Public projections return full records. Restricted or secret projections redact sensitive identity, source, provision, and provenance fields. Hidden projections are denied. Status owners and privileged services may inspect full status records. Projection never creates knowledge, evidence, memory, or history state.

## Queries and Snapshots

The runtime provides deterministic queries by instrument ID, government, polity, jurisdiction, territory, category, lifecycle, treaty agreement, provision instrument, Person entitlement, and Person status. All returned records, collections, applicability results, projections, and save data are clones. Later runtime mutation cannot alter an earlier snapshot.

## Time and Scheduling

`ProcessWorldTime` consumes an authoritative boundary ID and simulation time. Due activation, expiration, entitlement expiration, and transition-plan work is globally sorted by time, operation kind, and stable ID. `maximumOperations` bounds each call; remaining work is reported and deferred without loss. Reusing a transaction ID is idempotent. No system time, frame time, coroutine order, or scene update is used.

## Persistence and Validation

`LegalPersistenceParticipant` owns the `world.laws` payload and depends on `world.governments`. Prepare validates schema, world, null and duplicate records, definitions, authority compatibility, government and jurisdiction ownership, dates, publication state, instrument/provision indexes, version chronology and overlap, exception and succession links, entitlement targets, citizenship provenance, duplicate-status policy, transitions, and transaction identity. Commit restores only after prepare succeeds and retains rollback data.

`LegalRuntimeValidationService` is the common read-only validation entry point for persistence, tools, and future integration code. Restore does not replay enactments, grants, transitions, events, or economic mutations.

## Test Lab

The suite ID is `feature.13.9.laws-rights-permissions-citizenship`. It uses a fresh fixture-owned legal runtime and covers readiness, central law, authority separation, publication and effective time, amendment history, repeal and supersession, entitlements, immunity, citizenship, government lifecycle, territorial transition, treaty implementation, conflict resolution, visibility, and persistence.

Run it in the in-game Test Lab or through the command-side automation catalog. Both paths resolve the same suite definition and fixture requirements.

## Multiplayer Boundary

Future multiplayer implementations must keep instruments, versions, applicability, entitlements, status, transitions, visibility, and timestamps server-authoritative. Clients may receive access-filtered immutable projections. Client-supplied law state, citizenship, permits, timestamps, or selected applicable provisions must never be trusted.

## Current Limitations and Deferred Scope

The feature intentionally does not implement crime incidents, investigations, warrants, arrest, prosecution, court cases, judgments, sentencing, punishment, tax calculation or collection, customs, immigration processing, visas, passports, border checks, elections, autonomous lawmaking, legal-text generation, final legal UI, or networking. Transition plans preserve legal intent and provenance; later features execute domain-specific political, crime, court, taxation, and travel workflows through their owning runtimes.
