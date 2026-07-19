using System;
using System.Collections.Generic;

namespace UnityIsekaiGame.GameData.Persistence
{
    public enum PersistenceLoadPhase
    {
        Bootstrap = 0,
        ActorBase = 100,
        Inventory = 200,
        Equipment = 300,
        Statuses = 400,
        Vitals = 500,
        QuestsAndContracts = 600,
        PositionAndPlace = 700,
        Notification = 800,
        Prototype = 900
    }

    public enum PersistenceScope
    {
        Player = 0,
        SharedWorld = 100,
        RegionOrScene = 200,
        Account = 300,
        SessionOnly = 400
    }

    public enum PersistenceSaveStatus
    {
        Success,
        OperationAlreadyRunning,
        InvalidSlotId,
        NoParticipants,
        ParticipantCaptureFailed,
        ParticipantValidationFailed,
        SerializationFailed,
        DirectoryCreationFailed,
        TemporaryWriteFailed,
        BackupFailed,
        ReplacementFailed,
        UnknownException
    }

    public enum PersistenceLoadStatus
    {
        Success,
        OperationAlreadyRunning,
        InvalidSlotId,
        FileMissing,
        ReadFailed,
        MalformedJson,
        WrongFormatIdentifier,
        UnsupportedSchemaVersion,
        ChecksumMismatch,
        DuplicateParticipantKey,
        MissingRequiredParticipantPayload,
        MissingRuntimeParticipant,
        ParticipantPayloadParseFailed,
        ParticipantPrepareFailed,
        ParticipantCommitFailed,
        BackupAvailable,
        BackupLoadFailed,
        UnknownException
    }

    public enum PersistenceDeleteStatus
    {
        Success,
        OperationAlreadyRunning,
        InvalidSlotId,
        DeleteFailed,
        UnknownException
    }

    public enum PersistenceValidationStatus
    {
        Valid,
        InvalidSlotId,
        FileMissing,
        ReadFailed,
        MalformedJson,
        WrongFormatIdentifier,
        UnsupportedSchemaVersion,
        ChecksumMismatch,
        DuplicateParticipantKey,
        MissingRequiredParticipantPayload,
        MissingRuntimeParticipant,
        ParticipantPayloadParseFailed,
        ParticipantPrepareFailed,
        BackupAvailable,
        UnknownException
    }

    [Serializable]
    public sealed class GameSaveEnvelope
    {
        public string formatIdentifier;
        public int schemaVersion;
        public string gameVersion;
        public string saveId;
        public string slotId;
        public string displayName;
        public string worldId;
        public string playerId;
        public string accountId;
        public string createdUtc;
        public string lastWrittenUtc;
        public double playtimeSeconds;
        public string sceneSummary;
        public string placeSummary;
        public string playerSummary;
        public string contentChecksum;
        public List<SaveParticipantRecord> participants = new List<SaveParticipantRecord>();
    }

    [Serializable]
    public sealed class SaveParticipantRecord
    {
        public string participantKey;
        public int participantSchemaVersion;
        public bool required;
        public int persistenceScope;
        public string ownerId;
        public int loadPhase;
        public int loadPriority;
        public string payloadJson;
    }

    [Serializable]
    public sealed class SaveSlotMetadata
    {
        public string slotId;
        public string displayName;
        public string saveId;
        public string createdUtc;
        public string modifiedUtc;
        public double playtimeSeconds;
        public string currentPlaceSummary;
        public string playerSummary;
        public string worldId;
        public string playerId;
        public string accountId;
        public int schemaVersion;
        public string gameVersion;
        public long fileSizeBytes;
        public bool hasPrimary;
        public bool hasBackup;
        public bool isValid;
        public string status;
        public string message;
    }

    public sealed class PersistenceSaveResult
    {
        private PersistenceSaveResult(bool succeeded, PersistenceSaveStatus status, string slotId, string path, string message, Exception exception)
        {
            Succeeded = succeeded;
            Status = status;
            SlotId = slotId;
            Path = path;
            Message = message;
            Exception = exception;
        }

