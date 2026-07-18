using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractObjectiveTarget : MonoBehaviour
    {
        [SerializeField] private string targetCategory;

        public string TargetCategory => targetCategory;
    }
}
