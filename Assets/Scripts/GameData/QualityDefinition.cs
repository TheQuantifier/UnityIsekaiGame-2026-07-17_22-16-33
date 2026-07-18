using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    [CreateAssetMenu(fileName = "Quality", menuName = "Unity Isekai Game/Game Data/Quality")]
    public sealed class QualityDefinition : ScriptableObject, IGameDefinition
    {
        [SerializeField] private string qualityId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int rank;
        [SerializeField] private bool defaultQuality;

        public string QualityId => qualityId;
        public string Id => qualityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public int Rank => rank;
        public bool IsDefault => defaultQuality;

        private void OnValidate()
        {
            rank = Mathf.Max(0, rank);
        }
    }
}
