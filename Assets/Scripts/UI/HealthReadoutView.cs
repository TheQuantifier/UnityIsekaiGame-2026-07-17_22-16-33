using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.UI
{
    public sealed class HealthReadoutView : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private PlayerMana mana;
        [SerializeField] private Text label;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponent<Text>();
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
            }

            if (stamina != null)
            {
                stamina.StaminaChanged += OnStaminaChanged;
            }

            if (mana != null)
            {
                mana.ManaChanged += OnManaChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }

            if (stamina != null)
            {
                stamina.StaminaChanged -= OnStaminaChanged;
            }

            if (mana != null)
            {
                mana.ManaChanged -= OnManaChanged;
            }
        }

        private void OnHealthChanged(int currentHealth, int maximumHealth)
        {
            Refresh();
        }

        private void OnStaminaChanged(float currentStamina, float maximumStamina)
        {
            Refresh();
        }

        private void OnManaChanged(float currentMana, float maximumMana)
        {
            Refresh();
        }

        private void Refresh()
        {
            SetText();
        }

        private void SetText()
        {
            if (label != null)
            {
                string healthText = health == null ? "Health: -- / --" : $"Health: {health.CurrentHealth} / {health.MaximumHealth}";
                string staminaText = stamina == null ? "Stamina: -- / --" : $"Stamina: {stamina.CurrentStamina:0} / {stamina.MaximumStamina:0}";
                string manaText = mana == null ? "Mana: -- / --" : $"Mana: {mana.CurrentMana:0} / {mana.MaximumMana:0}";
                label.text = $"{healthText}\n{staminaText}\n{manaText}";
            }
        }
    }
}
