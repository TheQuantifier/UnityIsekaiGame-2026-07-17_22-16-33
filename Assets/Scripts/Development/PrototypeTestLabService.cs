#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.WorldEntities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabService
    {
        public const int DefaultHistoryLimit = 40;
        private const string PrototypeCatalogPath = "Assets/GameData/Prototype/PrototypeDefinitionCatalog.asset";
        private const string DevelopmentStatusSource = "development.prototype-test-lab";

        private readonly List<PrototypeTestLabOperation> history = new List<PrototypeTestLabOperation>();
        private readonly HashSet<string> pendingConfirmations = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<Type, List<IGameDefinition>> selectorCache = new Dictionary<Type, List<IGameDefinition>>();
        private PrototypeTestLabContext context;
        private DefinitionRegistry registry;
        private int historyLimit = DefaultHistoryLimit;
        private string lastSpawnedWorldEntityId;
        private ItemDefinition lastSpawnedWorldEntityItem;
        private string lastDestroyedWorldEntityId;
        private ItemDefinition lastDestroyedWorldEntityItem;
        private string lastWorldEntityOperationMessage;

        public event Action HistoryChanged;

        public IReadOnlyList<PrototypeTestLabOperation> History => history;
        public DefinitionRegistry Registry => registry;
        public string CurrentSlotId => context?.Persistence == null ? PersistenceService.PrototypeSlotId : context.Persistence.PrototypeSlotId;

        public void Configure(PrototypeTestLabContext newContext)
        {
            context = newContext;
            registry = CreateRegistry(context?.DefinitionCatalog);
            selectorCache.Clear();
        }

        public IReadOnlyList<TDefinition> GetDefinitions<TDefinition>()
            where TDefinition : class, IGameDefinition
        {
            Type type = typeof(TDefinition);
            if (!selectorCache.TryGetValue(type, out List<IGameDefinition> cached))
            {
                cached = registry == null
                    ? new List<IGameDefinition>()
                    : registry.DefinitionsById.Values
                        .Where(definition => definition is TDefinition)
                        .OrderBy(definition => definition.DisplayName)
                        .ThenBy(definition => definition.Id)
                        .ToList();
                selectorCache.Add(type, cached);
            }

            return cached.Cast<TDefinition>().ToList();
        }

        public IReadOnlyList<PrototypeTestPoint> GetTestPoints()
        {
            return UnityEngine.Object.FindObjectsByType<PrototypeTestPoint>(FindObjectsInactive.Exclude)
                .Where(point => point != null && !string.IsNullOrWhiteSpace(point.TestPointId))
                .OrderBy(point => point.TestPointId)
                .ThenBy(point => point.DisplayName)
                .ToList();
        }

        public string BuildOverview()
        {
            if (context == null)
            {
                return "Test Lab context is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                "Prototype Systems Test Lab",
                $"Build Boundary: {(Application.isEditor ? "Editor" : "Development Build")}",
                $"Player: {(context.PlayerTransform == null ? "Missing" : context.PlayerTransform.name)}",
                $"Health: {FormatHealth()}",
                $"Stamina: {FormatResource(context.PlayerStamina == null ? 0f : context.PlayerStamina.CurrentStamina, context.PlayerStamina == null ? 0f : context.PlayerStamina.MaximumStamina)}",
                $"Mana: {FormatResource(context.PlayerMana == null ? 0f : context.PlayerMana.CurrentMana, context.PlayerMana == null ? 0f : context.PlayerMana.MaximumMana)}",
                $"Stats: ATK {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.AttackPower)}, DEF {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.Defense)}",
                $"Statuses: {FormatStatuses(context.PlayerStatuses)}",
                $"Inventory: {FormatInventory()}",
                $"Equipped: {CountEquipped()} item(s)",
                $"Selected Spell: {(context.SpellLoadout == null || context.SpellLoadout.SelectedSpell == null ? "None" : FormatDefinition(context.SpellLoadout.SelectedSpell))}",
                $"Quests: {(context.QuestLog == null ? 0 : context.QuestLog.Quests.Count)}",
                $"Contracts: {(context.ContractJournal == null ? 0 : context.ContractJournal.Contracts.Count)}",
                $"Enemy: {FormatEnemy()}",
                $"Location: {FormatLocationOneLine()}",
                $"Definitions: {(registry == null ? 0 : registry.Count)}",
                $"Persistence Slot: {CurrentSlotId}",
                $"Modal Active: {PrototypeGameplayModalState.IsModalActive}"
            });
        }

        public string BuildLocationSummary()
        {
            string details = context?.Persistence == null
                ? "Player location persistence is missing."
                : context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return string.Join(Environment.NewLine, new[]
            {
                "Player Location Persistence",
                details,
                "Policy: same-scene restore is supported; cross-scene saves validate clearly and are not restored yet.",
                "Reach Location objectives are suppressed during persistence restore."
            });
        }

        public string BuildWorldEntitySummary()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Persistent World Entities",
                WorldEntityRegistry.BuildDiagnosticReport(),
                $"Last Spawned: {(string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) ? "None" : lastSpawnedWorldEntityId)}",
                $"Last Destroyed: {(string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) ? "None" : lastDestroyedWorldEntityId)}",
                $"Last Result: {(string.IsNullOrWhiteSpace(lastWorldEntityOperationMessage) ? "None" : lastWorldEntityOperationMessage)}"
            });
        }

        public string BuildSaveSlotSummary()
        {
            if (context?.Persistence == null)
            {
                return "Save slot persistence is missing.";
            }

            context.Persistence.EnsureInitialized();
            List<string> lines = new List<string>
            {
                "Save Slots, Autosave, and Load UI",
                context.Persistence.BuildSaveSlotDiagnosticSummary()
            };

            IReadOnlyList<SaveSlotDescriptor> descriptors = context.Persistence.BuildSaveSlotDescriptors();
            for (int i = 0; i < descriptors.Count; i++)
            {
                SaveSlotDescriptor descriptor = descriptors[i];
                lines.Add($"{descriptor.displayName}: {descriptor.compatibilityStatus} | {PrototypeSaveSlotCatalog.FormatLocalTimestamp(descriptor.lastSavedAtUtc)} | {PrototypeSaveSlotCatalog.FormatPlayTime(descriptor.playTimeSeconds)} | Backup={descriptor.backupExists}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string BuildPersistenceIntegrationSummary()
        {
            return context?.Persistence == null
                ? "Persistence integration service is missing."
                : context.Persistence.BuildPersistenceIntegrationDiagnosticSummary();
        }

        public PrototypeTestLabOperation GrantItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Item", "No item definition selected.", "MissingDefinition");
            }

            InventoryAddResult result = context.Inventory.AddItemOrInstances(item, Mathf.Max(1, quantity));
            return Record(result.AddedQuantity > 0, "Grant Item", result.Status.ToString(), $"Requested {result.RequestedQuantity} x {item.DisplayName}; added {result.AddedQuantity}.");
        }

        public PrototypeTestLabOperation GrantStatefulItem(ItemDefinition item)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Stateful Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Stateful Item", "No item definition selected.", "MissingDefinition");
            }

            ItemInstanceCreationResult creation = ItemInstanceFactory.CreateStateful(item, ItemInstanceMetadata.WithoutInstanceState());
            if (!creation.Succeeded)
            {
                return RecordFailure("Grant Stateful Item", creation.Message, creation.Status.ToString());
            }

            InventoryInstanceOperationResult result = context.Inventory.AddItemInstance(creation.ItemInstance);
            return Record(result.Succeeded, "Grant Stateful Item", result.Succeeded ? "Added" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation RemoveItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Remove Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Remove Item", "No item definition selected.", "MissingDefinition");
            }

            bool removed = context.Inventory.RemoveItem(item, Mathf.Max(1, quantity));
            return Record(removed, "Remove Item", removed ? "Removed" : "NotFound", removed ? $"Removed {quantity} x {item.DisplayName}." : $"{item.DisplayName} quantity was not available.");
        }

        public PrototypeTestLabOperation FillInventory(ItemDefinition filler)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Fill Inventory", "Player inventory is missing.", "MissingInventory");
            }

            if (filler == null)
            {
                return RecordFailure("Fill Inventory", "No filler item selected.", "MissingDefinition");
            }

            int safety = context.Inventory.SlotCapacity * Mathf.Max(1, filler.MaximumStackSize);
            int added = 0;
            for (int i = 0; i < safety && context.Inventory.DevelopmentOccupiedSlotCount() < context.Inventory.SlotCapacity; i++)
            {
                InventoryAddResult result = context.Inventory.AddItemOrInstances(filler, 1);
                if (result.AddedQuantity <= 0)
                {
                    break;
                }

                added += result.AddedQuantity;
            }

            return Record(added > 0, "Fill Inventory", added > 0 ? "Filled" : "NoChange", $"Added {added} filler item(s); occupied slots {context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity}.");
        }

        public PrototypeTestLabOperation ClearInventory(bool confirmed)
        {
            if (!RequireConfirmation("ClearInventory", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.Inventory?.DevelopmentClearInventory();
            return RecordSuccess("Clear Inventory", "Inventory cleared. Equipment was preserved.");
        }

        public PrototypeTestLabOperation EquipFirstCompatible(ItemDefinition item)
        {
            if (context?.Inventory == null || context.Equipment == null)
            {
                return RecordFailure("Equip Item", "Inventory or equipment is missing.", "MissingReference");
            }

            for (int i = 0; i < context.Inventory.Slots.Count; i++)
            {
                InventorySlot slot = context.Inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty && slot.Item == item)
                {
                    EquipmentOperationResult result = context.Equipment.EquipFromInventorySlot(i);
                    return Record(result.Succeeded, "Equip Item", result.Succeeded ? "Equipped" : "Failed", result.Message);
                }
            }

            return RecordFailure("Equip Item", "Selected item was not found in inventory.", "NotFound");
        }

        public PrototypeTestLabOperation UnequipAll(bool confirmed)
        {
            if (!RequireConfirmation("UnequipAll", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (context?.Equipment == null)
            {
                return RecordFailure("Unequip All", "Equipment is missing.", "MissingEquipment");
            }

            int changed = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty && context.Equipment.Unequip(slot.SlotType).Succeeded)
                {
                    changed++;
                }
            }

            return RecordSuccess("Unequip All", $"Unequipped {changed} slot(s).");
        }

        public PrototypeTestLabOperation DamagePlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Damage Player", "Player health is missing.", "MissingHealth");
            }

            int applied = context.PlayerHealth.Damage(Mathf.Max(0, amount));
            return RecordSuccess("Damage Player", $"Applied raw test damage {applied}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation HealPlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Heal Player", "Player health is missing.", "MissingHealth");
            }

            int healed = context.PlayerHealth.Heal(Mathf.Max(0, amount));
            return RecordSuccess("Heal Player", $"Healed {healed}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation SetHealth(int value)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Set Health", "Player health is missing.", "MissingHealth");
            }

            bool restored = context.PlayerHealth.TryRestoreForPersistence(Mathf.Clamp(value, 1, context.PlayerHealth.MaximumHealth), out string failureReason);
            return Record(restored, "Set Health", restored ? "Clamped" : "Failed", restored ? $"Health set to {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}." : failureReason);
        }

        public PrototypeTestLabOperation RestoreVitals()
        {
            context?.PlayerHealth?.ResetToMaximum();
            context?.PlayerMana?.RestoreToMaximum();
            context?.PlayerStamina?.RestoreToMaximum();
            return RecordSuccess("Restore Vitals", "Health, mana, and stamina restored to maximum.");
        }

        public PrototypeTestLabOperation DrainMana(float amount)
        {
            VitalChangeResult result = context?.PlayerMana == null
                ? VitalChangeResult.Failure(amount, "Player mana is missing.")
                : context.PlayerMana.Spend(Mathf.Max(0f, amount));
            return Record(result.Succeeded, "Drain Mana", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation DrainStamina(float amount)
        {
            VitalChangeResult result = context?.PlayerStamina == null
                ? VitalChangeResult.Failure(amount, "Player stamina is missing.")
                : context.PlayerStamina.Spend(Mathf.Max(0f, amount), "Development test");
            return Record(result.Succeeded, "Drain Stamina", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ApplyStatus(StatusEffectDefinition status, bool toEnemy)
        {
            StatusEffectController controller = toEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null)
            {
                return RecordFailure("Apply Status", "Target status controller is missing.", "MissingTarget");
            }

            if (status == null)
            {
                return RecordFailure("Apply Status", "No status definition selected.", "MissingDefinition");
            }

            StatusEffectApplicationRequest request = new StatusEffectApplicationRequest(status, null, DevelopmentStatusSource, 0f, string.Empty, Time.time);
            StatusApplicationResult result = controller.ApplyStatus(request);
            return Record(result.Succeeded, "Apply Status", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RemoveStatus(StatusEffectDefinition status, bool fromEnemy)
        {
            StatusEffectController controller = fromEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null || status == null)
            {
                return RecordFailure("Remove Status", "Target status controller or status definition is missing.", "MissingReference");
            }

            bool removed = controller.RemoveStatusesByDefinition(status.Id);
            return Record(removed, "Remove Status", removed ? "Removed" : "NotFound", removed ? $"Removed {status.DisplayName}." : $"{status.DisplayName} was not active.");
        }

        public PrototypeTestLabOperation ClearTemporaryStatuses()
        {
            context?.PlayerStatuses?.ClearTemporaryStatuses();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            return RecordSuccess("Clear Temporary Statuses", "Temporary player and enemy statuses cleared.");
        }

        public PrototypeTestLabOperation ApplyTypedDamage(DamageTypeDefinition damageType, float amount, bool targetEnemy, bool sourcePlayer)
        {
            if (damageType == null)
            {
                return RecordFailure("Apply Typed Damage", "No damage type selected.", "MissingDefinition");
            }

            IDamageable damageable = targetEnemy ? context?.EnemyHealth : context?.PlayerHealth;
            Transform targetTransform = targetEnemy ? context?.EnemyTransform : context?.PlayerTransform;
            GameObject source = sourcePlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (damageable == null || targetTransform == null)
            {
                return RecordFailure("Apply Typed Damage", "Damage target is missing.", "MissingTarget");
            }

            float rawAmount = Mathf.Max(0f, amount);
            DamageComponent component = new DamageComponent(damageType, rawAmount);
            DamagePacket packet = DamagePacket.Single(source, component);
            DamageInfo info = new DamageInfo(rawAmount, source, targetTransform.position, Vector3.forward, DamageType.Physical, packet);
            DamageResult result = damageable.ApplyDamage(in info);
            return Record(result.Applied, "Apply Typed Damage", result.Applied ? "Applied" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ResetEnemy()
        {
            context?.EnemyAttack?.ResetCooldown();
            context?.EnemyController?.ResetControllerState();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            context?.EnemyHealth?.ResetToMaximum();
            return RecordSuccess("Reset Enemy", "Enemy health, cooldown, controller state, and temporary statuses reset.");
        }

        public PrototypeTestLabOperation DefeatEnemy(DamageTypeDefinition damageType)
        {
            float amount = context?.EnemyHealth == null ? 9999f : context.EnemyHealth.MaximumHealth + 9999f;
            return ApplyTypedDamage(damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault(), amount, targetEnemy: true, sourcePlayer: true);
        }

        public PrototypeTestLabOperation StartQuest(QuestDefinition quest)
        {
            if (context?.QuestLog == null || quest == null)
            {
                return RecordFailure("Start Quest", "Quest log or quest definition is missing.", "MissingReference");
            }

            QuestOperationResult result = context.QuestLog.StartQuest(quest);
            return Record(result.Succeeded, "Start Quest", result.Succeeded ? "Started" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ReportTalk(PersonDefinition person)
        {
            if (person == null)
            {
                return RecordFailure("Report Talk", "No person definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportTalk(person.Id);
            return RecordSuccess("Report Talk", $"Reported talk with {FormatDefinition(person)}.");
        }

        public PrototypeTestLabOperation ReportReach(PlaceDefinition place)
        {
            if (place == null)
            {
                return RecordFailure("Report Reach Location", "No place definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportReachLocation(place);
            return RecordSuccess("Report Reach Location", $"Reported reach location {FormatDefinition(place)}.");
        }

        public PrototypeTestLabOperation ReportDefeat(string targetCategory)
        {
            if (string.IsNullOrWhiteSpace(targetCategory))
            {
                targetCategory = "prototype_enemy";
            }

            GameObject temporary = new GameObject("Development Contract Objective Target");
            try
            {
                ContractObjectiveTarget target = temporary.AddComponent<ContractObjectiveTarget>();
                target.DevelopmentSetTargetCategory(targetCategory);
                context?.QuestLog?.RecordDefeat(target);
                context?.ContractJournal?.RecordDefeat(target);
                return RecordSuccess("Report Defeat", $"Reported defeat target '{targetCategory}'.");
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(temporary);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }
        }

        public PrototypeTestLabOperation ClearQuestLog(bool confirmed)
        {
            if (!RequireConfirmation("ClearQuestLog", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.QuestLog?.DevelopmentClearQuestLog();
            return RecordSuccess("Clear Quest Log", "Quest log cleared.");
        }

        public PrototypeTestLabOperation AcceptContract(ContractDefinition contract)
        {
            if (context?.ContractJournal == null || contract == null)
            {
                return RecordFailure("Accept Contract", "Contract journal or contract definition is missing.", "MissingReference");
            }

            ContractOperationResult result = context.ContractJournal.AcceptContract(contract);
            return Record(result.Succeeded, "Accept Contract", result.Succeeded ? "Accepted" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ClearContractJournal(bool confirmed)
        {
            if (!RequireConfirmation("ClearContractJournal", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.ContractJournal?.DevelopmentClearContractJournal();
            return RecordSuccess("Clear Contract Journal", "Contract journal cleared.");
        }

        public PrototypeTestLabOperation Save()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SavePrototypeSlot();
            return Record(result.Succeeded, "Save Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation Load()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadPrototypeSlot();
            return Record(result.Succeeded, "Load Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateSave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidatePrototypeSlot();
            return Record(result.Succeeded, "Validate Prototype Slot", result.Status.ToString(), $"{result.Message} BackupAvailable={result.BackupAvailable}");
        }

        public PrototypeTestLabOperation DeleteSave(bool confirmed)
        {
            if (!RequireConfirmation("DeleteSave", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Delete Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.DeletePrototypeSlot();
            return Record(result.Succeeded, "Delete Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ForceAutosave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Force Autosave", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.ForceAutosave("TestLab");
            return Record(result.Succeeded, "Force Autosave", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation SetShortAutosaveInterval()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Set Autosave Interval", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.SetAutosaveIntervalForTesting(15f);
            return RecordSuccess("Set Autosave Interval", "Autosave interval set to 15 seconds for local testing.");
        }

        public PrototypeTestLabOperation MarkSaveDirty()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Dirty", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(true, "Test Lab marked save dirty.");
            return RecordSuccess("Mark Save Dirty", "Save dirty state set for confirmation and autosave testing.");
        }

        public PrototypeTestLabOperation MarkSaveClean()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Clean", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(false, "Test Lab marked save clean.");
            return RecordSuccess("Mark Save Clean", "Save dirty state cleared.");
        }

        public PrototypeTestLabOperation SaveManualSlotOne()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SaveManualSlot(0);
            return Record(result.Succeeded, "Save Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation LoadManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), loadBackup: true);
            return Record(result.Succeeded, "Load Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidateSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), validateBackup: true);
            return Record(result.Succeeded, "Validate Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RunRecoveryScan()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Run Recovery Scan", "Persistence service is missing.", "MissingPersistence");
            }

            SaveRecoveryScanReport report = persistence.RunRecoveryScan();
            return RecordSuccess("Run Recovery Scan", $"{report.candidates.Length} candidate(s). {report.recommendation}");
        }

        public PrototypeTestLabOperation PromoteManualSlotOneBackup(bool confirmed)
        {
            if (!RequireConfirmation("PromoteManualSlotOneBackup", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Promote Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.PromoteBackup(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Promote Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation QuarantineManualSlotOnePrimary(bool confirmed)
        {
            if (!RequireConfirmation("QuarantineManualSlotOnePrimary", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Quarantine Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.QuarantinePrimary(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Quarantine Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation CleanupTemporarySaves(bool confirmed)
        {
            if (!RequireConfirmation("CleanupTemporarySaves", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Cleanup Temporary Saves", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.CleanupStaleTemporaryFiles();
            return Record(result.Succeeded, "Cleanup Temporary Saves", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation InjectPrepareFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Prepare Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadPrepare);
            return RecordSuccess("Inject Prepare Failure", "Next load prepare phase will fail once.");
        }

        public PrototypeTestLabOperation InjectCommitFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Commit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadCommit);
            return RecordSuccess("Inject Commit Failure", "Next load commit phase will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation InjectAuditFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Audit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.ConsistencyAudit);
            return RecordSuccess("Inject Audit Failure", "Next consistency audit will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation RecordFingerprint()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Record Fingerprint", "Persistence service is missing.", "MissingPersistence");
            }

            return RecordSuccess("Record Fingerprint", persistence.BuildRuntimeStateFingerprint());
        }

        public PrototypeTestLabOperation Teleport(PrototypeTestPoint point)
        {
            if (context?.PlayerTransform == null || point == null)
            {
                return RecordFailure("Teleport", "Player transform or test point is missing.", "MissingReference");
            }

            CharacterController characterController = context.PlayerTransform.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            context.PlayerTransform.SetPositionAndRotation(point.transform.position, point.transform.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            return RecordSuccess("Teleport", $"Teleported to {point.DisplayName} ({point.TestPointId}).");
        }

        public PrototypeTestLabOperation ValidateCurrentLocation()
        {
            if (context?.Persistence == null)
            {
                return RecordFailure("Validate Current Location", "Persistence service is missing.", "MissingPersistence");
            }

            string summary = context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return RecordSuccess("Validate Current Location", summary.Replace(Environment.NewLine, " | "));
        }

        public PrototypeTestLabOperation RefreshWorldEntityDiagnostics()
        {
            return RecordSuccess("World Entity Diagnostics", $"Registered {WorldEntityRegistry.Count} world entity identity object(s).");
        }

        public PrototypeTestLabOperation SpawnPersistentWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Persistent World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Persistent Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.CreateRuntimeIdentity(pickup, "scene.prototype", PersistenceService.LocalWorldId, item.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Spawn Persistent World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = item;
            return RecordWorldEntityResult("Spawn Persistent World Loot", $"Spawned {item.DisplayName} as {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation SpawnTransientWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Transient World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.right * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = $"Transient Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntityIdentity identity = pickup.AddComponent<WorldEntityIdentity>();
            identity.TryMarkTransient(out _);
            return RecordWorldEntityResult("Spawn Transient World Loot", $"Spawned transient {item.DisplayName}; it is intentionally not persistently registered.");
        }

        public PrototypeTestLabOperation DestroyLastSpawnedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) || !WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out WorldEntityIdentity identity))
            {
                return RecordFailure("Destroy Spawned World Loot", "No spawned world entity is currently registered.", "MissingEntity");
            }

            WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
            lastDestroyedWorldEntityId = identity.EntityId;
            lastDestroyedWorldEntityItem = pickup == null ? null : pickup.Item;
            WorldEntityRegistry.Unregister(identity);
            DestroyTestObject(identity.gameObject);
            return RecordWorldEntityResult("Destroy Spawned World Loot", $"Destroyed {lastDestroyedWorldEntityId}.");
        }

        public PrototypeTestLabOperation RecreateDestroyedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) || lastDestroyedWorldEntityItem == null)
            {
                return RecordFailure("Recreate World Loot", "No destroyed persistent world loot is available to recreate.", "MissingSnapshot");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Restored Test Loot - {lastDestroyedWorldEntityItem.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(lastDestroyedWorldEntityItem, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(pickup, lastDestroyedWorldEntityId, "scene.prototype", PersistenceService.LocalWorldId, lastDestroyedWorldEntityItem.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Recreate World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = lastDestroyedWorldEntityItem;
            return RecordWorldEntityResult("Recreate World Loot", $"Recreated {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation AttemptDuplicateWorldEntityRegistration()
        {
            if (!TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity existingIdentity, out ItemDefinition item, out string failureReason))
            {
                return RecordWorldEntityFailure("Duplicate World Entity Proof", failureReason, "MissingEntity");
            }

            GameObject duplicate = new GameObject("Duplicate World Entity Proof");
            duplicate.name = "Duplicate World Entity Proof";
            duplicate.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(duplicate, lastSpawnedWorldEntityId, existingIdentity.SceneKey, existingIdentity.WorldId, item.Id);
            if (result.Succeeded)
            {
                WorldEntityRegistry.Unregister(result.Identity);
                DestroyTestObject(duplicate);
                return RecordWorldEntityFailure("Duplicate World Entity Proof", "Duplicate registration unexpectedly succeeded.", "UnexpectedSuccess");
            }

            DestroyTestObject(duplicate);
            return RecordWorldEntityResult("Duplicate World Entity Proof", $"Duplicate rejected: {result.Code}.");
        }

        private bool TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity identity, out ItemDefinition item, out string failureReason)
        {
            identity = null;
            item = null;
            failureReason = string.Empty;

            if (!string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                && WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out identity))
            {
                WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
                item = pickup == null ? lastSpawnedWorldEntityItem : pickup.Item;
                if (item != null)
                {
                    return true;
                }

                failureReason = "The spawned world entity has no item definition to duplicate.";
                return false;
            }

            foreach (WorldEntityIdentity candidate in WorldEntityRegistry.RegisteredEntities)
            {
                if (candidate == null
                    || candidate.IdentityKind == WorldEntityIdentityKind.Transient
                    || (!candidate.name.StartsWith("Persistent Test Loot", StringComparison.Ordinal)
                        && !candidate.name.StartsWith("Restored Test Loot", StringComparison.Ordinal)))
                {
                    continue;
                }

                WorldItemPickup pickup = candidate.GetComponent<WorldItemPickup>();
                if (pickup == null || pickup.Item == null)
                {
                    continue;
                }

                identity = candidate;
                item = pickup.Item;
                lastSpawnedWorldEntityId = identity.EntityId;
                lastSpawnedWorldEntityItem = item;
                return true;
            }

            failureReason = string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                ? "Spawn persistent world loot first."
                : $"World entity '{lastSpawnedWorldEntityId}' is no longer registered. Spawn persistent world loot again.";
            return false;
        }

        private PrototypeTestLabOperation RecordWorldEntityResult(string operationName, string message)
        {
            lastWorldEntityOperationMessage = message;
            Debug.Log($"{operationName}: {message}");
            return RecordSuccess(operationName, message);
        }

        private PrototypeTestLabOperation RecordWorldEntityFailure(string operationName, string message, string code)
        {
            lastWorldEntityOperationMessage = message;
            return RecordFailure(operationName, message, code);
        }

        private static void DestroyTestObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        public PrototypeTestLabOperation RunScenario(string scenarioId, ItemDefinition item, QuestDefinition quest, ContractDefinition contract, DamageTypeDefinition damageType)
        {
            switch (scenarioId)
            {
                case "clean":
                    context?.TestController?.ResetPrototypeState();
                    return RecordSuccess("Scenario: Clean Baseline", "Prototype reset executed; persistent player collections preserved.");
                case "combat":
                    context?.TestController?.ResetPrototypeState();
                    if (item != null)
                    {
                        GrantStatefulItem(item);
                        EquipFirstCompatible(item);
                    }

                    return RecordSuccess("Scenario: Combat Ready", "Reset vitals/enemy and attempted to grant/equip selected item.");
                case "full-inventory":
                    return FillInventory(item);
                case "quest":
                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    return RecordSuccess("Scenario: Quest Midpoint", "Started selected quest. Use Talk/Reach/Defeat actions to progress through normal events.");
                case "contract":
                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Contract Testing", "Accepted selected contract if available.");
                case "persistence":
                    RestoreVitals();
                    if (item != null)
                    {
                        GrantItem(item, 2);
                    }

                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Persistence Round Trip", "Prepared representative player state for save/load testing.");
                default:
                    return RecordFailure("Scenario", $"Unknown scenario '{scenarioId}'.", "UnknownScenario");
            }
        }

        public string RunDiagnostics()
        {
            List<string> lines = new List<string>
            {
                "Diagnostics",
                $"Definitions loaded: {(registry == null ? 0 : registry.Count)}"
            };

            AddDuplicateInstanceDiagnostics(lines);
            AddDuplicateStatusDiagnostics(lines, "Player", context?.PlayerStatuses);
            AddDuplicateStatusDiagnostics(lines, "Enemy", context?.EnemyStatuses);
            AddReferenceDiagnostic(lines, "Inventory", context?.Inventory);
            AddReferenceDiagnostic(lines, "Equipment", context?.Equipment);
            AddReferenceDiagnostic(lines, "Quest Log", context?.QuestLog);
            AddReferenceDiagnostic(lines, "Contract Journal", context?.ContractJournal);
            AddReferenceDiagnostic(lines, "Persistence", context?.Persistence);
            AddReferenceDiagnostic(lines, "Enemy Health", context?.EnemyHealth);

            string result = string.Join(Environment.NewLine, lines);
            RecordSuccess("Refresh Diagnostics", "Diagnostics refreshed.");
            return result;
        }

        public void ClearConfirmation(string confirmationKey)
        {
            if (!string.IsNullOrWhiteSpace(confirmationKey))
            {
                pendingConfirmations.Remove(confirmationKey);
            }
        }

        private bool EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence)
        {
            persistence = context?.Persistence;
            if (persistence == null)
            {
                return false;
            }

            persistence.EnsureInitialized();
            return true;
        }

        private bool RequireConfirmation(string key, bool confirmed, out PrototypeTestLabOperation result)
        {
            result = default;
            if (confirmed || pendingConfirmations.Remove(key))
            {
                return true;
            }

            pendingConfirmations.Add(key);
            result = RecordFailure("Confirmation Required", $"Press the same destructive action again to confirm '{key}'.", "ConfirmationRequired");
            return false;
        }

        private PrototypeTestLabOperation RecordSuccess(string operationName, string message)
        {
            return Record(true, operationName, "Success", message);
        }

        private PrototypeTestLabOperation RecordFailure(string operationName, string message, string code)
        {
            return Record(false, operationName, code, message);
        }

        private PrototypeTestLabOperation Record(bool succeeded, string operationName, string code, string message)
        {
            PrototypeTestLabOperation operation = new PrototypeTestLabOperation(DateTime.Now, operationName, succeeded, code, message);
            history.Insert(0, operation);
            while (history.Count > historyLimit)
            {
                history.RemoveAt(history.Count - 1);
            }

            if (!succeeded && !string.Equals(code, "ConfirmationRequired", StringComparison.Ordinal))
            {
                Debug.LogWarning($"{operationName}: {message}");
            }

            HistoryChanged?.Invoke();
            return operation;
        }

        private DefinitionRegistry CreateRegistry(DefinitionCatalog catalog)
        {
            if (catalog != null)
            {
                return catalog.CreateRegistry();
            }

#if UNITY_EDITOR
            DefinitionCatalog loaded = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(PrototypeCatalogPath);
            return loaded == null ? null : loaded.CreateRegistry();
#else
            return null;
#endif
        }

        private string FormatHealth()
        {
            return context?.PlayerHealth == null
                ? "Missing"
                : $"{context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth} Defeated={context.PlayerHealth.IsDefeated}";
        }

        private static string FormatResource(float current, float maximum)
        {
            return $"{current:0.#}/{maximum:0.#}";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        public static string FormatDefinition(IGameDefinition definition)
        {
            return definition == null ? "None" : $"{definition.DisplayName} ({definition.Id})";
        }

        private string FormatStatuses(StatusEffectController controller)
        {
            if (controller == null || controller.ActiveStatuses.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", controller.ActiveStatuses.Select(status => $"{status.Definition.DisplayName} x{status.StackCount} [{status.ApplicationId}]"));
        }

        private string FormatInventory()
        {
            if (context?.Inventory == null)
            {
                return "Missing";
            }

            return $"{context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity} slots";
        }

        private int CountEquipped()
        {
            if (context?.Equipment == null)
            {
                return 0;
            }

            int count = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private string FormatEnemy()
        {
            return context?.EnemyHealth == null
                ? "Missing"
                : $"{context.EnemyHealth.CurrentHealth:0.#}/{context.EnemyHealth.MaximumHealth:0.#} Defeated={context.EnemyHealth.IsDefeated}";
        }

        private string FormatLocationOneLine()
        {
            if (context?.Persistence == null)
            {
                return "Missing";
            }

            return context.Persistence.BuildPlayerLocationDiagnosticSummary().Replace(Environment.NewLine, " | ");
        }

        private void AddDuplicateInstanceDiagnostics(List<string> lines)
        {
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            if (context?.Inventory != null)
            {
                foreach (InventorySlot slot in context.Inventory.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            if (context?.Equipment != null)
            {
                foreach (EquipmentSlotState slot in context.Equipment.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            lines.Add(duplicates.Count == 0 ? "Duplicate item instance IDs: none" : $"Duplicate item instance IDs: {string.Join(", ", duplicates)}");
        }

        private static void AddDuplicateStatusDiagnostics(List<string> lines, string label, StatusEffectController controller)
        {
            if (controller == null)
            {
                lines.Add($"{label} statuses: missing controller");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            foreach (RuntimeStatusEffect status in controller.ActiveStatuses)
            {
                if (!ids.Add(status.ApplicationId))
                {
                    duplicates.Add(status.ApplicationId);
                }
            }

            lines.Add(duplicates.Count == 0 ? $"{label} duplicate status IDs: none" : $"{label} duplicate status IDs: {string.Join(", ", duplicates)}");
        }

        private static void AddReferenceDiagnostic(List<string> lines, string label, UnityEngine.Object value)
        {
            lines.Add($"{label}: {(value == null ? "Missing" : "OK")}");
        }
    }
}
#endif
