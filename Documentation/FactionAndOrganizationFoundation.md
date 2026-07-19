# Faction And Organization Foundation

Feature 3.12 adds a static faction and organization definition layer. The selected model is unified: `FactionDefinition` represents kingdoms, governments, guilds, companies, guards, churches, criminal groups, noble houses, settlement authorities, and informal groups. A separate `OrganizationDefinition` was deferred because the first version only needs one stable identity, taxonomy, hierarchy, and validation path.

## Responsibilities

`FactionDefinition` owns immutable authored metadata: stable `faction.*` ID, display name, description, icon, presentation color, category, tags, `FactionKind`, one optional parent faction, optional home/headquarters/default jurisdiction/founding places, optional default leader, authority flags, visibility, and placeholder culture/founding text.

It does not own current members, ranks, reputation, diplomacy, wars, laws, taxes, treasury, contracts, inventory, AI memory, or mutable territory control.

## Static Versus Runtime State

Static faction definitions describe what an organization is intended to be. Runtime faction state will later describe what is currently true: person membership, rank, role tags, standing, permissions, reputation, legal standing, fame, hostility, trust, leadership changes, diplomacy, laws, taxes, licenses, property, treasury, active contracts, and market state.

`DefinitionRegistry` resolves static `FactionDefinition` assets. A future `FactionStateRegistry` should resolve mutable faction-state objects if those systems exist.

## Faction Kinds

`FactionKind` is compact structural metadata: `Nation`, `Government`, `Guild`, `Company`, `Military`, `Religious`, `Criminal`, `NobleHouse`, `SettlementAuthority`, `InformalGroup`, and `Other`. Kinds help validation and simple queries. Categories and tags remain the flexible taxonomy system.

## Categories And Tags

Faction categories use the `faction-category.*` namespace so they do not collide with globally unique `faction.*` definition IDs.

Prototype categories added: `faction-category`, `faction-category.nation`, `faction-category.government`, `faction-category.guild`, `faction-category.company`, `faction-category.military`, `faction-category.criminal`, and `faction-category.settlement-authority`.

Prototype reusable tags added: `tag.adventurer`, `tag.merchant`, `tag.government`, `tag.criminal`, `tag.public`, `tag.secret`, `tag.local`, `tag.national`, and `tag.trade`. Existing `tag.prototype` is reused.

## Hierarchy

Each faction can have at most one primary parent faction. Parent hierarchy is for primary organization containment only, such as town guard under kingdom or branch under company. Parent hierarchy does not model alliances, vassalage, trade agreements, rivalries, wars, legal status, or diplomacy.

`FactionHierarchyUtility` provides stable-ID comparison, descendant checks, ancestor traversal, root lookup, nearest ancestor by kind/category, readable hierarchy paths, safe cycle detection, and shared-parent checks.

## Authority Metadata

`FactionAuthorityFlags` describes broad static capabilities such as issuing contracts, approving contracts, assigning ranks, enforcing law, operating markets, commanding guards, owning property, granting licenses, and reviewing reports. These flags do not execute actions. Future runtime systems must still validate permissions, resources, current rank, law, reputation, and world state.

## Place And Person Integration

Faction place references are typed `PlaceDefinition` references for home, headquarters, default jurisdiction, and founding place. Default jurisdiction is static intended responsibility, not current political control.

`PersonDefinition` can reference a primary faction, public role title, and leadership faction as static identity defaults. It still does not store mutable employment, rank, membership, or reputation.

`FactionDefinition.DefaultLeader` is initial/default metadata only. It does not mean the person is alive, loaded, currently in office, or valid after runtime world changes.

`BeingDefinition` and `ActorProfileDefinition` do not store faction affiliation. Being describes what kind of being something is; actor profiles describe base stats.

## Quest And Contract Integration

`QuestDefinition` can reference an optional source faction and related faction. Current quest flow, stages, objectives, rewards, and journal state remain unchanged.

`ContractDefinition` can reference an optional requester person, requester faction, posting faction, and approving organization placeholder. Legacy requester text remains as a fallback. Faction definitions do not own active contracts.

## Prototype Factions

The prototype catalog registers `faction.kingdom.prototype`, `faction.guild.adventurers`, `faction.guild.merchants`, `faction.guard.prototype-town`, and `faction.bandits.prototype`. The town guard is parented under the prototype kingdom. Guilds and bandits are independent in this first version.

## Save Data And Restoration

`FactionReferenceSaveData` stores only a faction ID. `FactionMembershipSaveData` documents the future runtime membership shape with faction ID, person ID, rank ID, role IDs, membership state, and a reputation placeholder. Definitions are not serialized into saves.

Future restoration order:

1. Resolve faction definitions.
2. Restore mutable faction-state objects.
3. Restore leadership changes.
4. Restore memberships and ranks.
5. Restore reputation and legal standing.
6. Restore diplomacy and relationships.
7. Restore faction-owned contracts, property, and treasury.
8. Notify quests, NPC AI, UI, and world simulation.

## Validation

`Tools > Game Data > Validate Definitions` validates faction IDs, display names, categories/tags, parent references, parent cycles, authority flags, visibility, place references, leader references, and suspicious kind/category/parent/jurisdiction combinations. Person, place, quest, and contract definitions also validate typed faction references.

## Creating A New Faction

1. Create a `FactionDefinition` asset.
2. Assign a stable `faction.*` ID.
3. Choose a `FactionKind`.
4. Assign a `faction-category.*` primary category and any relevant tags.
5. Optionally assign parent faction, home/headquarters/jurisdiction places, default leader, visibility, color, and authority flags.
6. Add the asset to the shared `DefinitionCatalog`.
7. Run `Tools > Game Data > Validate Definitions`.

## Known Limitations

This feature intentionally defers guild ranks, runtime memberships, reputation values, diplomacy, laws, taxes, licenses, property, treasury, economy, politics, war, AI leaders, save/load orchestration, and multiplayer-facing authority rules.
