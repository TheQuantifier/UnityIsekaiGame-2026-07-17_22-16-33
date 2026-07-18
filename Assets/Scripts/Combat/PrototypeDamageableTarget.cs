using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UnityIsekaiGame.Combat
{
    public sealed class PrototypeDamageableTarget : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 50f;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.25f);
        [SerializeField] private Color defeatedColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;

        private float currentHealth;
        private bool defeated;
        private Coroutine flashRoutine;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public bool Defeated => defeated;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            currentHealth = maximumHealth;
            SetColor(normalColor);
            RefreshLabel();
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
            hitFlashDuration = Mathf.Max(0.01f, hitFlashDuration);
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

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.RawAmount);
            float appliedDamage = previousHealth - currentHealth;
            bool defeatedNow = currentHealth <= 0f;

            if (defeatedNow)
            {
                defeated = true;
                SetColor(defeatedColor);
            }
            else
            {
                FlashHitColor();
            }

            RefreshLabel();

            string message = defeatedNow
                ? $"{name} took {appliedDamage:0.#} damage and was defeated."
                : $"{name} took {appliedDamage:0.#} damage. Health: {currentHealth:0.#} / {maximumHealth:0.#}.";
            Debug.Log(message);
            return DamageResult.Success(damageInfo.RawAmount, appliedDamage, defeatedNow, message);
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
            SetColor(defeated ? defeatedColor : normalColor);
            flashRoutine = null;
        }

        private void SetColor(Color color)
        {
            if (targetRenderer != null)
            {
                targetRenderer.material.color = color;
            }
        }

        private void RefreshLabel()
        {
            if (healthLabel != null)
            {
                string status = defeated ? "Defeated" : $"{currentHealth:0} / {maximumHealth:0}";
                healthLabel.text = $"Target: {status}";
            }
        }
    }
}
