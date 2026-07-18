namespace UnityIsekaiGame.Dialogue
{
    public readonly struct DialogueOperationResult
    {
        private DialogueOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static DialogueOperationResult Success(string message)
        {
            return new DialogueOperationResult(true, message);
        }

        public static DialogueOperationResult Failure(string message)
        {
            return new DialogueOperationResult(false, message);
        }
    }
}
