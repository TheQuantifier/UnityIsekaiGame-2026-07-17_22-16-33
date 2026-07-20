using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class Step3IntegrationCloseoutTests
    {
        private const string CatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";

        [Test]
        public void PrototypeCatalog_ValidatesAndRegistersRepresentativeStep3Definitions()
        {
            DefinitionCatalog catalog = LoadCatalog();
            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.False, report.GetSummary());

            DefinitionRegistry registry = catalog.CreateRegistry(report);
            Assert.That(report.HasErrors, Is.False, report.GetSummary());

            AssertRegistered(registry, "item.health-potion", "UnityIsekaiGame.Inventory.ItemDefinition");
            AssertRegistered(registry, "item.prototype-sword", "UnityIsekaiGame.Inventory.ItemDefinition");
            AssertRegistered(registry, "item.prototype-iron-ore", "UnityIsekaiGame.Inventory.ItemDefinition");
            AssertRegistered(registry, "item.weapon", "UnityIsekaiGame.GameData.CategoryDefinition");
            AssertRegistered(registry, "tag.arcane", "UnityIsekaiGame.GameData.TagDefinition");
            AssertRegistered(registry, "rarity.common", "UnityIsekaiGame.GameData.RarityDefinition");
            AssertRegistered(registry, "quality.standard", "UnityIsekaiGame.GameData.QualityDefinition");
            AssertRegistered(registry, "condition.good", "UnityIsekaiGame.GameData.ConditionDefinition");
            AssertRegistered(registry, "ability.arcane-bolt", "UnityIsekaiGame.Abilities.AbilityDefinition");
            AssertRegistered(registry, "effect.arcane-damage", "UnityIsekaiGame.Abilities.DamageEffectDefinition");
            AssertRegistered(registry, "status.prototype-might", "UnityIsekaiGame.StatusEffects.StatusEffectDefinition");
            AssertRegistered(registry, "being.prototype-enemy", "UnityIsekaiGame.Beings.BeingDefinition");
            AssertRegistered(registry, "actor-profile.enemy-prototype", "UnityIsekaiGame.Beings.ActorProfileDefinition");
            AssertRegistered(registry, "place.building.prototype-guild-board-area", "UnityIsekaiGame.Places.PlaceDefinition");
            AssertRegistered(registry, "faction.guild.adventurers", "UnityIsekaiGame.Factions.FactionDefinition");
            AssertRegistered(registry, "contract.prototype-enemy-elimination", "UnityIsekaiGame.Contracts.ContractDefinition");
            AssertRegistered(registry, "quest.prototype-strange-disturbance", "UnityIsekaiGame.Quests.QuestDefinition");
            AssertRegistered(registry, "person.prototype-npc", "UnityIsekaiGame.People.PersonDefinition");
            AssertRegistered(registry, "damage.magic.arcane", "UnityIsekaiGame.Combat.DamageTypeDefinition");
        }

        [Test]
        public void PrototypeCatalog_HasNoDuplicateGlobalDefinitionIds()
        {
            DefinitionCatalog catalog = LoadCatalog();
            string[] duplicateIds = catalog.GetDefinitions()
                .Where(definition => !string.IsNullOrWhiteSpace(definition.Id))
                .GroupBy(definition => definition.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateIds, Is.Empty, $"Duplicate definition IDs: {string.Join(", ", duplicateIds)}");
        }

        [Test]
        public void PrototypeCatalog_PreservesRepresentativeTypedCrossSystemReferences()
        {
            DefinitionRegistry registry = LoadCatalog().CreateRegistry();

            IGameDefinition sword = Required(registry, "item.prototype-sword");
            object equipment = Get<object>(sword, "Equipment");
            object meleeWeapon = Get<object>(equipment, "MeleeWeapon");
            Assert.That(Get<bool>(meleeWeapon, "IsWeapon"), Is.True);
            Assert.That(Get<IGameDefinition>(meleeWeapon, "DamageType").Id, Is.EqualTo("damage.physical.slashing"));

            IGameDefinition arcaneEffect = Required(registry, "effect.arcane-damage");
            Assert.That(Get<IGameDefinition>(arcaneEffect, "TypedDamageType").Id, Is.EqualTo("damage.magic.arcane"));

            IGameDefinition heavyArcaneEffect = Required(registry, "effect.heavy-arcane-damage");
            Assert.That(Get<IGameDefinition>(heavyArcaneEffect, "TypedDamageType").Id, Is.EqualTo("damage.magic.arcane"));

            IGameDefinition mightApplication = Required(registry, "effect.apply-prototype-might");
            Assert.That(Get<IGameDefinition>(mightApplication, "StatusEffect").Id, Is.EqualTo("status.prototype-might"));

            IGameDefinition enemyProfile = Required(registry, "actor-profile.enemy-prototype");
            Assert.That(Get<IGameDefinition>(enemyProfile, "BeingDefinition").Id, Is.EqualTo("being.prototype-enemy"));

            IGameDefinition guildBoardArea = Required(registry, "place.building.prototype-guild-board-area");
            Assert.That(Get<IGameDefinition>(guildBoardArea, "ParentPlace").Id, Is.EqualTo("place.settlement.prototype-town"));

            IGameDefinition adventurersGuild = Required(registry, "faction.guild.adventurers");
            Assert.That(Get<IGameDefinition>(adventurersGuild, "HeadquartersPlace").Id, Is.EqualTo("place.building.prototype-guild-board-area"));

            IGameDefinition enemyContract = Required(registry, "contract.prototype-enemy-elimination");
            Assert.That(Get<IGameDefinition>(enemyContract, "RequesterFaction").Id, Is.EqualTo("faction.guild.adventurers"));
            Assert.That(Get<IGameDefinition>(enemyContract, "PostingFaction").Id, Is.EqualTo("faction.guild.adventurers"));

            IGameDefinition quest = Required(registry, "quest.prototype-strange-disturbance");
            Assert.That(Get<IGameDefinition>(quest, "QuestGiver").Id, Is.EqualTo("person.prototype-npc"));
            Assert.That(Get<IGameDefinition>(quest, "QuestSourceFaction").Id, Is.EqualTo("faction.guild.adventurers"));
            Assert.That(Get<IGameDefinition>(quest, "RelatedFaction").Id, Is.EqualTo("faction.guild.adventurers"));
        }

        private static DefinitionCatalog LoadCatalog()
        {
            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Expected prototype definition catalog at {CatalogPath}.");
            return catalog;
        }

        private static void AssertRegistered(DefinitionRegistry registry, string id, string expectedTypeName)
        {
            IGameDefinition definition = Required(registry, id);
            Assert.That(definition.GetType().FullName, Is.EqualTo(expectedTypeName), $"Definition '{id}' resolved to an unexpected type.");
        }

        private static IGameDefinition Required(DefinitionRegistry registry, string id)
        {
            Assert.That(registry.TryGet(id, out IGameDefinition definition), Is.True, $"Expected definition '{id}' to be registered.");
            return definition;
        }

        private static T Get<T>(object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"Cannot read '{propertyName}' from a null target.");
            object value = target.GetType().GetProperty(propertyName).GetValue(target);
            Assert.That(value, Is.Not.Null, $"Expected '{target.GetType().Name}.{propertyName}' to be assigned.");
            return (T)value;
        }
    }
}
