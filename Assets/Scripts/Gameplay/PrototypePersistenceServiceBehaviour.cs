using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypePersistenceServiceBehaviour : MonoBehaviour
    {
        [SerializeField] private PrototypePersistenceState prototypeState;
        [SerializeField] private DefinitionCatalog definitionCatalog;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private bool registerPlayerInventoryEquipment = true;
        [SerializeField] private string prototypeSlotId = PersistenceService.PrototypeSlotId;

        private PersistenceService service;
        private PrototypePersistenceStateParticipant participant;
        private PlayerInventoryEquipmentPersistenceParticipant inventoryEquipmentParticipant;
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
        }

        public void ConfigurePlayerPersistence(DefinitionCatalog catalog, PlayerInventory inventory, PlayerEquipment equipment)
        {
            definitionCatalog = catalog;
            playerInventory = inventory;
            playerEquipment = equipment;
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
