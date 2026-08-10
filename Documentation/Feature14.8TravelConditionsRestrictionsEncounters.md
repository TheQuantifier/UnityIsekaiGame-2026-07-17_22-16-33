# Feature 14.8 - Travel Conditions, Restrictions, and Encounters

Feature 14.8 adds a scene-independent travel-condition layer under the World Locations step.

## Ownership

`TravelConditionRuntime` owns travel condition records, hazard exposure records, and travel encounter records. It does not own weather, biological conditions, injury, combat state, social simulation, legal permissions, scene spawning, or NPC decision logic.

Existing route and journey systems remain authoritative for their own records:

- `LocationRouteRuntime` owns route segments, route networks, and route planning.
- `TravelJourneyRuntime` owns journey lifecycle and progress.
- `TravelConditionRuntime` evaluates modifiers, blockers, travel requirements, hazard hooks, and encounter hooks for those systems.

## Definitions

Prototype definitions are provided by `PrototypeTravelConditionDefinitionFactory`:

- `TravelConditionDefinition`
- `TravelHazardDefinition`
- `TravelEncounterDefinition`

Catalog-authored definitions remain authoritative. Prototype fallbacks exist only to keep automation, persistence validation, and development scenes using one shared definition source.

## Evaluation

Condition evaluation is read-only and deterministic. It returns:

- applicable condition snapshots
- hard-block status
- movement-rate multiplier
- route-cost multiplier
- required and missing capabilities/equipment
- knowledge-safe encounter risk summary
- source revision and diagnostics

Hidden and secret conditions are omitted from knowledge-safe evaluation unless explicitly known or development visibility is requested. Hidden counts are not exposed through public risk summaries.

## Route And Journey Integration

Route planning can opt into `TravelConditionEvaluationMode.CurrentConditions` or `KnowledgeSafeCurrentConditions`. When enabled, the route planner applies:

- movement slowdowns as effective distance increases
- route-cost modifiers
- hard blockers and missing requirements as unusable edges
- condition revisions in route plan revalidation

Journey lifecycle requests can also opt into condition evaluation. Active conditions can adjust movement rate or block the current step. Checkpoint encounters can interrupt a journey by reference without creating combat state.

## Hazards And Encounters

Hazards and encounters are explicit records. Feature 14.8 does not implement probabilistic weather, survival, scene spawning, or autonomous encounter AI.

Hazard records may reference biology or combat consequence IDs, but those systems remain responsible for applying actual damage, disease, wounds, or other effects.

Encounter records may reference combat encounter definitions, but they do not create or duplicate combat state.

## Persistence

`TravelConditionPersistenceParticipant` captures, prepares, commits, and rolls back travel condition state. Invalid payloads are rejected before commit, and restore does not reroll hazards or retrigger encounters.
