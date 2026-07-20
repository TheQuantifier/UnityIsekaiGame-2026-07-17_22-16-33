using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.UI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityIsekaiGame.Development;
#endif
using CategoryDefinition = UnityIsekaiGame.GameData.CategoryDefinition;
using ItemInstance = UnityIsekaiGame.GameData.ItemInstance;
using InventorySlot = UnityIsekaiGame.Inventory.InventorySlot;
using ItemDefinition = UnityIsekaiGame.Inventory.ItemDefinition;
using TagDefinition = UnityIsekaiGame.GameData.TagDefinition;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventoryScreenView : MonoBehaviour
    {
        private const float NavigationButtonHeight = 42f;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private InventorySlotView[] slotViews;
        [SerializeField] private EquipmentSlotView[] equipmentSlotViews;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private GameObject selectedItemDetailsRoot;
        [SerializeField] private Text selectedItemHeaderText;
        [SerializeField] private Text selectedItemDetailsText;
        [SerializeField] private GameObject inventoryContentRoot;
        [SerializeField] private GameObject characterContentRoot;
        [SerializeField] private GameObject characterStatsRoot;
        [SerializeField] private Text characterStatsText;
        [SerializeField] private StatusEffectReadoutView statusReadoutView;
        [SerializeField] private Text statusReadoutText;
        [SerializeField] private GameObject spellsContentRoot;
        [SerializeField] private GameObject contractsContentRoot;
        [SerializeField] private GameObject saveLoadContentRoot;
        [SerializeField] private SaveLoadMenuView saveLoadView;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private GameObject testLabContentRoot;
        [SerializeField] private PrototypeTestLabView testLabView;
#endif
        [SerializeField] private Button inventoryMenuButton;
        [SerializeField] private Button characterMenuButton;
        [SerializeField] private Button spellsMenuButton;
        [SerializeField] private Button contractsMenuButton;
        [SerializeField] private Button saveLoadMenuButton;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Button testLabMenuButton;
#endif
        [SerializeField] private Image inventoryMenuButtonImage;
        [SerializeField] private Image characterMenuButtonImage;
        [SerializeField] private Image spellsMenuButtonImage;
        [SerializeField] private Image contractsMenuButtonImage;
        [SerializeField] private Image saveLoadMenuButtonImage;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Image testLabMenuButtonImage;
#endif
        [SerializeField] private Color inactiveMenuColor = new Color(0.12f, 0.14f, 0.16f, 0.95f);
        [SerializeField] private Color activeMenuColor = new Color(0.2f, 0.42f, 0.55f, 1f);

        private Action useSelected;
        private Action equipSelected;
        private Action unequipSelected;
        private InventoryMenuSection activeSection = InventoryMenuSection.Inventory;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public int SlotCount => slotViews == null ? 0 : slotViews.Length;

        public void Initialize(Action<int> onSlotSelected, Action onUseSelected, Action<EquipmentSlotType> onEquipmentSlotSelected = null, Action onEquipSelected = null, Action onUnequipSelected = null, Action<int, bool> onSlotHovered = null)
        {
            if (slotViews != null)
            {
                for (int i = 0; i < slotViews.Length; i++)
                {
                    if (slotViews[i] != null)
                    {
                        slotViews[i].Initialize(i, onSlotSelected, onSlotHovered);
                    }
                }
            }

            if (useButton != null)
            {
                useButton.onClick.RemoveListener(InvokeUseSelected);
                useButton.onClick.AddListener(InvokeUseSelected);
            }

            useSelected = onUseSelected;

            if (equipmentSlotViews != null)
            {
                for (int i = 0; i < equipmentSlotViews.Length; i++)
                {
                    if (equipmentSlotViews[i] != null)
                    {
                        equipmentSlotViews[i].Initialize((EquipmentSlotType)i, onEquipmentSlotSelected);
                    }
                }
            }

            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(InvokeEquipSelected);
                equipButton.onClick.AddListener(InvokeEquipSelected);
            }

            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveListener(InvokeUnequipSelected);
                unequipButton.onClick.AddListener(InvokeUnequipSelected);
            }

            if (inventoryMenuButton != null)
            {
                inventoryMenuButton.onClick.RemoveListener(ShowInventorySection);
                inventoryMenuButton.onClick.AddListener(ShowInventorySection);
            }

            if (characterMenuButton != null)
            {
                characterMenuButton.onClick.RemoveListener(ShowCharacterSection);
                characterMenuButton.onClick.AddListener(ShowCharacterSection);
            }

            if (spellsMenuButton != null)
            {
                spellsMenuButton.onClick.RemoveListener(ShowSpellsSection);
                spellsMenuButton.onClick.AddListener(ShowSpellsSection);
            }

            if (contractsMenuButton != null)
            {
                contractsMenuButton.onClick.RemoveListener(ShowContractsSection);
                contractsMenuButton.onClick.AddListener(ShowContractsSection);
            }

            EnsureSaveLoadMenuObjects();
            if (saveLoadMenuButton != null)
            {
                saveLoadMenuButton.onClick.RemoveListener(ShowSaveLoadSection);
                saveLoadMenuButton.onClick.AddListener(ShowSaveLoadSection);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureTestLabMenuObjects();
            if (testLabMenuButton != null)
            {
                testLabMenuButton.onClick.RemoveListener(ShowTestLabSection);
                testLabMenuButton.onClick.AddListener(ShowTestLabSection);
            }
#endif

            equipSelected = onEquipSelected;
            unequipSelected = onUnequipSelected;
            ApplyPrototypeMenuLayout();
            ApplyActiveSection();
        }

        public void InitializeSaveLoad(PrototypePersistenceServiceBehaviour persistence)
        {
            EnsureSaveLoadMenuObjects();
            saveLoadView?.Initialize(persistence);
            ApplyActiveSection();
        }

        public void RefreshSaveLoad()
        {
            saveLoadView?.Refresh();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void InitializeTestLab(PrototypeTestLabService service)
        {
            EnsureTestLabMenuObjects();
            testLabView?.Initialize(service);
            ApplyActiveSection();
        }

        public void RefreshTestLab()
        {
            testLabView?.Refresh();
        }
#endif

        public void Render(IReadOnlyList<InventorySlot> slots)
        {
            if (slotViews == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] == null)
                {
                    continue;
                }

                if (slots != null && i < slots.Count)
                {
                    slotViews[i].Render(slots[i]);
                    continue;
                }

                slotViews[i].RenderEmpty();
            }
        }

        public void RenderSelectedItemDetails(InventorySlot slot, bool includeDescription = false)
        {
            if (slot == null || slot.IsEmpty || slot.Item == null)
            {
                if (selectedItemDetailsRoot != null)
                {
                    selectedItemDetailsRoot.SetActive(false);
                }

                return;
            }

            EnsureItemDetailsPanel();

            if (selectedItemDetailsRoot != null)
            {
                selectedItemDetailsRoot.SetActive(true);
            }

            if (selectedItemHeaderText != null)
            {
                selectedItemHeaderText.text = InventoryItemDetailsFormatter.GetHeader(slot);
            }

            if (selectedItemDetailsText != null)
            {
                selectedItemDetailsText.text = InventoryItemDetailsFormatter.FormatDetails(slot, includeDescription);
            }
        }

        public void SetSelectedSlot(int selectedIndex)
        {
            if (slotViews == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] != null)
                {
                    slotViews[i].SetSelected(i == selectedIndex);
                }
            }
        }

        public void RenderEquipment(IReadOnlyList<EquipmentSlotState> equipmentSlots)
        {
            if (equipmentSlotViews == null)
            {
                return;
            }

            for (int i = 0; i < equipmentSlotViews.Length; i++)
            {
                if (equipmentSlotViews[i] == null)
                {
                    continue;
                }

                equipmentSlotViews[i].Render(equipmentSlots != null && i < equipmentSlots.Count ? equipmentSlots[i] : null);
            }
        }

        public void RenderCharacter(
            PlayerStats stats,
            PlayerHealth health,
            PlayerStamina stamina,
            PlayerMana mana,
            StatusEffectController statusEffects,
            CharacterAttributes attributes = null,
            CalculatedStatCollection calculatedStats = null,
            CharacterSkillCollection skills = null)
        {
            EnsureCharacterStatsPanel();

            if (characterStatsRoot != null)
            {
                characterStatsRoot.SetActive(true);
            }

            if (characterStatsText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Vitals");
            AppendLine(builder, "Health", health == null ? "--" : $"{FormatNumber(health.CurrentHealth)}/{FormatNumber(health.MaximumHealth)}");
            AppendLine(builder, "Stamina", stamina == null ? "--" : $"{FormatNumber(stamina.CurrentStamina)}/{FormatNumber(stamina.MaximumStamina)}");
            AppendLine(builder, "Mana", mana == null ? "--" : $"{FormatNumber(mana.CurrentMana)}/{FormatNumber(mana.MaximumMana)}");

            builder.AppendLine();
            builder.AppendLine("Combat Summary");
            AppendLine(builder, "Physical Power", stats == null ? "--" : FormatNumber(stats.AttackPower));
            AppendLine(builder, "Physical Defense", stats == null ? "--" : FormatNumber(stats.Defense));

            if (attributes != null && attributes.IsConfigured)
            {
                builder.AppendLine();
                builder.AppendLine("Base Attributes");
                List<string> parts = new List<string>();
                foreach (RuntimeAttributeValueRecord record in attributes.GetOrderedValues())
                {
                    parts.Add($"{FormatDefinitionName(record.attributeId)} {Mathf.FloorToInt(record.currentValue)}");
                }

                AppendCompactPairs(builder, parts);
            }

            if (calculatedStats != null && calculatedStats.IsConfigured)
            {
                builder.AppendLine();
                builder.AppendLine("Calculated Stats");
                List<string> parts = new List<string>();
                foreach (CalculatedStatDefinition definition in calculatedStats.GetOrderedDefinitions(characterMenuOnly: true))
                {
                    string resource = definition.IsResourceMaximum ? $" [{definition.LinkedFutureResourceId} max]" : string.Empty;
                    parts.Add($"{definition.DisplayName} {FormatNumber(calculatedStats.GetValue(definition.Id))}{resource}");
                }

                AppendCompactPairs(builder, parts);
            }

            if (skills != null)
            {
                builder.AppendLine();
                builder.AppendLine("Skills");
                IReadOnlyList<RuntimeSkillRecord> learned = skills.LearnedSkills;
                if (learned == null || learned.Count == 0)
                {
                    builder.AppendLine("None");
                }
                else
                {
                    List<string> parts = new List<string>();
                    foreach (RuntimeSkillRecord record in learned)
                    {
                        SkillGrade grade = SkillGradeUtility.Clamp((SkillGrade)record.currentGrade);
                        string progress = grade == SkillGrade.AAA ? "Mastered" : $"{record.currentXp} XP";
                        parts.Add($"{FormatDefinitionName(record.skillDefinitionId)} {grade} ({progress})");
                    }

                    AppendCompactPairs(builder, parts);
                }
            }

            characterStatsText.text = builder.ToString().TrimEnd();
            statusReadoutView?.SetStatusController(statusEffects);
        }

        public void SetSelectedEquipmentSlot(EquipmentSlotType selectedSlot)
        {
            if (equipmentSlotViews == null)
            {
                return;
            }

            for (int i = 0; i < equipmentSlotViews.Length; i++)
            {
                if (equipmentSlotViews[i] != null)
                {
                    equipmentSlotViews[i].SetSelected(i == (int)selectedSlot);
                }
            }
        }

        public void SetEquipmentActions(bool canEquip, bool canUnequip)
        {
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(canEquip);
            }

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(canUnequip);
            }
        }

        public void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        public void Show()
        {
            ApplyActiveSection();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
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

        private void InvokeUseSelected()
        {
            useSelected?.Invoke();
        }

        private void InvokeEquipSelected()
        {
            equipSelected?.Invoke();
        }

        private void InvokeUnequipSelected()
        {
            unequipSelected?.Invoke();
        }

        private void ShowInventorySection()
        {
            activeSection = InventoryMenuSection.Inventory;
            ApplyActiveSection();
        }

        private void ShowSpellsSection()
        {
            activeSection = InventoryMenuSection.Spells;
            ApplyActiveSection();
        }

        private void ShowCharacterSection()
        {
            activeSection = InventoryMenuSection.Character;
            ApplyActiveSection();
        }

        private void ShowContractsSection()
        {
            activeSection = InventoryMenuSection.Contracts;
            ApplyActiveSection();
        }

        private void ShowSaveLoadSection()
        {
            activeSection = InventoryMenuSection.SaveLoad;
            saveLoadView?.Refresh();
            ApplyActiveSection();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void ShowTestLabSection()
        {
            activeSection = InventoryMenuSection.TestLab;
            ApplyActiveSection();
        }
#endif

        private void ApplyActiveSection()
        {
            bool inventoryActive = activeSection == InventoryMenuSection.Inventory;
            bool characterActive = activeSection == InventoryMenuSection.Character;
            bool spellsActive = activeSection == InventoryMenuSection.Spells;
            bool contractsActive = activeSection == InventoryMenuSection.Contracts;
            bool saveLoadActive = activeSection == InventoryMenuSection.SaveLoad;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool testLabActive = activeSection == InventoryMenuSection.TestLab;
#endif

            if (inventoryContentRoot != null)
            {
                inventoryContentRoot.SetActive(inventoryActive);
            }

            if (characterContentRoot != null)
            {
                characterContentRoot.SetActive(characterActive);
            }

            if (spellsContentRoot != null)
            {
                spellsContentRoot.SetActive(spellsActive);
            }

            if (contractsContentRoot != null)
            {
                contractsContentRoot.SetActive(contractsActive);
            }

            if (saveLoadContentRoot != null)
            {
                saveLoadContentRoot.SetActive(saveLoadActive);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (testLabContentRoot != null)
            {
                testLabContentRoot.SetActive(testLabActive);
            }

            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(!testLabActive);
            }
#else
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(true);
            }
#endif

            if (inventoryMenuButtonImage != null)
            {
                inventoryMenuButtonImage.color = inventoryActive ? activeMenuColor : inactiveMenuColor;
            }

            if (characterMenuButtonImage != null)
            {
                characterMenuButtonImage.color = characterActive ? activeMenuColor : inactiveMenuColor;
            }

            if (spellsMenuButtonImage != null)
            {
                spellsMenuButtonImage.color = spellsActive ? activeMenuColor : inactiveMenuColor;
            }

            if (contractsMenuButtonImage != null)
            {
                contractsMenuButtonImage.color = contractsActive ? activeMenuColor : inactiveMenuColor;
            }

            if (saveLoadMenuButtonImage != null)
            {
                saveLoadMenuButtonImage.color = saveLoadActive ? activeMenuColor : inactiveMenuColor;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (testLabMenuButtonImage != null)
            {
                testLabMenuButtonImage.color = testLabActive ? activeMenuColor : inactiveMenuColor;
            }
#endif
        }

        private void ApplyPrototypeMenuLayout()
        {
            Transform navigationParent = characterMenuButton == null ? inventoryMenuButton == null ? null : inventoryMenuButton.transform.parent : characterMenuButton.transform.parent;
            if (navigationParent != null)
            {
                VerticalLayoutGroup layout = navigationParent.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = navigationParent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.UpperCenter;
            }

            ConfigureNavigationButton(characterMenuButton, "Character", 0);
            ConfigureNavigationButton(inventoryMenuButton, "Inventory", 1);
            ConfigureNavigationButton(spellsMenuButton, "Spells", 2);
            ConfigureNavigationButton(contractsMenuButton, "Journal", 3);
            ConfigureNavigationButton(saveLoadMenuButton, "Save/Load", 4);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ConfigureNavigationButton(testLabMenuButton, "Test Lab", 5);
#endif
        }

        private static void ConfigureNavigationButton(Button button, string label, int siblingIndex)
        {
            if (button == null)
            {
                return;
            }

            button.transform.SetSiblingIndex(siblingIndex);
            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }

            ApplyNavigationButtonLayout(layoutElement);

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = 12;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(6f, 2f);
                textRect.offsetMax = new Vector2(-6f, -2f);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void EnsureTestLabMenuObjects()
        {
            Font font = feedbackText == null ? null : feedbackText.font;
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (testLabMenuButton == null)
            {
                Transform buttonParent = contractsMenuButton == null ? transform : contractsMenuButton.transform.parent;
                GameObject buttonObject = new GameObject("Test Lab Menu Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(buttonParent, false);
                testLabMenuButtonImage = buttonObject.GetComponent<Image>();
                testLabMenuButtonImage.color = inactiveMenuColor;
                testLabMenuButton = buttonObject.GetComponent<Button>();

                Text label = CreateDetailsText("Label", buttonObject.transform, font, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(6f, 2f);
                labelRect.offsetMax = new Vector2(-6f, -2f);
                label.text = "Test Lab";

                LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
                ApplyNavigationButtonLayout(layout);
            }

            if (testLabContentRoot == null)
            {
                Transform contentParent = contractsContentRoot == null ? transform : contractsContentRoot.transform.parent;
                testLabContentRoot = new GameObject("Test Lab Content", typeof(RectTransform));
                testLabContentRoot.transform.SetParent(contentParent, false);
                RectTransform rectTransform = testLabContentRoot.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            if (testLabView == null)
            {
                testLabView = testLabContentRoot.GetComponent<PrototypeTestLabView>();
                if (testLabView == null)
                {
                    testLabView = testLabContentRoot.AddComponent<PrototypeTestLabView>();
                }
            }

            if (testLabMenuButtonImage == null && testLabMenuButton != null)
            {
                testLabMenuButtonImage = testLabMenuButton.GetComponent<Image>();
            }
        }
#endif

        private static void ApplyNavigationButtonLayout(LayoutElement layoutElement)
        {
            if (layoutElement == null)
            {
                return;
            }

            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = -1f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = NavigationButtonHeight;
            layoutElement.preferredHeight = NavigationButtonHeight;
            layoutElement.flexibleHeight = 0f;
        }

        private void EnsureSaveLoadMenuObjects()
        {
            Font font = feedbackText == null ? null : feedbackText.font;
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (saveLoadMenuButton == null)
            {
                Transform buttonParent = contractsMenuButton == null ? transform : contractsMenuButton.transform.parent;
                GameObject buttonObject = new GameObject("Save Load Menu Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(buttonParent, false);
                saveLoadMenuButtonImage = buttonObject.GetComponent<Image>();
                saveLoadMenuButtonImage.color = inactiveMenuColor;
                saveLoadMenuButton = buttonObject.GetComponent<Button>();

                Text label = CreateDetailsText("Label", buttonObject.transform, font, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(6f, 2f);
                labelRect.offsetMax = new Vector2(-6f, -2f);
                label.text = "Save/Load";

                LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
                ApplyNavigationButtonLayout(layout);
            }

            if (saveLoadContentRoot == null)
            {
                Transform contentParent = contractsContentRoot == null ? transform : contractsContentRoot.transform.parent;
                saveLoadContentRoot = new GameObject("Save Load Content", typeof(RectTransform));
                saveLoadContentRoot.transform.SetParent(contentParent, false);
                RectTransform rectTransform = saveLoadContentRoot.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            if (saveLoadView == null)
            {
                saveLoadView = saveLoadContentRoot.GetComponent<SaveLoadMenuView>();
                if (saveLoadView == null)
                {
                    saveLoadView = saveLoadContentRoot.AddComponent<SaveLoadMenuView>();
                }
            }

            if (saveLoadMenuButtonImage == null && saveLoadMenuButton != null)
            {
                saveLoadMenuButtonImage = saveLoadMenuButton.GetComponent<Image>();
            }
        }

        private void EnsureItemDetailsPanel()
        {
            if (selectedItemHeaderText != null && selectedItemDetailsText != null)
            {
                return;
            }

            Transform parent = inventoryContentRoot == null ? transform : inventoryContentRoot.transform;
            Font font = feedbackText == null ? null : feedbackText.font;
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            selectedItemDetailsRoot = selectedItemDetailsRoot == null
                ? CreateDetailsRoot(parent)
                : selectedItemDetailsRoot;

            if (selectedItemHeaderText == null)
            {
                selectedItemHeaderText = CreateDetailsText("Header", selectedItemDetailsRoot.transform, font, 18, FontStyle.Bold, TextAnchor.UpperLeft);
                RectTransform rectTransform = selectedItemHeaderText.rectTransform;
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.offsetMin = new Vector2(12f, -44f);
                rectTransform.offsetMax = new Vector2(-12f, -10f);
            }

            if (selectedItemDetailsText == null)
            {
                selectedItemDetailsText = CreateDetailsText("Details", selectedItemDetailsRoot.transform, font, 14, FontStyle.Normal, TextAnchor.UpperLeft);
                RectTransform rectTransform = selectedItemDetailsText.rectTransform;
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.offsetMin = new Vector2(12f, 12f);
                rectTransform.offsetMax = new Vector2(-12f, -50f);
            }
        }

        private void EnsureCharacterStatsPanel()
        {
            if (characterStatsText != null)
            {
                return;
            }

            Transform parent = characterContentRoot == null ? transform : characterContentRoot.transform;
            Font font = feedbackText == null ? null : feedbackText.font;
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            characterStatsRoot = characterStatsRoot == null
                ? CreateCharacterStatsRoot(parent)
                : characterStatsRoot;

            characterStatsText = CreateDetailsText("Character Stats And Status", characterStatsRoot.transform, font, 12, FontStyle.Normal, TextAnchor.UpperLeft);
            RectTransform rectTransform = characterStatsText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.28f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(18f, 16f);
            rectTransform.offsetMax = new Vector2(-18f, -16f);

            statusReadoutText = statusReadoutText == null
                ? CreateDetailsText("Status Effects", characterStatsRoot.transform, font, 16, FontStyle.Normal, TextAnchor.UpperLeft)
                : statusReadoutText;
            RectTransform statusRect = statusReadoutText.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = new Vector2(1f, 0.25f);
            statusRect.offsetMin = new Vector2(18f, 16f);
            statusRect.offsetMax = new Vector2(-18f, -8f);

            if (statusReadoutView == null)
            {
                statusReadoutView = statusReadoutText.GetComponent<StatusEffectReadoutView>();
                if (statusReadoutView == null)
                {
                    statusReadoutView = statusReadoutText.gameObject.AddComponent<StatusEffectReadoutView>();
                }
            }
        }

        private static GameObject CreateCharacterStatsRoot(Transform parent)
        {
            GameObject root = new GameObject("Character Stats And Status", typeof(RectTransform), typeof(Image), typeof(Outline));
            root.transform.SetParent(parent, false);

            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(410f, -56f);
            rectTransform.sizeDelta = new Vector2(430f, 470f);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);

            Outline outline = root.GetComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.35f, 0.39f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            return root;
        }

        private static void AppendLine(StringBuilder builder, string label, string value)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.AppendLine(value);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        private static string FormatDefinitionName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "Unknown";
            }

            int index = id.IndexOf('.');
            string name = index >= 0 && index + 1 < id.Length ? id.Substring(index + 1) : id;
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace('-', ' '));
        }

        private static void AppendCompactPairs(StringBuilder builder, IReadOnlyList<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (int i = 0; i < parts.Count; i += 2)
            {
                builder.Append(parts[i]);
                if (i + 1 < parts.Count)
                {
                    builder.Append(" | ");
                    builder.Append(parts[i + 1]);
                }

                builder.AppendLine();
            }
        }

        private static GameObject CreateDetailsRoot(Transform parent)
        {
            GameObject root = new GameObject("Selected Item Details", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.52f, 0.02f);
            rectTransform.anchorMax = new Vector2(0.98f, 0.36f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.06f, 0.08f, 0.09f, 0.92f);

            return root;
        }

        private static Text CreateDetailsText(string name, Transform parent, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;

            return text;
        }

        private enum InventoryMenuSection
        {
            Inventory,
            Character,
            Spells,
            Contracts,
            SaveLoad,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TestLab
#endif
        }
    }

    public static class InventoryItemDetailsFormatter
    {
        public static string GetHeader(InventorySlot slot)
        {
            ItemDefinition item = slot == null || slot.IsEmpty ? null : slot.Item;
            if (item == null)
            {
                return "No item selected";
            }

            return string.IsNullOrWhiteSpace(item.DisplayName) ? item.ItemId : item.DisplayName;
        }

        public static string FormatDetails(InventorySlot slot, bool includeDescription = false)
        {
            ItemDefinition item = slot == null || slot.IsEmpty ? null : slot.Item;
            if (item == null)
            {
                return "Select an inventory slot to inspect item type, tags, stack size, and stats.";
            }

            StringBuilder builder = new StringBuilder();
            AppendLine(builder, "Definition ID", string.IsNullOrWhiteSpace(item.ItemId) ? "Unassigned" : item.ItemId);
            AppendLine(builder, "Type", GetCategoryName(item.PrimaryCategory));
            AppendLine(builder, "Rarity", item.Rarity == null ? "Unassigned" : item.Rarity.DisplayName);
            AppendLine(builder, "Tags", FormatTags(item.Tags));
            AppendLine(builder, "Quantity", slot.Quantity.ToString());
            AppendLine(builder, "Stack", item.Stackable ? $"Stackable, max {item.MaximumStackSize}" : "Not stackable");
            AppendLine(builder, "Instance Mode", slot.IsStateful ? "Stateful instance" : item.InstanceMode.ToString());
            AppendLine(builder, "Capability", FormatCapabilities(item));
            AppendInstanceDetails(builder, slot.ItemInstance, item);

            if (includeDescription && !string.IsNullOrWhiteSpace(item.Description))
            {
                builder.AppendLine();
                builder.AppendLine(item.Description);
                AppendDescriptionInstanceId(builder, slot.ItemInstance);
            }

            AppendUseDetails(builder, item);
            AppendEquipmentDetails(builder, item.Equipment);

            return builder.ToString().TrimEnd();
        }

        private static void AppendInstanceDetails(StringBuilder builder, ItemInstance itemInstance, ItemDefinition item)
        {
            if (itemInstance == null)
            {
                if (item != null && item.InstanceMode != UnityIsekaiGame.GameData.ItemInstanceMode.DefinitionOnly)
                {
                    AppendLine(builder, "Unique Instance ID", "Not instanced");
                }

                return;
            }

            AppendLine(builder, "Unique Instance ID", itemInstance.HasPersistentIdentity ? itemInstance.InstanceId : "Runtime only");

            if (itemInstance.Metadata == null || !itemInstance.Metadata.HasAnyState)
            {
                return;
            }

            if (itemInstance.Metadata.HasQuality)
            {
                AppendLine(builder, "Quality", itemInstance.Metadata.Quality == null ? "Unassigned" : itemInstance.Metadata.Quality.DisplayName);
            }

            if (itemInstance.Metadata.HasCondition)
            {
                AppendLine(builder, "Condition", $"{Mathf.RoundToInt(itemInstance.Metadata.ConditionNormalized * 100f)}%");
            }
        }

        private static void AppendDescriptionInstanceId(StringBuilder builder, ItemInstance itemInstance)
        {
            if (itemInstance == null)
            {
                return;
            }

            builder.AppendLine();
            AppendLine(builder, "Unique Instance ID", itemInstance.HasPersistentIdentity ? itemInstance.InstanceId : "Runtime only");
        }

        private static void AppendUseDetails(StringBuilder builder, ItemDefinition item)
        {
            if (item == null || !item.IsUsable)
            {
                return;
            }

            builder.AppendLine();
            AppendLine(builder, "Use Effects", $"{item.UseEffectCount} configured");

            if (item.HasMissingUseEffect)
            {
                AppendLine(builder, "Use Warning", "Missing effect reference");
            }
        }

        private static void AppendEquipmentDetails(StringBuilder builder, EquipmentData equipment)
        {
            if (equipment == null || !equipment.Equippable)
            {
                return;
            }

            builder.AppendLine();
            AppendLine(builder, "Equip Slot", SplitPascalCase(equipment.SlotType.ToString()));
            AppendLine(builder, "Stats", FormatStats(equipment.StatModifiers));

            if (equipment.MeleeWeapon != null && equipment.MeleeWeapon.IsWeapon)
            {
                AppendLine(builder, "Attack", equipment.MeleeWeapon.AttackName);
                AppendLine(builder, "Damage", FormatNumber(equipment.MeleeWeapon.BaseDamage));
                AppendLine(builder, "Range", FormatNumber(equipment.MeleeWeapon.AttackRange));
                AppendLine(builder, "Cooldown", $"{FormatNumber(equipment.MeleeWeapon.AttackCooldown)}s");
                AppendLine(builder, "Stamina Cost", FormatNumber(equipment.MeleeWeapon.StaminaCost));
            }
        }

        private static string FormatCapabilities(ItemDefinition item)
        {
            bool usable = item != null && item.IsUsable;
            bool equippable = item != null && item.IsEquippable;

            if (usable && equippable)
            {
                return "Usable, equippable";
            }

            if (usable)
            {
                return "Usable";
            }

            if (equippable)
            {
                return "Equippable";
            }

            return "Inventory item";
        }

        private static string FormatStats(StatModifiers stats)
        {
            List<string> parts = new List<string>();
            AddStat(parts, "Max Health", stats.MaximumHealth);
            AddStat(parts, "Max Stamina", stats.MaximumStamina);
            AddStat(parts, "Max Mana", stats.MaximumMana);
            AddStat(parts, "Attack", stats.AttackPower);
            AddStat(parts, "Defense", stats.Defense);

            return parts.Count == 0 ? "None" : string.Join(", ", parts);
        }

        private static void AddStat(List<string> parts, string label, float value)
        {
            if (value == 0f)
            {
                return;
            }

            string sign = value > 0f ? "+" : string.Empty;
            parts.Add($"{label} {sign}{FormatNumber(value)}");
        }

        private static string FormatTags(IReadOnlyList<TagDefinition> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return "None";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                {
                    names.Add(tags[i].DisplayName);
                }
            }

            return names.Count == 0 ? "None" : string.Join(", ", names);
        }

        private static string GetCategoryName(CategoryDefinition category)
        {
            if (category == null)
            {
                return "Uncategorized";
            }

            return category.DisplayName;
        }

        private static void AppendLine(StringBuilder builder, string label, string value)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.AppendLine(value);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && !char.IsWhiteSpace(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(value[i]);
            }

            return builder.ToString();
        }
    }
}
