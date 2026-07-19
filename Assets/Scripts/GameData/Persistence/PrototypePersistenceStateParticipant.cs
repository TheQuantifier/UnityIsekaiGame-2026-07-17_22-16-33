using UnityEngine;

namespace UnityIsekaiGame.GameData.Persistence
{
    public sealed class PrototypePersistenceStateParticipant : IPersistenceParticipant
    {
        public const string Key = "prototype.state";
        public const int CurrentParticipantSchemaVersion = 1;

        private readonly IPrototypePersistenceState state;

        public PrototypePersistenceStateParticipant(IPrototypePersistenceState state)
        {
            this.state = state;
        }

        public string ParticipantKey => Key;
        public int ParticipantSchemaVersion => CurrentParticipantSchemaVersion;
        public bool IsRequired => true;
        public PersistenceScope Scope => PersistenceScope.Player;
        public string OwnerId => PersistenceService.LocalPlayerId;
        public PersistenceLoadPhase LoadPhase => PersistenceLoadPhase.Prototype;
        public int LoadPriority => 0;

        public PersistenceParticipantSaveResult CapturePayload()
        {
            if (state == null)
            {
                return PersistenceParticipantSaveResult.Failure("Prototype persistence state is missing.");
            }

            PrototypePersistenceStateSaveData saveData = state.CreateSaveData();
            if (saveData == null)
            {
                return PersistenceParticipantSaveResult.Failure("Prototype persistence state returned no save data.");
            }

            return PersistenceParticipantSaveResult.Success(JsonUtility.ToJson(saveData));
        }

        public PersistenceParticipantPrepareResult PreparePayload(string payloadJson, int payloadSchemaVersion)
        {
            if (payloadSchemaVersion != CurrentParticipantSchemaVersion)
            {
                return PersistenceParticipantPrepareResult.Failure($"Unsupported prototype participant schema version {payloadSchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return PersistenceParticipantPrepareResult.Failure("Prototype payload is empty.");
            }

            PrototypePersistenceStateSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<PrototypePersistenceStateSaveData>(payloadJson);
            }
            catch
            {
                return PersistenceParticipantPrepareResult.Failure("Prototype payload is malformed JSON.");
            }

            if (saveData == null)
            {
                return PersistenceParticipantPrepareResult.Failure("Prototype payload did not parse.");
            }

            if (saveData.testValue < -999999 || saveData.testValue > 999999)
            {
                return PersistenceParticipantPrepareResult.Failure("Prototype test value is outside the supported range.");
            }

            if (saveData.note != null && saveData.note.Length > 256)
            {
                return PersistenceParticipantPrepareResult.Failure("Prototype note is too long.");
            }

            return PersistenceParticipantPrepareResult.Success(saveData);
        }

        public PersistenceParticipantCommitResult CommitPreparedPayload(object preparedPayload)
        {
            if (state == null)
            {
                return PersistenceParticipantCommitResult.Failure("Prototype persistence state is missing.");
            }

            if (preparedPayload is not PrototypePersistenceStateSaveData saveData)
            {
                return PersistenceParticipantCommitResult.Failure("Prepared prototype payload has the wrong type.");
            }

            state.RestoreFromSaveData(saveData);
            return PersistenceParticipantCommitResult.Success("Prototype persistence state restored.");
        }

        public void DiscardPreparedPayload(object preparedPayload)
        {
        }
    }

    public interface IPrototypePersistenceState
    {
        PrototypePersistenceStateSaveData CreateSaveData();
        void RestoreFromSaveData(PrototypePersistenceStateSaveData saveData);
    }
}
