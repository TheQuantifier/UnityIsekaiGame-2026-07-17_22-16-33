using System;
using UnityEngine;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int startingHealth;

        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => maximumHealth;
        public bool IsAtMaximum => currentHealth >= maximumHealth;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            currentHealth = startingHealth > 0 ? Mathf.Min(startingHealth, maximumHealth) : maximumHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
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
            int clampedHealth = Mathf.Clamp(value, 0, maximumHealth);
            if (currentHealth == clampedHealth)
            {
                return;
            }

            currentHealth = clampedHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }
    }
}
