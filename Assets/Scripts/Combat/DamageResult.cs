namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(bool applied, float requestedAmount, float appliedAmount, bool defeated, string message)
        {
            Applied = applied;
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            Defeated = defeated;
            Message = message;
        }

        public bool Applied { get; }
        public float RequestedAmount { get; }
        public float AppliedAmount { get; }
        public bool Defeated { get; }
        public string Message { get; }

        public static DamageResult Success(float requestedAmount, float appliedAmount, bool defeated, string message)
        {
            return new DamageResult(true, requestedAmount, appliedAmount, defeated, message);
        }

        public static DamageResult Failure(float requestedAmount, string message)
        {
            return new DamageResult(false, requestedAmount, 0f, false, message);
        }
    }
}
