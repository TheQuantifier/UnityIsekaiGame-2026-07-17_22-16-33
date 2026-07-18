using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    public sealed class PlayerContractJournal : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField, Min(1)] private int activeContractLimit = 6;

        private readonly List<ContractInstance> contracts = new List<ContractInstance>();

        public IReadOnlyList<ContractInstance> Contracts => contracts;
        public event Action JournalChanged;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }
        }

        private void OnDestroy()
        {
            foreach (ContractInstance contract in contracts)
            {
                Unsubscribe(contract);
                contract.Dispose();
            }
        }

        public ContractOperationResult AcceptContract(ContractDefinition definition)
        {
            if (definition == null)
            {
                return ContractOperationResult.Failure("Invalid contract.");
            }

            if (string.IsNullOrWhiteSpace(definition.ContractId))
            {
                return ContractOperationResult.Failure("Contract is missing a stable ID.");
            }

            if (FindContract(definition.ContractId) != null)
            {
                return ContractOperationResult.Failure($"{definition.DisplayTitle} is already in the journal.");
            }

            if (GetActiveCount() >= activeContractLimit)
            {
                return ContractOperationResult.Failure("Active contract limit reached.");
            }

            ContractInstance contract = new ContractInstance(definition, new ContractObjectiveContext(inventory));
            Subscribe(contract);
            contracts.Add(contract);
            contract.Activate();
            JournalChanged?.Invoke();
            PrototypeHudMessageBus.Show($"Accepted contract: {definition.DisplayTitle}");
            return ContractOperationResult.Success($"Accepted contract: {definition.DisplayTitle}.");
        }

        public ContractOperationResult AbandonContract(ContractInstance contract)
        {
            if (contract == null || !contracts.Contains(contract))
            {
                return ContractOperationResult.Failure("No contract selected.");
            }

            ContractOperationResult result = contract.Abandon();
            JournalChanged?.Invoke();
            return result;
        }

        public ContractOperationResult ClaimReward(ContractInstance contract)
        {
            if (contract == null || !contracts.Contains(contract))
            {
                return ContractOperationResult.Failure("No contract selected.");
            }

            ContractOperationResult result = contract.ClaimReward();
            if (result.Succeeded)
            {
                PrototypeHudMessageBus.Show(result.Message);
            }

            JournalChanged?.Invoke();
            return result;
        }

        public ContractOperationResult DeliverTo(string destinationId)
        {
            foreach (ContractInstance contract in contracts)
            {
                if (!contract.IsActive)
                {
                    continue;
                }

                ContractOperationResult result = contract.TryDeliver(destinationId);
                if (result.Succeeded)
                {
                    JournalChanged?.Invoke();
                    PrototypeHudMessageBus.Show(result.Message);
                    return result;
                }
            }

            return ContractOperationResult.Failure("No active contract needs this delivery.");
        }

        public void RecordDefeat(ContractObjectiveTarget target)
        {
            if (target == null)
            {
                return;
            }

            foreach (ContractInstance contract in contracts)
            {
                contract.RecordDefeat(target.TargetCategory);
            }

            JournalChanged?.Invoke();
        }

        public ContractInstance FindContract(string contractId)
        {
            foreach (ContractInstance contract in contracts)
            {
                if (contract.Definition != null && contract.Definition.ContractId == contractId)
                {
                    return contract;
                }
            }

            return null;
        }

        private int GetActiveCount()
        {
            int count = 0;
            foreach (ContractInstance contract in contracts)
            {
                if (contract.IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        private void Subscribe(ContractInstance contract)
        {
            contract.ProgressChanged += OnContractChanged;
            contract.Completed += OnContractCompleted;
            contract.Failed += OnContractChanged;
            contract.Abandoned += OnContractChanged;
            contract.RewardClaimed += OnContractChanged;
        }

        private void Unsubscribe(ContractInstance contract)
        {
            contract.ProgressChanged -= OnContractChanged;
            contract.Completed -= OnContractCompleted;
            contract.Failed -= OnContractChanged;
            contract.Abandoned -= OnContractChanged;
            contract.RewardClaimed -= OnContractChanged;
        }

        private void OnContractChanged(ContractInstance contract)
        {
            JournalChanged?.Invoke();
        }

        private void OnContractCompleted(ContractInstance contract)
        {
            PrototypeHudMessageBus.Show($"Contract complete: {contract.Definition.DisplayTitle}");
            JournalChanged?.Invoke();
        }
    }
}
