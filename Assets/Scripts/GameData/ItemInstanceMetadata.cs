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
