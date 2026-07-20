using System;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.ResourceSystem;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerMana : MonoBehaviour
    {
        [SerializeField] private VitalResource mana = new VitalResource();
        [SerializeField, Min(0f)] private float regenerationPerSecond = 8f;
        [SerializeField, Min(0f)] private float regenerationDelay = 1.5f;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private CharacterResourceCollection resources;

        private float regenerationBlockedUntil;
        private bool resourceEventsSubscribed;

        public float CurrentMana => UseResourceRuntime ? resources.GetCurrent(ResourceIds.Mana) : mana.CurrentValue;
        public float MaximumMana => UseResourceRuntime ? resources.GetMaximum(ResourceIds.Mana) : mana.MaximumValue;
        private bool UseResourceRuntime => EnsureResourceRuntime() && resources.HasResource(ResourceIds.Mana);
        public event Action<float, float> ManaChanged;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (stats != null)
            {
                mana.SetMaximum(stats.MaximumMana);
            }

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            mana.Initialize();
        }

        private void OnEnable()
        {
            mana.ValueChanged += OnManaChanged;

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
            mana.ValueChanged -= OnManaChanged;

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

        private void Update()
        {
            if (UseResourceRuntime)
            {
                return;
            }

            if (regenerationPerSecond <= 0f || Time.time < regenerationBlockedUntil || mana.IsAtMaximum)
            {
                return;
            }

            mana.Restore(regenerationPerSecond * Time.deltaTime, "Mana");
        }

        private void OnValidate()
        {
            mana.Validate();
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
        }

        public bool CanSpend(float amount)
        {
            if (UseResourceRuntime)
            {
                return amount <= 0f || resources.CanSpend(ResourceIds.Mana, amount);
            }

            return mana.CanSpend(amount);
        }

        public VitalChangeResult Spend(float amount)
        {
            if (UseResourceRuntime)
            {
                ResourceChangeResult resourceResult = resources.TrySpend(ResourceIds.Mana, amount, "player.mana", "Mana spend", allowPartial: false);
                return ToVitalChangeResult(resourceResult, "mana");
            }

            VitalChangeResult result = mana.Spend(amount, "Mana");
            if (result.Succeeded)
            {
                regenerationBlockedUntil = Time.time + regenerationDelay;
            }

            return result;
        }

        public VitalChangeResult Restore(float amount)
        {
            if (UseResourceRuntime)
            {
                return ToVitalChangeResult(resources.TryGain(ResourceIds.Mana, amount, "player.mana", "Mana restore"), "mana");
            }

            return mana.Restore(amount, "Mana");
        }

        public void RestoreToMaximum()
        {
            regenerationBlockedUntil = 0f;
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Mana, resources.GetMaximum(ResourceIds.Mana), "player.mana", "Restore to maximum", restoration: true);
                return;
            }

            mana.SetCurrent(mana.MaximumValue);
        }

        public bool TryRestoreForPersistence(float restoredMana, out string failureReason)
        {
            failureReason = string.Empty;
            if (float.IsNaN(restoredMana) || float.IsInfinity(restoredMana) || restoredMana < 0f)
            {
                failureReason = $"Mana value {restoredMana} is invalid for save restoration.";
                return false;
            }

            regenerationBlockedUntil = 0f;
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Mana, Mathf.Clamp(restoredMana, 0f, MaximumMana), "player.mana", "Persistence restore", restoration: true);
                return true;
            }

            mana.SetCurrent(Mathf.Clamp(restoredMana, 0f, mana.MaximumValue));
            return true;
        }

        private void OnManaChanged(float current, float maximum)
        {
            ManaChanged?.Invoke(current, maximum);
        }

        private void OnStatsChanged()
        {
            if (UseResourceRuntime)
            {
                resources.ReconcileResource(ResourceIds.Mana);
                return;
            }

            mana.SetMaximum(stats.MaximumMana);
        }

        private void OnResourceChanged(CharacterResourceCollection collection, ResourceChangeResult result)
        {
            if (string.Equals(result.Request.ResourceId, ResourceIds.Mana, StringComparison.Ordinal))
            {
                ManaChanged?.Invoke(CurrentMana, MaximumMana);
            }
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (string.Equals(snapshot.ResourceId, ResourceIds.Mana, StringComparison.Ordinal))
            {
                ManaChanged?.Invoke(CurrentMana, MaximumMana);
            }
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            ManaChanged?.Invoke(CurrentMana, MaximumMana);
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
