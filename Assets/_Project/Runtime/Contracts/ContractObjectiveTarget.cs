using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractObjectiveTarget : MonoBehaviour
    {
        [SerializeField] private string targetCategory;

        public string TargetCategory => targetCategory;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DevelopmentSetTargetCategory(string value)
        {
            targetCategory = value;
        }
#endif
    }
}
