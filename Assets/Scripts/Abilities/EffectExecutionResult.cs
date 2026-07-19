namespace UnityIsekaiGame.Abilities
{
    public readonly struct EffectExecutionResult
    {
        private EffectExecutionResult(EffectExecutionStatus status, string message, float appliedMagnitude)
        {
            Status = status;
            Message = message;
            AppliedMagnitude = appliedMagnitude;
        }

        public EffectExecutionStatus Status { get; }
        public string Message { get; }
        public float AppliedMagnitude { get; }
        public bool Succeeded => Status == EffectExecutionStatus.Success;

        public static EffectExecutionResult Success(string message, float appliedMagnitude = 0f)
        {
            return new EffectExecutionResult(EffectExecutionStatus.Success, message, appliedMagnitude);
        }

        public static EffectExecutionResult Failure(EffectExecutionStatus status, string message)
        {
            return new EffectExecutionResult(status, message, 0f);
        }
    }
}
