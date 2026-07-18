using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UnityIsekaiGame.Combat
{
    public class PrototypeEnemyPresentation : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.25f);
        [SerializeField] private Color defeatedColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;

        private Coroutine flashRoutine;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            SetColor(normalColor);
            Refresh();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
                health.Defeated += OnDefeated;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.Defeated -= OnDefeated;
            }
        }

        private void OnValidate()
        {
            hitFlashDuration = Mathf.Max(0.01f, hitFlashDuration);
        }

        private void OnHealthChanged(float currentHealth, float maximumHealth)
        {
            if (health != null && !health.IsDefeated)
            {
                FlashHitColor();
            }

            Refresh();
        }

        private void OnDefeated()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            SetColor(defeatedColor);
            Refresh();
        }

        private void FlashHitColor()
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            SetColor(hitColor);
            yield return new WaitForSeconds(hitFlashDuration);
            SetColor(health != null && health.IsDefeated ? defeatedColor : normalColor);
            flashRoutine = null;
        }

        private void Refresh()
        {
            if (healthLabel == null || health == null)
            {
                return;
            }

            string status = health.IsDefeated ? "Defeated" : $"{health.CurrentHealth:0} / {health.MaximumHealth:0}";
            healthLabel.text = $"{name}: {status}";
        }

        private void SetColor(Color color)
        {
            if (targetRenderer != null)
            {
                targetRenderer.material.color = color;
            }
        }
    }
}
