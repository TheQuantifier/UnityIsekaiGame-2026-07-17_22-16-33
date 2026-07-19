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

    public enum SaveSlotKind
    {
        Manual = 0,
        Autosave = 100,
        Quicksave = 200,
        Recovery = 300,
        Development = 400
    }

    public enum SaveCompatibilityStatus
    {
        Compatible = 0,
        OlderSupported = 100,
        MigrationRequired = 200,
        FutureVersion = 300,
        MissingContent = 400,
        WrongWorld = 500,
        WrongPlayer = 600,
        Corrupted = 700,
        Empty = 800,
        Unknown = 900
    }

    public enum PersistenceOperationState
    {
        Idle = 0,
        Capturing = 100,
        Validating = 200,
        Writing = 300,
        RotatingAutosaves = 400,
        PreparingLoad = 500,
        CommittingParticipants = 600,
        RecoveringBackup = 700,
        Deleting = 800,
        ValidatingSlot = 900,
        Failed = 1000
    }

    public enum SaveEligibilityStatus
    {
        Allowed = 0,
        OperationInProgress = 100,
        InvalidPlayerState = 200,
        SceneTransition = 300,
        NoActivePlayer = 400,
        ModalBlocked = 500,
        ParticipantCaptureFailed = 600,
        Unknown = 900
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
        public string lastWrittenUtc;
        public string modifiedUtc;
        public double playtimeSeconds;
        public string sceneSummary;
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

    [Serializable]
    public sealed class SaveSlotDescriptor
    {
        public string slotId;
        public SaveSlotKind slotKind;
        public string displayName;
        public bool exists;
        public bool isValid;
        public bool primaryExists;
        public bool backupExists;
        public int schemaVersion;
        public string createdAtUtc;
        public string lastSavedAtUtc;
        public double playTimeSeconds;
        public string sceneKey;
        public string placeId;
        public string placeDisplayName;
        public string playerDisplayName;
        public string currentHealthSummary;
        public PersistenceValidationStatus validationStatus;
        public SaveCompatibilityStatus compatibilityStatus;
        public string lastErrorCode;
        public string message;
        public long fileSizeBytes;
        public int saveGeneration;
        public bool isNewestAutosave;

        public bool CanSave => slotKind == SaveSlotKind.Manual;
        public bool CanLoad => exists && isValid && compatibilityStatus == SaveCompatibilityStatus.Compatible;
        public bool CanDelete => slotKind == SaveSlotKind.Manual && exists;
        public bool CanValidate => exists || backupExists;
        public bool CanLoadBackup => backupExists;
    }

    public readonly struct SaveEligibilityResult
    {
        private SaveEligibilityResult(bool allowed, SaveEligibilityStatus status, string message)
        {
            Allowed = allowed;
            Status = status;
            Message = string.IsNullOrWhiteSpace(message) ? status.ToString() : message;
        }

        public bool Allowed { get; }
        public SaveEligibilityStatus Status { get; }
        public string Message { get; }

        public static SaveEligibilityResult Allow(string message = "Saving is allowed.")
        {
            return new SaveEligibilityResult(true, SaveEligibilityStatus.Allowed, message);
        }

        public static SaveEligibilityResult Block(SaveEligibilityStatus status, string message)
        {
            return new SaveEligibilityResult(false, status, message);
        }
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
