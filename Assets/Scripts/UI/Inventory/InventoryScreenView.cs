using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Equipment;

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
        [SerializeField] private GameObject inventoryContentRoot;
        [SerializeField] private GameObject characterContentRoot;
        [SerializeField] private GameObject spellsContentRoot;
        [SerializeField] private Button inventoryMenuButton;
        [SerializeField] private Button characterMenuButton;
        [SerializeField] private Button spellsMenuButton;
        [SerializeField] private Image inventoryMenuButtonImage;
        [SerializeField] private Image characterMenuButtonImage;
        [SerializeField] private Image spellsMenuButtonImage;
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

            equipSelected = onEquipSelected;
            unequipSelected = onUnequipSelected;
            ApplyActiveSection();
        }

        public void Render(IReadOnlyList<UnityIsekaiGame.Inventory.InventorySlot> slots)
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

        private void ApplyActiveSection()
        {
            bool inventoryActive = activeSection == InventoryMenuSection.Inventory;
            bool characterActive = activeSection == InventoryMenuSection.Character;
            bool spellsActive = activeSection == InventoryMenuSection.Spells;

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
        }

        private enum InventoryMenuSection
        {
            Inventory,
            Character,
            Spells
        }
    }
}
