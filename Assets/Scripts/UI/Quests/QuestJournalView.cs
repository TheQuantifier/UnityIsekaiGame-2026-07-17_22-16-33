using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Quests;

namespace UnityIsekaiGame.UI.Quests
{
    public sealed class QuestJournalView : MonoBehaviour
    {
        [SerializeField] private Transform questListRoot;
        [SerializeField] private Button questButtonTemplate;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Text rewardLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Button abandonButton;
        [SerializeField] private Button claimRewardButton;

        private readonly List<Button> questButtons = new List<Button>();
        private Action<int> questSelected;
        private Action abandonRequested;
        private Action rewardClaimRequested;

        private void Awake()
        {
            if (questButtonTemplate != null)
            {
                questButtonTemplate.gameObject.SetActive(false);
            }
        }

        public void Initialize(Action<int> onQuestSelected, Action onAbandonRequested, Action onRewardClaimRequested)
        {
            questSelected = onQuestSelected;
            abandonRequested = onAbandonRequested;
            rewardClaimRequested = onRewardClaimRequested;

            if (abandonButton != null)
            {
                abandonButton.onClick.RemoveListener(InvokeAbandonRequested);
                abandonButton.onClick.AddListener(InvokeAbandonRequested);
            }

            if (claimRewardButton != null)
            {
                claimRewardButton.onClick.RemoveListener(InvokeRewardClaimRequested);
                claimRewardButton.onClick.AddListener(InvokeRewardClaimRequested);
            }
        }

        public void Render(IReadOnlyList<QuestInstance> quests, int selectedIndex)
        {
            RenderQuestList(quests, selectedIndex);
            QuestInstance selected = quests != null && selectedIndex >= 0 && selectedIndex < quests.Count ? quests[selectedIndex] : null;
            RenderDetails(selected);
        }

        public void SetFeedback(string message)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = message;
            }
        }

        private void RenderQuestList(IReadOnlyList<QuestInstance> quests, int selectedIndex)
        {
            ClearQuestButtons();
            if (quests == null || questButtonTemplate == null || questListRoot == null)
            {
                return;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                int index = i;
                QuestInstance quest = quests[i];
                Button button = Instantiate(questButtonTemplate, questListRoot);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => questSelected?.Invoke(index));

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    string marker = i == selectedIndex ? "> " : string.Empty;
                    string title = quest?.Definition == null ? "Missing Quest" : quest.Definition.Title;
                    label.text = $"{marker}{title}\n{quest?.Definition?.Category} - {quest?.State}";
                }

                questButtons.Add(button);
            }
        }

        private void RenderDetails(QuestInstance quest)
        {
            if (titleLabel != null)
            {
                titleLabel.text = quest?.Definition == null ? "No Quest Selected" : quest.Definition.Title;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = quest?.Definition == null
                    ? "Speak with the prototype NPC to start a side quest."
                    : BuildDescription(quest);
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.text = BuildObjectiveText(quest);
            }

            if (rewardLabel != null)
            {
                rewardLabel.text = quest?.Definition?.Reward == null ? "Reward: None" : $"Reward: {quest.Definition.Reward.GetSummary()}";
            }

            if (abandonButton != null)
            {
                abandonButton.gameObject.SetActive(quest != null && quest.State == QuestState.Active && quest.Definition.CanAbandon);
            }

            if (claimRewardButton != null)
            {
                claimRewardButton.gameObject.SetActive(quest != null && quest.State == QuestState.Completed);
            }
        }

        private static string BuildDescription(QuestInstance quest)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(quest.Definition.Category);
            if (!string.IsNullOrWhiteSpace(quest.Definition.QuestSourceDisplayName))
            {
                builder.Append(" - ");
                builder.Append(quest.Definition.QuestSourceDisplayName);
            }

            builder.AppendLine();
            builder.AppendLine(quest.Definition.Summary);
            builder.Append("State: ");
            builder.Append(quest.State);
            builder.Append(" | Stage ");
            builder.Append(quest.CurrentStageIndex + 1);
            builder.AppendLine();
            builder.Append(quest.CurrentStage == null ? "No active stage." : quest.CurrentStage.Description);
            return builder.ToString();
        }

        private static string BuildObjectiveText(QuestInstance quest)
        {
            if (quest == null)
            {
                return "Objectives: None";
            }

            StringBuilder builder = new StringBuilder("Objectives:");
            IReadOnlyList<ContractObjectiveInstance> objectives = quest.CurrentObjectives;
            if (objectives.Count == 0)
            {
                builder.Append(" Complete");
                return builder.ToString();
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                ContractObjectiveInstance objective = objectives[i];
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(objective.Description);
                builder.Append(" (");
                builder.Append(objective.CurrentProgress);
                builder.Append(" / ");
                builder.Append(objective.RequiredProgress);
                builder.Append(objective.IsComplete ? ", complete)" : ")");
            }

            return builder.ToString();
        }

        private void ClearQuestButtons()
        {
            for (int i = questButtons.Count - 1; i >= 0; i--)
            {
                if (questButtons[i] != null)
                {
                    Destroy(questButtons[i].gameObject);
                }
            }

            questButtons.Clear();
        }

        private void InvokeAbandonRequested()
        {
            abandonRequested?.Invoke();
        }

        private void InvokeRewardClaimRequested()
        {
            rewardClaimRequested?.Invoke();
        }
    }
}
