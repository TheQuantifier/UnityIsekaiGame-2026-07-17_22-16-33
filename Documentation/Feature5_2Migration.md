# Feature 5.2 Migration

Feature 5.2 changes the character stat model:

- persistent long-term growth belongs to Base Attributes;
- current capability belongs to Calculated Stats;
- legacy `StatType` APIs remain as a compatibility surface, but are no longer the model authority once calculated stats are configured.

Development save compatibility:

- a new required player-scoped participant, `player.attributes`, owns Base Attribute persistence;
- saves without this participant are development saves from before Feature 5.2 and should be rejected rather than silently migrated;
- calculated stat values are not persisted authoritatively;
- after load, `player.identity-progression` restores identity records, `player.attributes` restores Base Attribute sources/events, and vitals/statuses restore after derived stats are available.

Legacy ID behavior:

- obsolete display/API names such as `AttackPower` and `MaximumHealth` are not registered definition IDs;
- canonical definitions use `attribute.*`, `calculated-stat.*`, and `calculated-stat-formula.*`;
- no legacy aliases are retained for old stat IDs.

Feature 5.3 and later should move new skill/effect/equipment systems toward direct calculated-stat contributions and stop introducing new dependencies on legacy base-stat assumptions.
