using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UnityIsekaiGame.GameData.Persistence
{
    public sealed class PersistenceService
    {
        public const string FormatIdentifier = "UnityIsekaiGame.Save";
        public const int CurrentSchemaVersion = 1;
        public const string PrototypeSlotId = "slot-0001";
        public const string LocalWorldId = "local-world";
        public const string LocalPlayerId = "local-player";
        public const string LocalAccountId = "local-account";

        private readonly PersistencePathProvider pathProvider;
        private readonly List<IPersistenceParticipant> participants = new List<IPersistenceParticipant>();
        private readonly Dictionary<string, IPersistenceParticipant> participantsByKey = new Dictionary<string, IPersistenceParticipant>(StringComparer.Ordinal);
        private bool operationInProgress;

        public PersistenceService(
            PersistencePathProvider pathProvider = null,
            string gameVersion = "0.4.1-prototype",
            string worldId = LocalWorldId,
            string playerId = LocalPlayerId,
            string accountId = LocalAccountId)
        {
            this.pathProvider = pathProvider ?? new PersistencePathProvider();
            GameVersion = gameVersion;
            WorldId = string.IsNullOrWhiteSpace(worldId) ? LocalWorldId : worldId;
            PlayerId = string.IsNullOrWhiteSpace(playerId) ? LocalPlayerId : playerId;
            AccountId = string.IsNullOrWhiteSpace(accountId) ? LocalAccountId : accountId;
        }

        public event Action<PersistenceSaveResult> SaveCompleted;
        public event Action<PersistenceLoadResult> LoadCompleted;
        public event Action SaveStarted;
        public event Action LoadStarted;
        public event Action SaveSlotsChanged;

        public string GameVersion { get; }
        public string WorldId { get; }
        public string PlayerId { get; }
        public string AccountId { get; }
        public bool OperationInProgress => operationInProgress;
        public int ParticipantCount => participants.Count;
        public PersistencePathProvider PathProvider => pathProvider;

        public bool RegisterParticipant(IPersistenceParticipant participant, out string failureReason)
        {
            failureReason = string.Empty;
            if (participant == null)
            {
                failureReason = "Cannot register a null persistence participant.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(participant.ParticipantKey))
            {
                failureReason = "Cannot register a persistence participant with no key.";
                return false;
            }

            if (participantsByKey.ContainsKey(participant.ParticipantKey))
            {
                failureReason = $"Persistence participant '{participant.ParticipantKey}' is already registered.";
                return false;
            }

            participants.Add(participant);
            participantsByKey.Add(participant.ParticipantKey, participant);
            participants.Sort(CompareParticipants);
            return true;
        }

        public void UnregisterParticipant(IPersistenceParticipant participant)
        {
            if (participant == null || string.IsNullOrWhiteSpace(participant.ParticipantKey))
            {
                return;
            }

            participants.Remove(participant);
            if (participantsByKey.TryGetValue(participant.ParticipantKey, out IPersistenceParticipant found) && ReferenceEquals(found, participant))
            {
                participantsByKey.Remove(participant.ParticipantKey);
            }
        }

        public void ResetParticipants()
        {
            participants.Clear();
            participantsByKey.Clear();
            operationInProgress = false;
        }

        public PersistenceSaveResult Save(string slotId, string displayName = null)
        {
            if (operationInProgress)
            {
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.OperationAlreadyRunning, slotId, string.Empty, "A save or load operation is already running.");
            }

            if (!pathProvider.TryGetPaths(slotId, out SaveSlotPaths paths, out string pathFailure))
            {
                return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.InvalidSlotId, slotId, string.Empty, pathFailure));
            }

            if (participants.Count == 0)
            {
                return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.NoParticipants, slotId, paths.PrimaryPath, "No persistence participants are registered."));
            }

            operationInProgress = true;
            SaveStarted?.Invoke();

            try
            {
                GameSaveEnvelope envelope = BuildEnvelope(slotId, displayName, paths);
                string serialized;
                try
                {
                    serialized = JsonUtility.ToJson(envelope, true);
                    if (string.IsNullOrWhiteSpace(serialized))
                    {
                        return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.SerializationFailed, slotId, paths.PrimaryPath, "Save envelope serialized to empty JSON."));
                    }
                }
                catch (Exception exception)
                {
                    return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.SerializationFailed, slotId, paths.PrimaryPath, "Save serialization failed.", exception));
                }

                try
                {
                    pathProvider.EnsureDirectory();
                }
                catch (Exception exception)
                {
                    return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.DirectoryCreationFailed, slotId, paths.PrimaryPath, "Could not create save directory.", exception));
                }

                PersistenceSaveResult writeResult = WriteAtomically(paths, serialized);
                if (!writeResult.Succeeded)
                {
                    return FinishSave(writeResult);
                }

                SaveSlotsChanged?.Invoke();
                return FinishSave(PersistenceSaveResult.Success(slotId, paths.PrimaryPath, $"Saved slot '{slotId}'."));
            }
            catch (ParticipantSaveException exception)
            {
                return FinishSave(PersistenceSaveResult.Failure(exception.Status, slotId, paths.PrimaryPath, exception.Message, exception));
            }
            catch (Exception exception)
            {
                return FinishSave(PersistenceSaveResult.Failure(PersistenceSaveStatus.UnknownException, slotId, paths.PrimaryPath, "Unexpected save failure.", exception));
            }
        }

        public PersistenceLoadResult Load(string slotId, bool loadBackup = false)
        {
            if (operationInProgress)
            {
                return PersistenceLoadResult.Failure(PersistenceLoadStatus.OperationAlreadyRunning, slotId, string.Empty, "A save or load operation is already running.");
            }

            if (!pathProvider.TryGetPaths(slotId, out SaveSlotPaths paths, out string pathFailure))
            {
                return FinishLoad(PersistenceLoadResult.Failure(PersistenceLoadStatus.InvalidSlotId, slotId, string.Empty, pathFailure));
            }

            string path = loadBackup ? paths.BackupPath : paths.PrimaryPath;
            operationInProgress = true;
            LoadStarted?.Invoke();

            try
            {
                PersistenceValidationResult validation = ValidatePath(slotId, path, loadBackup, validateParticipants: true);
                if (!validation.Succeeded)
                {
                    bool backupAvailable = !loadBackup && IsBackupValid(slotId, paths.BackupPath);
                    PersistenceLoadStatus status = backupAvailable ? PersistenceLoadStatus.BackupAvailable : MapValidationToLoadStatus(validation.Status);
                    return FinishLoad(PersistenceLoadResult.Failure(status, slotId, path, validation.Message, backupAvailable, validation.Exception));
                }

                PersistenceLoadResult prepared = PrepareAndCommit(slotId, path, validation.Envelope, loadBackup);
                return FinishLoad(prepared);
            }
            catch (Exception exception)
            {
                return FinishLoad(PersistenceLoadResult.Failure(loadBackup ? PersistenceLoadStatus.BackupLoadFailed : PersistenceLoadStatus.UnknownException, slotId, path, "Unexpected load failure.", false, exception));
            }
        }

        public PersistenceValidationResult ValidateSlot(string slotId, bool validateBackup = false)
        {
            if (!pathProvider.TryGetPaths(slotId, out SaveSlotPaths paths, out string pathFailure))
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.InvalidSlotId, slotId, string.Empty, pathFailure);
            }

            string path = validateBackup ? paths.BackupPath : paths.PrimaryPath;
            PersistenceValidationResult result = ValidatePath(slotId, path, validateBackup, validateParticipants: true);
            if (result.Succeeded)
            {
                return result;
            }

            bool backupAvailable = !validateBackup && IsBackupValid(slotId, paths.BackupPath);
            if (backupAvailable)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.BackupAvailable, slotId, path, result.Message, true, result.Exception);
            }

            return result;
        }

        public PersistenceDeleteResult DeleteSlot(string slotId)
        {
            if (operationInProgress)
            {
                return PersistenceDeleteResult.Failure(PersistenceDeleteStatus.OperationAlreadyRunning, slotId, "A save or load operation is already running.");
            }

            if (!pathProvider.TryGetPaths(slotId, out SaveSlotPaths paths, out string pathFailure))
            {
                return PersistenceDeleteResult.Failure(PersistenceDeleteStatus.InvalidSlotId, slotId, pathFailure);
            }

            operationInProgress = true;
            try
            {
                DeleteIfExists(paths.PrimaryPath);
                DeleteIfExists(paths.BackupPath);
                DeleteIfExists(paths.TemporaryPath);
                SaveSlotsChanged?.Invoke();
                return PersistenceDeleteResult.Success(slotId, $"Deleted save slot '{slotId}'.");
            }
            catch (Exception exception)
            {
                return PersistenceDeleteResult.Failure(PersistenceDeleteStatus.DeleteFailed, slotId, "Could not delete every file for the save slot.", exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        public IReadOnlyList<SaveSlotMetadata> ListSaveSlots()
        {
            List<SaveSlotMetadata> metadata = new List<SaveSlotMetadata>();
            foreach (SaveSlotPaths paths in pathProvider.EnumerateKnownSlots())
            {
                metadata.Add(ReadMetadata(paths));
            }

            return metadata;
        }

        private GameSaveEnvelope BuildEnvelope(string slotId, string displayName, SaveSlotPaths paths)
        {
            string now = DateTime.UtcNow.ToString("o");
            GameSaveEnvelope previous = TryReadEnvelopeHeader(paths.PrimaryPath);
            GameSaveEnvelope envelope = new GameSaveEnvelope
            {
                formatIdentifier = FormatIdentifier,
                schemaVersion = CurrentSchemaVersion,
                gameVersion = GameVersion,
                saveId = string.IsNullOrWhiteSpace(previous?.saveId) ? Guid.NewGuid().ToString("N") : previous.saveId,
                slotId = slotId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? slotId : displayName,
                worldId = WorldId,
                playerId = PlayerId,
                accountId = AccountId,
                createdUtc = string.IsNullOrWhiteSpace(previous?.createdUtc) ? now : previous.createdUtc,
                lastWrittenUtc = now,
                playtimeSeconds = previous?.playtimeSeconds ?? 0,
                sceneSummary = "Prototype scene placeholder",
                placeSummary = "Prototype place placeholder",
                playerSummary = "Prototype player placeholder"
            };

            foreach (IPersistenceParticipant participant in participants)
            {
                PersistenceParticipantSaveResult result = participant.CapturePayload();
                if (result == null || !result.Succeeded)
                {
                    throw new ParticipantSaveException(PersistenceSaveStatus.ParticipantCaptureFailed, $"Participant '{participant.ParticipantKey}' failed capture: {result?.Message ?? "No result."}");
                }

                if (string.IsNullOrWhiteSpace(result.PayloadJson))
                {
                    throw new ParticipantSaveException(PersistenceSaveStatus.ParticipantValidationFailed, $"Participant '{participant.ParticipantKey}' produced an empty payload.");
                }

                PersistenceParticipantPrepareResult prepareResult = participant.PreparePayload(result.PayloadJson, participant.ParticipantSchemaVersion);
                if (prepareResult == null || !prepareResult.Succeeded)
                {
                    throw new ParticipantSaveException(PersistenceSaveStatus.ParticipantValidationFailed, $"Participant '{participant.ParticipantKey}' failed self-validation: {prepareResult?.Message ?? "No result."}");
                }

                participant.DiscardPreparedPayload(prepareResult.PreparedPayload);
                envelope.participants.Add(new SaveParticipantRecord
                {
                    participantKey = participant.ParticipantKey,
                    participantSchemaVersion = participant.ParticipantSchemaVersion,
                    required = participant.IsRequired,
                    persistenceScope = (int)participant.Scope,
                    ownerId = participant.OwnerId ?? string.Empty,
                    loadPhase = (int)participant.LoadPhase,
                    loadPriority = participant.LoadPriority,
                    payloadJson = result.PayloadJson
                });
            }

            envelope.participants.Sort(CompareRecords);
            envelope.contentChecksum = ComputeChecksum(envelope);
            return envelope;
        }

        private PersistenceSaveResult WriteAtomically(SaveSlotPaths paths, string serialized)
        {
            try
            {
                DeleteIfExists(paths.TemporaryPath);
                File.WriteAllText(paths.TemporaryPath, serialized, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.TemporaryWriteFailed, paths.SlotId, paths.PrimaryPath, "Could not write temporary save file.", exception);
            }

            PersistenceValidationResult tempValidation = ValidatePath(paths.SlotId, paths.TemporaryPath, isBackup: false, validateParticipants: false);
            if (!tempValidation.Succeeded)
            {
                DeleteIfExists(paths.TemporaryPath);
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.TemporaryWriteFailed, paths.SlotId, paths.PrimaryPath, $"Temporary save validation failed: {tempValidation.Message}");
            }

            try
            {
                if (File.Exists(paths.PrimaryPath))
                {
                    File.Copy(paths.PrimaryPath, paths.BackupPath, true);
                }
            }
            catch (Exception exception)
            {
                DeleteIfExists(paths.TemporaryPath);
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.BackupFailed, paths.SlotId, paths.PrimaryPath, "Could not preserve previous save as backup.", exception);
            }

            try
            {
                if (File.Exists(paths.PrimaryPath))
                {
                    File.Delete(paths.PrimaryPath);
                }

                File.Move(paths.TemporaryPath, paths.PrimaryPath);
                return PersistenceSaveResult.Success(paths.SlotId, paths.PrimaryPath, "Atomic save write completed.");
            }
            catch (Exception exception)
            {
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.ReplacementFailed, paths.SlotId, paths.PrimaryPath, "Could not replace primary save file.", exception);
            }
        }

        private PersistenceLoadResult PrepareAndCommit(string slotId, string path, GameSaveEnvelope envelope, bool loadedBackup)
        {
            List<PreparedParticipant> prepared = new List<PreparedParticipant>();
            Dictionary<string, SaveParticipantRecord> records = BuildRecordMap(envelope, out PersistenceLoadResult mapFailure);
            if (mapFailure != null)
            {
                return mapFailure;
            }

            foreach (IPersistenceParticipant participant in participants)
            {
                if (!records.TryGetValue(participant.ParticipantKey, out SaveParticipantRecord record))
                {
                    if (participant.IsRequired)
                    {
                        DiscardPrepared(prepared);
                        return PersistenceLoadResult.Failure(PersistenceLoadStatus.MissingRequiredParticipantPayload, slotId, path, $"Save is missing required participant '{participant.ParticipantKey}'.");
                    }

                    continue;
                }

                PersistenceParticipantPrepareResult prepare = participant.PreparePayload(record.payloadJson, record.participantSchemaVersion);
                if (prepare == null || !prepare.Succeeded)
                {
                    DiscardPrepared(prepared);
                    return PersistenceLoadResult.Failure(PersistenceLoadStatus.ParticipantPrepareFailed, slotId, path, $"Participant '{participant.ParticipantKey}' failed prepare: {prepare?.Message ?? "No result."}");
                }

                prepared.Add(new PreparedParticipant(participant, prepare.PreparedPayload));
            }

            foreach (SaveParticipantRecord record in envelope.participants)
            {
                if (!participantsByKey.TryGetValue(record.participantKey, out _) && record.required)
                {
                    DiscardPrepared(prepared);
                    return PersistenceLoadResult.Failure(PersistenceLoadStatus.MissingRuntimeParticipant, slotId, path, $"Required save participant '{record.participantKey}' is not registered at runtime.");
                }
            }

            for (int i = 0; i < prepared.Count; i++)
            {
                PersistenceParticipantCommitResult commit = prepared[i].Participant.CommitPreparedPayload(prepared[i].PreparedPayload);
                if (commit == null || !commit.Succeeded)
                {
                    return PersistenceLoadResult.Failure(PersistenceLoadStatus.ParticipantCommitFailed, slotId, path, $"Participant '{prepared[i].Participant.ParticipantKey}' failed commit: {commit?.Message ?? "No result."}");
                }
            }

            return PersistenceLoadResult.Success(slotId, path, loadedBackup, loadedBackup ? $"Loaded backup save slot '{slotId}'." : $"Loaded save slot '{slotId}'.");
        }

        private PersistenceValidationResult ValidatePath(string slotId, string path, bool isBackup, bool validateParticipants)
        {
            if (!File.Exists(path))
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.FileMissing, slotId, path, isBackup ? "Backup save file is missing." : "Save file is missing.");
            }

            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.ReadFailed, slotId, path, "Could not read save file.", false, exception);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.MalformedJson, slotId, path, "Save file is empty.");
            }

            GameSaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<GameSaveEnvelope>(json);
            }
            catch (Exception exception)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.MalformedJson, slotId, path, "Save file is malformed JSON.", false, exception);
            }

            if (envelope == null)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.MalformedJson, slotId, path, "Save file did not contain a readable envelope.");
            }

            PersistenceValidationResult envelopeResult = ValidateEnvelope(slotId, path, envelope, validateParticipants);
            return envelopeResult.Succeeded
                ? PersistenceValidationResult.Success(slotId, path, envelope, "Save file is valid.")
                : envelopeResult;
        }

        private PersistenceValidationResult ValidateEnvelope(string slotId, string path, GameSaveEnvelope envelope, bool validateParticipants)
        {
            if (envelope.formatIdentifier != FormatIdentifier)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.WrongFormatIdentifier, slotId, path, $"Wrong save format '{envelope.formatIdentifier}'.");
            }

            if (envelope.schemaVersion > CurrentSchemaVersion || envelope.schemaVersion < 1)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.UnsupportedSchemaVersion, slotId, path, $"Unsupported save schema version {envelope.schemaVersion}.");
            }

            string expectedChecksum = ComputeChecksum(envelope);
            if (!string.Equals(envelope.contentChecksum, expectedChecksum, StringComparison.Ordinal))
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.ChecksumMismatch, slotId, path, "Save checksum does not match the envelope content.");
            }

            Dictionary<string, SaveParticipantRecord> records = BuildRecordMap(envelope, out PersistenceLoadResult mapFailure);
            if (mapFailure != null)
            {
                return PersistenceValidationResult.Failure(PersistenceValidationStatus.DuplicateParticipantKey, slotId, path, mapFailure.Message);
            }

            if (!validateParticipants)
            {
                return PersistenceValidationResult.Success(slotId, path, envelope, "Save envelope is valid.");
            }

            foreach (IPersistenceParticipant participant in participants)
            {
                if (!records.TryGetValue(participant.ParticipantKey, out SaveParticipantRecord record))
                {
                    if (participant.IsRequired)
                    {
                        return PersistenceValidationResult.Failure(PersistenceValidationStatus.MissingRequiredParticipantPayload, slotId, path, $"Save is missing required participant '{participant.ParticipantKey}'.");
                    }

                    continue;
                }

                PersistenceParticipantPrepareResult prepare = participant.PreparePayload(record.payloadJson, record.participantSchemaVersion);
                if (prepare == null || !prepare.Succeeded)
                {
                    return PersistenceValidationResult.Failure(PersistenceValidationStatus.ParticipantPrepareFailed, slotId, path, $"Participant '{participant.ParticipantKey}' failed validation: {prepare?.Message ?? "No result."}");
                }

                participant.DiscardPreparedPayload(prepare.PreparedPayload);
            }

            foreach (SaveParticipantRecord record in envelope.participants)
            {
                if (!participantsByKey.ContainsKey(record.participantKey) && record.required)
                {
                    return PersistenceValidationResult.Failure(PersistenceValidationStatus.MissingRuntimeParticipant, slotId, path, $"Required save participant '{record.participantKey}' is not registered.");
                }
            }

            return PersistenceValidationResult.Success(slotId, path, envelope, "Save file is valid.");
        }

        private SaveSlotMetadata ReadMetadata(SaveSlotPaths paths)
        {
            FileInfo primary = new FileInfo(paths.PrimaryPath);
            PersistenceValidationResult validation = ValidatePath(paths.SlotId, paths.PrimaryPath, isBackup: false, validateParticipants: false);
            GameSaveEnvelope envelope = validation.Envelope;
            return new SaveSlotMetadata
            {
                slotId = paths.SlotId,
                displayName = envelope == null ? paths.SlotId : envelope.displayName,
                saveId = envelope == null ? string.Empty : envelope.saveId,
                createdUtc = envelope == null ? string.Empty : envelope.createdUtc,
                modifiedUtc = primary.Exists ? primary.LastWriteTimeUtc.ToString("o") : string.Empty,
                playtimeSeconds = envelope?.playtimeSeconds ?? 0,
                currentPlaceSummary = envelope == null ? string.Empty : envelope.placeSummary,
                playerSummary = envelope == null ? string.Empty : envelope.playerSummary,
                worldId = envelope == null ? string.Empty : envelope.worldId,
                playerId = envelope == null ? string.Empty : envelope.playerId,
                accountId = envelope == null ? string.Empty : envelope.accountId,
                schemaVersion = envelope?.schemaVersion ?? 0,
                gameVersion = envelope == null ? string.Empty : envelope.gameVersion,
                fileSizeBytes = primary.Exists ? primary.Length : 0,
                hasPrimary = primary.Exists,
                hasBackup = File.Exists(paths.BackupPath),
                isValid = validation.Succeeded,
                status = validation.Status.ToString(),
                message = validation.Message
            };
        }

        private bool IsBackupValid(string slotId, string backupPath)
        {
            return ValidatePath(slotId, backupPath, isBackup: true, validateParticipants: false).Succeeded;
        }

        private Dictionary<string, SaveParticipantRecord> BuildRecordMap(GameSaveEnvelope envelope, out PersistenceLoadResult failure)
        {
            failure = null;
            Dictionary<string, SaveParticipantRecord> records = new Dictionary<string, SaveParticipantRecord>(StringComparer.Ordinal);
            IReadOnlyList<SaveParticipantRecord> participantRecords = envelope.participants == null
                ? Array.Empty<SaveParticipantRecord>()
                : envelope.participants;
            for (int i = 0; i < participantRecords.Count; i++)
            {
                SaveParticipantRecord record = participantRecords[i];
                if (record == null || string.IsNullOrWhiteSpace(record.participantKey))
                {
                    failure = PersistenceLoadResult.Failure(PersistenceLoadStatus.DuplicateParticipantKey, envelope.slotId, string.Empty, "Save contains an empty participant key.");
                    return records;
                }

                if (records.ContainsKey(record.participantKey))
                {
                    failure = PersistenceLoadResult.Failure(PersistenceLoadStatus.DuplicateParticipantKey, envelope.slotId, string.Empty, $"Save contains duplicate participant '{record.participantKey}'.");
                    return records;
                }

                records.Add(record.participantKey, record);
            }

            return records;
        }

        public static string ComputeChecksum(GameSaveEnvelope envelope)
        {
            if (envelope == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(envelope.formatIdentifier).Append('|')
                .Append(envelope.schemaVersion).Append('|')
                .Append(envelope.gameVersion).Append('|')
                .Append(envelope.saveId).Append('|')
                .Append(envelope.slotId).Append('|')
                .Append(envelope.displayName).Append('|')
                .Append(envelope.worldId).Append('|')
                .Append(envelope.playerId).Append('|')
                .Append(envelope.accountId).Append('|')
                .Append(envelope.createdUtc).Append('|')
                .Append(envelope.lastWrittenUtc).Append('|')
                .Append(envelope.playtimeSeconds).Append('|')
                .Append(envelope.sceneSummary).Append('|')
                .Append(envelope.placeSummary).Append('|')
                .Append(envelope.playerSummary);

            IReadOnlyList<SaveParticipantRecord> records = envelope.participants == null
                ? Array.Empty<SaveParticipantRecord>()
                : envelope.participants;
            for (int i = 0; i < records.Count; i++)
            {
                SaveParticipantRecord record = records[i];
                builder.Append('|')
                    .Append(record?.participantKey).Append(':')
                    .Append(record?.participantSchemaVersion ?? 0).Append(':')
                    .Append(record?.required ?? false).Append(':')
                    .Append(record?.persistenceScope ?? 0).Append(':')
                    .Append(record?.ownerId).Append(':')
                    .Append(record?.loadPhase ?? 0).Append(':')
                    .Append(record?.loadPriority ?? 0).Append(':')
                    .Append(record?.payloadJson);
            }

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            StringBuilder hex = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                hex.Append(hash[i].ToString("x2"));
            }

            return hex.ToString();
        }

        private GameSaveEnvelope TryReadEnvelopeHeader(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<GameSaveEnvelope>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch
            {
                return null;
            }
        }

        private PersistenceSaveResult FinishSave(PersistenceSaveResult result)
        {
            operationInProgress = false;
            SaveCompleted?.Invoke(result);
            return result;
        }

        private PersistenceLoadResult FinishLoad(PersistenceLoadResult result)
        {
            operationInProgress = false;
            LoadCompleted?.Invoke(result);
            return result;
        }

        private static PersistenceLoadStatus MapValidationToLoadStatus(PersistenceValidationStatus status)
        {
            return status switch
            {
                PersistenceValidationStatus.FileMissing => PersistenceLoadStatus.FileMissing,
                PersistenceValidationStatus.ReadFailed => PersistenceLoadStatus.ReadFailed,
                PersistenceValidationStatus.MalformedJson => PersistenceLoadStatus.MalformedJson,
                PersistenceValidationStatus.WrongFormatIdentifier => PersistenceLoadStatus.WrongFormatIdentifier,
                PersistenceValidationStatus.UnsupportedSchemaVersion => PersistenceLoadStatus.UnsupportedSchemaVersion,
                PersistenceValidationStatus.ChecksumMismatch => PersistenceLoadStatus.ChecksumMismatch,
                PersistenceValidationStatus.DuplicateParticipantKey => PersistenceLoadStatus.DuplicateParticipantKey,
                PersistenceValidationStatus.MissingRequiredParticipantPayload => PersistenceLoadStatus.MissingRequiredParticipantPayload,
                PersistenceValidationStatus.MissingRuntimeParticipant => PersistenceLoadStatus.MissingRuntimeParticipant,
                PersistenceValidationStatus.ParticipantPrepareFailed => PersistenceLoadStatus.ParticipantPrepareFailed,
                PersistenceValidationStatus.BackupAvailable => PersistenceLoadStatus.BackupAvailable,
                _ => PersistenceLoadStatus.UnknownException
            };
        }

        private static int CompareParticipants(IPersistenceParticipant a, IPersistenceParticipant b)
        {
            int phase = a.LoadPhase.CompareTo(b.LoadPhase);
            if (phase != 0)
            {
                return phase;
            }

            int priority = a.LoadPriority.CompareTo(b.LoadPriority);
            return priority != 0 ? priority : string.CompareOrdinal(a.ParticipantKey, b.ParticipantKey);
        }

        private static int CompareRecords(SaveParticipantRecord a, SaveParticipantRecord b)
        {
            int phase = a.loadPhase.CompareTo(b.loadPhase);
            if (phase != 0)
            {
                return phase;
            }

            int priority = a.loadPriority.CompareTo(b.loadPriority);
            return priority != 0 ? priority : string.CompareOrdinal(a.participantKey, b.participantKey);
        }

        private static void DiscardPrepared(List<PreparedParticipant> prepared)
        {
            for (int i = 0; i < prepared.Count; i++)
            {
                prepared[i].Participant.DiscardPreparedPayload(prepared[i].PreparedPayload);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private sealed class PreparedParticipant
        {
            public PreparedParticipant(IPersistenceParticipant participant, object preparedPayload)
            {
                Participant = participant;
                PreparedPayload = preparedPayload;
            }

            public IPersistenceParticipant Participant { get; }
            public object PreparedPayload { get; }
        }

        private sealed class ParticipantSaveException : Exception
        {
            public ParticipantSaveException(PersistenceSaveStatus status, string message)
                : base(message)
            {
                Status = status;
            }

            public PersistenceSaveStatus Status { get; }
        }
    }
}
