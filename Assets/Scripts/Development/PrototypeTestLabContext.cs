#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabContext
    {
        public DefinitionCatalog DefinitionCatalog;
        public PlayerInventory Inventory;
        public PlayerEquipment Equipment;
        public PlayerStats PlayerStats;
        public PlayerHealth PlayerHealth;
        public PlayerMana PlayerMana;
        public PlayerStamina PlayerStamina;
        public StatusEffectController PlayerStatuses;
        public PlayerSpellcaster Spellcaster;
        public PlayerSpellLoadout SpellLoadout;
        public PlayerQuestLog QuestLog;
        public PlayerContractJournal ContractJournal;
        public PrototypeTestController TestController;
        public PrototypePersistenceServiceBehaviour Persistence;
        public Transform PlayerTransform;
        public EnemyHealth EnemyHealth;
        public PrototypeEnemyController EnemyController;
        public EnemyMeleeAttack EnemyAttack;
        public StatusEffectController EnemyStatuses;
        public Transform EnemyTransform;
    }
}
#endif
