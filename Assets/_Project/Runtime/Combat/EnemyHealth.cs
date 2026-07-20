using System;
using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.Combat
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 50f;
        [SerializeField, Min(0f)] private float defense;
        [SerializeField] private ActorStats stats;

        private float currentHealth;
        private float effectiveMaximumHealth;
        private bool defeated;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => effectiveMaximumHealth;
        public bool IsDefeated => defeated;
        public event Action<float, float> HealthChanged;
        public event Action Defeated;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<ActorStats>();
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
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.StatsChanged -= OnStatsChanged;
            }
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
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - calculation.FinalAmount);
            float changedAmount = previousHealth - currentHealth;
            HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);

            bool defeatedNow = currentHealth <= 0f;
            if (defeatedNow)
            {
                defeated = true;
                Defeated?.Invoke();
                PrototypeHudMessageBus.Show($"{name} defeated");
            }

            string message = defeatedNow
                ? $"{name} took {changedAmount:0.#} damage and was defeated."
                : $"{name} took {changedAmount:0.#} damage after {calculation.Defense:0.#} defense. Health: {currentHealth:0.#} / {effectiveMaximumHealth:0.#}.";
            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, calculation, changedAmount, currentHealth, defeatedNow, message);
        }

        public void ResetToMaximum()
        {
            defeated = false;
            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = effectiveMaximumHealth;
            HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
        }

        private void OnStatsChanged()
        {
            float previousMaximum = effectiveMaximumHealth;
            effectiveMaximumHealth = GetConfiguredMaximumHealth();
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
    }
}
