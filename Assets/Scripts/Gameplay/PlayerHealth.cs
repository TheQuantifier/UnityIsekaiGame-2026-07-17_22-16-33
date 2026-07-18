using System;
using UnityEngine;
using UnityIsekaiGame.Equipment;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int startingHealth;
        [SerializeField] private PlayerStats stats;

        private int currentHealth;
        private int effectiveMaximumHealth;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => effectiveMaximumHealth;
        public bool IsAtMaximum => currentHealth >= effectiveMaximumHealth;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = startingHealth > 0 ? Mathf.Min(startingHealth, effectiveMaximumHealth) : effectiveMaximumHealth;
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
            SetHealth(currentHealth - amount);
            return previousHealth - currentHealth;
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || IsAtMaximum)
            {
                return 0;
            }

            int previousHealth = currentHealth;
            SetHealth(currentHealth + amount);
            return currentHealth - previousHealth;
        }

        private void SetHealth(int value)
        {
            int clampedHealth = Mathf.Clamp(value, 0, effectiveMaximumHealth);
            if (currentHealth == clampedHealth)
            {
                return;
            }

            currentHealth = clampedHealth;
            HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
        }

        private void OnStatsChanged()
        {
            int previousMaximum = effectiveMaximumHealth;
            effectiveMaximumHealth = GetConfiguredMaximumHealth();
            currentHealth = Mathf.Clamp(currentHealth, 0, effectiveMaximumHealth);

            if (previousMaximum != effectiveMaximumHealth)
            {
                HealthChanged?.Invoke(currentHealth, effectiveMaximumHealth);
            }
        }

        private int GetConfiguredMaximumHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(stats == null ? maximumHealth : stats.MaximumHealth));
        }
    }
}
