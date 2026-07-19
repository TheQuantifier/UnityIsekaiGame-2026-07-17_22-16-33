using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public static class DamageCalculator
    {
        public const float DefaultMinimumDamage = 1f;

        public static float CalculateAppliedDamage(float rawDamage, float defense, float minimumDamage = 1f)
        {
            if (rawDamage <= 0f)
            {
                return 0f;
            }

            // Prototype formula: subtract flat defense, but any valid hit still deals at least 1 damage.
            return Mathf.Max(minimumDamage, rawDamage - Mathf.Max(0f, defense));
        }

        public static DamageCalculation Calculate(float rawDamage, float defense, float minimumDamage = DefaultMinimumDamage)
        {
            float preMitigationAmount = Mathf.Max(0f, rawDamage);
            float clampedDefense = Mathf.Max(0f, defense);
            float finalAmount = CalculateAppliedDamage(preMitigationAmount, clampedDefense, minimumDamage);
            float mitigatedAmount = Mathf.Max(0f, preMitigationAmount - finalAmount);
            return new DamageCalculation(preMitigationAmount, clampedDefense, mitigatedAmount, finalAmount);
        }
    }
}
