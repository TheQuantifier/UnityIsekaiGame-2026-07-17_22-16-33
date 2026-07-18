using System;
using UnityEngine;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.Combat
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 50f;
        [SerializeField, Min(0f)] private float defense;

        private float currentHealth;
        private bool defeated;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public bool IsDefeated => defeated;
        public event Action<float, float> HealthChanged;
        public event Action Defeated;

        private void Awake()
        {
            currentHealth = maximumHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
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

            float appliedDamage = DamageCalculator.CalculateAppliedDamage(damageInfo.RawAmount, defense);
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            float changedAmount = previousHealth - currentHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);

            bool defeatedNow = currentHealth <= 0f;
            if (defeatedNow)
            {
                defeated = true;
                Defeated?.Invoke();
                PrototypeHudMessageBus.Show($"{name} defeated");
            }

            string message = defeatedNow
                ? $"{name} took {changedAmount:0.#} damage and was defeated."
                : $"{name} took {changedAmount:0.#} damage. Health: {currentHealth:0.#} / {maximumHealth:0.#}.";
            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, changedAmount, defeatedNow, message);
        }

        public void ResetToMaximum()
        {
            defeated = false;
            currentHealth = maximumHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }
    }
}
