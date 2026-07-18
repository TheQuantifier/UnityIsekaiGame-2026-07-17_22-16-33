using System;

namespace UnityIsekaiGame.GameData
{
    [Serializable]
    public sealed class ItemInstanceSaveData
    {
        public string definitionId;
        public string instanceId;
        public bool hasQuality;
        public string qualityId;
        public bool hasCondition;
        public float conditionNormalized;
    }
}
