using System.IO;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.Editor
{
    public static class PersistenceMenu
    {
        private const string PrototypeCatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";

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

        [MenuItem("Tools/Persistence/List Save Slot Descriptors")]
        public static void ListSaveSlotDescriptors()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            foreach (SaveSlotDescriptor descriptor in service.BuildSaveSlotDescriptors())
            {
                Debug.Log($"{descriptor.displayName} ({descriptor.slotId}): Kind={descriptor.slotKind}, Exists={descriptor.exists}, Valid={descriptor.isValid}, Compatibility={descriptor.compatibilityStatus}, Saved={PrototypeSaveSlotCatalog.FormatLocalTimestamp(descriptor.lastSavedAtUtc)}, PlayTime={PrototypeSaveSlotCatalog.FormatPlayTime(descriptor.playTimeSeconds)}, Backup={descriptor.backupExists}");
            }
        }

        [MenuItem("Tools/Persistence/Force Autosave")]
        public static void ForceAutosave()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceSaveResult result = service.ForceAutosave("EditorMenu");
            Debug.Log($"Force autosave result: {result.Status} - {result.Message}");
        }

        [MenuItem("Tools/Persistence/Integration Diagnostics")]
        public static void IntegrationDiagnostics()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            Debug.Log(service.BuildPersistenceIntegrationDiagnosticSummary());
        }

        [MenuItem("Tools/Persistence/Run Recovery Scan")]
        public static void RunRecoveryScan()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            SaveRecoveryScanReport report = service.RunRecoveryScan();
            Debug.Log($"Recovery scan: {report.candidates.Length} candidate(s). {report.recommendation}");
        }

        [MenuItem("Tools/Persistence/Promote Manual Slot 1 Backup")]
        public static void PromoteManualSlotOneBackup()
        {
            PrototypePersistenceServiceBehaviour service = GetOrCreatePrototypeService();
            if (service == null)
            {
                return;
            }

            PersistenceSaveResult result = service.PromoteBackup(PrototypeSaveSlotCatalog.ManualSlotId(0));
            Debug.Log($"Promote backup result: {result.Status} - {result.Message}");
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
            GameObject playerRoot = inventory == null ? equipment == null ? null : equipment.gameObject : inventory.gameObject;
            PlayerStats stats = playerRoot == null ? Object.FindAnyObjectByType<PlayerStats>() : playerRoot.GetComponent<PlayerStats>();
            PlayerHealth health = playerRoot == null ? Object.FindAnyObjectByType<PlayerHealth>() : playerRoot.GetComponent<PlayerHealth>();
            PlayerMana mana = playerRoot == null ? Object.FindAnyObjectByType<PlayerMana>() : playerRoot.GetComponent<PlayerMana>();
            PlayerStamina stamina = playerRoot == null ? Object.FindAnyObjectByType<PlayerStamina>() : playerRoot.GetComponent<PlayerStamina>();
            StatusEffectController statusController = playerRoot == null ? Object.FindAnyObjectByType<StatusEffectController>() : playerRoot.GetComponent<StatusEffectController>();
            PlayerQuestLog questLog = playerRoot == null ? Object.FindAnyObjectByType<PlayerQuestLog>() : playerRoot.GetComponent<PlayerQuestLog>();
            PlayerContractJournal contractJournal = playerRoot == null ? Object.FindAnyObjectByType<PlayerContractJournal>() : playerRoot.GetComponent<PlayerContractJournal>();
            PlayerIdentityProgression identityProgression = playerRoot == null ? Object.FindAnyObjectByType<PlayerIdentityProgression>() : playerRoot.GetComponent<PlayerIdentityProgression>();
            service.ConfigurePlayerPersistence(catalog, inventory, equipment, stats, health, mana, stamina, statusController, identityProgression, questLog, contractJournal);
        }
    }
}
