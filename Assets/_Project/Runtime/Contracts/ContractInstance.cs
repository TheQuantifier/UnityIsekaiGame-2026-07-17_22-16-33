using System;
using System.Collections.Generic;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractInstance : IDisposable
    {
        private readonly List<ContractObjectiveInstance> objectives = new List<ContractObjectiveInstance>();
        private readonly PlayerInventory inventory;
        private ContractState state;

        public ContractInstance(ContractDefinition definition, ContractObjectiveContext context, string runtimeInstanceId = null)
        {
            Definition = definition;
            inventory = context.Inventory;
            state = ContractState.Active;
            RuntimeInstanceId = string.IsNullOrWhiteSpace(runtimeInstanceId) ? Guid.NewGuid().ToString("D") : runtimeInstanceId;

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
        public string RuntimeInstanceId { get; }
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

                if (!inventory.CanAddItemOrInstances(reward.Item, reward.Quantity))
                {
                    return ContractOperationResult.Failure("Not enough inventory space for the full reward.");
                }
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                inventory.AddItemOrInstances(rewards[i].Item, rewards[i].Quantity);
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

        public ContractInstanceSaveData CreateSaveData()
        {
            ContractInstanceSaveData saveData = new ContractInstanceSaveData
            {
                contractDefinitionId = Definition == null ? string.Empty : Definition.ContractId,
                runtimeInstanceId = RuntimeInstanceId,
                state = state
            };

            for (int i = 0; i < objectives.Count; i++)
            {
                string objectiveId = objectives[i].Definition == null ? string.Empty : objectives[i].Definition.ObjectiveId;
                saveData.objectives.Add(objectives[i].CreateSaveData(i, objectiveId));
            }

            return saveData;
        }

        public static bool TryRestoreFromSaveData(
            ContractDefinition definition,
            ContractInstanceSaveData saveData,
            ContractObjectiveContext context,
            out ContractInstance instance,
            out string failureReason)
        {
            instance = null;
            failureReason = string.Empty;
            if (definition == null)
            {
                failureReason = "Contract definition is missing.";
                return false;
            }

            if (saveData == null)
            {
                failureReason = "Contract save data is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(saveData.runtimeInstanceId) || !Guid.TryParseExact(saveData.runtimeInstanceId, "D", out _))
            {
                failureReason = $"Contract '{definition.ContractId}' has an invalid runtime instance ID.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContractState), saveData.state))
            {
                failureReason = $"Contract '{definition.ContractId}' has invalid state '{saveData.state}'.";
                return false;
            }

            ContractInstance restored = new ContractInstance(definition, context, saveData.runtimeInstanceId);
            if (!restored.TryRestoreObjectives(saveData, out failureReason))
            {
                restored.Dispose();
                return false;
            }

            restored.state = saveData.state;
            if (restored.state == ContractState.Active)
            {
                restored.ActivateForRestore();
            }
            else
            {
                restored.DeactivateObjectives();
            }

            instance = restored;
            return true;
        }

        private void OnObjectiveProgressChanged(ContractObjectiveInstance objective)
        {
            ProgressChanged?.Invoke(this);
            EvaluateCompletion();
        }

        private bool TryRestoreObjectives(ContractInstanceSaveData saveData, out string failureReason)
        {
            failureReason = string.Empty;
            IReadOnlyList<ObjectiveProgressSaveData> entries = saveData.objectives == null ? Array.Empty<ObjectiveProgressSaveData>() : saveData.objectives;
            if (entries.Count != objectives.Count)
            {
                failureReason = $"Contract '{Definition.ContractId}' expected {objectives.Count} objective entries but save has {entries.Count}.";
                return false;
            }

            Dictionary<string, ContractObjectiveInstance> objectivesById = new Dictionary<string, ContractObjectiveInstance>();
            for (int i = 0; i < objectives.Count; i++)
            {
                string objectiveId = objectives[i].Definition == null ? string.Empty : objectives[i].Definition.ObjectiveId;
                if (string.IsNullOrWhiteSpace(objectiveId) || objectivesById.ContainsKey(objectiveId))
                {
                    failureReason = $"Contract '{Definition.ContractId}' has invalid objective ID '{objectiveId}'.";
                    return false;
                }

                objectivesById.Add(objectiveId, objectives[i]);
            }

            HashSet<string> restoredObjectiveIds = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                ObjectiveProgressSaveData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.objectiveId))
                {
                    failureReason = $"Contract '{Definition.ContractId}' has missing objective ID.";
                    return false;
                }

                if (!restoredObjectiveIds.Add(entry.objectiveId))
                {
                    failureReason = $"Contract '{Definition.ContractId}' save has duplicate objective ID '{entry.objectiveId}'.";
                    return false;
                }

                if (!objectivesById.TryGetValue(entry.objectiveId, out ContractObjectiveInstance objective))
                {
                    failureReason = $"Contract '{Definition.ContractId}' could not resolve saved objective ID '{entry.objectiveId}'.";
                    return false;
                }

                if (!objective.TryRestoreFromSaveData(entry, out failureReason))
                {
                    return false;
                }
            }

            return true;
        }

        private void ActivateForRestore()
        {
            foreach (ContractObjectiveInstance objective in objectives)
            {
                if (!objective.IsComplete)
                {
                    objective.ActivateForRestore();
                }
            }
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
