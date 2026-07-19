using System.IO;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Editor
{
    public static class PersistenceMenu
    {
        private const string PrototypeCatalogPath = "Assets/GameData/Prototype/PrototypeDefinitionCatalog.asset";

        [MenuItem("Tools/Persistence/Save Prototype Slot")]
        public static void SavePrototypeSlot()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceSaveResult result = service.SavePrototypeSlot();
            Debug.Log($"Persistence save result: {result.Status} - {result.Message}");
        }

        [MenuItem("Tools/Persistence/Load Prototype Slot")]
        public static void LoadPrototypeSlot()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceLoadResult result = service.LoadPrototypeSlot();
            Debug.Log($"Persistence load result: {result.Status} - {result.Message}");
        }

        [MenuItem("Tools/Persistence/Load Prototype Backup")]
        public static void LoadPrototypeBackup()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceLoadResult result = service.LoadPrototypeBackup();
            Debug.Log($"Persistence backup load result: {result.Status} - {result.Message}");
        }

        [MenuItem("Tools/Persistence/Validate Prototype Slot")]
        public static void ValidatePrototypeSlot()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceValidationResult result = service.ValidatePrototypeSlot();
            Debug.Log($"Persistence validation result: {result.Status} - {result.Message} BackupAvailable={result.BackupAvailable}");
        }

        [MenuItem("Tools/Persistence/List Save Slots")]
        public static void ListSaveSlots()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            foreach (SaveSlotMetadata metadata in service.ListSaveSlots())
            {
                Debug.Log($"Slot {metadata.slotId}: Valid={metadata.isValid}, Primary={metadata.hasPrimary}, Backup={metadata.hasBackup}, Modified={metadata.modifiedUtc}, Message={metadata.message}");
            }
        }

        [MenuItem("Tools/Persistence/Delete Prototype Slot")]
        public static void DeletePrototypeSlot()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceDeleteResult result = service.DeletePrototypeSlot();
            Debug.Log($"Persistence delete result: {result.Status} - {result.Message}");
        }

        [MenuItem("Tools/Persistence/Increment Prototype Value")]
        public static void IncrementPrototypeValue()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            service.PrototypeState.IncrementValue();
        }

        [MenuItem("Tools/Persistence/Toggle Prototype Flag")]
        public static void TogglePrototypeFlag()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            service.PrototypeState.ToggleFlag();
        }

        [MenuItem("Tools/Persistence/Corrupt Prototype Primary File")]
        public static void CorruptPrototypePrimaryFile()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            if (!service.Service.PathProvider.TryGetPaths(service.PrototypeSlotId, out SaveSlotPaths paths, out string failureReason))
            {
                Debug.LogWarning(failureReason);
                return;
            }

            File.WriteAllText(paths.PrimaryPath, "{ bad json", System.Text.Encoding.UTF8);
            Debug.Log($"Corrupted prototype primary save at {paths.PrimaryPath}");
        }

        [MenuItem("Tools/Persistence/Open Prototype Save Folder")]
        public static void OpenPrototypeSaveFolder()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            service.Service.PathProvider.EnsureDirectory();
            EditorUtility.RevealInFinder(service.Service.PathProvider.RootDirectory);
        }

        private static PrototypePersistenceServiceBehaviour GetOrCreatePrototypeService()
        {
            PrototypePersistenceServiceBehaviour service = Object.FindAnyObjectByType<PrototypePersistenceServiceBehaviour>();
            if (service != null)
            {
                ConfigurePrototypePlayerPersistence(service);
                service.EnsureInitialized();
                return service;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("Prototype persistence commands require Play Mode for Feature 4.1 runtime proof.");
                return null;
            }

            GameObject root = new GameObject("Prototype Persistence");
            service = root.AddComponent<PrototypePersistenceServiceBehaviour>();
            ConfigurePrototypePlayerPersistence(service);
            service.EnsureInitialized();
            Debug.Log("Created scene-local Prototype Persistence runtime root for testing.");
            return service;
        }

        private static void ConfigurePrototypePlayerPersistence(PrototypePersistenceServiceBehaviour service)
        {
            if (service == null)
            {
                return;
            }

            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(PrototypeCatalogPath);
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            PlayerEquipment equipment = inventory == null ? Object.FindAnyObjectByType<PlayerEquipment>() : inventory.GetComponent<PlayerEquipment>();
            service.ConfigurePlayerPersistence(catalog, inventory, equipment);
        }
    }
}
