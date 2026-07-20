using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Progression
{
    public interface IProgressionRandomSource
    {
        float Next01();
        int NextInclusive(int minimumInclusive, int maximumInclusive);
    }

    public sealed class SeededProgressionRandomSource : IProgressionRandomSource
    {
        private readonly System.Random random;

        public SeededProgressionRandomSource(int seed)
        {
            random = new System.Random(seed);
        }

        public float Next01()
        {
            return (float)random.NextDouble();
        }

        public int NextInclusive(int minimumInclusive, int maximumInclusive)
        {
            if (maximumInclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return random.Next(minimumInclusive, maximumInclusive + 1);
        }
    }

    public sealed class CharacterOriginGenerationResult
    {
        private CharacterOriginGenerationResult(bool succeeded, string message, OriginFamilyDefinition family, OriginDefinition origin, BirthGiftDefinition birthGift, bool originInfluencedGift, long startingGold)
        {
            Succeeded = succeeded;
            Message = message;
            Family = family;
            Origin = origin;
            BirthGift = birthGift;
            OriginInfluencedGift = originInfluencedGift;
            StartingGold = startingGold;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public OriginFamilyDefinition Family { get; }
        public OriginDefinition Origin { get; }
        public BirthGiftDefinition BirthGift { get; }
        public bool OriginInfluencedGift { get; }
        public long StartingGold { get; }

        public static CharacterOriginGenerationResult Success(OriginFamilyDefinition family, OriginDefinition origin, BirthGiftDefinition birthGift, bool originInfluencedGift, long startingGold)
        {
            return new CharacterOriginGenerationResult(true, "Origin generated.", family, origin, birthGift, originInfluencedGift, startingGold);
        }

        public static CharacterOriginGenerationResult Failure(string message)
        {
            return new CharacterOriginGenerationResult(false, message, null, null, null, false, 0L);
        }
    }

    public sealed class CharacterOriginGenerator
    {
        private readonly DefinitionRegistry registry;
        private readonly IProgressionRandomSource random;
        private readonly float originInfluenceChance;

        public CharacterOriginGenerator(DefinitionRegistry registry, IProgressionRandomSource random, float originInfluenceChance = 0.5f)
        {
            this.registry = registry;
            this.random = random;
            this.originInfluenceChance = Mathf.Clamp01(originInfluenceChance);
        }

        public CharacterOriginGenerationResult Generate()
        {
            if (registry == null)
            {
                return CharacterOriginGenerationResult.Failure("Definition registry is missing.");
            }

            if (random == null)
            {
                return CharacterOriginGenerationResult.Failure("Random source is missing.");
            }

            List<OriginFamilyDefinition> families = registry.DefinitionsById.Values
                .OfType<OriginFamilyDefinition>()
                .Where(family => family.EnabledForAlpha && family.SelectionWeight > 0f)
                .ToList();
            OriginFamilyDefinition selectedFamily = SelectWeighted(families, family => family.SelectionWeight);
            if (selectedFamily == null)
            {
                return CharacterOriginGenerationResult.Failure("No enabled origin families are available.");
            }

            List<OriginDefinition> origins = registry.DefinitionsById.Values
                .OfType<OriginDefinition>()
                .Where(origin => origin.EnabledForAlpha && origin.Family == selectedFamily && origin.SelectionWeight > 0f)
                .ToList();
            OriginDefinition selectedOrigin = SelectWeighted(origins, origin => origin.SelectionWeight);
            if (selectedOrigin == null)
            {
                return CharacterOriginGenerationResult.Failure($"No enabled origins are available for family '{selectedFamily.DisplayName}'.");
            }

            bool originInfluenced = random.Next01() < originInfluenceChance;
            BirthGiftDefinition gift = SelectBirthGift(selectedFamily, selectedOrigin, originInfluenced);
            if (gift == null)
            {
                return CharacterOriginGenerationResult.Failure("No enabled birth gifts are available.");
            }

            long startingGold = RollStartingGold(selectedOrigin);
            return CharacterOriginGenerationResult.Success(selectedFamily, selectedOrigin, gift, originInfluenced, startingGold);
        }

        private BirthGiftDefinition SelectBirthGift(OriginFamilyDefinition family, OriginDefinition origin, bool originInfluenced)
        {
            List<BirthGiftDefinition> gifts = registry.DefinitionsById.Values
                .OfType<BirthGiftDefinition>()
                .Where(gift => gift.EnabledForAlpha && gift.SelectionWeight > 0f)
                .ToList();

            if (gifts.Count == 0)
            {
                return null;
            }

            HashSet<string> influencedPool = originInfluenced
                ? new HashSet<string>(origin.InfluencedGiftPool.Where(gift => gift != null).Select(gift => gift.Id), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            return SelectWeighted(gifts, gift =>
            {
                float weight = gift.SelectionWeight;
                if (originInfluenced && influencedPool.Count > 0)
                {
                    weight *= influencedPool.Contains(gift.Id) ? 2f : 0.75f;
                }

                if (originInfluenced)
                {
                    weight *= ResolveGiftModifier(origin.GiftWeightModifiers, gift.Id);
                }

                if (gift.Rarity != null)
                {
                    weight *= ResolveRarityModifier(family.GiftRarityWeightModifiers, gift.Rarity.Id);
                    weight *= ResolveRarityModifier(origin.GiftRarityWeightModifiers, gift.Rarity.Id);
                }

                return Mathf.Max(0f, weight);
            });
        }

        private long RollStartingGold(OriginDefinition origin)
        {
            ProgressionCurrencyGrantDefinition grant = origin.StartingGold ?? origin.Family?.DefaultStartingMoney;
            if (grant == null)
            {
                return 0L;
            }

            long variation = grant.RandomVariation <= 0L ? 0L : random.NextInclusive(0, (int)Math.Min(int.MaxValue, grant.RandomVariation));
            return Math.Max(0L, grant.BaseAmount + variation);
        }

        private T SelectWeighted<T>(IReadOnlyList<T> values, Func<T, float> weightProvider)
            where T : class
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            float total = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                total += Mathf.Max(0f, weightProvider(values[i]));
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = random.Next01() * total;
            float cumulative = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                cumulative += Mathf.Max(0f, weightProvider(values[i]));
                if (roll <= cumulative)
                {
                    return values[i];
                }
            }

            return values[values.Count - 1];
        }

        private static float ResolveGiftModifier(IReadOnlyList<BirthGiftWeightModifierDefinition> modifiers, string giftId)
        {
            if (modifiers == null)
            {
                return 1f;
            }

            float multiplier = 1f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i]?.Gift != null && string.Equals(modifiers[i].Gift.Id, giftId, StringComparison.Ordinal))
                {
                    multiplier *= modifiers[i].WeightMultiplier;
                }
            }

            return multiplier;
        }

        private static float ResolveRarityModifier(IReadOnlyList<RarityWeightModifierDefinition> modifiers, string rarityId)
        {
            if (modifiers == null)
            {
                return 1f;
            }

            float multiplier = 1f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i]?.Rarity != null && string.Equals(modifiers[i].Rarity.Id, rarityId, StringComparison.Ordinal))
                {
                    multiplier *= modifiers[i].WeightMultiplier;
                }
            }

            return multiplier;
        }
    }
}
