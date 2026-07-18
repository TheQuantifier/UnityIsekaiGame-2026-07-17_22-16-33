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

        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private bool isOpen;

        private void Awake()
        {
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
            }
        }
    }
}
