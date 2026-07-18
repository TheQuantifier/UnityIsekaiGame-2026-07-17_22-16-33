using System;
using UnityEngine;
using UnityIsekaiGame.Equipment;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private VitalResource stamina = new VitalResource();
        [SerializeField, Min(0f)] private float sprintDrainPerSecond = 20f;
        [SerializeField, Min(0f)] private float regenerationPerSecond = 15f;
        [SerializeField, Min(0f)] private float regenerationDelay = 1f;
        [SerializeField, Min(0f)] private float restartThreshold = 20f;
        [SerializeField] private PlayerStats stats;

        private float regenerationBlockedUntil;
        private bool exhausted;
        private bool sprintingThisFrame;

        public float CurrentStamina => stamina.CurrentValue;
        public float MaximumStamina => stamina.MaximumValue;
        public bool CanSprint => !exhausted;
        public event Action<float, float> StaminaChanged;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (stats != null)
            {
                stamina.SetMaximum(stats.MaximumStamina);
            }

            stamina.Initialize();
            exhausted = stamina.IsEmpty;
        }

        private void OnEnable()
        {
            stamina.ValueChanged += OnStaminaChanged;

            if (stats != null)
            {
                stats.StatsChanged += OnStatsChanged;
            }
        }

        private void OnDisable()
        {
            stamina.ValueChanged -= OnStaminaChanged;

            if (stats != null)
            {
                stats.StatsChanged -= OnStatsChanged;
            }
        }

        private void LateUpdate()
        {
            if (!sprintingThisFrame)
            {
                Regenerate(Time.deltaTime);
            }

            sprintingThisFrame = false;
        }

        private void OnValidate()
        {
            stamina.Validate();
            sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
            restartThreshold = Mathf.Max(0f, restartThreshold);
        }

        public bool EvaluateSprint(bool wantsSprint, bool isMoving, bool gameplayInputBlocked, float deltaTime)
        {
            if (gameplayInputBlocked || !wantsSprint || !isMoving || sprintDrainPerSecond <= 0f)
            {
                return false;
            }

            if (exhausted)
            {
                return false;
            }

            VitalChangeResult result = stamina.Spend(sprintDrainPerSecond * deltaTime, "Stamina");
            if (!result.Succeeded)
            {
                stamina.SetCurrent(0f);
                exhausted = true;
                return false;
            }

            regenerationBlockedUntil = Time.time + regenerationDelay;

            if (stamina.IsEmpty)
            {
                exhausted = true;
            }

            sprintingThisFrame = true;
            return result.ChangedAmount > 0f;
        }

        public VitalChangeResult Restore(float amount)
        {
            VitalChangeResult result = stamina.Restore(amount, "Stamina");
            if (CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }

            return result;
        }

        private void Regenerate(float deltaTime)
        {
            if (regenerationPerSecond <= 0f || Time.time < regenerationBlockedUntil || stamina.IsAtMaximum)
            {
                return;
            }

            stamina.Restore(regenerationPerSecond * deltaTime, "Stamina");

            if (exhausted && CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }
        }

        private void OnStaminaChanged(float current, float maximum)
        {
            StaminaChanged?.Invoke(current, maximum);
        }

        private void OnStatsChanged()
        {
            stamina.SetMaximum(stats.MaximumStamina);
            if (exhausted && CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }
        }
    }
}
