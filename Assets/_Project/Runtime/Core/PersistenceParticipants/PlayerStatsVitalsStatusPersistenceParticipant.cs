using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Beings;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.StatusEffects;

namespace UnityIsekaiGame.Persistence
{
    public sealed class PlayerStatsVitalsStatusPersistenceParticipant : IPersistenceParticipant
    {
        public const string Key = "player.stats-vitals-status";
        public const int CurrentParticipantSchemaVersion = 1;

        private readonly PlayerStats stats;
        private readonly PlayerHealth health;
        private readonly PlayerMana mana;
        private readonly PlayerStamina stamina;
        private readonly StatusEffectController statusController;
        private readonly Func<DefinitionRegistry> registryProvider;
        private readonly string ownerId;

        public PlayerStatsVitalsStatusPersistenceParticipant(
            PlayerStats stats,
            PlayerHealth health,
            PlayerMana mana,
            PlayerStamina stamina,
            StatusEffectController statusController,
            Func<DefinitionRegistry> registryProvider,
            string ownerId = PersistenceService.LocalPlayerId)
        {
            this.stats = stats;
            this.health = health;
            this.mana = mana;
            this.stamina = stamina;
            this.statusController = statusController;
            this.registryProvider = registryProvider;
            this.ownerId = string.IsNullOrWhiteSpace(ownerId) ? PersistenceService.LocalPlayerId : ownerId;
        }

        public string ParticipantKey => Key;
        public int ParticipantSchemaVersion => CurrentParticipantSchemaVersion;
        public bool IsRequired => true;
        public PersistenceScope Scope => PersistenceScope.Player;
        public string OwnerId => ownerId;
        public PersistenceLoadPhase LoadPhase => PersistenceLoadPhase.Statuses;
        public int LoadPriority => 0;

        public PersistenceParticipantSaveResult CapturePayload()
        {
            if (!ValidateRuntimeReferences(out string failureReason))
            {
                return PersistenceParticipantSaveResult.Failure(failureReason);
            }

            if (health.IsDefeated || health.CurrentHealth <= 0)
            {
                return PersistenceParticipantSaveResult.Failure("Cannot save the prototype player while defeated. Reset or recover before saving.");
            }

            PlayerStatsVitalsStatusSaveData saveData = new PlayerStatsVitalsStatusSaveData
            {
                schemaVersion = CurrentParticipantSchemaVersion,
                actorProfileId = stats.ActorProfile == null ? string.Empty : stats.ActorProfile.Id,
                currentHealth = health.CurrentHealth,
                currentMana = mana.CurrentMana,
                currentStamina = stamina.CurrentStamina,
                statuses = statusController.CreateSaveData(saveEligibleOnly: true)
            };

            PersistenceParticipantPrepareResult validation = PreparePayload(JsonUtility.ToJson(saveData), CurrentParticipantSchemaVersion);
            if (validation == null || !validation.Succeeded)
            {
                return PersistenceParticipantSaveResult.Failure(validation?.Message ?? "Stats/vitals/status snapshot failed validation.");
            }

            DiscardPreparedPayload(validation.PreparedPayload);
            return PersistenceParticipantSaveResult.Success(JsonUtility.ToJson(saveData));
        }

        public PersistenceParticipantPrepareResult PreparePayload(string payloadJson, int payloadSchemaVersion)
        {
            if (payloadSchemaVersion != CurrentParticipantSchemaVersion)
            {
                return PersistenceParticipantPrepareResult.Failure($"Unsupported stats/vitals/status participant schema version {payloadSchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return PersistenceParticipantPrepareResult.Failure("Stats/vitals/status payload is empty.");
            }

            PlayerStatsVitalsStatusSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<PlayerStatsVitalsStatusSaveData>(payloadJson);
            }
            catch
            {
                return PersistenceParticipantPrepareResult.Failure("Stats/vitals/status payload is malformed JSON.");
            }

            if (saveData == null)
            {
                return PersistenceParticipantPrepareResult.Failure("Stats/vitals/status payload did not parse.");
            }

            if (saveData.schemaVersion != CurrentParticipantSchemaVersion)
            {
                return PersistenceParticipantPrepareResult.Failure($"Unsupported stats/vitals/status payload schema version {saveData.schemaVersion}.");
            }

            if (!ValidateRuntimeReferences(out string failureReason))
            {
                return PersistenceParticipantPrepareResult.Failure(failureReason);
            }

            DefinitionRegistry registry = registryProvider?.Invoke();
            if (registry == null)
            {
                return PersistenceParticipantPrepareResult.Failure("Definition registry is not available for stats/vitals/status restore.");
            }

            if (!ValidateActorProfile(saveData.actorProfileId, registry, out failureReason)
                || !ValidateVitals(saveData, out failureReason)
                || !ValidateStatuses(saveData.statuses, registry, out failureReason)
                || !ValidateOnTemporaryRuntime(saveData.statuses, registry, out failureReason))
            {
                return PersistenceParticipantPrepareResult.Failure(failureReason);
            }

            return PersistenceParticipantPrepareResult.Success(new PreparedPayload(saveData));
        }

