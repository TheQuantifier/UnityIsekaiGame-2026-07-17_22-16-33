using System;
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int startingHealth;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerInputReader input;

        private int currentHealth;
        private int effectiveMaximumHealth;
        private bool defeated;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => effectiveMaximumHealth;
        public bool IsAtMaximum => currentHealth >= effectiveMaximumHealth;
        public bool IsDefeated => defeated;
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
            bool defeatedNow = currentHealth <= 0;
            string message = defeatedNow
                ? $"Player took {damageApplied} damage and was defeated."
                : $"Player took {damageApplied} damage after {calculation.Defense:0.#} defense. Health: {currentHealth} / {effectiveMaximumHealth}.";

            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, calculation, damageApplied, currentHealth, defeatedNow, message);
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

        public void ResetToMaximum()
        {
            defeated = false;
            input?.SetDefeatedInputBlocked(false);
            SetHealth(effectiveMaximumHealth);
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

            if (currentHealth <= 0 && !defeated)
            {
                defeated = true;
                input?.SetDefeatedInputBlocked(true);
                Defeated?.Invoke();
                Debug.Log("Player defeated. Prototype gameplay input is blocked.");
                PrototypeHudMessageBus.Show("Defeated - Press R to reset");
            }
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
