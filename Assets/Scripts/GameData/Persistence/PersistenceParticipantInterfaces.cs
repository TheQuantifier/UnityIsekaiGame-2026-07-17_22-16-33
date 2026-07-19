namespace UnityIsekaiGame.GameData.Persistence
{
    public interface IPersistenceParticipant
    {
        string ParticipantKey { get; }
        int ParticipantSchemaVersion { get; }
        bool IsRequired { get; }
        PersistenceScope Scope { get; }
        string OwnerId { get; }
        PersistenceLoadPhase LoadPhase { get; }
        int LoadPriority { get; }

        PersistenceParticipantSaveResult CapturePayload();
        PersistenceParticipantPrepareResult PreparePayload(string payloadJson, int payloadSchemaVersion);
        PersistenceParticipantCommitResult CommitPreparedPayload(object preparedPayload);
        void DiscardPreparedPayload(object preparedPayload);
    }

    public sealed class PersistenceParticipantSaveResult
    {
        private PersistenceParticipantSaveResult(bool succeeded, string payloadJson, string message)
        {
            Succeeded = succeeded;
            PayloadJson = payloadJson;
            Message = message;
        }

        public bool Succeeded { get; }
        public string PayloadJson { get; }
        public string Message { get; }

        public static PersistenceParticipantSaveResult Success(string payloadJson)
        {
            return new PersistenceParticipantSaveResult(true, payloadJson, "Participant payload captured.");
        }

        public static PersistenceParticipantSaveResult Failure(string message)
        {
            return new PersistenceParticipantSaveResult(false, string.Empty, message);
        }
    }

    public sealed class PersistenceParticipantPrepareResult
    {
        private PersistenceParticipantPrepareResult(bool succeeded, object preparedPayload, string message)
        {
            Succeeded = succeeded;
            PreparedPayload = preparedPayload;
            Message = message;
        }

        public bool Succeeded { get; }
        public object PreparedPayload { get; }
        public string Message { get; }

        public static PersistenceParticipantPrepareResult Success(object preparedPayload)
        {
            return new PersistenceParticipantPrepareResult(true, preparedPayload, "Participant payload prepared.");
        }

        public static PersistenceParticipantPrepareResult Failure(string message)
        {
            return new PersistenceParticipantPrepareResult(false, null, message);
        }
    }

    public sealed class PersistenceParticipantCommitResult
    {
        private PersistenceParticipantCommitResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static PersistenceParticipantCommitResult Success(string message = "Participant payload committed.")
        {
            return new PersistenceParticipantCommitResult(true, message);
        }

        public static PersistenceParticipantCommitResult Failure(string message)
        {
            return new PersistenceParticipantCommitResult(false, message);
        }
    }
}
