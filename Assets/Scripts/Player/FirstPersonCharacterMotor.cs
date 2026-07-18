using UnityEngine;
using UnityIsekaiGame.Configuration;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonCharacterMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovementSettings movementSettings;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (input == null || movementSettings == null)
            {
                return;
            }

            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 localMove = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = input.SprintHeld ? movementSettings.SprintSpeed : movementSettings.WalkSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -movementSettings.GroundedStickForce;
            }

            if (controller.isGrounded && input.ConsumeJump())
            {
                verticalVelocity = Mathf.Sqrt(2f * movementSettings.Gravity * movementSettings.JumpHeight);
            }

            verticalVelocity -= movementSettings.Gravity * Time.deltaTime;

            Vector3 horizontalVelocity = transform.TransformDirection(localMove) * speed;
            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