        public PersistenceParticipantCommitResult CommitPreparedPayload(object preparedPayload)
        {
            if (!ValidateRuntimeReferences(out string failureReason))
            {
                return PersistenceParticipantCommitResult.Failure(failureReason);
            }

            if (preparedPayload is not PreparedPayload prepared || prepared.SaveData == null)
            {
                return PersistenceParticipantCommitResult.Failure("Prepared stats/vitals/status payload has the wrong type.");
            }

            DefinitionRegistry registry = registryProvider?.Invoke();
            if (registry == null)
            {
                return PersistenceParticipantCommitResult.Failure("Definition registry is not available for stats/vitals/status commit.");
            }

            List<StatusEffectSaveData> rollbackStatuses = statusController.CreateSaveData();
            int rollbackHealth = health.CurrentHealth;
            float rollbackMana = mana.CurrentMana;
            float rollbackStamina = stamina.CurrentStamina;

            statusController.ClearAllStatuses();
            StatusEffectRestoreResult statusResult = StatusEffectRestoreUtility.Restore(statusController, prepared.SaveData.statuses, registry, statusController.gameObject, Time.time);
            if (!statusResult.Succeeded)
            {
                statusController.ClearAllStatuses();
                StatusEffectRestoreUtility.Restore(statusController, rollbackStatuses, registry, statusController.gameObject, Time.time);
                RestoreVitalsForRollback(rollbackHealth, rollbackMana, rollbackStamina);
                return PersistenceParticipantCommitResult.Failure($"Status commit failed after preparation; rollback attempted: {statusResult.Message}");
            }

            if (!health.TryRestoreForPersistence(prepared.SaveData.currentHealth, out failureReason)
                || !mana.TryRestoreForPersistence(prepared.SaveData.currentMana, out failureReason)
                || !stamina.TryRestoreForPersistence(prepared.SaveData.currentStamina, out failureReason))
            {
                statusController.ClearAllStatuses();
                StatusEffectRestoreUtility.Restore(statusController, rollbackStatuses, registry, statusController.gameObject, Time.time);
                RestoreVitalsForRollback(rollbackHealth, rollbackMana, rollbackStamina);
                return PersistenceParticipantCommitResult.Failure($"Vital commit failed after preparation; rollback attempted: {failureReason}");
            }

            return PersistenceParticipantCommitResult.Success("Player stats, vitals, and statuses restored.");
        }

        public void DiscardPreparedPayload(object preparedPayload)
        {
        }

        private bool ValidateRuntimeReferences(out string failureReason)
        {
            failureReason = string.Empty;
            if (stats == null)
            {
                failureReason = "Player stats are missing.";
                return false;
            }

            if (health == null)
            {
                failureReason = "Player health is missing.";
                return false;
            }

            if (mana == null)
            {
                failureReason = "Player mana is missing.";
                return false;
            }

            if (stamina == null)
            {
                failureReason = "Player stamina is missing.";
                return false;
            }

            if (statusController == null)
            {
                failureReason = "Player status effect controller is missing.";
                return false;
            }

            return true;
        }

        private bool ValidateActorProfile(string actorProfileId, DefinitionRegistry registry, out string failureReason)
        {
            failureReason = string.Empty;
            string currentProfileId = stats.ActorProfile == null ? string.Empty : stats.ActorProfile.Id;
            if (string.IsNullOrWhiteSpace(actorProfileId))
            {
                return true;
            }

            if (!registry.TryGet(actorProfileId, out ActorProfileDefinition _))
            {
                failureReason = $"Actor profile definition '{actorProfileId}' was not found.";
                return false;
            }

            if (!string.Equals(actorProfileId, currentProfileId, StringComparison.Ordinal))
            {
                failureReason = $"Saved actor profile '{actorProfileId}' does not match current player profile '{currentProfileId}'.";
                return false;
            }

            return true;
        }

