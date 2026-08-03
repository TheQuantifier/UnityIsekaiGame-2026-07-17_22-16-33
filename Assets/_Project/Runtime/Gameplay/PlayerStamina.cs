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
        [SerializeField] private bool publishProjectedStaminaChanges = true;
        [SerializeField, Min(0.05f)] private float staminaNotificationInterval = 0.1f;
        [SerializeField, Min(0f)] private float staminaNotificationThreshold = 0.5f;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private CharacterResourceCollection resources;

        private float regenerationBlockedUntil;
        private float pendingSprintSpend;
        private float pendingRegenerationGain;
        private float nextStaminaNotificationTime;
        private float lastNotifiedCurrentStamina = float.NaN;
        private float lastNotifiedMaximumStamina = float.NaN;
        private bool exhausted;
        private bool sprintingThisFrame;
        private bool staminaEventsSubscribed;
        private bool resourceEventsSubscribed;
        private bool pendingStaminaNotification;
        private bool pendingCommittedStaminaNotification;
        private bool publishLocalStaminaChangesImmediately;
        private bool publishResourceStaminaChangesImmediately;

        public float CurrentStamina => Mathf.Clamp(GetCommittedStamina() + pendingRegenerationGain - pendingSprintSpend, GetMinimumStamina(), MaximumStamina);
        public float MaximumStamina => UseResourceRuntime ? resources.GetMaximum(ResourceIds.Stamina) : stamina.MaximumValue;
        public bool CanSprint => !exhausted;
        private bool UseResourceRuntime => EnsureResourceRuntime() && resources.HasResource(ResourceIds.Stamina);
        public event Action<float, float> StaminaChanged;
        public event Action<float, float> CommittedStaminaChanged;

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
            SubscribeLocalStaminaEvents();

            if (stats != null)
            {
                stats.StatsChanged += OnStatsChanged;
            }

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            SubscribeResourceEvents();
            SuppressAutomaticStaminaTick();
        }

        private void OnDisable()
        {
            FlushPendingStaminaProjection();
            UnsuppressAutomaticStaminaTick();
            UnsubscribeLocalStaminaEvents();

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
                FlushPendingSprintResourceSpend();

                Regenerate(Time.deltaTime);
                if (exhausted && CurrentStamina > restartThreshold)
                {
                    exhausted = false;
                }
            }

            PublishPendingStaminaNotificationIfDue();
            sprintingThisFrame = false;
        }

        private void OnValidate()
        {
            stamina.Validate();
            sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
            restartThreshold = Mathf.Max(0f, restartThreshold);
            staminaNotificationInterval = Mathf.Max(0.05f, staminaNotificationInterval);
            staminaNotificationThreshold = Mathf.Max(0f, staminaNotificationThreshold);
        }

        public bool EvaluateSprint(bool wantsSprint, bool isMoving, bool gameplayInputBlocked, float deltaTime)
        {
            if (gameplayInputBlocked || !wantsSprint || !isMoving)
            {
                FlushPendingSprintResourceSpend();
                return false;
            }

            if (sprintDrainPerSecond <= 0f)
            {
                sprintingThisFrame = true;
                return true;
            }

            if (exhausted)
            {
                FlushPendingSprintResourceSpend();
                return false;
            }

            return ReserveSprintSpend(deltaTime);
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

            FlushPendingSprintResourceSpend();
            VitalChangeResult result = ExecuteLocalStaminaMutation(() => stamina.Restore(amount, "Stamina"), immediateNotification: true);

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
            pendingSprintSpend = 0f;
            pendingRegenerationGain = 0f;
            ResetStaminaNotificationCache();
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Stamina, resources.GetMaximum(ResourceIds.Stamina), "player.stamina", "Restore to maximum", restoration: true);
                return;
            }

            ExecuteLocalStaminaMutation(() =>
            {
                stamina.SetCurrent(stamina.MaximumValue);
                return VitalChangeResult.Success(0f, 0f, "Stamina restored.");
            }, immediateNotification: true);
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
            pendingSprintSpend = 0f;
            pendingRegenerationGain = 0f;
            ResetStaminaNotificationCache();
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Stamina, Mathf.Clamp(restoredStamina, 0f, MaximumStamina), "player.stamina", "Persistence restore", restoration: true);
                exhausted = CurrentStamina <= 0f;
                return true;
            }

            ExecuteLocalStaminaMutation(() =>
            {
                stamina.SetCurrent(Mathf.Clamp(restoredStamina, 0f, stamina.MaximumValue));
                return VitalChangeResult.Success(0f, 0f, "Stamina restored from persistence.");
            }, immediateNotification: true);
            exhausted = stamina.IsEmpty;
            return true;
        }

        public bool CanSpend(float amount)
        {
            if (UseResourceRuntime)
            {
                return amount <= 0f || resources.CanSpend(ResourceIds.Stamina, amount + pendingSprintSpend);
            }

            return amount <= 0f || stamina.CurrentValue >= amount + pendingSprintSpend;
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

            VitalChangeResult result = ExecuteLocalStaminaMutation(() => stamina.Spend(amount, "Stamina"), immediateNotification: true);
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
            FlushPendingStaminaProjection();
        }

        private void FlushPendingStaminaProjection()
        {
            if (pendingSprintSpend <= CharacterResourceCollection.Epsilon)
            {
                pendingSprintSpend = 0f;
            }

            if (pendingRegenerationGain <= CharacterResourceCollection.Epsilon)
            {
                pendingRegenerationGain = 0f;
            }

            if (pendingSprintSpend <= CharacterResourceCollection.Epsilon && pendingRegenerationGain <= CharacterResourceCollection.Epsilon)
            {
                pendingSprintSpend = 0f;
                pendingRegenerationGain = 0f;
                return;
            }

            bool committedSprintSpend = pendingSprintSpend > CharacterResourceCollection.Epsilon;
            float projected = CurrentStamina;
            pendingSprintSpend = 0f;
            pendingRegenerationGain = 0f;
            ResetStaminaNotificationCache();
            if (committedSprintSpend)
            {
                regenerationBlockedUntil = Time.time + regenerationDelay;
            }

            if (UseResourceRuntime)
            {
                publishResourceStaminaChangesImmediately = true;
                ResourceChangeResult resourceResult;
                try
                {
                    resourceResult = resources.ApplyChange(new ResourceChangeRequest(
                        ResourceIds.Stamina,
                        ResourceChangeOperation.Administrative,
                        projected,
                        ResourceChangeSourceCategory.Gameplay,
                        "player.stamina",
                        "Stamina projection commit."));
                }
                finally
                {
                    publishResourceStaminaChangesImmediately = false;
                }

                if (!resourceResult.Succeeded || resources.GetCurrent(ResourceIds.Stamina) <= resources.GetMinimum(ResourceIds.Stamina) + CharacterResourceCollection.Epsilon)
                {
                    exhausted = true;
                }

                return;
            }

            VitalChangeResult result = ExecuteLocalStaminaMutation(
                () =>
                {
                    float previous = stamina.CurrentValue;
                    stamina.SetCurrent(projected);
                    return VitalChangeResult.Success(0f, Mathf.Abs(projected - previous), "Stamina projection committed.");
                },
                immediateNotification: true);

            if (!result.Succeeded || stamina.IsEmpty)
            {
                exhausted = true;
            }
        }

        private bool ReserveSprintSpend(float deltaTime)
        {
            float requested = sprintDrainPerSecond * Mathf.Max(0f, deltaTime);
            if (requested <= 0f)
            {
                return false;
            }

            float available = GetCommittedStamina() - GetMinimumStamina() - pendingSprintSpend;
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

            pendingSprintSpend += requested;
            sprintingThisFrame = true;
            if (pendingSprintSpend >= GetCommittedStamina() - GetMinimumStamina() - CharacterResourceCollection.Epsilon)
            {
                FlushPendingSprintResourceSpend();
            }
            else if (publishProjectedStaminaChanges)
            {
                NotifyStaminaChangedIfMeaningful();
            }

            return true;
        }

        private void Regenerate(float deltaTime)
        {
            if (regenerationPerSecond <= 0f || Time.time < regenerationBlockedUntil || CurrentStamina >= MaximumStamina - CharacterResourceCollection.Epsilon)
            {
                return;
            }

            float availableRecovery = MaximumStamina - CurrentStamina;
            float amount = Mathf.Min(availableRecovery, regenerationPerSecond * Mathf.Max(0f, deltaTime));
            if (amount <= CharacterResourceCollection.Epsilon)
            {
                return;
            }

            pendingRegenerationGain += amount;
            if (CurrentStamina >= MaximumStamina - CharacterResourceCollection.Epsilon)
            {
                FlushPendingStaminaProjection();
            }
            else if (publishProjectedStaminaChanges)
            {
                NotifyStaminaChangedIfMeaningful();
            }

            if (exhausted && CurrentStamina > restartThreshold)
            {
                exhausted = false;
            }
        }

        private void OnStaminaChanged(float current, float maximum)
        {
            NotifyStaminaChanged(force: true, committed: true, immediate: publishLocalStaminaChangesImmediately);
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

            FlushPendingSprintResourceSpend();
            ExecuteLocalStaminaMutation(() =>
            {
                stamina.SetMaximum(stats.MaximumStamina);
                return VitalChangeResult.Success(0f, 0f, "Stamina maximum changed.");
            }, immediateNotification: true);
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

            NotifyStaminaChanged(force: true, committed: true, immediate: publishResourceStaminaChangesImmediately);
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (string.Equals(snapshot.ResourceId, ResourceIds.Stamina, StringComparison.Ordinal))
            {
                NotifyStaminaChanged(force: true, committed: true, immediate: true);
            }
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            exhausted = CurrentStamina <= 0f;
            NotifyStaminaChanged(force: true, committed: true, immediate: true);
        }

        private void NotifyStaminaChangedIfMeaningful()
        {
            float current = CurrentStamina;
            float maximum = MaximumStamina;
            float threshold = Mathf.Max(0f, staminaNotificationThreshold);
            if (float.IsNaN(lastNotifiedCurrentStamina)
                || Mathf.Abs(current - lastNotifiedCurrentStamina) >= threshold
                || !Mathf.Approximately(maximum, lastNotifiedMaximumStamina))
            {
                NotifyStaminaChanged(force: true, committed: false, immediate: false);
            }
        }

        private void NotifyStaminaChanged(bool force, bool committed, bool immediate)
        {
            float current = CurrentStamina;
            float maximum = MaximumStamina;
            if (!force
                && !float.IsNaN(lastNotifiedCurrentStamina)
                && Mathf.Abs(current - lastNotifiedCurrentStamina) < Mathf.Max(0f, staminaNotificationThreshold)
                && Mathf.Approximately(maximum, lastNotifiedMaximumStamina))
            {
                return;
            }

            if (!immediate && ShouldQueueStaminaNotification())
            {
                pendingStaminaNotification = true;
                pendingCommittedStaminaNotification |= committed;
                return;
            }

            PublishStaminaChanged(current, maximum, committed);
        }

        private bool ShouldQueueStaminaNotification()
        {
            return Application.isPlaying
                && staminaNotificationInterval > 0f
                && Time.unscaledTime < nextStaminaNotificationTime;
        }

        private void PublishPendingStaminaNotificationIfDue()
        {
            if (!pendingStaminaNotification || Application.isPlaying && Time.unscaledTime < nextStaminaNotificationTime)
            {
                return;
            }

            PublishStaminaChanged(CurrentStamina, MaximumStamina, pendingCommittedStaminaNotification);
        }

        private void PublishStaminaChanged(float current, float maximum, bool committed)
        {
            lastNotifiedCurrentStamina = current;
            lastNotifiedMaximumStamina = maximum;
            nextStaminaNotificationTime = Application.isPlaying ? Time.unscaledTime + staminaNotificationInterval : 0f;
            pendingStaminaNotification = false;
            pendingCommittedStaminaNotification = false;
            StaminaChanged?.Invoke(current, maximum);
            if (committed)
            {
                CommittedStaminaChanged?.Invoke(current, maximum);
            }
        }

        private void ResetStaminaNotificationCache()
        {
            lastNotifiedCurrentStamina = float.NaN;
            lastNotifiedMaximumStamina = float.NaN;
            pendingStaminaNotification = false;
            pendingCommittedStaminaNotification = false;
            nextStaminaNotificationTime = 0f;
        }

        private void NotifyCommittedStaminaChangedWhenLocalEventsAreInactive()
        {
            if (!UseResourceRuntime && !staminaEventsSubscribed)
            {
                NotifyStaminaChanged(force: true, committed: true, immediate: true);
            }
        }

        private VitalChangeResult ExecuteLocalStaminaMutation(Func<VitalChangeResult> mutation, bool immediateNotification)
        {
            publishLocalStaminaChangesImmediately = immediateNotification;
            try
            {
                VitalChangeResult result = mutation();
                if (result.Succeeded)
                {
                    NotifyCommittedStaminaChangedWhenLocalEventsAreInactive();
                }

                return result;
            }
            finally
            {
                publishLocalStaminaChangesImmediately = false;
            }
        }

        private float GetCommittedStamina()
        {
            return UseResourceRuntime ? resources.GetCurrent(ResourceIds.Stamina) : stamina.CurrentValue;
        }

        private float GetMinimumStamina()
        {
            return UseResourceRuntime ? resources.GetMinimum(ResourceIds.Stamina) : 0f;
        }

        private bool EnsureResourceRuntime()
        {
            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            SubscribeResourceEvents();
            SuppressAutomaticStaminaTick();
            return resources != null;
        }

        private void SubscribeLocalStaminaEvents()
        {
            if (staminaEventsSubscribed)
            {
                return;
            }

            stamina.ValueChanged += OnStaminaChanged;
            staminaEventsSubscribed = true;
        }

        private void UnsubscribeLocalStaminaEvents()
        {
            if (!staminaEventsSubscribed)
            {
                return;
            }

            stamina.ValueChanged -= OnStaminaChanged;
            staminaEventsSubscribed = false;
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

        private void SuppressAutomaticStaminaTick()
        {
            if (resources != null)
            {
                resources.SetAutomaticResourceTickSuppressed(ResourceIds.Stamina, true);
            }
        }

        private void UnsuppressAutomaticStaminaTick()
        {
            if (resources != null)
            {
                resources.SetAutomaticResourceTickSuppressed(ResourceIds.Stamina, false);
            }
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
