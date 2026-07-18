using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    [CreateAssetMenu(fileName = "Contract", menuName = "Unity Isekai Game/Contracts/Contract")]
    public sealed class ContractDefinition : ScriptableObject
    {
        [SerializeField] private string contractId;
        [SerializeField] private string displayTitle;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string requesterName;
        [SerializeField] private string recommendedRank;
        [SerializeField] private ContractObjectiveDefinition[] objectives;
        [SerializeField] private ContractRewardDefinition reward;
        [SerializeField] private string expirationPlaceholder;

        public string ContractId => contractId;
        public string DisplayTitle => displayTitle;
        public string Description => description;
        public string RequesterName => requesterName;
        public string RecommendedRank => recommendedRank;
        public IReadOnlyList<ContractObjectiveDefinition> Objectives => objectives ?? System.Array.Empty<ContractObjectiveDefinition>();
        public ContractRewardDefinition Reward => reward;
        public string ExpirationPlaceholder => expirationPlaceholder;
    }
}
