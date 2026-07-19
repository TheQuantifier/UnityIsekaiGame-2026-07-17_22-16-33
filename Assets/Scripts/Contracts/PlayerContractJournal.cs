using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

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

        public List<ContractInstanceSaveData> CreateSaveData()
        {
            List<ContractInstanceSaveData> saveData = new List<ContractInstanceSaveData>(contracts.Count);
            for (int i = 0; i < contracts.Count; i++)
            {
                saveData.Add(contracts[i].CreateSaveData());
            }

            return saveData;
        }

        public bool TryRestoreFromSaveData(IReadOnlyList<ContractInstanceSaveData> saveData, DefinitionRegistry registry, out string failureReason)
        {
            failureReason = string.Empty;
            if (registry == null)
            {
                failureReason = "Definition registry is missing for contract restore.";
                return false;
            }

            List<ContractInstance> restored = new List<ContractInstance>();
            HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> contractIds = new HashSet<string>(StringComparer.Ordinal);
            int activeCount = 0;
            IReadOnlyList<ContractInstanceSaveData> entries = saveData ?? Array.Empty<ContractInstanceSaveData>();

            for (int i = 0; i < entries.Count; i++)
            {
                ContractInstanceSaveData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.contractDefinitionId))
                {
                    failureReason = "Contract save entry is missing a contract definition ID.";
                    DisposeAll(restored);
                    return false;
                }

                if (!runtimeIds.Add(entry.runtimeInstanceId))
                {
                    failureReason = $"Duplicate contract runtime instance ID '{entry.runtimeInstanceId}'.";
                    DisposeAll(restored);
                    return false;
                }

                if (!registry.TryGet(entry.contractDefinitionId, out ContractDefinition definition))
                {
                    failureReason = $"Contract definition '{entry.contractDefinitionId}' was not found.";
                    DisposeAll(restored);
                    return false;
                }

                if (!contractIds.Add(definition.ContractId))
                {
                    failureReason = $"Duplicate contract definition '{definition.ContractId}' in save data.";
                    DisposeAll(restored);
                    return false;
                }

                if (entry.state == ContractState.Active)
                {
                    activeCount++;
                    if (activeCount > activeContractLimit)
                    {
                        failureReason = "Saved active contracts exceed the active contract limit.";
                        DisposeAll(restored);
                        return false;
                    }
                }

                if (!ContractInstance.TryRestoreFromSaveData(definition, entry, new ContractObjectiveContext(inventory), out ContractInstance contract, out failureReason))
                {
                    DisposeAll(restored);
                    return false;
                }

                Subscribe(contract);
                restored.Add(contract);
            }

            ClearAllContracts();
            contracts.AddRange(restored);
            JournalChanged?.Invoke();
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DevelopmentClearContractJournal()
        {
            ClearAllContracts();
            JournalChanged?.Invoke();
        }
#endif

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

        private void ClearAllContracts()
        {
            foreach (ContractInstance contract in contracts)
            {
                Unsubscribe(contract);
                contract.Dispose();
            }

            contracts.Clear();
        }

        private static void DisposeAll(List<ContractInstance> contractInstances)
        {
            for (int i = 0; i < contractInstances.Count; i++)
            {
                contractInstances[i].Dispose();
            }
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
