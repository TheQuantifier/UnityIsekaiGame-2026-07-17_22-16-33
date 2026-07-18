using System;
using System.Collections.Generic;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestInstance : IDisposable
    {
        private readonly List<ContractObjectiveInstance> currentObjectives = new List<ContractObjectiveInstance>();
        private readonly PlayerInventory inventory;
        private QuestState state;
        private int currentStageIndex = -1;
        private bool stageTransitioning;

        public QuestInstance(QuestDefinition definition, ContractObjectiveContext context)
        {
            Definition = definition;
            inventory = context.Inventory;
            state = QuestState.Active;
        }

        public QuestDefinition Definition { get; }
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

                if (!inventory.CanAddItem(reward.Item, reward.Quantity))
                {
                    return QuestOperationResult.Failure("Not enough inventory space for the full quest reward.");
                }
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                inventory.AddItem(rewards[i].Item, rewards[i].Quantity);
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

            foreach (ContractObjectiveInstance objective in currentObjectives)
            {
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

            foreach (ContractObjectiveInstance objective in currentObjectives)
            {
                objective.Activate();
            }

            StageChanged?.Invoke(this);
            EvaluateStageCompletion();
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
    }
}
