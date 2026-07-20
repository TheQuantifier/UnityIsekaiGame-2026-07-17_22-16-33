# Resource Transactions

`CharacterResourceCollection` changes resource state only through `ResourceChangeRequest` and returns `ResourceChangeResult`.

Supported operations are `Gain`, `Spend`, `Damage`, `Heal`, `Set`, `Regenerate`, `Degenerate`, `Initialize`, `Reconcile`, `Restore`, and `Administrative`.

Transactions validate finite positive amounts where required, resource existence, operation permission from the definition, available current value for non-partial spends, and clamp/overfill/underflow rules.

Event IDs are optional. When provided, repeated event IDs are treated as duplicate resource events and do not apply the change again. This is a local idempotency proof only; future multiplayer authority must still live on the server.

Threshold events are emitted when resources become empty/full or leave empty/full. UI code must observe these events but must not own or mutate resource state directly.

Regeneration and degeneration run through the same transaction path as gameplay changes. Mana and Stamina currently regenerate by definition-driven ticks after their configured spend delay.
