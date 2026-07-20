using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.UI.Contracts
{
    public sealed class ContractJournalView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform contractListRoot;
        [SerializeField] private Button contractButtonTemplate;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Text rewardLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Button abandonButton;
        [SerializeField] private Button claimRewardButton;

        private readonly List<Button> contractButtons = new List<Button>();
        private Action<int> contractSelected;
        private Action abandonRequested;
        private Action rewardClaimRequested;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (contractButtonTemplate != null)
            {
                contractButtonTemplate.gameObject.SetActive(false);
            }
        }

        public void Initialize(Action<int> onContractSelected, Action onAbandonRequested, Action onRewardClaimRequested)
        {
            contractSelected = onContractSelected;
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

        public void Render(IReadOnlyList<ContractInstance> contracts, int selectedIndex)
        {
            RenderContractList(contracts, selectedIndex);
            ContractInstance selected = contracts != null && selectedIndex >= 0 && selectedIndex < contracts.Count ? contracts[selectedIndex] : null;
            RenderDetails(selected);
        }

        public void SetFeedback(string message)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = message;
            }
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void RenderContractList(IReadOnlyList<ContractInstance> contracts, int selectedIndex)
        {
            ClearContractButtons();
            if (contracts == null || contractButtonTemplate == null || contractListRoot == null)
            {
                return;
            }

            for (int i = 0; i < contracts.Count; i++)
            {
                int index = i;
                ContractInstance contract = contracts[i];
                Button button = Instantiate(contractButtonTemplate, contractListRoot);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => contractSelected?.Invoke(index));

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    string marker = i == selectedIndex ? "> " : string.Empty;
                    string title = contract?.Definition == null ? "Missing Contract" : contract.Definition.DisplayTitle;
                    label.text = $"{marker}{title}\n{contract?.State}";
                }

                contractButtons.Add(button);
            }

            if (EventSystem.current != null && contractButtons.Count > 0 && selectedIndex >= 0 && selectedIndex < contractButtons.Count)
            {
                EventSystem.current.SetSelectedGameObject(contractButtons[selectedIndex].gameObject);
            }
        }

        private void RenderDetails(ContractInstance contract)
        {
            if (titleLabel != null)
            {
                titleLabel.text = contract?.Definition == null ? "No Contract Selected" : contract.Definition.DisplayTitle;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = contract?.Definition == null
                    ? "Accept a prototype contract from the board."
                    : $"{contract.Definition.RequesterDisplayName}\n{contract.Definition.Description}\nState: {contract.State}";
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.text = BuildObjectiveText(contract);
            }

            if (rewardLabel != null)
            {
                rewardLabel.text = contract?.Definition?.Reward == null ? "Reward: None" : $"Reward: {contract.Definition.Reward.GetSummary()}";
            }

            if (abandonButton != null)
            {
                abandonButton.gameObject.SetActive(contract != null && contract.State == ContractState.Active);
            }

            if (claimRewardButton != null)
            {
                claimRewardButton.gameObject.SetActive(contract != null && contract.State == ContractState.Completed);
            }
        }

        private static string BuildObjectiveText(ContractInstance contract)
        {
            if (contract == null)
            {
                return "Objectives: None";
            }

            StringBuilder builder = new StringBuilder("Objectives:");
            for (int i = 0; i < contract.Objectives.Count; i++)
            {
                ContractObjectiveInstance objective = contract.Objectives[i];
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

        private void ClearContractButtons()
        {
            for (int i = contractButtons.Count - 1; i >= 0; i--)
            {
                if (contractButtons[i] != null)
                {
                    Destroy(contractButtons[i].gameObject);
                }
            }

            contractButtons.Clear();
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
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