        private bool ValidateVitals(PlayerStatsVitalsStatusSaveData saveData, out string failureReason)
        {
            failureReason = string.Empty;
            if (saveData.currentHealth <= 0)
            {
                failureReason = "Saved health must be above zero; defeated prototype saves are not supported.";
                return false;
            }

            if (!IsFiniteNonNegative(saveData.currentMana) || !IsFiniteNonNegative(saveData.currentStamina))
            {
                failureReason = "Saved mana and stamina must be finite non-negative values.";
                return false;
            }

            return true;
        }

        private static bool ValidateStatuses(IReadOnlyList<StatusEffectSaveData> statuses, DefinitionRegistry registry, out string failureReason)
        {
            failureReason = string.Empty;
            HashSet<string> applicationIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<StatusEffectSaveData> entries = statuses ?? Array.Empty<StatusEffectSaveData>();

            for (int i = 0; i < entries.Count; i++)
            {
                StatusEffectSaveData entry = entries[i];
                if (entry == null)
                {
                    failureReason = "Status save entry is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.applicationId) || !applicationIds.Add(entry.applicationId))
                {
                    failureReason = $"Status application ID '{entry.applicationId}' is missing or duplicated.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.statusDefinitionId) || !registry.TryGet(entry.statusDefinitionId, out StatusEffectDefinition definition))
                {
                    failureReason = $"Status definition '{entry.statusDefinitionId}' was not found.";
                    return false;
                }

                if (!IsSaveEligible(definition))
                {
                    failureReason = $"Status '{definition.DisplayName}' is not save-eligible.";
                    return false;
                }

                if (entry.persistencePolicy != definition.PersistencePolicy)
                {
                    failureReason = $"Status '{definition.DisplayName}' persistence policy does not match its definition.";
                    return false;
                }

                if (entry.stackCount < 1 || entry.stackCount > definition.MaximumStacks)
                {
                    failureReason = $"Status '{definition.DisplayName}' has invalid stack count {entry.stackCount}.";
                    return false;
                }

                if (definition.DurationModel == StatusDurationModel.Timed && (!IsFinitePositive(entry.remainingDuration) || entry.durationModel != StatusDurationModel.Timed))
                {
                    failureReason = $"Timed status '{definition.DisplayName}' has invalid remaining duration.";
                    return false;
                }

                if (definition.DurationModel != StatusDurationModel.Timed && (!IsFiniteNonNegative(entry.remainingDuration) || entry.durationModel != definition.DurationModel))
                {
                    failureReason = $"Status '{definition.DisplayName}' has invalid duration data.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateOnTemporaryRuntime(IReadOnlyList<StatusEffectSaveData> statuses, DefinitionRegistry registry, out string failureReason)
        {
            failureReason = string.Empty;
            GameObject temporary = new GameObject("Stats Vitals Status Persistence Validation");
            temporary.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                temporary.AddComponent<PlayerStats>();
                StatusEffectController temporaryController = temporary.AddComponent<StatusEffectController>();
                StatusEffectRestoreResult result = StatusEffectRestoreUtility.Restore(temporaryController, statuses, registry, temporary, 0f);
                if (!result.Succeeded)
                {
                    failureReason = $"Status restore validation failed: {result.Message}";
                    return false;
                }

                return true;
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

        private void RestoreVitalsForRollback(int rollbackHealth, float rollbackMana, float rollbackStamina)
        {
            health.TryRestoreForPersistence(Mathf.Max(1, rollbackHealth), out _);
            mana.TryRestoreForPersistence(rollbackMana, out _);
            stamina.TryRestoreForPersistence(rollbackStamina, out _);
        }

        private static bool IsSaveEligible(StatusEffectDefinition definition)
        {
            return definition != null
                && definition.DurationModel != StatusDurationModel.Instant
                && (definition.PersistencePolicy == StatusPersistencePolicy.SaveRemainingDuration
                    || definition.PersistencePolicy == StatusPersistencePolicy.PersistentUntilRemoved);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private sealed class PreparedPayload
        {
            public PreparedPayload(PlayerStatsVitalsStatusSaveData saveData)
            {
                SaveData = saveData;
            }

            public PlayerStatsVitalsStatusSaveData SaveData { get; }
        }
    }
}
