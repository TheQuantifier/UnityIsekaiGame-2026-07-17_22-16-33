namespace UnityIsekaiGame.Contracts
{
    public readonly struct ContractOperationResult
    {
        private ContractOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static ContractOperationResult Success(string message)
        {
            return new ContractOperationResult(true, message);
        }

        public static ContractOperationResult Failure(string message)
        {
            return new ContractOperationResult(false, message);
        }
    }
}