        public bool Succeeded { get; }
        public PersistenceSaveStatus Status { get; }
        public string SlotId { get; }
        public string Path { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public static PersistenceSaveResult Success(string slotId, string path, string message)
        {
            return new PersistenceSaveResult(true, PersistenceSaveStatus.Success, slotId, path, message, null);
        }

        public static PersistenceSaveResult Failure(PersistenceSaveStatus status, string slotId, string path, string message, Exception exception = null)
        {
            return new PersistenceSaveResult(false, status, slotId, path, message, exception);
        }
    }

    public sealed class PersistenceLoadResult
    {
        private PersistenceLoadResult(bool succeeded, PersistenceLoadStatus status, string slotId, string path, bool loadedBackup, bool backupAvailable, string message, Exception exception)
        {
            Succeeded = succeeded;
            Status = status;
            SlotId = slotId;
            Path = path;
            LoadedBackup = loadedBackup;
            BackupAvailable = backupAvailable;
            Message = message;
            Exception = exception;
        }

        public bool Succeeded { get; }
        public PersistenceLoadStatus Status { get; }
        public string SlotId { get; }
        public string Path { get; }
        public bool LoadedBackup { get; }
        public bool BackupAvailable { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public static PersistenceLoadResult Success(string slotId, string path, bool loadedBackup, string message)
        {
            return new PersistenceLoadResult(true, PersistenceLoadStatus.Success, slotId, path, loadedBackup, false, message, null);
        }

        public static PersistenceLoadResult Failure(PersistenceLoadStatus status, string slotId, string path, string message, bool backupAvailable = false, Exception exception = null)
        {
            return new PersistenceLoadResult(false, status, slotId, path, false, backupAvailable, message, exception);
        }
    }

    public sealed class PersistenceDeleteResult
    {
        private PersistenceDeleteResult(bool succeeded, PersistenceDeleteStatus status, string slotId, string message, Exception exception)
        {
            Succeeded = succeeded;
            Status = status;
            SlotId = slotId;
            Message = message;
            Exception = exception;
        }

        public bool Succeeded { get; }
        public PersistenceDeleteStatus Status { get; }
        public string SlotId { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public static PersistenceDeleteResult Success(string slotId, string message)
        {
            return new PersistenceDeleteResult(true, PersistenceDeleteStatus.Success, slotId, message, null);
        }

        public static PersistenceDeleteResult Failure(PersistenceDeleteStatus status, string slotId, string message, Exception exception = null)
        {
            return new PersistenceDeleteResult(false, status, slotId, message, exception);
        }
    }

    public sealed class PersistenceValidationResult
    {
        private PersistenceValidationResult(bool succeeded, PersistenceValidationStatus status, string slotId, string path, bool backupAvailable, GameSaveEnvelope envelope, string message, Exception exception)
        {
            Succeeded = succeeded;
            Status = status;
            SlotId = slotId;
            Path = path;
            BackupAvailable = backupAvailable;
            Envelope = envelope;
            Message = message;
            Exception = exception;
        }

        public bool Succeeded { get; }
        public PersistenceValidationStatus Status { get; }
        public string SlotId { get; }
        public string Path { get; }
        public bool BackupAvailable { get; }
        public GameSaveEnvelope Envelope { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public static PersistenceValidationResult Success(string slotId, string path, GameSaveEnvelope envelope, string message)
        {
            return new PersistenceValidationResult(true, PersistenceValidationStatus.Valid, slotId, path, false, envelope, message, null);
        }

        public static PersistenceValidationResult Failure(PersistenceValidationStatus status, string slotId, string path, string message, bool backupAvailable = false, Exception exception = null)
        {
            return new PersistenceValidationResult(false, status, slotId, path, backupAvailable, null, message, exception);
        }
    }
}
