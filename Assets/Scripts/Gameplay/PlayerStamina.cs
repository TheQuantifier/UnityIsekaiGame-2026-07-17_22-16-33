using System;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.ResourceSystem;

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
        [SerializeField] private CharacterResourceCollection resources;

        private float regenerationBlockedUntil;
        private bool exhausted;
        private bool sprintingThisFrame;
        private bool resourceEventsSubscribed;

        public float CurrentStamina => UseResourceRuntime ? resources.GetCurrent(ResourceIds.Stamina) : stamina.CurrentValue;
        public float MaximumStamina => UseResourceRuntime ? resources.GetMaximum(ResourceIds.Stamina) : stamina.MaximumValue;
        public bool CanSprint => !exhausted;
        private bool UseResourceRuntime => EnsureResourceRuntime() && resources.HasResource(ResourceIds.Stamina);
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

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            stamina.Initialize();
            exhausted = CurrentStamina <= 0f;
        }

        private void OnEnable()
        {
            stamina.ValueChanged += OnStaminaChanged;

            if (stats != null)
            {
                stats.StatsChanged += OnStatsChanged;
            }

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            SubscribeResourceEvents();
        }

        private void OnDisable()
        {
            stamina.ValueChanged -= OnStaminaChanged;

            if (stats != null)
            {
                stats.StatsChanged -= OnStatsChanged;
            }

            if (resources != null)
            {
                resources.ResourceChanged -= OnResourceChanged;
                resources.ResourceMaximumChanged -= OnResourceMaximumChanged;
                resources.ResourcesRestored -= OnResourcesRestored;
            }

            resourceEventsSubscribed = false;
        }

        private void LateUpdate()
        {
            if (!sprintingThisFrame)
            {
                if (!UseResourceRuntime)
                {
                    Regenerate(Time.deltaTime);
                }
                else if (exhausted && CurrentStamina > restartThreshold)
                {
                    exhausted = false;
                }
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

            VitalChangeResult result = Spend(sprintDrainPerSecond * deltaTime, "Sprint");
            if (!result.Succeeded)
            {
                if (!UseResourceRuntime)
                {
                    stamina.SetCurrent(0f);
                }

                exhausted = true;
                return false;
            }

            if (CurrentStamina <= 0f)
            {
                exhausted = true;
            }

            sprintingThisFrame = true;
            return result.ChangedAmount > 0f;
        }

        public VitalChangeResult Restore(float amount)
        {
            if (UseResourceRuntime)
            {
                VitalChangeResult resourceResult = ToVitalChangeResult(resources.TryGain(ResourceIds.Stamina, amount, "player.stamina", "Stamina restore"), "stamina");
                if (CurrentStamina > restartThreshold)
                {
                    exhausted = false;
                }

                return resourceResult;
            }

            VitalChangeResult result = stamina.Restore(amount, "Stamina");
            if (CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }

            return result;
        }

        public void RestoreToMaximum()
        {
            exhausted = false;
            sprintingThisFrame = false;
            regenerationBlockedUntil = 0f;
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Stamina, resources.GetMaximum(ResourceIds.Stamina), "player.stamina", "Restore to maximum", restoration: true);
                return;
            }

            stamina.SetCurrent(stamina.MaximumValue);
        }

        public bool TryRestoreForPersistence(float restoredStamina, out string failureReason)
        {
            failureReason = string.Empty;
            if (float.IsNaN(restoredStamina) || float.IsInfinity(restoredStamina) || restoredStamina < 0f)
            {
                failureReason = $"Stamina value {restoredStamina} is invalid for save restoration.";
                return false;
            }

            sprintingThisFrame = false;
            regenerationBlockedUntil = 0f;
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Stamina, Mathf.Clamp(restoredStamina, 0f, MaximumStamina), "player.stamina", "Persistence restore", restoration: true);
                exhausted = CurrentStamina <= 0f;
                return true;
            }

            stamina.SetCurrent(Mathf.Clamp(restoredStamina, 0f, stamina.MaximumValue));
            exhausted = stamina.IsEmpty;
            return true;
        }

        public bool CanSpend(float amount)
        {
            if (UseResourceRuntime)
            {
                return amount <= 0f || resources.CanSpend(ResourceIds.Stamina, amount);
            }

            return amount <= 0f || stamina.CanSpend(amount);
        }

        public VitalChangeResult Spend(float amount, string reason)
        {
            if (amount <= 0f)
            {
                return VitalChangeResult.Success(0f, 0f, "No stamina spent.");
            }

            if (UseResourceRuntime)
            {
                ResourceChangeResult resourceResult = resources.TrySpend(ResourceIds.Stamina, amount, "player.stamina", reason);
                if (!resourceResult.Succeeded)
                {
                    exhausted = CurrentStamina <= 0f;
                    return VitalChangeResult.Failure(resourceResult.RequestedAmount, resourceResult.Message);
                }

                if (CurrentStamina <= 0f)
                {
                    exhausted = true;
                }

                string resourceMessage = string.IsNullOrWhiteSpace(reason)
                    ? resourceResult.Message
                    : $"{reason} spent {resourceResult.AppliedAmount:0.#} stamina.";
                return VitalChangeResult.Success(resourceResult.RequestedAmount, resourceResult.AppliedAmount, resourceMessage);
            }

            VitalChangeResult result = stamina.Spend(amount, "Stamina");
            if (!result.Succeeded)
            {
                exhausted = stamina.IsEmpty;
                return result;
            }

            regenerationBlockedUntil = Time.time + regenerationDelay;
            if (stamina.IsEmpty)
            {
                exhausted = true;
            }

            string message = string.IsNullOrWhiteSpace(reason)
                ? result.Message
                : $"{reason} spent {result.ChangedAmount:0.#} stamina.";
            return VitalChangeResult.Success(result.RequestedAmount, result.ChangedAmount, message);
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
            if (UseResourceRuntime)
            {
                resources.ReconcileResource(ResourceIds.Stamina);
                if (exhausted && CurrentStamina > restartThreshold)
                {
                    exhausted = false;
                }

                return;
            }

            stamina.SetMaximum(stats.MaximumStamina);
            if (exhausted && CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }
        }

        private void OnResourceChanged(CharacterResourceCollection collection, ResourceChangeResult result)
        {
            if (!string.Equals(result.Request.ResourceId, ResourceIds.Stamina, StringComparison.Ordinal))
            {
                return;
            }

            if (exhausted && CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }
            else if (CurrentStamina <= 0f)
            {
                exhausted = true;
            }

            StaminaChanged?.Invoke(CurrentStamina, MaximumStamina);
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (string.Equals(snapshot.ResourceId, ResourceIds.Stamina, StringComparison.Ordinal))
            {
                StaminaChanged?.Invoke(CurrentStamina, MaximumStamina);
            }
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            exhausted = CurrentStamina <= 0f;
            StaminaChanged?.Invoke(CurrentStamina, MaximumStamina);
        }

        private bool EnsureResourceRuntime()
        {
            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            SubscribeResourceEvents();
            return resources != null;
        }

        private void SubscribeResourceEvents()
        {
            if (resourceEventsSubscribed || resources == null || !isActiveAndEnabled)
            {
                return;
            }

            resources.ResourceChanged += OnResourceChanged;
            resources.ResourceMaximumChanged += OnResourceMaximumChanged;
            resources.ResourcesRestored += OnResourcesRestored;
            resourceEventsSubscribed = true;
        }

        private static VitalChangeResult ToVitalChangeResult(ResourceChangeResult result, string resourceName)
        {
            if (result == null)
            {
                return VitalChangeResult.Failure(0f, $"Unable to change {resourceName}.");
            }

            return result.Succeeded
                ? VitalChangeResult.Success(result.RequestedAmount, result.AppliedAmount, result.Message)
                : VitalChangeResult.Failure(result.RequestedAmount, result.Message);
        }
    }
}
