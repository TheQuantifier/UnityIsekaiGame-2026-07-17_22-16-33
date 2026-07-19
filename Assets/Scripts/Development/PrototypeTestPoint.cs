#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestPoint : MonoBehaviour
    {
        [SerializeField] private string testPointId;
        [SerializeField] private string displayName;

        public string TestPointId => string.IsNullOrWhiteSpace(testPointId) ? name : testPointId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TestPointId : displayName;
    }
}
#endif
