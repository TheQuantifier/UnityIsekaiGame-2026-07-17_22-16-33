using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Equipment;
using CategoryDefinition = UnityIsekaiGame.GameData.CategoryDefinition;
using InventorySlot = UnityIsekaiGame.Inventory.InventorySlot;
using ItemDefinition = UnityIsekaiGame.Inventory.ItemDefinition;
using TagDefinition = UnityIsekaiGame.GameData.TagDefinition;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventoryScreenView : MonoBehaviour
    {
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
        [SerializeField] private GameObject spellsContentRoot;
        [SerializeField] private GameObject contractsContentRoot;
        [SerializeField] private Button inventoryMenuButton;
        [SerializeField] private Button characterMenuButton;
        [SerializeField] private Button spellsMenuButton;
        [SerializeField] private Button contractsMenuButton;
        [SerializeField] private Image inventoryMenuButtonImage;
        [SerializeField] private Image characterMenuButtonImage;
        [SerializeField] private Image spellsMenuButtonImage;
        [SerializeField] private Image contractsMenuButtonImage;
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

        public void Initialize(Action<int> onSlotSelected, Action onUseSelected, Action<EquipmentSlotType> onEquipmentSlotSelected = null, Action onEquipSelected = null, Action onUnequipSelected = null)
        {
            if (slotViews != null)
            {
                for (int i = 0; i < slotViews.Length; i++)
                {
                    if (slotViews[i] != null)
                    {
                        slotViews[i].Initialize(i, onSlotSelected);
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

            equipSelected = onEquipSelected;
            unequipSelected = onUnequipSelected;
            ApplyActiveSection();
        }

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

        public void RenderSelectedItemDetails(InventorySlot slot)
        {
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
                selectedItemDetailsText.text = InventoryItemDetailsFormatter.Format(slot);
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

        private void ApplyActiveSection()
        {
            bool inventoryActive = activeSection == InventoryMenuSection.Inventory;
            bool characterActive = activeSection == InventoryMenuSection.Character;
            bool spellsActive = activeSection == InventoryMenuSection.Spells;
            bool contractsActive = activeSection == InventoryMenuSection.Contracts;

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
            Contracts
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

        public static string Format(InventorySlot slot)
        {
            ItemDefinition item = slot == null || slot.IsEmpty ? null : slot.Item;
            if (item == null)
            {
                return "Select an inventory slot to inspect item type, tags, stack size, and stats.";
            }

            StringBuilder builder = new StringBuilder();
            AppendLine(builder, "ID", string.IsNullOrWhiteSpace(item.ItemId) ? "Unassigned" : item.ItemId);
            AppendLine(builder, "Type", GetCategoryName(item.PrimaryCategory));
            AppendLine(builder, "Tags", FormatTags(item.Tags));
            AppendLine(builder, "Quantity", slot.Quantity.ToString());
            AppendLine(builder, "Stack", item.Stackable ? $"Stackable, max {item.MaximumStackSize}" : "Not stackable");
            AppendLine(builder, "Capability", FormatCapabilities(item));

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                builder.AppendLine();
                builder.AppendLine(item.Description);
            }

            AppendUseDetails(builder, item);
            AppendEquipmentDetails(builder, item.Equipment);

            return builder.ToString().TrimEnd();
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
