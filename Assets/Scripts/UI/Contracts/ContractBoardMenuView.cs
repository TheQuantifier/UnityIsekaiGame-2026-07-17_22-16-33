using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.UI.Contracts
{
    public sealed class ContractBoardMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text detailsLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Transform contractListRoot;
        [SerializeField] private Button contractButtonTemplate;
        [SerializeField] private Button acceptSelectedButton;
        [SerializeField] private Button closeButton;

        private readonly List<Button> contractButtons = new List<Button>();
        private readonly List<ContractDefinition> displayedContracts = new List<ContractDefinition>();
        private PlayerContractJournal journal;
        private IReadOnlyList<ContractDefinition> availableContracts;
        private PlayerInputReader input;
        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private bool hasCursorState;
        private int selectedContractIndex;
        private bool isOpen;

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

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (acceptSelectedButton != null)
            {
                acceptSelectedButton.onClick.RemoveListener(AcceptSelectedContract);
            }

            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Close();
            }
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
            }

            if (input != null)
            {
                if (input.ConsumeCancel() || input.ConsumeInventory())
                {
                    Close();
                    return;
                }

                if (input.ConsumeInventoryNavigate(out Vector2 direction))
                {
                    MoveSelection(direction.y < 0f || direction.x > 0f ? 1 : -1);
                }

                if (input.ConsumeInventoryUse())
                {
                    AcceptSelectedContract();
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            if (keyboard == null)
            {
                HandleMouseSelection();
                return;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                MoveSelection(1);
            }

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                MoveSelection(-1);
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                AcceptSelectedContract();
            }

            TryAcceptNumberShortcut(keyboard);
            HandleMouseSelection();
        }

        private void TryAcceptNumberShortcut(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                AcceptContractAt(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                AcceptContractAt(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                AcceptContractAt(2);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            {
                AcceptContractAt(3);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
            {
                AcceptContractAt(4);
            }
            else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
            {
                AcceptContractAt(5);
            }
        }

        private void HandleMouseSelection()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 pointerPosition = mouse.position.ReadValue();
            if (acceptSelectedButton != null
                && mouse.leftButton.wasPressedThisFrame
                && RectTransformUtility.RectangleContainsScreenPoint(acceptSelectedButton.GetComponent<RectTransform>(), pointerPosition))
            {
                AcceptSelectedContract();
                return;
            }

            if (closeButton != null
                && mouse.leftButton.wasPressedThisFrame
                && RectTransformUtility.RectangleContainsScreenPoint(closeButton.GetComponent<RectTransform>(), pointerPosition))
            {
                Close();
                return;
            }

            for (int i = 0; i < contractButtons.Count; i++)
            {
                Button button = contractButtons[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform == null || !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition))
                {
                    continue;
                }

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    selectedContractIndex = i;
                    RefreshButtonLabels();
                }

                return;
            }
        }

        public void Open(PlayerContractJournal targetJournal, IReadOnlyList<ContractDefinition> contracts)
        {
            if (isOpen)
            {
                return;
            }

            journal = targetJournal;
            availableContracts = contracts;
            selectedContractIndex = 0;

            if (titleLabel != null)
            {
                titleLabel.text = "Contract Board";
            }

            if (feedbackLabel != null)
            {
                feedbackLabel.text = "Select a contract to accept.";
            }

            RenderContracts();
            CaptureGameplayState();
            SetVisible(true);
            PrototypeGameplayModalState.SetContractMenuActive(true);
            isOpen = true;
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            PrototypeGameplayModalState.SetContractMenuActive(false);
            SetVisible(false);
            RestoreGameplayState();
            isOpen = false;
        }

        public static ContractBoardMenuView FindOrCreate()
        {
            ContractBoardMenuView existing = FindAnyObjectByType<ContractBoardMenuView>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            return CreateRuntimeMenu();
        }

        private void RenderContracts()
        {
            ClearContractButtons();

            if (contractButtonTemplate == null || contractListRoot == null)
            {
                RenderSelectedContractDetails();
                return;
            }

            displayedContracts.Clear();
            if (availableContracts == null)
            {
                RenderSelectedContractDetails();
                return;
            }

            for (int i = 0; i < availableContracts.Count; i++)
            {
                ContractDefinition contract = availableContracts[i];
                if (contract == null)
                {
                    continue;
                }

                displayedContracts.Add(contract);
                Button button = Instantiate(contractButtonTemplate, contractListRoot);
                button.gameObject.SetActive(true);

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.raycastTarget = false;
                }

                contractButtons.Add(button);
            }

            RefreshButtonLabels();
        }

        private void MoveSelection(int delta)
        {
            if (displayedContracts.Count == 0)
            {
                return;
            }

            selectedContractIndex = Mathf.Clamp(selectedContractIndex + delta, 0, displayedContracts.Count - 1);
            RefreshButtonLabels();
        }

        private void AcceptSelectedContract()
        {
            AcceptContractAt(selectedContractIndex);
        }

        private void AcceptContractAt(int index)
        {
            if (index < 0 || index >= displayedContracts.Count)
            {
                return;
            }

            selectedContractIndex = index;
            RefreshButtonLabels();
            AcceptContract(displayedContracts[index]);
        }

        private void RefreshButtonLabels()
        {
            for (int i = 0; i < contractButtons.Count && i < displayedContracts.Count; i++)
            {
                Text label = contractButtons[i] == null ? null : contractButtons[i].GetComponentInChildren<Text>(true);
                ContractDefinition contract = displayedContracts[i];
                if (label == null || contract == null)
                {
                    continue;
                }

                string marker = i == selectedContractIndex ? "> " : $"{i + 1}. ";
                string requester = string.IsNullOrWhiteSpace(contract.RequesterDisplayName) ? "Unknown requester" : contract.RequesterDisplayName;
                label.text = $"{marker}{contract.DisplayTitle}\n{requester}";

                Image image = contractButtons[i].targetGraphic as Image;
                if (image != null)
                {
                    image.color = i == selectedContractIndex
                        ? new Color(0.34f, 0.42f, 0.56f, 1f)
                        : new Color(0.24f, 0.24f, 0.24f, 1f);
                }
            }

            RenderSelectedContractDetails();
        }

        private void AcceptContract(ContractDefinition contract)
        {
            ContractOperationResult result = journal == null
                ? ContractOperationResult.Failure("No contract journal found.")
                : journal.AcceptContract(contract);

            if (feedbackLabel != null)
            {
                feedbackLabel.text = result.Message;
            }

            PrototypeHudMessageBus.Show(result.Message);
        }

        private void RenderSelectedContractDetails()
        {
            if (detailsLabel == null)
            {
                return;
            }

            if (displayedContracts.Count == 0)
            {
                detailsLabel.text = "No contracts are posted.";
                return;
            }

            int index = Mathf.Clamp(selectedContractIndex, 0, displayedContracts.Count - 1);
            ContractDefinition contract = displayedContracts[index];
            if (contract == null)
            {
                detailsLabel.text = "Selected contract is missing.";
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(contract.DisplayTitle);
            builder.AppendLine();
            builder.Append("Requester: ");
            builder.AppendLine(string.IsNullOrWhiteSpace(contract.RequesterDisplayName) ? "Unknown" : contract.RequesterDisplayName);
            builder.AppendLine();
            builder.AppendLine(contract.Description);
            builder.AppendLine();
            builder.Append("Reward: ");
            builder.AppendLine(contract.Reward == null ? "None" : contract.Reward.GetSummary());

            detailsLabel.text = builder.ToString();
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
            displayedContracts.Clear();
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

        private void CaptureGameplayState()
        {
            if (!hasCursorState)
            {
                previousLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                hasCursorState = true;
            }

            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
            }

            input?.SetGameplayInputBlocked(true);
            input?.ClearCancel();
            input?.ClearInventoryUiActions();
            input?.ClearDialogueActions();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreGameplayState()
        {
            input?.ClearGameplayActionQueues();
            input?.ClearInventoryUiActions();
            input?.ClearDialogueActions();
            input?.SetGameplayInputBlocked(false);

            if (!hasCursorState)
            {
                return;
            }

            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
            hasCursorState = false;
        }

        private static ContractBoardMenuView CreateRuntimeMenu()
        {
            GameObject canvasObject = new GameObject("Contract Board Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ContractBoardMenuView));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            ContractBoardMenuView view = canvasObject.GetComponent<ContractBoardMenuView>();
            view.canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panel = CreatePanel("Panel", canvasObject.transform, new Color(0.08f, 0.08f, 0.08f, 0.94f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(880f, 620f);

            view.titleLabel = CreateText("Title", panel.transform, font, 28, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(view.titleLabel.rectTransform, new Vector2(24f, -20f), new Vector2(832f, 44f));

            view.detailsLabel = CreateText("Details", panel.transform, font, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(view.detailsLabel.rectTransform, new Vector2(24f, -76f), new Vector2(500f, 420f));

            view.feedbackLabel = CreateText("Feedback", panel.transform, font, 16, FontStyle.Normal, TextAnchor.LowerLeft);
            SetRect(view.feedbackLabel.rectTransform, new Vector2(24f, -520f), new Vector2(500f, 54f));

            GameObject listPanel = CreatePanel("Contract List", panel.transform, new Color(0.14f, 0.14f, 0.14f, 0.92f));
            RectTransform listRect = listPanel.GetComponent<RectTransform>();
            SetRect(listRect, new Vector2(548f, -76f), new Vector2(308f, 420f));
            VerticalLayoutGroup layout = listPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            view.contractListRoot = listPanel.transform;

            view.contractButtonTemplate = CreateButton("Contract Button Template", listPanel.transform, font, "Contract", new Vector2(280f, 72f));
            view.contractButtonTemplate.gameObject.SetActive(false);

            view.acceptSelectedButton = CreateButton("Accept Selected Button", panel.transform, font, "Accept Selected", new Vector2(180f, 44f));
            SetRect(view.acceptSelectedButton.GetComponent<RectTransform>(), new Vector2(548f, -548f), new Vector2(170f, 44f));

            view.closeButton = CreateButton("Close Button", panel.transform, font, "Close", new Vector2(130f, 44f));
            SetRect(view.closeButton.GetComponent<RectTransform>(), new Vector2(726f, -548f), new Vector2(130f, 44f));

            return view;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = size;
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = size.y;
            layout.preferredHeight = size.y;
            layout.flexibleHeight = 0f;
            layout.minWidth = size.x;
            layout.preferredWidth = size.x;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.24f, 0.24f, 0.24f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Label", buttonObject.transform, font, 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.text = label;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
