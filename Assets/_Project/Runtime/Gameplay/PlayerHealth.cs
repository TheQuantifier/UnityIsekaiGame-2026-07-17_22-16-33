using System;
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.ResourceSystem;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int startingHealth;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CharacterResourceCollection resources;

        private int currentHealth;
        private int effectiveMaximumHealth;
        private bool defeated;
        private bool resourceEventsSubscribed;

        public int CurrentHealth => UseResourceRuntime ? Mathf.RoundToInt(resources.GetCurrent(ResourceIds.Health)) : currentHealth;
        public int MaximumHealth => UseResourceRuntime ? Mathf.RoundToInt(resources.GetMaximum(ResourceIds.Health)) : effectiveMaximumHealth;
        public bool IsAtMaximum => CurrentHealth >= MaximumHealth;
        public bool IsDefeated => defeated;
        private bool UseResourceRuntime => EnsureResourceRuntime() && resources.HasResource(ResourceIds.Health);
        public event Action<int, int> HealthChanged;
        public event Action Defeated;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = startingHealth > 0 ? Mathf.Min(startingHealth, effectiveMaximumHealth) : effectiveMaximumHealth;
            PublishHealthChanged();
        }

        private void OnEnable()
        {
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

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            startingHealth = Mathf.Clamp(startingHealth, 0, maximumHealth);
        }

        public int Damage(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previousHealth = currentHealth;
            if (UseResourceRuntime)
            {
                ResourceChangeResult result = resources.ApplyDamage(ResourceIds.Health, amount, "player.health", "Damage");
                return Mathf.RoundToInt(result.AppliedAmount);
            }

            SetHealth(currentHealth - amount);
            return previousHealth - currentHealth;
        }

        public DamageResult ApplyDamage(in DamageInfo damageInfo)
        {
            if (defeated)
            {
                return DamageResult.Failure(damageInfo.RawAmount, "Player is already defeated.");
            }

            if (damageInfo.RawAmount <= 0f)
            {
                return DamageResult.Failure(damageInfo.RawAmount, "Damage must be greater than zero.");
            }

            DamageCalculation calculation = DamageCalculator.CalculatePacket(
                damageInfo.DamagePacket,
                CombatStatUtility.GetDefense(gameObject),
                GetComponentInParent<IDamageResistanceReceiver>());
            int roundedDamage = calculation.FinalAmount <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(calculation.FinalAmount));
            int damageApplied = Damage(roundedDamage);
            bool defeatedNow = CurrentHealth <= 0;
            string message = defeatedNow
                ? $"Player took {damageApplied} damage and was defeated."
                : $"Player took {damageApplied} damage after {calculation.Defense:0.#} defense. Health: {CurrentHealth} / {MaximumHealth}.";

            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, calculation, damageApplied, CurrentHealth, defeatedNow, message);
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || IsAtMaximum)
            {
                return 0;
            }

            int previousHealth = currentHealth;
            if (UseResourceRuntime)
            {
                ResourceChangeResult result = resources.ApplyHealing(ResourceIds.Health, amount, "player.health", "Heal");
                return Mathf.RoundToInt(result.AppliedAmount);
            }

            SetHealth(currentHealth + amount);
            return currentHealth - previousHealth;
        }

        public void ResetToMaximum()
        {
            defeated = false;
            input?.SetDefeatedInputBlocked(false);
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Health, resources.GetMaximum(ResourceIds.Health), "player.health", "Reset to maximum", restoration: true);
                return;
            }

            SetHealth(effectiveMaximumHealth);
        }

        public bool TryRestoreForPersistence(int restoredHealth, out string failureReason)
        {
            failureReason = string.Empty;
            if (restoredHealth <= 0)
            {
                failureReason = "Defeated player health is not valid for prototype save restoration.";
                return false;
            }

            defeated = false;
            input?.SetDefeatedInputBlocked(false);
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Health, Mathf.Clamp(restoredHealth, 1, MaximumHealth), "player.health", "Persistence restore", restoration: true);
                return true;
            }

            SetHealth(Mathf.Clamp(restoredHealth, 1, effectiveMaximumHealth));
            return true;
        }

        private void SetHealth(int value)
        {
            int clampedHealth = Mathf.Clamp(value, 0, effectiveMaximumHealth);
            if (currentHealth == clampedHealth)
            {
                return;
            }

            currentHealth = clampedHealth;
            PublishHealthChanged();

            if (currentHealth <= 0 && !defeated)
            {
                MarkDefeated();
            }
        }

        private void OnStatsChanged()
        {
            int previousMaximum = effectiveMaximumHealth;
            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            if (UseResourceRuntime)
            {
                resources.ReconcileResource(ResourceIds.Health);
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, effectiveMaximumHealth);

            if (previousMaximum != effectiveMaximumHealth)
            {
                PublishHealthChanged();
            }
        }

        private void OnResourceChanged(CharacterResourceCollection collection, ResourceChangeResult result)
        {
            if (!string.Equals(result.Request.ResourceId, ResourceIds.Health, StringComparison.Ordinal))
            {
                return;
            }

            PublishHealthChanged();
            if (CurrentHealth <= 0 && !defeated)
            {
                MarkDefeated();
            }
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (string.Equals(snapshot.ResourceId, ResourceIds.Health, StringComparison.Ordinal))
            {
                PublishHealthChanged();
            }
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            PublishHealthChanged();
        }

        private void PublishHealthChanged()
        {
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
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

        private void MarkDefeated()
        {
            defeated = true;
            input?.SetDefeatedInputBlocked(true);
            Defeated?.Invoke();
            Debug.Log("Player defeated. Prototype gameplay input is blocked.");
            PrototypeHudMessageBus.Show("Defeated - Press R to reset");
        }

        private int GetConfiguredMaximumHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(stats == null ? maximumHealth : stats.MaximumHealth));
        }
    }
}
