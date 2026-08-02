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
        private float pendingSprintResourceSpend;
        private float lastNotifiedCurrentStamina = float.NaN;
        private float lastNotifiedMaximumStamina = float.NaN;
        private bool exhausted;
        private bool sprintingThisFrame;
        private bool resourceEventsSubscribed;
        private const float StaminaNotificationThreshold = 0.5f;

        public float CurrentStamina => UseResourceRuntime ? Mathf.Max(resources.GetMinimum(ResourceIds.Stamina), resources.GetCurrent(ResourceIds.Stamina) - pendingSprintResourceSpend) : stamina.CurrentValue;
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
            FlushPendingSprintResourceSpend();
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
                    FlushPendingSprintResourceSpend();
                    exhausted = false;
                }
                else
                {
                    FlushPendingSprintResourceSpend();
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
                FlushPendingSprintResourceSpend();
                return false;
            }

            if (exhausted)
            {
                FlushPendingSprintResourceSpend();
                return false;
            }

            if (UseResourceRuntime)
            {
                return EvaluateResourceRuntimeSprint(deltaTime);
            }

            VitalChangeResult result = Spend(sprintDrainPerSecond * deltaTime, "Sprint");
            if (!result.Succeeded)
            {
                stamina.SetCurrent(0f);
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
                FlushPendingSprintResourceSpend();
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
            pendingSprintResourceSpend = 0f;
            ResetStaminaNotificationCache();
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
            pendingSprintResourceSpend = 0f;
            ResetStaminaNotificationCache();
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
                return amount <= 0f || resources.CanSpend(ResourceIds.Stamina, amount + pendingSprintResourceSpend);
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
                FlushPendingSprintResourceSpend();
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

        public void FlushPendingSprintResourceSpend()
        {
            if (pendingSprintResourceSpend <= CharacterResourceCollection.Epsilon || resources == null || !resources.HasResource(ResourceIds.Stamina))
            {
                pendingSprintResourceSpend = 0f;
                return;
            }

            float amount = pendingSprintResourceSpend;
            pendingSprintResourceSpend = 0f;
            ResetStaminaNotificationCache();
            ResourceChangeResult resourceResult = resources.TrySpend(ResourceIds.Stamina, amount, "player.stamina", "Sprint", allowPartial: true);
            if (!resourceResult.Succeeded || resources.GetCurrent(ResourceIds.Stamina) <= resources.GetMinimum(ResourceIds.Stamina) + CharacterResourceCollection.Epsilon)
            {
                exhausted = true;
            }
        }

        private bool EvaluateResourceRuntimeSprint(float deltaTime)
        {
            float requested = sprintDrainPerSecond * Mathf.Max(0f, deltaTime);
            if (requested <= 0f)
            {
                return false;
            }

            float available = resources.GetCurrent(ResourceIds.Stamina) - resources.GetMinimum(ResourceIds.Stamina) - pendingSprintResourceSpend;
            if (available <= CharacterResourceCollection.Epsilon)
            {
                FlushPendingSprintResourceSpend();
                exhausted = true;
                return false;
            }

            if (requested > available + CharacterResourceCollection.Epsilon)
            {
                FlushPendingSprintResourceSpend();
                exhausted = true;
                return false;
            }

            pendingSprintResourceSpend += requested;
            sprintingThisFrame = true;
            if (pendingSprintResourceSpend >= resources.GetCurrent(ResourceIds.Stamina) - resources.GetMinimum(ResourceIds.Stamina) - CharacterResourceCollection.Epsilon)
            {
                FlushPendingSprintResourceSpend();
            }
            else
            {
                NotifyStaminaChangedIfMeaningful();
            }

            return true;
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
            lastNotifiedCurrentStamina = current;
            lastNotifiedMaximumStamina = maximum;
            StaminaChanged?.Invoke(current, maximum);
        }

        private void OnStatsChanged()
        {
            if (UseResourceRuntime)
            {
                FlushPendingSprintResourceSpend();
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

            NotifyStaminaChanged(force: true);
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (string.Equals(snapshot.ResourceId, ResourceIds.Stamina, StringComparison.Ordinal))
            {
                NotifyStaminaChanged(force: true);
            }
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            exhausted = CurrentStamina <= 0f;
            NotifyStaminaChanged(force: true);
        }

        private void NotifyStaminaChangedIfMeaningful()
        {
            float current = CurrentStamina;
            float maximum = MaximumStamina;
            if (float.IsNaN(lastNotifiedCurrentStamina)
                || Mathf.Abs(current - lastNotifiedCurrentStamina) >= StaminaNotificationThreshold
                || !Mathf.Approximately(maximum, lastNotifiedMaximumStamina))
            {
                NotifyStaminaChanged(force: true);
            }
        }

        private void NotifyStaminaChanged(bool force)
        {
            float current = CurrentStamina;
            float maximum = MaximumStamina;
            if (!force
                && !float.IsNaN(lastNotifiedCurrentStamina)
                && Mathf.Abs(current - lastNotifiedCurrentStamina) < StaminaNotificationThreshold
                && Mathf.Approximately(maximum, lastNotifiedMaximumStamina))
            {
                return;
            }

            lastNotifiedCurrentStamina = current;
            lastNotifiedMaximumStamina = maximum;
            StaminaChanged?.Invoke(current, maximum);
        }

        private void ResetStaminaNotificationCache()
        {
            lastNotifiedCurrentStamina = float.NaN;
            lastNotifiedMaximumStamina = float.NaN;
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
