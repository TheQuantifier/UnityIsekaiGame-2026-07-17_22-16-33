namespace UnityIsekaiGame.Quests
{
    public readonly struct QuestOperationResult
    {
        private QuestOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static QuestOperationResult Success(string message)
        {
            return new QuestOperationResult(true, message);
        }

        public static QuestOperationResult Failure(string message)
        {
            return new QuestOperationResult(false, message);
        }
    }
}
