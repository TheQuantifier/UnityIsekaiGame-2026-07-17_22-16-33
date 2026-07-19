using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Quests
{
    public sealed class PlayerQuestLog : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;

        private readonly List<QuestInstance> quests = new List<QuestInstance>();

        public IReadOnlyList<QuestInstance> Quests => quests;
        public event Action QuestLogChanged;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }
        }

        private void OnDestroy()
        {
            ClearAllQuests();
        }

        public QuestOperationResult StartQuest(QuestDefinition definition)
        {
            if (definition == null)
            {
                return QuestOperationResult.Failure("Invalid quest.");
            }

            if (string.IsNullOrWhiteSpace(definition.QuestId))
            {
                return QuestOperationResult.Failure("Quest is missing a stable ID.");
            }

            QuestInstance existing = FindQuest(definition.QuestId);
            if (existing != null && !definition.Repeatable)
            {
                return QuestOperationResult.Failure($"{definition.Title} is already in the quest log.");
            }

            for (int i = 0; i < definition.PrerequisiteQuestIds.Count; i++)
            {
                string prerequisiteId = definition.PrerequisiteQuestIds[i];
                if (!string.IsNullOrWhiteSpace(prerequisiteId) && !IsQuestComplete(prerequisiteId))
                {
                    return QuestOperationResult.Failure($"Missing prerequisite quest: {prerequisiteId}.");
                }
            }

            QuestInstance quest = new QuestInstance(definition, new ContractObjectiveContext(inventory));
            Subscribe(quest);
            quests.Add(quest);
            quest.Start();
            QuestLogChanged?.Invoke();
            PrototypeHudMessageBus.Show($"Quest started: {definition.Title}");
            return QuestOperationResult.Success($"Quest started: {definition.Title}.");
        }

        public QuestOperationResult AbandonQuest(QuestInstance quest)
        {
            if (quest == null || !quests.Contains(quest))
            {
                return QuestOperationResult.Failure("No quest selected.");
            }

            QuestOperationResult result = quest.Abandon();
            QuestLogChanged?.Invoke();
            return result;
        }

        public QuestOperationResult ClaimReward(QuestInstance quest)
        {
            if (quest == null || !quests.Contains(quest))
            {
                return QuestOperationResult.Failure("No quest selected.");
            }

            QuestOperationResult result = quest.ClaimReward();
            if (result.Succeeded)
            {
                PrototypeHudMessageBus.Show(result.Message);
            }

            QuestLogChanged?.Invoke();
            return result;
        }

        public QuestOperationResult DeliverTo(string destinationId)
        {
            foreach (QuestInstance quest in quests)
            {
                if (!quest.IsActive)
                {
                    continue;
                }

                QuestOperationResult result = quest.TryDeliver(destinationId);
                if (result.Succeeded)
                {
                    QuestLogChanged?.Invoke();
                    PrototypeHudMessageBus.Show(result.Message);
                    return result;
                }
            }

            return QuestOperationResult.Failure("No active quest needs this delivery.");
        }

        public void RecordDefeat(ContractObjectiveTarget target)
        {
            if (target == null)
            {
                return;
            }

            foreach (QuestInstance quest in quests)
            {
                quest.RecordDefeat(target.TargetCategory);
            }

            QuestLogChanged?.Invoke();
        }

        public QuestInstance FindQuest(string questId)
        {
            foreach (QuestInstance quest in quests)
            {
                if (quest.Definition != null && quest.Definition.QuestId == questId)
                {
                    return quest;
                }
            }

            return null;
        }

        public bool IsQuestComplete(string questId)
        {
            QuestInstance quest = FindQuest(questId);
            return quest != null && quest.IsComplete;
        }

        public bool IsQuestActive(string questId)
        {
            QuestInstance quest = FindQuest(questId);
            return quest != null && quest.IsActive;
        }

        public int GetQuestStageIndex(string questId)
        {
            QuestInstance quest = FindQuest(questId);
            return quest == null ? -1 : quest.CurrentStageIndex;
        }

        public void ResetQuestForPrototypeTesting(string questId)
        {
            for (int i = quests.Count - 1; i >= 0; i--)
            {
                QuestInstance quest = quests[i];
                if (quest.Definition == null || quest.Definition.QuestId != questId)
                {
                    continue;
                }

                Unsubscribe(quest);
                quest.Dispose();
                quests.RemoveAt(i);
            }

            QuestLogChanged?.Invoke();
            PrototypeHudMessageBus.Show("Prototype quest reset");
        }

        public List<QuestInstanceSaveData> CreateSaveData()
        {
            List<QuestInstanceSaveData> saveData = new List<QuestInstanceSaveData>(quests.Count);
            for (int i = 0; i < quests.Count; i++)
            {
                saveData.Add(quests[i].CreateSaveData());
            }

            return saveData;
        }

        public bool TryRestoreFromSaveData(IReadOnlyList<QuestInstanceSaveData> saveData, DefinitionRegistry registry, out string failureReason)
        {
            failureReason = string.Empty;
            if (registry == null)
            {
                failureReason = "Definition registry is missing for quest restore.";
                return false;
            }

            List<QuestInstance> restored = new List<QuestInstance>();
            HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> nonRepeatableQuestIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<QuestInstanceSaveData> entries = saveData ?? Array.Empty<QuestInstanceSaveData>();

            for (int i = 0; i < entries.Count; i++)
            {
                QuestInstanceSaveData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.questDefinitionId))
                {
                    failureReason = "Quest save entry is missing a quest definition ID.";
                    DisposeAll(restored);
                    return false;
                }

                if (!runtimeIds.Add(entry.runtimeInstanceId))
                {
                    failureReason = $"Duplicate quest runtime instance ID '{entry.runtimeInstanceId}'.";
                    DisposeAll(restored);
                    return false;
                }

                if (!registry.TryGet(entry.questDefinitionId, out QuestDefinition definition))
                {
                    failureReason = $"Quest definition '{entry.questDefinitionId}' was not found.";
                    DisposeAll(restored);
                    return false;
                }

                if (!definition.Repeatable && !nonRepeatableQuestIds.Add(definition.QuestId))
                {
                    failureReason = $"Duplicate non-repeatable quest '{definition.QuestId}' in save data.";
                    DisposeAll(restored);
                    return false;
                }

                if (!QuestInstance.TryRestoreFromSaveData(definition, entry, new ContractObjectiveContext(inventory), out QuestInstance quest, out failureReason))
                {
                    DisposeAll(restored);
                    return false;
                }

                Subscribe(quest);
                restored.Add(quest);
            }

            ClearAllQuests();
            quests.AddRange(restored);
            QuestLogChanged?.Invoke();
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DevelopmentClearQuestLog()
        {
            ClearAllQuests();
            QuestLogChanged?.Invoke();
        }
