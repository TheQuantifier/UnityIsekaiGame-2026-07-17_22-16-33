using System;
using UnityEngine;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    [Serializable]
    public sealed class ContractRewardItem
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemDefinition Item => item;
        public int Quantity => Mathf.Max(1, quantity);
        public string Description => item == null ? "Missing item reward" : $"{Quantity} x {item.DisplayName}";
    }
}
