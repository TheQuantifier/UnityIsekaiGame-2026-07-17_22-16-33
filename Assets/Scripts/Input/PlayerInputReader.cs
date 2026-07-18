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

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private bool jumpQueued;
        private bool interactQueued;
        private bool pointerLook;

        public Vector2 Move => moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
        public Vector2 Look => lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
        public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();
        public bool IsPointerLook => pointerLook;

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
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(lookAction);
            EnableAction(jumpAction);
            EnableAction(sprintAction);
            EnableAction(interactAction);

            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
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

            DisableAction(interactAction);
            DisableAction(sprintAction);
            DisableAction(jumpAction);
            DisableAction(lookAction);
            DisableAction(moveAction);
        }

        public bool ConsumeJump()
        {
            if (!jumpQueued)
            {
                return false;
            }

            jumpQueued = false;
            return true;
        }

        public bool ConsumeInteract()
        {
            if (!interactQueued)
            {
                return false;
            }

            interactQueued = false;
            return true;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            jumpQueued = true;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            interactQueued = true;
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
