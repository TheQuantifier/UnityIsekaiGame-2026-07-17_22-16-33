using System;
using UnityEngine;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PlayerMana : MonoBehaviour
    {
        [SerializeField] private VitalResource mana = new VitalResource();
        [SerializeField, Min(0f)] private float regenerationPerSecond = 8f;
        [SerializeField, Min(0f)] private float regenerationDelay = 1.5f;

        private float regenerationBlockedUntil;

        public float CurrentMana => mana.CurrentValue;
        public float MaximumMana => mana.MaximumValue;
        public event Action<float, float> ManaChanged;

        private void Awake()
        {
            mana.Initialize();
        }

        private void OnEnable()
        {
            mana.ValueChanged += OnManaChanged;
        }

        private void OnDisable()
        {
            mana.ValueChanged -= OnManaChanged;
        }

        private void Update()
        {
            if (regenerationPerSecond <= 0f || Time.time < regenerationBlockedUntil || mana.IsAtMaximum)
            {
                return;
            }

            mana.Restore(regenerationPerSecond * Time.deltaTime, "Mana");
        }

        private void OnValidate()
        {
            mana.Validate();
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
        }

        public bool CanSpend(float amount)
        {
            return mana.CanSpend(amount);
        }

        public VitalChangeResult Spend(float amount)
        {
            VitalChangeResult result = mana.Spend(amount, "Mana");
            if (result.Succeeded)
            {
                regenerationBlockedUntil = Time.time + regenerationDelay;
            }

            return result;
        }

        public VitalChangeResult Restore(float amount)
        {
            return mana.Restore(amount, "Mana");
        }

        private void OnManaChanged(float current, float maximum)
        {
            ManaChanged?.Invoke(current, maximum);
        }
    }
}
