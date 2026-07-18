using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public static class DamageCalculator
    {
        public static float CalculateAppliedDamage(float rawDamage, float defense, float minimumDamage = 1f)
        {
            if (rawDamage <= 0f)
            {
                return 0f;
            }

            // Prototype formula: subtract flat defense, but any valid hit still deals at least 1 damage.
            return Mathf.Max(minimumDamage, rawDamage - Mathf.Max(0f, defense));
        }
    }
}
