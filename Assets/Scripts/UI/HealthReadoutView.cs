using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.UI
{
    public sealed class HealthReadoutView : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
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

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(int currentHealth, int maximumHealth)
        {
            SetText(currentHealth, maximumHealth);
        }

        private void Refresh()
        {
            if (health != null)
            {
                SetText(health.CurrentHealth, health.MaximumHealth);
            }
        }

        private void SetText(int currentHealth, int maximumHealth)
        {
            if (label != null)
            {
                label.text = $"Health: {currentHealth} / {maximumHealth}";
            }
        }
    }
}
