using UnityEngine;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventoryScreenController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private InventoryScreenView view;
        [SerializeField] private GameObject itemUser;
        [SerializeField, Min(1)] private int columns = 4;

        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private bool isOpen;
        private int selectedSlotIndex;

        private void Awake()
        {
            if (itemUser == null && inventory != null)
            {
                itemUser = inventory.gameObject;
            }

            if (view != null)
            {
                view.Initialize(SelectSlot, UseSelectedItem);
            }

            Close(false);
            Refresh();
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (input.ConsumeInventory())
            {
                SetOpen(!isOpen);
                return;
            }

            if (isOpen && input.ConsumeCancel())
            {
                SetOpen(false);
                return;
            }

            if (!isOpen)
            {
                return;
            }

            if (input.ConsumeInventoryNavigate(out Vector2 direction))
            {
                MoveSelection(direction);
            }

            if (input.ConsumeInventoryUse())
            {
                UseSelectedItem();
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
            }

            if (isOpen)
            {
                Close(true);
            }
        }

        private void SetOpen(bool open)
        {
            if (open)
            {
                Open();
                return;
            }

            Close(true);
        }

        private void Open()
        {
            if (isOpen)
            {
                return;
            }

            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            isOpen = true;

            if (input != null)
            {
                input.SetGameplayInputBlocked(true);
                input.ClearCancel();
                input.ClearInventoryUiActions();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();

            if (view != null)
            {
                view.Show();
            }
        }

        private void Close(bool restoreCursor)
        {
            if (input != null)
            {
                input.ClearGameplayActionQueues();
                input.ClearInventoryUiActions();
                input.SetGameplayInputBlocked(false);
            }

            if (view != null)
            {
                view.Hide();
            }

            if (restoreCursor)
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisible;
            }

            isOpen = false;
        }

        private void Refresh()
        {
            if (view != null && inventory != null)
            {
                view.Render(inventory.Slots);
                ClampSelection();
                view.SetSelectedSlot(selectedSlotIndex);
            }
        }

        public void UseSelectedItem()
        {
            if (!isOpen || inventory == null)
            {
                return;
            }

            ItemUseResult result = inventory.UseItem(selectedSlotIndex, itemUser);
            if (!result.Succeeded)
            {
                Debug.Log(result.Message);
            }

            if (view != null)
            {
                view.SetFeedback(result.Message);
            }

            Refresh();
        }

        private void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = Mathf.Max(0, slotIndex);

            if (view != null)
            {
                view.SetSelectedSlot(selectedSlotIndex);
                view.SetFeedback(string.Empty);
            }
        }

        private void MoveSelection(Vector2 direction)
        {
            if (view == null || view.SlotCount <= 0)
            {
                return;
            }

            int delta;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                delta = direction.x > 0f ? 1 : -1;
            }
            else
            {
                delta = direction.y > 0f ? -columns : columns;
            }

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex + delta, 0, view.SlotCount - 1);
            view.SetSelectedSlot(selectedSlotIndex);
            view.SetFeedback(string.Empty);
        }

        private void ClampSelection()
        {
            int slotCount = view == null ? 0 : view.SlotCount;
            selectedSlotIndex = slotCount <= 0 ? 0 : Mathf.Clamp(selectedSlotIndex, 0, slotCount - 1);
        }
    }
}
