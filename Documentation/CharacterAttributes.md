# Base Attributes

Feature 5.4a standardizes the official player-facing term as Base Attributes. Existing serialized/runtime type names such as `AttributeDefinition`, `CharacterAttributes`, and `PlayerAttributesSaveData` are retained for Unity serialization safety, but docs, UI, and Test Lab labels should say Base Attributes.

Base Attributes are permanent long-term person qualities. They are not current combat stats and are not resource current/maximum pairs.

Alpha Base Attributes:

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

- uninfluenced Base Attributes start at `1.0`;
- runtime values keep fractional precision;
- standard display uses floor;
- Base Attributes have no maximum;
- only permanent sources and action-growth events mutate Base Attributes;
- temporary effects, equipment, Skills, Roles, Social Status, and conditions do not rewrite Base Attributes.

Runtime ownership lives in `CharacterAttributes`. It stores permanent source contributions and training events, then rebuilds full fractional Base Attribute values from those records. UI and gameplay code read values through this component. The Character menu uses floor display; Test Lab diagnostics show both full precision and display values.

Prototype origin and birth-gift permanent stat grants are redirected through `StatTypeCalculatedStatBridge.TryMapPermanentGrantToAttribute`. The old Feature 5.1 records remain for diagnostics and current development-save shape, but the applied effect is a Base Attribute source when `CharacterAttributes` is present.

Feature 5.3 Skills are separate from Base Attributes. Skills can be learned, gain XP, and contribute to Calculated Stats, but they do not rewrite permanent Base Attribute source records or Base Attribute growth history.

See also:

- `Documentation/BaseAttributes.md`
- `Documentation/CharacterNumericalModel.md`
