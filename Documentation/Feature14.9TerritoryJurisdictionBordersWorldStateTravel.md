# Feature 14.9 - Territory, Jurisdiction, Borders, and World-State Travel Integration

Feature 14.9 adds a political travel layer over Step 14 physical locations, routes, journeys, and travel conditions. It does not replace those systems and does not own Step 13 government, law, crime, diplomacy, or justice records.

## Ownership Boundary

- `PoliticalTravelRuntime` owns border checkpoints, traveler crossing authorizations, political crossing records, and political travel transaction history.
- `LocationRuntime`, `LocationConnectionRuntime`, `LocationRouteRuntime`, `TravelJourneyRuntime`, and `TravelConditionRuntime` remain authoritative for physical place identity, traversability, route planning, journey state, hazards, and encounters.
- `GovernmentRuntime` remains authoritative for territories, governments, jurisdictions, sovereignty, and territorial membership.
- `LegalRuntime` remains authoritative for legal permission, prohibition, duty, entitlement, and legal status.
- `CrimeRuntime` and `JusticeRuntime` remain authoritative for wanted status, warrants, custody, courts, and punishment.

Political travel combines those sources into an evaluation projection:

- physical travel possible or blocked
- internal movement, entry, exit, cross-border, or contested-border classification
- origin and destination territory
- origin and destination jurisdiction
- legal state and required legal actions
- checkpoint state and authorization requirements
- visible wanted/warrant enforcement summary

## Runtime APIs

`PoliticalTravelRuntime` exposes:

- `EvaluateCrossing` for non-mutating physical/legal/political evaluation.
- `RecordCrossing` for committed crossing records.
- `CreateCheckpoint` for border checkpoint records.
- `GrantAuthorization` for crossing authorization records.
- `BuildPoliticalRouteRequirements` for route requirement summaries containing legal actions, checkpoints, and involved political territories.
- `CreateSaveData`, `RestoreFromSaveData`, and `ValidateSaveData` for deterministic persistence.

Legal compliance is explicit through `TravelLegalComplianceMode`:

- `RequireLegalTravel` blocks legally prohibited or unauthorized crossings.
- `AllowIllegalTravel` can record physically possible illegal crossings.
- `PreferLegalTravel` reports illegal state without replacing route feasibility.
- `StructuralOnlyDevelopment` lets existing physical route tests stay focused on topology unless political checks are requested.

## Persistence

`PoliticalTravelPersistenceParticipant` stores only political-travel-owned records. It declares dependencies on government, legal, location, and route persistence, with crime data as an optional dependency for wanted and warrant validation.

Restore validates graph references before commit and rolls back if restore fails.

## Test Lab

The `feature.14.9.territory-jurisdiction-borders-world-state-travel` automation suite runs with fresh fixture-owned runtimes and builds a two-territory political graph from prototype locations. Scenarios verify:

- runtime readiness and Step 13 ownership boundaries
- territory and jurisdiction resolution
- legal compliance mode behavior
- checkpoint authorization gating
- wanted/warrant visibility redaction
- political route requirements
- persistence validation and fixture snapshot restore
