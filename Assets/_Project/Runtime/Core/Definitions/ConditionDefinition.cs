using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    [CreateAssetMenu(fileName = "Condition", menuName = "Unity Isekai Game/Game Data/Condition")]
    public sealed class ConditionDefinition : ScriptableObject, IGameDefinition
    {
        [SerializeField] private string conditionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int rank;
        [SerializeField, Range(0f, 1f)] private float minimumNormalized;
        [SerializeField, Range(0f, 1f)] private float maximumNormalized = 1f;
        [SerializeField] private bool unusable;
        [SerializeField] private bool defaultCondition;

        public string ConditionId => conditionId;
        public string Id => conditionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public int Rank => rank;
        public float MinimumNormalized => minimumNormalized;
        public float MaximumNormalized => maximumNormalized;
        public bool IsUnusable => unusable;
        public bool IsDefault => defaultCondition;

        private void OnValidate()
        {
            rank = Mathf.Max(0, rank);
            minimumNormalized = Mathf.Clamp01(minimumNormalized);
            maximumNormalized = Mathf.Clamp01(maximumNormalized);
        }
    }
}
