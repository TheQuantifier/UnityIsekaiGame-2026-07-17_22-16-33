# Resource Definitions

Resource definitions are catalog definitions with canonical IDs using the `resource.` namespace.

Current alpha definitions:

- `resource.health`: vital body state, damage/healing allowed, no passive regeneration.
- `resource.stamina`: physical exertion reserve, spend/gain allowed, regenerates after spend delay.
- `resource.mana`: magical reserve, spend/gain allowed, regenerates after spend delay.

Each resource must link to exactly one `CalculatedStatDefinition` whose purpose is `ResourceMaximum`. The linked calculated stat must point back to the same resource ID through `LinkedFutureResourceId`.

Validation checks missing or non-`resource.` IDs, missing linked maximum stats, linked maximum stats not present in the catalog, linked stats not marked `ResourceMaximum`, mismatched resource IDs and maximum-stat resource links, duplicate resource claims on one maximum stat, invalid initialization/reconciliation enum values, and invalid regeneration or degeneration rates.

The current implementation is prototype-quality and replaceable. The definition layer is intentionally broad enough for later resources such as hunger, fatigue, corruption, morale, threat, or environmental meters without implementing those systems now.
