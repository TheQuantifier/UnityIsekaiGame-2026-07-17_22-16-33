using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityIsekaiGame.Dialogue;

namespace UnityIsekaiGame.UI
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text speakerLabel;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject continueHint;
        [SerializeField] private Text continueHintLabel;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonTemplate;

        private readonly List<Button> choiceButtons = new List<Button>();
        private Action<int> choiceSelected;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (choiceButtonTemplate != null)
            {
                choiceButtonTemplate.gameObject.SetActive(false);
            }

            Hide();
        }

        public void Initialize(Action<int> onChoiceSelected)
        {
            choiceSelected = onChoiceSelected;
        }

        public void Render(DialogueNodeDefinition node)
        {
            if (node == null)
            {
                ClearChoices();
                return;
            }

            if (speakerLabel != null)
            {
                speakerLabel.text = node.SpeakerName;
            }

            if (dialogueText != null)
            {
                dialogueText.text = node.DialogueText;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = node.Portrait;
                portraitImage.enabled = node.Portrait != null;
            }

            RenderChoices(node.Choices);

            if (continueHint != null)
            {
                continueHint.SetActive(!node.HasChoices);
            }

            if (continueHintLabel == null && continueHint != null)
            {
                continueHintLabel = continueHint.GetComponent<Text>();
            }

            if (continueHintLabel != null)
            {
                continueHintLabel.text = node.EndsConversation
                    ? "Press Escape / B to close"
                    : "Press Enter / Space to continue";
            }
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            ClearChoices();
            SetVisible(false);
        }

        private void RenderChoices(IReadOnlyList<DialogueChoice> choices)
        {
            ClearChoices();
            if (choices == null || choices.Count == 0 || choiceButtonTemplate == null || choiceContainer == null)
            {
                return;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                int choiceIndex = i;
                Button button = Instantiate(choiceButtonTemplate, choiceContainer);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => choiceSelected?.Invoke(choiceIndex));

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = choices[i] == null ? "Continue" : choices[i].ChoiceText;
                }

                choiceButtons.Add(button);
            }

            if (EventSystem.current != null && choiceButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
            }
        }

        private void ClearChoices()
        {
            for (int i = choiceButtons.Count - 1; i >= 0; i--)
            {
                if (choiceButtons[i] != null)
                {
                    Destroy(choiceButtons[i].gameObject);
                }
            }

            choiceButtons.Clear();
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
    }
}
