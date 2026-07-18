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

        private Action useSelected;
        private Action equipSelected;
        private Action unequipSelected;

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

            equipSelected = onEquipSelected;
            unequipSelected = onUnequipSelected;
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
    }
}
