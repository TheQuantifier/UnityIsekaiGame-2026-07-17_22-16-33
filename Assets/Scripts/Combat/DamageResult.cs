using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(bool applied, float requestedAmount, float appliedAmount, bool defeated, string message)
            : this(applied, requestedAmount, requestedAmount, 0f, Mathf.Max(0f, requestedAmount - appliedAmount), appliedAmount, 0f, defeated, message)
        {
        }

        public DamageResult(
            bool applied,
            float requestedAmount,
            float preMitigationAmount,
            float defense,
            float mitigatedAmount,
            float appliedAmount,
            float remainingHealth,
            bool defeated,
            string message)
        {
            Applied = applied;
            RequestedAmount = requestedAmount;
            PreMitigationAmount = preMitigationAmount;
            Defense = defense;
            MitigatedAmount = mitigatedAmount;
            AppliedAmount = appliedAmount;
            RemainingHealth = remainingHealth;
            Defeated = defeated;
            Message = message;
        }

        public bool Applied { get; }
        public float RequestedAmount { get; }
        public float PreMitigationAmount { get; }
        public float Defense { get; }
        public float MitigatedAmount { get; }
        public float AppliedAmount { get; }
        public float RemainingHealth { get; }
        public bool Defeated { get; }
        public string Message { get; }

        public static DamageResult Success(float requestedAmount, float appliedAmount, bool defeated, string message)
        {
            return new DamageResult(true, requestedAmount, appliedAmount, defeated, message);
        }

        public static DamageResult Success(
            float requestedAmount,
            DamageCalculation calculation,
            float appliedAmount,
            float remainingHealth,
            bool defeated,
            string message)
        {
            return new DamageResult(
                true,
                requestedAmount,
                calculation.PreMitigationAmount,
                calculation.Defense,
                calculation.MitigatedAmount,
                appliedAmount,
                remainingHealth,
                defeated,
                message);
        }

        public static DamageResult Failure(float requestedAmount, string message)
        {
            return new DamageResult(false, requestedAmount, 0f, false, message);
        }
    }
}
