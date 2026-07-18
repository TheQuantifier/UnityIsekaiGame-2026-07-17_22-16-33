using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    [Serializable]
    public sealed class ContractRewardDefinition
    {
        [SerializeField] private ContractRewardItem[] itemRewards;

        public IReadOnlyList<ContractRewardItem> ItemRewards => itemRewards ?? Array.Empty<ContractRewardItem>();

        public string GetSummary()
        {
            if (ItemRewards.Count == 0)
            {
                return "No reward";
            }

            string[] parts = new string[ItemRewards.Count];
            for (int i = 0; i < ItemRewards.Count; i++)
            {
                parts[i] = ItemRewards[i] == null ? "Missing reward" : ItemRewards[i].Description;
            }

            return string.Join(", ", parts);
        }
    }
}
