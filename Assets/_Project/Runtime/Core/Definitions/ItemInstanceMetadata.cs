using System;
using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    [Serializable]
    public sealed class ItemInstanceMetadata
    {
        [SerializeField] private QualityDefinition quality;
        [SerializeField] private bool hasCondition;
        [SerializeField, Range(0f, 1f)] private float conditionNormalized = 1f;

        public ItemInstanceMetadata()
        {
        }

        public ItemInstanceMetadata(QualityDefinition quality, float? conditionNormalized = null)
        {
            this.quality = quality;
            hasCondition = conditionNormalized.HasValue;
            this.conditionNormalized = Mathf.Clamp01(conditionNormalized.GetValueOrDefault(1f));
        }

        public QualityDefinition Quality => quality;
        public bool HasQuality => quality != null;
        public bool HasCondition => hasCondition;
        public float ConditionNormalized => Mathf.Clamp01(conditionNormalized);
        public bool HasAnyState => HasQuality || HasCondition;

        public ItemInstanceMetadata Clone()
        {
            return new ItemInstanceMetadata(quality, hasCondition ? ConditionNormalized : null);
        }

        public ItemInstanceMetadata WithQualityValue(QualityDefinition newQuality)
        {
            return new ItemInstanceMetadata(newQuality, hasCondition ? ConditionNormalized : null);
        }

        public ItemInstanceMetadata WithConditionValue(float newConditionNormalized)
        {
            return new ItemInstanceMetadata(quality, newConditionNormalized);
        }

        public ItemInstanceMetadata WithoutCondition()
        {
            return new ItemInstanceMetadata(quality);
        }

        public static bool AreEquivalentForStacking(ItemInstanceMetadata left, ItemInstanceMetadata right)
        {
            bool leftHasState = left != null && left.HasAnyState;
            bool rightHasState = right != null && right.HasAnyState;

            if (!leftHasState && !rightHasState)
            {
                return true;
            }

            if (leftHasState != rightHasState)
            {
                return false;
            }

            return ReferenceEquals(left.Quality, right.Quality)
                && left.HasCondition == right.HasCondition
                && (!left.HasCondition || left.ConditionNormalized.Equals(right.ConditionNormalized));
        }

        public static ItemInstanceMetadata WithoutInstanceState()
        {
            return new ItemInstanceMetadata();
        }

        public static ItemInstanceMetadata WithQuality(QualityDefinition quality)
        {
            return new ItemInstanceMetadata(quality);
        }

        public static ItemInstanceMetadata WithCondition(float conditionNormalized)
        {
            return new ItemInstanceMetadata(null, conditionNormalized);
        }

        public static ItemInstanceMetadata WithQualityAndCondition(QualityDefinition quality, float conditionNormalized)
        {
            return new ItemInstanceMetadata(quality, conditionNormalized);
        }
    }
}
