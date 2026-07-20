using UnityEngine;

namespace UnityIsekaiGame.Configuration
{
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Unity Isekai Game/Player Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0f)] private float gravity = 24f;
        [SerializeField, Min(0f)] private float groundedStickForce = 2f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 140f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-85f, 85f);

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float GroundedStickForce => groundedStickForce;
        public float MouseSensitivity => mouseSensitivity;
        public float GamepadSensitivity => gamepadSensitivity;
        public Vector2 PitchLimits => pitchLimits;
    }
}
