using System;
using System.Collections.Generic;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestInstance : IDisposable
    {
        private readonly List<ContractObjectiveInstance> currentObjectives = new List<ContractObjectiveInstance>();
        private readonly PlayerInventory inventory;
        private QuestState state;
        private int currentStageIndex = -1;
        private bool stageTransitioning;

        public QuestInstance(QuestDefinition definition, ContractObjectiveContext context, string runtimeInstanceId = null)
        {
            Definition = definition;
            inventory = context.Inventory;
            state = QuestState.Active;
            RuntimeInstanceId = string.IsNullOrWhiteSpace(runtimeInstanceId)
                ? definition != null && !definition.Repeatable ? definition.QuestId : Guid.NewGuid().ToString("D")
                : runtimeInstanceId;
        }

        public QuestDefinition Definition { get; }
        public string RuntimeInstanceId { get; }
        public QuestState State => state;
        public int CurrentStageIndex => currentStageIndex;
        public QuestStageDefinition CurrentStage => IsStageIndexValid(currentStageIndex) ? Definition.Stages[currentStageIndex] : null;
        public IReadOnlyList<ContractObjectiveInstance> CurrentObjectives => currentObjectives;
        public bool IsActive => state == QuestState.Active;
        public bool IsComplete => state == QuestState.Completed || state == QuestState.RewardClaimed;

        public event Action<QuestInstance> Started;
        public event Action<QuestInstance> StageChanged;
        public event Action<QuestInstance> ProgressChanged;
        public event Action<QuestInstance> Completed;
        public event Action<QuestInstance> Failed;
        public event Action<QuestInstance> Abandoned;
        public event Action<QuestInstance> RewardClaimed;

        public void Start()
        {
            if (Definition == null || Definition.Stages.Count == 0)
            {
                CompleteQuest();
                return;
            }

            Started?.Invoke(this);
            ActivateStage(0);
        }

        public QuestOperationResult Abandon()
        {
            if (state != QuestState.Active)
            {
                return QuestOperationResult.Failure("Only active quests can be abandoned.");
            }

            if (Definition != null && !Definition.CanAbandon)
            {
                return QuestOperationResult.Failure("This quest cannot be abandoned.");
            }

            state = QuestState.Abandoned;
            DeactivateCurrentObjectives();
            Abandoned?.Invoke(this);
            return QuestOperationResult.Success($"Abandoned {Definition.Title}.");
        }

        public QuestOperationResult Fail(string reason)
        {
            if (state != QuestState.Active)
            {
                return QuestOperationResult.Failure("Only active quests can fail.");
            }

            state = QuestState.Failed;
            DeactivateCurrentObjectives();
            Failed?.Invoke(this);
            return QuestOperationResult.Success(string.IsNullOrWhiteSpace(reason) ? "Quest failed." : reason);
        }

        public QuestOperationResult ClaimReward()
        {
            if (state != QuestState.Completed)
            {
                return QuestOperationResult.Failure("Quest reward is not ready to claim.");
            }

            if (inventory == null)
            {
                return QuestOperationResult.Failure("No inventory available for quest reward.");
            }

            IReadOnlyList<ContractRewardItem> rewards = Definition.Reward == null
                ? Array.Empty<ContractRewardItem>()
                : Definition.Reward.ItemRewards;

            for (int i = 0; i < rewards.Count; i++)
            {
                ContractRewardItem reward = rewards[i];
                if (reward == null || reward.Item == null)
                {
                    return QuestOperationResult.Failure("Quest has an invalid reward.");
                }

                if (!inventory.CanAddItemOrInstances(reward.Item, reward.Quantity))
                {
                    return QuestOperationResult.Failure("Not enough inventory space for the full quest reward.");
                }
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                inventory.AddItemOrInstances(rewards[i].Item, rewards[i].Quantity);
            }

            state = QuestState.RewardClaimed;
            RewardClaimed?.Invoke(this);
            return QuestOperationResult.Success($"Claimed reward for {Definition.Title}.");
        }

        public void RecordDefeat(string targetCategory)
        {
            if (state != QuestState.Active)
            {
                return;
            }

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                ContractObjectiveInstance objective = currentObjectives[i];
                if (objective is DefeatObjectiveInstance defeat)
                {
                    defeat.RecordDefeat(targetCategory);
                }
            }

            EvaluateStageCompletion();
        }

        public QuestOperationResult TryDeliver(string destinationId)
        {
            if (state != QuestState.Active)
            {
                return QuestOperationResult.Failure("No active delivery objective.");
            }

            foreach (ContractObjectiveInstance objective in currentObjectives)
            {
                if (objective is DeliveryObjectiveInstance delivery && !delivery.IsComplete && delivery.DestinationId == destinationId)
                {
                    ContractOperationResult contractResult = delivery.TryDeliver(destinationId);
                    EvaluateStageCompletion();
                    return contractResult.Succeeded
                        ? QuestOperationResult.Success(contractResult.Message)
                        : QuestOperationResult.Failure(contractResult.Message);
                }
            }

            return QuestOperationResult.Failure("No matching active quest delivery objective.");
        }

        public void Dispose()
        {
            DeactivateCurrentObjectives();
        }

        public QuestInstanceSaveData CreateSaveData()
        {
            QuestInstanceSaveData saveData = new QuestInstanceSaveData
            {
                questDefinitionId = Definition == null ? string.Empty : Definition.QuestId,
                runtimeInstanceId = RuntimeInstanceId,
                state = state,
                currentStageIndex = currentStageIndex,
                currentStageId = CurrentStage == null ? string.Empty : CurrentStage.StageId
            };

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                string objectiveId = currentObjectives[i].Definition == null ? string.Empty : currentObjectives[i].Definition.ObjectiveId;
                saveData.objectives.Add(currentObjectives[i].CreateSaveData(i, objectiveId));
            }

            return saveData;
        }

        public static bool TryRestoreFromSaveData(
            QuestDefinition definition,
            QuestInstanceSaveData saveData,
            ContractObjectiveContext context,
            out QuestInstance instance,
            out string failureReason)
        {
            instance = null;
            failureReason = string.Empty;
            if (definition == null)
            {
                failureReason = "Quest definition is missing.";
                return false;
            }

            if (saveData == null)
            {
                failureReason = "Quest save data is missing.";
                return false;
            }

            if (!ValidateRuntimeInstanceId(definition, saveData.runtimeInstanceId))
            {
                failureReason = $"Quest '{definition.QuestId}' has an invalid runtime instance ID.";
                return false;
            }

            if (!Enum.IsDefined(typeof(QuestState), saveData.state))
            {
                failureReason = $"Quest '{definition.QuestId}' has invalid state '{saveData.state}'.";
                return false;
            }

            QuestInstance restored = new QuestInstance(definition, context, saveData.runtimeInstanceId);
            restored.state = saveData.state;
            restored.currentStageIndex = saveData.currentStageIndex;

            if (restored.state == QuestState.Active)
            {
                if (!TryFindStageIndexById(definition, saveData.currentStageId, out int restoredStageIndex))
                {
                    failureReason = $"Quest '{definition.QuestId}' could not resolve saved stage ID '{saveData.currentStageId}'.";
                    return false;
                }

                restored.currentStageIndex = restoredStageIndex;

                if (!restored.TryRestoreCurrentStageObjectives(saveData, context, out failureReason))
                {
                    restored.Dispose();
                    return false;
                }

                restored.ActivateCurrentObjectivesForRestore();
            }
            else
            {
                restored.DeactivateCurrentObjectives();
            }

            instance = restored;
            return true;
        }

        private void ActivateStage(int stageIndex)
        {
            if (state != QuestState.Active || !IsStageIndexValid(stageIndex))
            {
                CompleteQuest();
                return;
            }

            DeactivateCurrentObjectives();
            currentStageIndex = stageIndex;
            QuestStageDefinition stage = Definition.Stages[currentStageIndex];

            for (int i = 0; i < stage.Objectives.Count; i++)
            {
                ContractObjectiveDefinition objectiveDefinition = stage.Objectives[i];
                if (objectiveDefinition == null)
                {
                    continue;
                }

                ContractObjectiveInstance objective = objectiveDefinition.CreateInstance(new ContractObjectiveContext(inventory));
                objective.ProgressChanged += OnObjectiveProgressChanged;
                objective.Completed += OnObjectiveCompleted;
                currentObjectives.Add(objective);
            }

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                currentObjectives[i].Activate();
            }

            StageChanged?.Invoke(this);
            EvaluateStageCompletion();
        }

        private bool TryRestoreCurrentStageObjectives(QuestInstanceSaveData saveData, ContractObjectiveContext context, out string failureReason)
        {
            failureReason = string.Empty;
            DeactivateCurrentObjectives();
            QuestStageDefinition stage = Definition.Stages[currentStageIndex];
            for (int i = 0; i < stage.Objectives.Count; i++)
            {
                ContractObjectiveDefinition objectiveDefinition = stage.Objectives[i];
                if (objectiveDefinition == null)
                {
                    continue;
                }

                ContractObjectiveInstance objective = objectiveDefinition.CreateInstance(context);
                objective.ProgressChanged += OnObjectiveProgressChanged;
                objective.Completed += OnObjectiveCompleted;
                currentObjectives.Add(objective);
            }

            IReadOnlyList<ObjectiveProgressSaveData> entries = saveData.objectives == null ? Array.Empty<ObjectiveProgressSaveData>() : saveData.objectives;
            if (entries.Count != currentObjectives.Count)
            {
                failureReason = $"Quest '{Definition.QuestId}' expected {currentObjectives.Count} objective entries but save has {entries.Count}.";
                return false;
            }

            Dictionary<string, ContractObjectiveInstance> objectivesById = new Dictionary<string, ContractObjectiveInstance>();
            for (int i = 0; i < currentObjectives.Count; i++)
            {
                string objectiveId = currentObjectives[i].Definition == null ? string.Empty : currentObjectives[i].Definition.ObjectiveId;
                if (string.IsNullOrWhiteSpace(objectiveId) || objectivesById.ContainsKey(objectiveId))
                {
                    failureReason = $"Quest '{Definition.QuestId}' has invalid objective ID '{objectiveId}' in stage '{stage.StageId}'.";
                    return false;
                }

                objectivesById.Add(objectiveId, currentObjectives[i]);
            }

            HashSet<string> restoredObjectiveIds = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                ObjectiveProgressSaveData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.objectiveId))
                {
                    failureReason = $"Quest '{Definition.QuestId}' has missing objective ID in stage '{stage.StageId}'.";
                    return false;
                }

                if (!restoredObjectiveIds.Add(entry.objectiveId))
                {
                    failureReason = $"Quest '{Definition.QuestId}' save has duplicate objective ID '{entry.objectiveId}' in stage '{stage.StageId}'.";
                    return false;
                }

                if (!objectivesById.TryGetValue(entry.objectiveId, out ContractObjectiveInstance objective))
                {
                    failureReason = $"Quest '{Definition.QuestId}' could not resolve saved objective ID '{entry.objectiveId}' in stage '{stage.StageId}'.";
                    return false;
                }

                if (!objective.TryRestoreFromSaveData(entry, out failureReason))
                {
                    return false;
                }
            }

            return true;
        }

        private void ActivateCurrentObjectivesForRestore()
        {
            for (int i = 0; i < currentObjectives.Count; i++)
            {
                ContractObjectiveInstance objective = currentObjectives[i];
                if (!objective.IsComplete)
                {
                    objective.ActivateForRestore();
                }
            }
        }

        private void DeactivateCurrentObjectives()
        {
            foreach (ContractObjectiveInstance objective in currentObjectives)
            {
                objective.ProgressChanged -= OnObjectiveProgressChanged;
                objective.Completed -= OnObjectiveCompleted;
                objective.Deactivate();
            }

            currentObjectives.Clear();
        }

        private void OnObjectiveProgressChanged(ContractObjectiveInstance objective)
        {
            ProgressChanged?.Invoke(this);
            EvaluateStageCompletion();
        }

        private void OnObjectiveCompleted(ContractObjectiveInstance objective)
        {
            EvaluateStageCompletion();
        }

        private void EvaluateStageCompletion()
        {
            if (state != QuestState.Active || stageTransitioning)
            {
                return;
            }

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                if (!currentObjectives[i].IsComplete)
                {
                    return;
                }
            }

            AdvanceStage();
        }

        private void AdvanceStage()
        {
            stageTransitioning = true;
            QuestStageDefinition currentStage = CurrentStage;
            int nextStageIndex = currentStage == null || currentStage.NextStageIndex < 0
                ? currentStageIndex + 1
                : currentStage.NextStageIndex;
            stageTransitioning = false;

            if (IsStageIndexValid(nextStageIndex))
            {
                ActivateStage(nextStageIndex);
                return;
            }

            CompleteQuest();
        }

        private void CompleteQuest()
        {
            if (state != QuestState.Active)
            {
                return;
            }

            state = QuestState.Completed;
            DeactivateCurrentObjectives();
            Completed?.Invoke(this);
        }

        private bool IsStageIndexValid(int stageIndex)
        {
            return Definition != null && stageIndex >= 0 && stageIndex < Definition.Stages.Count;
        }

        private static bool ValidateRuntimeInstanceId(QuestDefinition definition, string runtimeInstanceId)
        {
            if (definition == null || string.IsNullOrWhiteSpace(runtimeInstanceId))
            {
                return false;
            }

            return !definition.Repeatable
                ? string.Equals(runtimeInstanceId, definition.QuestId, StringComparison.Ordinal)
                : Guid.TryParseExact(runtimeInstanceId, "D", out _);
        }

        private static bool TryFindStageIndexById(QuestDefinition definition, string stageId, out int stageIndex)
        {
            stageIndex = -1;
            if (definition == null || string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            for (int i = 0; i < definition.Stages.Count; i++)
            {
                if (definition.Stages[i] != null && string.Equals(definition.Stages[i].StageId, stageId, StringComparison.Ordinal))
                {
                    stageIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
