# Character Attributes

Feature 5.2 introduces persistent character Attributes as long-term person qualities. Attributes are not current combat stats.

Alpha Attributes:

- `attribute.strength`
- `attribute.agility`
- `attribute.endurance`
- `attribute.vitality`
- `attribute.intellect`
- `attribute.willpower`
- `attribute.perception`
- `attribute.charisma`
- `attribute.mana-capacity`

Rules:

- uninfluenced Attributes start at `1.0`;
- runtime values keep fractional precision;
- standard display uses floor;
- Attributes have no maximum;
- only permanent sources and action-growth events mutate Attributes;
- temporary effects, equipment, roles, and conditions do not rewrite Attributes.

Runtime ownership lives in `CharacterAttributes`. It stores permanent source contributions and training events, then rebuilds current attribute values from those records. UI and gameplay code read values through this component.

Prototype origin and birth-gift permanent stat grants are now redirected through `StatTypeCalculatedStatBridge.TryMapPermanentGrantToAttribute`. The old Feature 5.1 records remain for diagnostics and current development-save shape, but the applied effect is an Attribute source when `CharacterAttributes` is present.

Feature 5.3 Skills are separate from Attributes. Skills can be learned, gain XP, and contribute to Calculated Stats, but they do not rewrite permanent Attribute source records or Attribute growth history.