#endif

        private void ClearAllQuests()
        {
            foreach (QuestInstance quest in quests)
            {
                Unsubscribe(quest);
                quest.Dispose();
            }

            quests.Clear();
        }

        private static void DisposeAll(List<QuestInstance> questInstances)
        {
            for (int i = 0; i < questInstances.Count; i++)
            {
                questInstances[i].Dispose();
            }
        }

        private void Subscribe(QuestInstance quest)
        {
            quest.Started += OnQuestChanged;
            quest.StageChanged += OnQuestStageChanged;
            quest.ProgressChanged += OnQuestChanged;
            quest.Completed += OnQuestCompleted;
            quest.Failed += OnQuestChanged;
            quest.Abandoned += OnQuestChanged;
            quest.RewardClaimed += OnQuestChanged;
        }

        private void Unsubscribe(QuestInstance quest)
        {
            quest.Started -= OnQuestChanged;
            quest.StageChanged -= OnQuestStageChanged;
            quest.ProgressChanged -= OnQuestChanged;
            quest.Completed -= OnQuestCompleted;
            quest.Failed -= OnQuestChanged;
            quest.Abandoned -= OnQuestChanged;
            quest.RewardClaimed -= OnQuestChanged;
        }

        private void OnQuestChanged(QuestInstance quest)
        {
            QuestLogChanged?.Invoke();
        }

        private void OnQuestStageChanged(QuestInstance quest)
        {
            PrototypeHudMessageBus.Show($"Quest updated: {quest.Definition.Title}");
            QuestLogChanged?.Invoke();
        }

        private void OnQuestCompleted(QuestInstance quest)
        {
            PrototypeHudMessageBus.Show($"Quest complete: {quest.Definition.Title}");
            QuestLogChanged?.Invoke();
        }
    }
}
