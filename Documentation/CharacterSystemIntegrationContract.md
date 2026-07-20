# Character System Integration Contract

Future systems should integrate through the coordinator or owning subsystem APIs.

Use `CharacterSystemCoordinator.Query` for read-only checks:

- read Base Attributes and Calculated Stats;
- read current Resources;
- check Skill grades;
- check Trait ownership and lifecycle;
- check capabilities, resistances, and immunities;
- evaluate `RequirementSetDefinition` without mutation.

Use owner APIs for mutations:

- damage/healing/resource spend through `CharacterResourceCollection` or current wrapper APIs that delegate to it;
- permanent Base Attribute changes through `CharacterAttributes`;
- source-safe stat effects through `CalculatedStatCollection`;
- Skill learning and grants through `CharacterSkillCollection`;
- Trait grants/removals/suppression through `CharacterTraitCollection`;
- roles, social statuses, titles, and wallet through `PlayerIdentityProgression`.

Systems should check `CharacterSystemCoordinator.IsReady` before executing player actions that depend on character state. UI should render snapshots and refresh on coordinator revision events.

Step 6 systems should treat person ID and actor/body ID as separate inputs. Dialogue, jobs, contracts, reputation, professions, crafting, economy, and NPC simulation should depend on explicit character ownership rather than a static local-player singleton.

