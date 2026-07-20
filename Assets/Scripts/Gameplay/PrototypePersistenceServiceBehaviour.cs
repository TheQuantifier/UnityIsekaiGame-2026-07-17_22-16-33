using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.UI.Inventory;
using UnityIsekaiGame.WorldEntities;

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
        [SerializeField] private CharacterAttributes playerAttributes;
        [SerializeField] private CalculatedStatCollection playerCalculatedStats;
        [SerializeField] private CharacterSkillCollection playerSkills;
        [SerializeField] private PlayerSkillActionEventSource playerSkillActionEventSource;
        [SerializeField] private StatusEffectController statusEffectController;
        [SerializeField] private PlayerIdentityProgression playerIdentityProgression;
        [SerializeField] private OverallLevelConfiguration overallLevelConfiguration;
        [SerializeField] private PlayerQuestLog playerQuestLog;
        [SerializeField] private PlayerContractJournal playerContractJournal;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private InventoryScreenController inventoryScreenController;
        [SerializeField] private CurrentPlaceTracker currentPlaceTracker;
        [SerializeField] private string sceneKey = "scene.prototype";
        [SerializeField] private string defaultSpawnPointId = "spawn.prototype.default";
        [SerializeField] private bool registerPlayerInventoryEquipment = true;
        [SerializeField] private bool registerPlayerIdentityProgression = true;
        [SerializeField] private bool registerPlayerAttributes = true;
        [SerializeField] private bool registerPlayerSkills = true;
        [SerializeField] private bool registerPlayerStatsVitalsStatus = true;
        [SerializeField] private bool registerPlayerQuestContract = true;
        [SerializeField] private bool registerPlayerLocation = true;
        [SerializeField] private string prototypeSlotId = PersistenceService.PrototypeSlotId;
        [Header("Save Slots")]
        [SerializeField, Min(1)] private int manualSlotCount = PrototypeSaveSlotCatalog.DefaultManualSlotCount;
        [SerializeField, Min(1)] private int autosaveSlotCount = PrototypeSaveSlotCatalog.DefaultAutosaveSlotCount;
        [SerializeField, Min(5f)] private float autosaveIntervalSeconds = 300f;
        [SerializeField] private PlayTimeTracker playTimeTracker;
        [SerializeField] private GameSaveDirtyTracker dirtyTracker;
        [SerializeField] private AutosaveCoordinator autosaveCoordinator;

        private PersistenceService service;
        private PrototypePersistenceStateParticipant participant;
        private PlayerIdentityProgressionPersistenceParticipant identityProgressionParticipant;
        private PlayerAttributesPersistenceParticipant playerAttributesParticipant;
        private PlayerSkillsPersistenceParticipant playerSkillsParticipant;
        private PlayerInventoryEquipmentPersistenceParticipant inventoryEquipmentParticipant;
        private PlayerStatsVitalsStatusPersistenceParticipant statsVitalsStatusParticipant;
        private PlayerQuestContractPersistenceParticipant questContractParticipant;
        private PlayerLocationPersistenceParticipant playerLocationParticipant;
        private DefinitionRegistry definitionRegistry;
        private bool dirtyEventsSubscribed;

        public PersistenceService Service => service;
        public PrototypePersistenceState PrototypeState => prototypeState;
        public string PrototypeSlotId => string.IsNullOrWhiteSpace(prototypeSlotId) ? PersistenceService.PrototypeSlotId : prototypeSlotId;
        public int ManualSlotCount => Mathf.Max(1, manualSlotCount);
        public int AutosaveSlotCount => Mathf.Max(1, autosaveSlotCount);
        public PlayTimeTracker PlayTime => playTimeTracker;
        public GameSaveDirtyTracker DirtyTracker => dirtyTracker;
        public AutosaveCoordinator Autosave => autosaveCoordinator;

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

            if (service != null && identityProgressionParticipant != null)
            {
                service.UnregisterParticipant(identityProgressionParticipant);
                identityProgressionParticipant = null;
            }

            if (service != null && playerAttributesParticipant != null)
            {
                service.UnregisterParticipant(playerAttributesParticipant);
                playerAttributesParticipant = null;
            }

            if (service != null && playerSkillsParticipant != null)
            {
                service.UnregisterParticipant(playerSkillsParticipant);
                playerSkillsParticipant = null;
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

            if (service != null && playerLocationParticipant != null)
            {
                service.UnregisterParticipant(playerLocationParticipant);
                playerLocationParticipant = null;
            }

            UnsubscribeDirtyEvents();
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
            PlayerIdentityProgression identityProgression,
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
            playerIdentityProgression = identityProgression;
            playerQuestLog = questLog;
            playerContractJournal = contractJournal;
            playerRoot = inventory == null ? playerRoot : inventory.transform;
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

            EnsureRuntimeHelpers();
            service ??= new PersistenceService();
            service.PlaytimeSecondsProvider = () => playTimeTracker == null ? 0d : playTimeTracker.CumulativeSeconds;
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

            EnsurePlayerIdentityProgressionParticipant();
            EnsurePlayerAttributesParticipant();
            EnsurePlayerSkillsParticipant();
            EnsurePlayerInventoryEquipmentParticipant();
            EnsurePlayerStatsVitalsStatusParticipant();
            EnsurePlayerQuestContractParticipant();
            EnsurePlayerLocationParticipant();
            SubscribeDirtyEvents();
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

        public IReadOnlyList<SaveSlotDescriptor> BuildSaveSlotDescriptors()
        {
            EnsureInitialized();
            return PrototypeSaveSlotCatalog.BuildDescriptors(service, ManualSlotCount, AutosaveSlotCount);
        }

        public SaveEligibilityResult CheckSaveEligibility(bool showDetailedPlayerMessage)
        {
            EnsureInitialized();
            if (service.OperationInProgress)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.OperationInProgress, "A persistence operation is already running.");
            }

            ResolvePlayerPersistenceReferences();
            if (playerRoot == null)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.NoActivePlayer, "No active player root is available.");
            }

            if (playerHealth != null && playerHealth.IsDefeated)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.InvalidPlayerState, "Cannot save while the player is defeated.");
            }

            return SaveEligibilityResult.Allow(showDetailedPlayerMessage ? "Saving is available." : "Allowed");
        }

        public PersistenceSaveResult SaveManualSlot(int zeroBasedIndex)
        {
            string slotId = PrototypeSaveSlotCatalog.ManualSlotId(zeroBasedIndex);
            return SaveNamedSlot(slotId, PrototypeSaveSlotCatalog.ManualDisplayName(zeroBasedIndex), markClean: true);
        }

        public PersistenceSaveResult SaveNamedSlot(string slotId, string displayName, bool markClean)
        {
            EnsureInitialized();
            SaveEligibilityResult eligibility = CheckSaveEligibility(showDetailedPlayerMessage: true);
            if (!eligibility.Allowed)
            {
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.ParticipantCaptureFailed, slotId, string.Empty, eligibility.Message);
            }

            PersistenceSaveResult result = service.Save(slotId, displayName);
            Report(result.Succeeded, result.Message);
            if (result.Succeeded && markClean)
            {
                dirtyTracker?.MarkClean($"Saved {displayName}.");
                autosaveCoordinator?.ResetTimer();
            }

            return result;
        }

        public PersistenceSaveResult SaveAutosave(string reason)
        {
            EnsureInitialized();
            string staging = PrototypeSaveSlotCatalog.AutosaveStagingSlotId;
            PersistenceSaveResult saveResult = SaveNamedSlot(staging, $"Autosave ({reason})", markClean: false);
            if (!saveResult.Succeeded)
            {
                return saveResult;
            }

            PersistenceSaveResult rotate = service.RotateAutosaveSlots(staging, PrototypeSaveSlotCatalog.BuildAutosaveSlotIds(AutosaveSlotCount));
            Report(rotate.Succeeded, rotate.Message);
            if (rotate.Succeeded)
            {
                dirtyTracker?.MarkClean($"Autosaved: {reason}.");
            }

            return rotate;
        }

        public PersistenceSaveResult ForceAutosave(string reason = "DevelopmentCommand")
        {
            EnsureInitialized();
            return autosaveCoordinator == null ? SaveAutosave(reason) : autosaveCoordinator.ForceAutosave(reason);
        }

        public PersistenceLoadResult LoadSaveSlot(string slotId, bool loadBackup = false)
        {
            EnsureInitialized();
            PersistenceValidationResult preValidation = service.ValidateSlot(slotId, loadBackup);
            PersistenceLoadResult result = service.Load(slotId, loadBackup);
            Report(result.Succeeded, result.Message);
            if (result.Succeeded)
            {
                playTimeTracker?.Restore(preValidation.Envelope == null ? 0d : preValidation.Envelope.playtimeSeconds);
                dirtyTracker?.MarkClean(loadBackup ? "Loaded backup save." : "Loaded save.");
                autosaveCoordinator?.ResetTimer();
            }

            return result;
        }

        public PersistenceValidationResult ValidateSaveSlot(string slotId, bool validateBackup = false)
        {
            EnsureInitialized();
            PersistenceValidationResult result = service.ValidateSlot(slotId, validateBackup);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult DeleteSaveSlot(string slotId)
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.DeleteSlot(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public void SetAutosaveIntervalForTesting(float seconds)
        {
            autosaveIntervalSeconds = Mathf.Max(5f, seconds);
            autosaveCoordinator?.SetIntervalForTesting(autosaveIntervalSeconds);
        }

        public string BuildSaveSlotDiagnosticSummary()
        {
            EnsureInitialized();
            PersistenceTransactionDiagnostics diagnostics = service.BuildTransactionDiagnostics();
            return $"Operation={service.OperationState} Phase={diagnostics.phase} Safety={diagnostics.runtimeSafety} Dirty={dirtyTracker != null && dirtyTracker.IsDirty} PlayTime={PrototypeSaveSlotCatalog.FormatPlayTime(playTimeTracker == null ? 0d : playTimeTracker.CumulativeSeconds)} Autosave={autosaveCoordinator?.LastResult ?? "None"}";
        }

        public string BuildPersistenceIntegrationDiagnosticSummary()
        {
            EnsureInitialized();
            PersistenceDependencyReport dependencies = service.BuildParticipantDependencyReport();
            PersistenceTransactionDiagnostics diagnostics = service.BuildTransactionDiagnostics();
            string order = dependencies.orderedParticipantKeys == null || dependencies.orderedParticipantKeys.Length == 0
                ? "None"
                : string.Join(" -> ", dependencies.orderedParticipantKeys);
            return string.Join("\n", new[]
            {
                "Persistence Integration",
                $"Transaction: {diagnostics.transactionId}",
                $"Phase: {diagnostics.phase}",
                $"Operation: {diagnostics.operationState}",
                $"Safety: {diagnostics.runtimeSafety}",
                $"Guard Active: {PersistenceRestorationGuard.IsActive}",
                $"Participant Dependencies: {(dependencies.succeeded ? "Valid" : "Invalid")}",
                $"Participant Order: {order}",
                $"Dependency Detail: {dependencies.message}",
                $"Fingerprint: {BuildRuntimeStateFingerprint()}",
                $"Last Audit: {diagnostics.lastConsistencyAudit}",
                $"Last Recovery: {diagnostics.lastRecoveryRecommendation}"
            });
        }

        public string BuildRuntimeStateFingerprint()
        {
            EnsureInitialized();
            return service.BuildRuntimeStateFingerprint();
        }

        public SaveRecoveryScanReport RunRecoveryScan()
        {
            EnsureInitialized();
            SaveRecoveryScanReport report = service.ScanRecoverySources();
            Report(true, report.recommendation);
            return report;
        }

        public PersistenceSaveResult PromoteBackup(string slotId)
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.PromoteBackup(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceSaveResult QuarantinePrimary(string slotId)
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.QuarantinePrimary(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult CleanupStaleTemporaryFiles()
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.CleanupStaleTemporaryFiles();
            Report(result.Succeeded, result.Message);
            return result;
        }

        public void InjectNextPersistenceFault(PersistenceFaultInjectionPoint point)
        {
            EnsureInitialized();
            service.FaultInjection.nextFailurePoint = point;
            service.FaultInjection.message = $"Injected {point} fault.";
            Report(true, $"Next persistence fault: {point}");
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

        private void EnsurePlayerIdentityProgressionParticipant()
        {
            if (!registerPlayerIdentityProgression || identityProgressionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Player identity/progression persistence participant was not registered because the prototype player identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player identity/progression persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            DefinitionRegistry registry = GetDefinitionRegistry();
            playerIdentityProgression.ConfigureIdentity(service.AccountId, service.PlayerId);
            playerIdentityProgression.RegisterDefinitionCache(registry);

            identityProgressionParticipant = new PlayerIdentityProgressionPersistenceParticipant(
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId,
                service.AccountId);

            service.RegisterParticipant(identityProgressionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                identityProgressionParticipant = null;
            }
        }

        private void EnsureRuntimeHelpers()
        {
            if (playTimeTracker == null)
            {
                playTimeTracker = GetComponent<PlayTimeTracker>();
                if (playTimeTracker == null)
                {
                    playTimeTracker = gameObject.AddComponent<PlayTimeTracker>();
                }
            }

            if (dirtyTracker == null)
            {
                dirtyTracker = GetComponent<GameSaveDirtyTracker>();
                if (dirtyTracker == null)
                {
                    dirtyTracker = gameObject.AddComponent<GameSaveDirtyTracker>();
                }
            }

            if (autosaveCoordinator == null)
            {
                autosaveCoordinator = GetComponent<AutosaveCoordinator>();
                if (autosaveCoordinator == null)
                {
                    autosaveCoordinator = gameObject.AddComponent<AutosaveCoordinator>();
                }
            }

            autosaveCoordinator.Configure(this, autosaveIntervalSeconds);
        }

        private void EnsurePlayerAttributesParticipant()
        {
            if (!registerPlayerAttributes || playerAttributesParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerAttributes == null || playerIdentityProgression == null)
            {
                Debug.LogWarning("Player attributes persistence participant was not registered because the prototype player attributes or identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player attributes persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerAttributes.Configure(GetDefinitionRegistry());
            playerAttributesParticipant = new PlayerAttributesPersistenceParticipant(
                playerAttributes,
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerAttributesParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerAttributesParticipant = null;
            }
        }

        private void EnsurePlayerSkillsParticipant()
        {
            if (!registerPlayerSkills || playerSkillsParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerSkills == null || playerIdentityProgression == null)
            {
                Debug.LogWarning("Player Skills persistence participant was not registered because the prototype player Skill collection or identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player Skills persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerSkills.Configure(GetDefinitionRegistry(), playerCalculatedStats, playerRoot == null ? null : playerRoot.GetComponent<PlayerSpellLoadout>());
            playerSkillsParticipant = new PlayerSkillsPersistenceParticipant(
                playerSkills,
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerSkillsParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerSkillsParticipant = null;
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

            GameObject playerObject = playerInventory == null ? playerEquipment == null ? null : playerEquipment.gameObject : playerInventory.gameObject;
            if (playerObject == null && playerStats != null)
            {
                playerObject = playerStats.gameObject;
            }

            if (playerStats == null)
            {
                playerStats = playerObject == null ? Object.FindAnyObjectByType<PlayerStats>() : playerObject.GetComponent<PlayerStats>();
            }

            if (playerObject == null && playerStats != null)
            {
                playerObject = playerStats.gameObject;
            }

            if (playerAttributes == null)
            {
                playerAttributes = playerObject == null ? Object.FindAnyObjectByType<CharacterAttributes>() : playerObject.GetComponent<CharacterAttributes>();
            }

            if (playerAttributes == null && playerObject != null)
            {
                playerAttributes = playerObject.AddComponent<CharacterAttributes>();
            }

            if (playerCalculatedStats == null)
            {
                playerCalculatedStats = playerObject == null ? Object.FindAnyObjectByType<CalculatedStatCollection>() : playerObject.GetComponent<CalculatedStatCollection>();
            }

            if (playerCalculatedStats == null && playerObject != null)
            {
                playerCalculatedStats = playerObject.AddComponent<CalculatedStatCollection>();
            }

            if (playerSkills == null)
            {
                playerSkills = playerObject == null ? Object.FindAnyObjectByType<CharacterSkillCollection>() : playerObject.GetComponent<CharacterSkillCollection>();
            }

            if (playerSkills == null && playerObject != null)
            {
                playerSkills = playerObject.AddComponent<CharacterSkillCollection>();
            }

            if (definitionCatalog != null)
            {
                DefinitionRegistry registry = GetDefinitionRegistry();
                playerAttributes?.Configure(registry);
                playerCalculatedStats?.Configure(registry, playerAttributes);
                playerSkills?.Configure(registry, playerCalculatedStats, playerObject == null ? null : playerObject.GetComponent<PlayerSpellLoadout>());
                playerStats?.ConfigureDerivedStats(registry);
                playerStats?.RefreshEquipmentModifiers();
            }

            if (playerHealth == null)
            {
                playerHealth = playerObject == null ? Object.FindAnyObjectByType<PlayerHealth>() : playerObject.GetComponent<PlayerHealth>();
            }

            if (playerMana == null)
            {
                playerMana = playerObject == null ? Object.FindAnyObjectByType<PlayerMana>() : playerObject.GetComponent<PlayerMana>();
            }

            if (playerStamina == null)
            {
                playerStamina = playerObject == null ? Object.FindAnyObjectByType<PlayerStamina>() : playerObject.GetComponent<PlayerStamina>();
            }

            if (statusEffectController == null)
            {
                statusEffectController = playerObject == null ? Object.FindAnyObjectByType<StatusEffectController>() : playerObject.GetComponent<StatusEffectController>();
            }

            if (playerQuestLog == null)
            {
                playerQuestLog = playerObject == null ? Object.FindAnyObjectByType<PlayerQuestLog>() : playerObject.GetComponent<PlayerQuestLog>();
            }

            if (playerContractJournal == null)
            {
                playerContractJournal = playerObject == null ? Object.FindAnyObjectByType<PlayerContractJournal>() : playerObject.GetComponent<PlayerContractJournal>();
            }

            if (playerIdentityProgression == null)
            {
                playerIdentityProgression = playerObject == null ? Object.FindAnyObjectByType<PlayerIdentityProgression>() : playerObject.GetComponent<PlayerIdentityProgression>();
            }

            if (playerIdentityProgression == null && playerObject != null)
            {
                playerIdentityProgression = playerObject.AddComponent<PlayerIdentityProgression>();
            }

            if (playerRoot == null)
            {
                playerRoot = playerObject == null ? null : playerObject.transform;
            }

            if (playerIdentityProgression != null)
            {
                WorldEntityIdentity worldEntityIdentity = playerRoot == null ? null : playerRoot.GetComponent<WorldEntityIdentity>();
                playerIdentityProgression.ConfigureRuntimeReferences(playerStats, worldEntityIdentity, playTimeTracker, overallLevelConfiguration);
                if (definitionCatalog != null)
                {
                    playerIdentityProgression.RegisterDefinitionCache(GetDefinitionRegistry());
                }
            }

            if (playerSkillActionEventSource == null)
            {
                playerSkillActionEventSource = playerObject == null ? Object.FindAnyObjectByType<PlayerSkillActionEventSource>() : playerObject.GetComponent<PlayerSkillActionEventSource>();
            }

            if (playerSkillActionEventSource == null && playerObject != null)
            {
                playerSkillActionEventSource = playerObject.AddComponent<PlayerSkillActionEventSource>();
            }

            if (playerSkillActionEventSource != null && playerObject != null)
            {
                playerSkillActionEventSource.Configure(
                    playerSkills,
                    playerIdentityProgression,
                    playerObject.GetComponent<PlayerMeleeCombat>(),
                    playerObject.GetComponent<PlayerSpellcaster>(),
                    playerEquipment,
                    playTimeTracker);
            }

            if (playerInput == null)
            {
                playerInput = playerRoot == null ? Object.FindAnyObjectByType<PlayerInputReader>() : playerRoot.GetComponentInChildren<PlayerInputReader>();
            }

            if (inventoryScreenController == null)
            {
                inventoryScreenController = Object.FindAnyObjectByType<InventoryScreenController>();
            }

            if (currentPlaceTracker == null && playerRoot != null)
            {
                currentPlaceTracker = playerRoot.GetComponent<CurrentPlaceTracker>();
                if (currentPlaceTracker == null)
                {
                    currentPlaceTracker = playerRoot.gameObject.AddComponent<CurrentPlaceTracker>();
                }
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

        private void EnsurePlayerLocationParticipant()
        {
            if (!registerPlayerLocation || playerLocationParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerRoot == null)
            {
                Debug.LogWarning("Player location persistence participant was not registered because the prototype player root is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player location persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerLocationParticipant = new PlayerLocationPersistenceParticipant(
                playerRoot,
                GetDefinitionRegistry,
                service.PlayerId,
                ResolveSceneKey(),
                defaultSpawnPointId,
                playerInput,
                inventoryScreenController,
                currentPlaceTracker);

            playerLocationParticipant.LocationFallbackUsed += OnLocationFallbackUsed;
            service.RegisterParticipant(playerLocationParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerLocationParticipant.LocationFallbackUsed -= OnLocationFallbackUsed;
                playerLocationParticipant = null;
            }
        }

        public string BuildPlayerLocationDiagnosticSummary()
        {
            EnsureInitialized();
            return playerLocationParticipant == null
                ? $"Scene: {ResolveSceneKey()}\nPlayer location participant: not registered"
                : playerLocationParticipant.BuildDiagnosticSummary();
        }

        private void OnLocationFallbackUsed(LocationFallbackEventArgs args)
        {
            string message = args == null ? "Player location fallback was used." : args.Message;
            Debug.LogWarning(message);
            PrototypeHudMessageBus.Show(message);
        }

        private void SubscribeDirtyEvents()
        {
            if (dirtyEventsSubscribed)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += OnMeaningfulRuntimeStateChanged;
            }

            if (playerEquipment != null)
            {
                playerEquipment.EquipmentChanged += OnMeaningfulRuntimeStateChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged += OnVitalsChanged;
            }

            if (playerMana != null)
            {
                playerMana.ManaChanged += OnResourceChanged;
            }

            if (playerStamina != null)
            {
                playerStamina.StaminaChanged += OnResourceChanged;
            }

            if (statusEffectController != null)
            {
                statusEffectController.StatusAdded += OnStatusChanged;
                statusEffectController.StatusChanged += OnStatusChanged;
                statusEffectController.StatusRemoved += OnStatusChanged;
                statusEffectController.StatusExpired += OnStatusChanged;
            }

            if (playerQuestLog != null)
            {
                playerQuestLog.QuestLogChanged += OnQuestContractChanged;
            }

            if (playerContractJournal != null)
            {
                playerContractJournal.JournalChanged += OnQuestContractChanged;
            }

            if (currentPlaceTracker != null)
            {
                currentPlaceTracker.CurrentPlaceChanged += OnPlaceChanged;
            }

            if (playerIdentityProgression != null)
            {
                playerIdentityProgression.ProgressionChanged += OnIdentityProgressionChanged;
            }

            if (playerAttributes != null)
            {
                playerAttributes.AttributesChanged += OnAttributesChanged;
            }

            if (playerCalculatedStats != null)
            {
                playerCalculatedStats.CalculatedStatsChanged += OnCalculatedStatsChanged;
            }

            if (playerSkills != null)
            {
                playerSkills.SkillsChanged += OnSkillsChanged;
                playerSkills.HiddenProgressChanged += OnSkillHiddenProgressChanged;
            }

            dirtyEventsSubscribed = true;
        }

        private void UnsubscribeDirtyEvents()
        {
            if (!dirtyEventsSubscribed)
            {
                return;
            }

            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= OnMeaningfulRuntimeStateChanged;
            }

            if (playerEquipment != null)
            {
                playerEquipment.EquipmentChanged -= OnMeaningfulRuntimeStateChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnVitalsChanged;
            }

            if (playerMana != null)
            {
                playerMana.ManaChanged -= OnResourceChanged;
            }

            if (playerStamina != null)
            {
                playerStamina.StaminaChanged -= OnResourceChanged;
            }

            if (statusEffectController != null)
            {
                statusEffectController.StatusAdded -= OnStatusChanged;
                statusEffectController.StatusChanged -= OnStatusChanged;
                statusEffectController.StatusRemoved -= OnStatusChanged;
                statusEffectController.StatusExpired -= OnStatusChanged;
            }

            if (playerIdentityProgression != null)
            {
                playerIdentityProgression.ProgressionChanged -= OnIdentityProgressionChanged;
            }

            if (playerAttributes != null)
            {
                playerAttributes.AttributesChanged -= OnAttributesChanged;
            }

            if (playerCalculatedStats != null)
            {
                playerCalculatedStats.CalculatedStatsChanged -= OnCalculatedStatsChanged;
            }

            if (playerSkills != null)
            {
                playerSkills.SkillsChanged -= OnSkillsChanged;
                playerSkills.HiddenProgressChanged -= OnSkillHiddenProgressChanged;
            }

            if (playerQuestLog != null)
            {
                playerQuestLog.QuestLogChanged -= OnQuestContractChanged;
            }

            if (playerContractJournal != null)
            {
                playerContractJournal.JournalChanged -= OnQuestContractChanged;
            }

            if (currentPlaceTracker != null)
            {
                currentPlaceTracker.CurrentPlaceChanged -= OnPlaceChanged;
            }

            dirtyEventsSubscribed = false;
        }

        private void OnMeaningfulRuntimeStateChanged()
        {
            dirtyTracker?.MarkDirty("Player state changed.");
        }

        private void OnQuestContractChanged()
        {
            dirtyTracker?.MarkDirty("Quest or contract state changed.");
            autosaveCoordinator?.RequestAutosave("Progression");
        }

        private void OnVitalsChanged(int current, int maximum)
        {
            dirtyTracker?.MarkDirty("Player vitals changed.");
        }

        private void OnResourceChanged(float current, float maximum)
        {
            dirtyTracker?.MarkDirty("Player resource changed.");
        }

        private void OnStatusChanged(RuntimeStatusEffect status)
        {
            dirtyTracker?.MarkDirty("Status effect state changed.");
        }

        private void OnPlaceChanged(PlaceDefinition place, bool entered)
        {
            dirtyTracker?.MarkDirty("Player location changed.");
        }

        private void OnIdentityProgressionChanged(PlayerIdentityProgression progression, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player identity/progression changed.");
        }

        private void OnAttributesChanged(CharacterAttributes attributes, IReadOnlyList<string> attributeIds, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player attributes changed.");
        }

        private void OnCalculatedStatsChanged(CalculatedStatCollection stats, IReadOnlyList<string> statIds, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player calculated stats changed.");
        }

        private void OnSkillsChanged(CharacterSkillCollection skills, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player Skills changed.");
        }

        private void OnSkillHiddenProgressChanged(CharacterSkillCollection skills, SkillLearningProgressRecord progress, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player hidden Skill learning progress changed.");
        }

        private string ResolveSceneKey()
        {
            if (!string.IsNullOrWhiteSpace(sceneKey))
            {
                return sceneKey;
            }

            SceneKeyIdentity identity = Object.FindAnyObjectByType<SceneKeyIdentity>();
            return identity == null || string.IsNullOrWhiteSpace(identity.SceneKey) ? "scene.prototype" : identity.SceneKey;
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
