using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypePersistenceServiceBehaviour : MonoBehaviour
    {
        [SerializeField] private PrototypePersistenceState prototypeState;
        [SerializeField] private DefinitionCatalog definitionCatalog;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerMana playerMana;
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private StatusEffectController statusEffectController;
        [SerializeField] private PlayerQuestLog playerQuestLog;
        [SerializeField] private PlayerContractJournal playerContractJournal;
        [SerializeField] private bool registerPlayerInventoryEquipment = true;
        [SerializeField] private bool registerPlayerStatsVitalsStatus = true;
        [SerializeField] private bool registerPlayerQuestContract = true;
        [SerializeField] private string prototypeSlotId = PersistenceService.PrototypeSlotId;

        private PersistenceService service;
        private PrototypePersistenceStateParticipant participant;
        private PlayerInventoryEquipmentPersistenceParticipant inventoryEquipmentParticipant;
        private PlayerStatsVitalsStatusPersistenceParticipant statsVitalsStatusParticipant;
        private PlayerQuestContractPersistenceParticipant questContractParticipant;
        private DefinitionRegistry definitionRegistry;

        public PersistenceService Service => service;
        public PrototypePersistenceState PrototypeState => prototypeState;
        public string PrototypeSlotId => string.IsNullOrWhiteSpace(prototypeSlotId) ? PersistenceService.PrototypeSlotId : prototypeSlotId;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (service != null && participant != null)
            {
                service.UnregisterParticipant(participant);
                participant = null;
            }

            if (service != null && inventoryEquipmentParticipant != null)
            {
                service.UnregisterParticipant(inventoryEquipmentParticipant);
                inventoryEquipmentParticipant = null;
            }

            if (service != null && statsVitalsStatusParticipant != null)
            {
                service.UnregisterParticipant(statsVitalsStatusParticipant);
                statsVitalsStatusParticipant = null;
            }

            if (service != null && questContractParticipant != null)
            {
                service.UnregisterParticipant(questContractParticipant);
                questContractParticipant = null;
            }
        }

        public void ConfigurePlayerPersistence(
            DefinitionCatalog catalog,
            PlayerInventory inventory,
            PlayerEquipment equipment,
            PlayerStats stats,
            PlayerHealth health,
            PlayerMana mana,
            PlayerStamina stamina,
            StatusEffectController statusController,
            PlayerQuestLog questLog,
            PlayerContractJournal contractJournal)
        {
            definitionCatalog = catalog;
            playerInventory = inventory;
            playerEquipment = equipment;
            playerStats = stats;
            playerHealth = health;
            playerMana = mana;
            playerStamina = stamina;
            statusEffectController = statusController;
            playerQuestLog = questLog;
            playerContractJournal = contractJournal;
            definitionRegistry = null;
        }

        public void EnsureInitialized()
        {
            if (prototypeState == null)
            {
                prototypeState = GetComponent<PrototypePersistenceState>();
            }

            if (prototypeState == null)
            {
                prototypeState = gameObject.AddComponent<PrototypePersistenceState>();
            }

            service ??= new PersistenceService();
            if (participant == null)
            {
                participant = new PrototypePersistenceStateParticipant(prototypeState);
                service.RegisterParticipant(participant, out string failureReason);
                if (!string.IsNullOrWhiteSpace(failureReason))
                {
                    Debug.LogWarning(failureReason);
                    participant = null;
                }
            }

            EnsurePlayerInventoryEquipmentParticipant();
            EnsurePlayerStatsVitalsStatusParticipant();
            EnsurePlayerQuestContractParticipant();
        }

        public PersistenceSaveResult SavePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.Save(PrototypeSlotId, "Prototype Slot");
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeSlot()
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeBackup()
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId, loadBackup: true);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceValidationResult ValidatePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceValidationResult result = service.ValidateSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult DeletePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.DeleteSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public IReadOnlyList<SaveSlotMetadata> ListSaveSlots()
        {
            EnsureInitialized();
            return service.ListSaveSlots();
        }

        private static void Report(bool succeeded, string message)
        {
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            PrototypeHudMessageBus.Show(message);
        }

        private void EnsurePlayerInventoryEquipmentParticipant()
        {
            if (!registerPlayerInventoryEquipment || inventoryEquipmentParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerInventory == null || playerEquipment == null)
            {
                Debug.LogWarning("Player inventory/equipment persistence participant was not registered because the prototype player inventory or equipment component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player inventory/equipment persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            inventoryEquipmentParticipant = new PlayerInventoryEquipmentPersistenceParticipant(
                playerInventory,
                playerEquipment,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(inventoryEquipmentParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                inventoryEquipmentParticipant = null;
            }
        }

        private void ResolvePlayerPersistenceReferences()
        {
            if (playerInventory == null)
            {
                playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
            }

            if (playerEquipment == null && playerInventory != null)
            {
                playerEquipment = playerInventory.GetComponent<PlayerEquipment>();
            }

            if (playerEquipment == null)
            {
                playerEquipment = Object.FindAnyObjectByType<PlayerEquipment>();
            }

            if (playerInventory == null && playerEquipment != null)
            {
                playerInventory = playerEquipment.GetComponent<PlayerInventory>();
            }

            GameObject playerRoot = playerInventory == null ? playerEquipment == null ? null : playerEquipment.gameObject : playerInventory.gameObject;
            if (playerRoot == null && playerStats != null)
            {
                playerRoot = playerStats.gameObject;
            }

            if (playerStats == null)
            {
                playerStats = playerRoot == null ? Object.FindAnyObjectByType<PlayerStats>() : playerRoot.GetComponent<PlayerStats>();
            }

            if (playerHealth == null)
            {
                playerHealth = playerRoot == null ? Object.FindAnyObjectByType<PlayerHealth>() : playerRoot.GetComponent<PlayerHealth>();
            }

            if (playerMana == null)
            {
                playerMana = playerRoot == null ? Object.FindAnyObjectByType<PlayerMana>() : playerRoot.GetComponent<PlayerMana>();
            }

            if (playerStamina == null)
            {
                playerStamina = playerRoot == null ? Object.FindAnyObjectByType<PlayerStamina>() : playerRoot.GetComponent<PlayerStamina>();
            }

            if (statusEffectController == null)
            {
                statusEffectController = playerRoot == null ? Object.FindAnyObjectByType<StatusEffectController>() : playerRoot.GetComponent<StatusEffectController>();
            }

            if (playerQuestLog == null)
            {
                playerQuestLog = playerRoot == null ? Object.FindAnyObjectByType<PlayerQuestLog>() : playerRoot.GetComponent<PlayerQuestLog>();
            }

            if (playerContractJournal == null)
            {
                playerContractJournal = playerRoot == null ? Object.FindAnyObjectByType<PlayerContractJournal>() : playerRoot.GetComponent<PlayerContractJournal>();
            }
        }

        private void EnsurePlayerStatsVitalsStatusParticipant()
        {
            if (!registerPlayerStatsVitalsStatus || statsVitalsStatusParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerStats == null || playerHealth == null || playerMana == null || playerStamina == null || statusEffectController == null)
            {
                Debug.LogWarning("Player stats/vitals/status persistence participant was not registered because one or more prototype player runtime components are missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player stats/vitals/status persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            statsVitalsStatusParticipant = new PlayerStatsVitalsStatusPersistenceParticipant(
                playerStats,
                playerHealth,
                playerMana,
                playerStamina,
                statusEffectController,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(statsVitalsStatusParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                statsVitalsStatusParticipant = null;
            }
        }

        private void EnsurePlayerQuestContractParticipant()
        {
            if (!registerPlayerQuestContract || questContractParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerQuestLog == null || playerContractJournal == null || playerInventory == null)
            {
                Debug.LogWarning("Player quest/contract persistence participant was not registered because the prototype player quest log, contract journal, or inventory is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player quest/contract persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            questContractParticipant = new PlayerQuestContractPersistenceParticipant(
                playerQuestLog,
                playerContractJournal,
                playerInventory,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(questContractParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                questContractParticipant = null;
            }
        }

        private DefinitionRegistry GetDefinitionRegistry()
        {
            if (definitionCatalog == null)
            {
                return null;
            }

            return definitionRegistry ??= definitionCatalog.CreateRegistry();
        }
    }
}
