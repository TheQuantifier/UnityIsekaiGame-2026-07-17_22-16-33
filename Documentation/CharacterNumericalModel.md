# Character Numerical Model

Feature 5.4a separates the current character number layers.

## Layers

Base Attributes:

- permanent/intrinsic player or person foundation;
- fractional storage with floor display;
- persisted as player/person-owned contribution and growth records;
- never temporarily modified.

Calculated Stats:

- derived and cached;
- rebuilt from Base Attributes and active source contributions;
- can describe combat outputs, utility values, and resource maximums;
- not saved as authoritative values.

Current Vitals:

- specialized runtime values for Health, Mana, and Stamina;
- clamp against calculated maximums where configured;
- remain owned by existing `PlayerHealth`, `PlayerMana`, and `PlayerStamina` components;
- are not generalized into ResourceDefinitions in Feature 5.4a.

## Source Contributions

Permanent source contributions belong to Base Attributes. Examples include origin or birth-gift permanent grants.

Temporary or active contributions belong to Calculated Stats. Examples include status modifiers, equipment modifiers, and Skill grade packages. These sources should be removed or rebuilt by their owning systems.

## Persistence

Base Attribute records are persisted by `player.attributes`. Calculated Stats rebuild after load. Current Health, Mana, and Stamina persist through the existing vitals/status participant work from Step 4.

This model is still local-prototype only, but ownership remains compatible with multiplayer persistence: player-owned values live in player participants, while future shared-world state must remain server-owned.
