using System;
using System.Collections.Generic;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractInstance : IDisposable
    {
        private readonly List<ContractObjectiveInstance> objectives = new List<ContractObjectiveInstance>();
        private readonly PlayerInventory inventory;
        private ContractState state;

        public ContractInstance(ContractDefinition definition, ContractObjectiveContext context)
        {
            Definition = definition;
            inventory = context.Inventory;
            state = ContractState.Active;

            if (definition != null)
            {
                for (int i = 0; i < definition.Objectives.Count; i++)
                {
                    ContractObjectiveDefinition objectiveDefinition = definition.Objectives[i];
                    if (objectiveDefinition == null)
                    {
                        continue;
                    }

                    ContractObjectiveInstance objective = objectiveDefinition.CreateInstance(context);
                    objective.ProgressChanged += OnObjectiveProgressChanged;
                    objective.Completed += OnObjectiveCompleted;
                    objectives.Add(objective);
                }
            }
        }

        public ContractDefinition Definition { get; }
        public ContractState State => state;
        public IReadOnlyList<ContractObjectiveInstance> Objectives => objectives;
        public bool IsActive => state == ContractState.Active;
        public bool IsCompleted => state == ContractState.Completed || state == ContractState.RewardClaimed;

        public event Action<ContractInstance> ProgressChanged;
        public event Action<ContractInstance> Completed;
        public event Action<ContractInstance> Failed;
        public event Action<ContractInstance> Abandoned;
        public event Action<ContractInstance> RewardClaimed;

        public void Activate()
        {
            foreach (ContractObjectiveInstance objective in objectives)
            {
                objective.Activate();
            }

            EvaluateCompletion();
        }

        public ContractOperationResult Abandon()
        {
            if (state != ContractState.Active)
            {
                return ContractOperationResult.Failure("Only active contracts can be abandoned.");
            }

            state = ContractState.Abandoned;
            DeactivateObjectives();
            Abandoned?.Invoke(this);
            return ContractOperationResult.Success($"Abandoned {Definition.DisplayTitle}.");
        }

        public ContractOperationResult Fail(string reason)
        {
            if (state != ContractState.Active)
            {
                return ContractOperationResult.Failure("Only active contracts can fail.");
            }

            state = ContractState.Failed;
            DeactivateObjectives();
            Failed?.Invoke(this);
            return ContractOperationResult.Success(string.IsNullOrWhiteSpace(reason) ? "Contract failed." : reason);
        }

        public ContractOperationResult ClaimReward()
        {
            if (state != ContractState.Completed)
            {
                return ContractOperationResult.Failure("Contract reward is not ready to claim.");
            }

            if (inventory == null)
            {
                return ContractOperationResult.Failure("No inventory available for contract reward.");
            }

            IReadOnlyList<ContractRewardItem> rewards = Definition.Reward == null
                ? Array.Empty<ContractRewardItem>()
                : Definition.Reward.ItemRewards;

            for (int i = 0; i < rewards.Count; i++)
            {
                ContractRewardItem reward = rewards[i];
                if (reward == null || reward.Item == null)
                {
                    return ContractOperationResult.Failure("Contract has an invalid reward.");
                }

                if (!inventory.CanAddItem(reward.Item, reward.Quantity))
                {
                    return ContractOperationResult.Failure("Not enough inventory space for the full reward.");
                }
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                inventory.AddItem(rewards[i].Item, rewards[i].Quantity);
            }

            state = ContractState.RewardClaimed;
            RewardClaimed?.Invoke(this);
            return ContractOperationResult.Success($"Claimed reward for {Definition.DisplayTitle}.");
        }

        public ContractOperationResult TryDeliver(string destinationId)
        {
            if (state != ContractState.Active)
            {
                return ContractOperationResult.Failure("No active delivery objective.");
            }

            foreach (ContractObjectiveInstance objective in objectives)
            {
                if (objective is DeliveryObjectiveInstance delivery && !delivery.IsComplete && delivery.DestinationId == destinationId)
                {
                    ContractOperationResult result = delivery.TryDeliver(destinationId);
                    EvaluateCompletion();
                    return result;
                }
            }

            return ContractOperationResult.Failure("No matching active delivery objective.");
        }

        public void RecordDefeat(string targetCategory)
        {
            if (state != ContractState.Active)
            {
                return;
            }

            foreach (ContractObjectiveInstance objective in objectives)
            {
                if (objective is DefeatObjectiveInstance defeat)
                {
                    defeat.RecordDefeat(targetCategory);
                }
            }

            EvaluateCompletion();
        }

        public void Dispose()
        {
            DeactivateObjectives();
        }

        private void OnObjectiveProgressChanged(ContractObjectiveInstance objective)
        {
            ProgressChanged?.Invoke(this);
            EvaluateCompletion();
        }

        private void OnObjectiveCompleted(ContractObjectiveInstance objective)
        {
            EvaluateCompletion();
        }

        private void EvaluateCompletion()
        {
            if (state != ContractState.Active || objectives.Count == 0)
            {
                return;
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                if (!objectives[i].IsComplete)
                {
                    return;
                }
            }

            state = ContractState.Completed;
            DeactivateObjectives();
            Completed?.Invoke(this);
        }

        private void DeactivateObjectives()
        {
            foreach (ContractObjectiveInstance objective in objectives)
            {
                objective.ProgressChanged -= OnObjectiveProgressChanged;
                objective.Completed -= OnObjectiveCompleted;
                objective.Deactivate();
            }
        }
    }
}
