using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    public static class RarityQualityConditionUtility
    {
        public static RarityDefinition GetRarity(IHasRarity definition)
        {
            return definition?.Rarity;
        }

        public static int CompareRarityRank(RarityDefinition left, RarityDefinition right)
        {
            return CompareRanks(left?.Rank, right?.Rank);
        }

        public static int CompareQualityRank(QualityDefinition left, QualityDefinition right)
        {
            return CompareRanks(left?.Rank, right?.Rank);
        }

        public static bool IsUnusable(ConditionDefinition condition)
        {
            return condition != null && condition.IsUnusable;
        }

        public static bool TryResolveCondition(
            IReadOnlyList<ConditionDefinition> conditions,
            float normalizedCondition,
            out ConditionDefinition condition)
        {
            condition = ResolveConditionOrNull(conditions, normalizedCondition);
            return condition != null;
        }

        public static ConditionDefinition ResolveConditionOrNull(
            IReadOnlyList<ConditionDefinition> conditions,
            float normalizedCondition)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return null;
            }

            float clampedValue = Mathf.Clamp01(normalizedCondition);
            ConditionDefinition best = null;

            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionDefinition candidate = conditions[i];
                if (candidate == null)
                {
                    continue;
                }

                bool isLastRange = Mathf.Approximately(candidate.MaximumNormalized, 1f);
                bool inRange = clampedValue >= candidate.MinimumNormalized
                    && (clampedValue < candidate.MaximumNormalized || (isLastRange && clampedValue <= candidate.MaximumNormalized));

                if (!inRange)
                {
                    continue;
                }

                if (best == null || candidate.Rank < best.Rank)
                {
                    best = candidate;
                }
            }

            return best;
        }

        public static bool CanShareDefinitionOnlyStack(IInventoryItemDefinition existing, IInventoryItemDefinition candidate)
        {
            return existing != null
                && candidate != null
                && ReferenceEquals(existing, candidate)
                && existing.Stackable;
        }

        private static int CompareRanks(int? leftRank, int? rightRank)
        {
            if (!leftRank.HasValue && !rightRank.HasValue)
            {
                return 0;
            }

            if (!leftRank.HasValue)
            {
                return -1;
            }

            if (!rightRank.HasValue)
            {
                return 1;
            }

            return leftRank.Value.CompareTo(rightRank.Value);
        }
    }
}
