# Character Numerical Model

Feature 5.4b separates the current character number layers.

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

Current Resources:

- generalized runtime values for Health, Mana, and Stamina;
- clamp against calculated maximums where configured;
- are owned by `CharacterResourceCollection`;
- remain exposed through existing `PlayerHealth`, `PlayerMana`, and `PlayerStamina` wrapper APIs for compatibility.

## Source Contributions

Permanent source contributions belong to Base Attributes. Examples include origin or birth-gift permanent grants.

Temporary or active contributions belong to Calculated Stats. Examples include status modifiers, equipment modifiers, and Skill grade packages. These sources should be removed or rebuilt by their owning systems.

## Persistence

Base Attribute records are persisted by `player.attributes`. Calculated Stats rebuild after load. Current Health, Mana, and Stamina now persist through optional `player.resources`, with the older vitals/status payload retained for compatibility.

This model is still local-prototype only, but ownership remains compatible with multiplayer persistence: player-owned values live in player participants, while future shared-world state must remain server-owned.
