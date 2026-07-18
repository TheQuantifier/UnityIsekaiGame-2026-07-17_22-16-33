using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityIsekaiGame.Input
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerActionMap = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string inventoryActionName = "Inventory";
        [SerializeField] private string uiActionMap = "UI";
        [SerializeField] private string cancelActionName = "Cancel";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction inventoryAction;
        private InputAction cancelAction;
        private bool jumpQueued;
        private bool interactQueued;
        private bool inventoryQueued;
        private bool cancelQueued;
        private bool pointerLook;
        private bool gameplayInputBlocked;

        public Vector2 Move => gameplayInputBlocked || moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
        public Vector2 Look => gameplayInputBlocked || lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
        public bool SprintHeld => !gameplayInputBlocked && sprintAction != null && sprintAction.IsPressed();
        public bool IsPointerLook => pointerLook;
        public bool GameplayInputBlocked => gameplayInputBlocked;

        private void Awake()
        {
            if (inputActions == null)
            {
                throw new InvalidOperationException($"{nameof(PlayerInputReader)} requires an InputActionAsset.");
            }

            InputActionMap map = inputActions.FindActionMap(playerActionMap, true);
            moveAction = map.FindAction(moveActionName, true);
            lookAction = map.FindAction(lookActionName, true);
            jumpAction = map.FindAction(jumpActionName, true);
            sprintAction = map.FindAction(sprintActionName, false);
            interactAction = map.FindAction(interactActionName, true);
            inventoryAction = map.FindAction(inventoryActionName, true);

            InputActionMap uiMap = inputActions.FindActionMap(uiActionMap, false);
            cancelAction = uiMap?.FindAction(cancelActionName, false);
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(lookAction);
            EnableAction(jumpAction);
            EnableAction(sprintAction);
            EnableAction(interactAction);
            EnableAction(inventoryAction);
            EnableAction(cancelAction);

            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }

            if (inventoryAction != null)
            {
                inventoryAction.performed += OnInventoryPerformed;
            }

            if (cancelAction != null)
            {
                cancelAction.performed += OnCancelPerformed;
            }

            if (lookAction != null)
            {
                lookAction.performed += OnLookPerformed;
            }
        }

        private void OnDisable()
        {
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
            }

            if (lookAction != null)
            {
                lookAction.performed -= OnLookPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }

            if (inventoryAction != null)
            {
                inventoryAction.performed -= OnInventoryPerformed;
            }

            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
            }

            DisableAction(cancelAction);
            DisableAction(inventoryAction);
            DisableAction(interactAction);
            DisableAction(sprintAction);
            DisableAction(jumpAction);
            DisableAction(lookAction);
            DisableAction(moveAction);
        }

        public bool ConsumeJump()
        {
            if (gameplayInputBlocked)
            {
                jumpQueued = false;
                return false;
            }

            if (!jumpQueued)
            {
                return false;
            }

            jumpQueued = false;
            return true;
        }

        public bool ConsumeInteract()
        {
            if (gameplayInputBlocked)
            {
                interactQueued = false;
                return false;
            }

            if (!interactQueued)
            {
                return false;
            }

            interactQueued = false;
            return true;
        }

        public bool ConsumeInventory()
        {
            if (!inventoryQueued)
            {
                return false;
            }

            inventoryQueued = false;
            return true;
        }

        public bool ConsumeCancel()
        {
            if (!cancelQueued)
            {
                return false;
            }

            cancelQueued = false;
            return true;
        }

        public void SetGameplayInputBlocked(bool blocked)
        {
            gameplayInputBlocked = blocked;
            ClearGameplayActionQueues();
        }

        public void ClearGameplayActionQueues()
        {
            jumpQueued = false;
            interactQueued = false;
        }

        public void ClearCancel()
        {
            cancelQueued = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            jumpQueued = true;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            interactQueued = true;
        }

        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            inventoryQueued = true;
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            cancelQueued = true;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            pointerLook = context.control != null && context.control.device is Pointer;
        }

        private static void EnableAction(InputAction action)
        {
            if (action != null)
            {
                action.Enable();
            }
        }

        private static void DisableAction(InputAction action)
        {
            if (action != null)
            {
                action.Disable();
            }
        }
    }
}
