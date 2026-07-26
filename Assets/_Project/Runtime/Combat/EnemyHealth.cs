using System;
using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.Combat
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 50f;
        [SerializeField, Min(0f)] private float defense;
        [SerializeField] private ActorStats stats;
        [SerializeField] private CharacterResourceCollection resources;

        private float currentHealth;
        private float effectiveMaximumHealth;
        private bool defeated;
        private bool resourceEventsSubscribed;

        public float CurrentHealth => UseResourceRuntime ? resources.GetCurrent(ResourceIds.Health) : currentHealth;
        public float MaximumHealth => UseResourceRuntime ? resources.GetMaximum(ResourceIds.Health) : effectiveMaximumHealth;
        public bool IsDefeated => defeated;
        private bool UseResourceRuntime => EnsureResourceRuntime() && resources.HasResource(ResourceIds.Health);
        public event Action<float, float> HealthChanged;
        public event Action Defeated;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<ActorStats>();
            }

            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = effectiveMaximumHealth;
            HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
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
            maximumHealth = Mathf.Max(1f, maximumHealth);
            defense = Mathf.Max(0f, defense);
        }

        public DamageResult ApplyDamage(in DamageInfo damageInfo)
        {
            if (defeated)
            {
                return DamageResult.Failure(damageInfo.RawAmount, $"{name} is already defeated.");
            }

            if (damageInfo.RawAmount <= 0f)
            {
                return DamageResult.Failure(damageInfo.RawAmount, "Damage must be greater than zero.");
            }

            DamageCalculation calculation = DamageCalculator.CalculatePacket(
                damageInfo.DamagePacket,
                GetConfiguredDefense(),
                GetComponentInParent<IDamageResistanceReceiver>());
            if (UseResourceRuntime && SceneCombatDamageBridge.TryApplyCurrentResourceDamage(gameObject, in damageInfo, "enemy-health.compat", "Legacy damage endpoint bridge", out DamageResult pipelineResult))
            {
                if (pipelineResult.Defeated)
                {
                    MarkDefeated();
                }

                Debug.Log(pipelineResult.Message);
                return pipelineResult;
            }

            float previousHealth = CurrentHealth;
            float changedAmount;
            float resultingHealth;
            if (UseResourceRuntime)
            {
                ResourceChangeResult resourceResult = resources.ApplyDamage(ResourceIds.Health, calculation.FinalAmount, "enemy.health", "Damage");
                if (!resourceResult.Succeeded)
                {
                    return DamageResult.Failure(damageInfo.RawAmount, resourceResult.Message);
                }

                changedAmount = resourceResult.AppliedAmount;
                resultingHealth = resourceResult.NewCurrent;
            }
            else
            {
                currentHealth = Mathf.Max(0f, currentHealth - calculation.FinalAmount);
                changedAmount = previousHealth - currentHealth;
                resultingHealth = currentHealth;
                HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
            }

            bool defeatedNow = resultingHealth <= 0f;
            if (defeatedNow)
            {
                MarkDefeated();
            }

            string message = defeatedNow
                ? $"{name} took {changedAmount:0.#} damage and was defeated."
                : $"{name} took {changedAmount:0.#} damage after {calculation.Defense:0.#} defense. Health: {CurrentHealth:0.#} / {MaximumHealth:0.#}.";
            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, calculation, changedAmount, CurrentHealth, defeatedNow, message);
        }

        public void ResetToMaximum()
        {
            defeated = false;
            if (UseResourceRuntime)
            {
                resources.SetCurrent(ResourceIds.Health, resources.GetMaximum(ResourceIds.Health), "enemy.health", "Reset to maximum", restoration: true);
                return;
            }

            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = effectiveMaximumHealth;
            HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
        }

        private void OnStatsChanged()
        {
            float previousMaximum = effectiveMaximumHealth;
            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            if (UseResourceRuntime)
            {
                resources.ReconcileResource(ResourceIds.Health);
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, effectiveMaximumHealth);

            if (!Mathf.Approximately(previousMaximum, effectiveMaximumHealth))
            {
                HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
            }
        }

        private float GetConfiguredMaximumHealth()
        {
            return Mathf.Max(1f, stats == null ? maximumHealth : stats.MaximumHealth);
        }

        private float GetConfiguredDefense()
        {
            return stats == null ? defense : CombatStatUtility.GetDefense(gameObject);
        }

        public void RefreshResourceRuntime()
        {
            if (resources == null)
            {
                resources = GetComponent<CharacterResourceCollection>();
            }

            SubscribeResourceEvents();
            if (resources != null && resources.TryGetResource(ResourceIds.Health, out ResourceSnapshot snapshot))
            {
                SyncFromResource(snapshot);
                HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            }
        }

        private void OnResourceChanged(CharacterResourceCollection collection, ResourceChangeResult result)
        {
            if (!string.Equals(result.Request.ResourceId, ResourceIds.Health, StringComparison.Ordinal))
            {
                return;
            }

            effectiveMaximumHealth = result.Maximum;
            currentHealth = result.NewCurrent;
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            if (CurrentHealth <= result.Minimum + CharacterResourceCollection.Epsilon)
            {
                MarkDefeated();
            }
        }

        private void OnResourceMaximumChanged(CharacterResourceCollection collection, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (!string.Equals(snapshot.ResourceId, ResourceIds.Health, StringComparison.Ordinal))
            {
                return;
            }

            SyncFromResource(snapshot);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        }

        private void OnResourcesRestored(CharacterResourceCollection collection, bool restoring)
        {
            RefreshResourceRuntime();
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

        private void SyncFromResource(ResourceSnapshot snapshot)
        {
            currentHealth = snapshot.Current;
            effectiveMaximumHealth = snapshot.Maximum;
            if (currentHealth > snapshot.Minimum + CharacterResourceCollection.Epsilon)
            {
                defeated = false;
            }
        }

        private void MarkDefeated()
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            Defeated?.Invoke();
            PrototypeHudMessageBus.Show($"{name} defeated");
        }
    }
}
