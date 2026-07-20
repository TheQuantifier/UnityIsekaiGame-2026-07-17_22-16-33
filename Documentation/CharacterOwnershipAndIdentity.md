# Character Ownership And Identity

The Character System keeps identity layers distinct.

| Layer | Owns |
| --- | --- |
| Account | Account-level ownership and future authentication linkage. It does not own ordinary character progression. |
| Player | Player metadata and local save ownership. |
| Person | Persistent character identity: origin, birth gifts, Base Attributes, Skills, Traits, roles, social statuses, titles, wallet, history, and current actor association. |
| Actor/body | Runtime presence: transform, scene/world entity, equipment, statuses, current resources where body-specific, combat participation, and temporary body conditions. |

`PlayerIdentityProgression.PersonId` is the persistent person ID. `CharacterSystemCoordinator.ActorId` is resolved from `WorldEntityIdentity` when available or from a cached runtime actor ID fallback. These IDs must not collapse into one value.

Current prototype ownership decisions:

- Person-owned: identity/progression, origin, birth gifts, Base Attribute permanent sources, Skills, Traits, roles, social statuses, titles, wallet.
- Actor/body-owned: transform, equipment, statuses, combat state, scene presence, resource current values for the embodied actor.
- Mixed: Calculated Stats are derived from person-owned and actor-owned sources; Current Resources persist as player state today but are architecturally actor/body resources.
- Player-scoped: HUD, Tab menu, Test Lab, save-slot controls, and current local persistence participants.

Future NPCs may instantiate the coordinator with reduced subsystems and no account component. Missing components are warnings for reduced characters but errors when a present subsystem is unconfigured.

