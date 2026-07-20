using UnityEngine;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.Inventory
{
    [CreateAssetMenu(fileName = "RestoreHealthEffect", menuName = "Unity Isekai Game/Inventory/Effects/Restore Health")]
    public sealed class RestoreHealthItemUseEffect : ItemUseEffect
    {
        [SerializeField] private RestoreVitalEffectDefinition restoreEffect;
        [SerializeField, Min(1)] private int healingAmount = 25;

        public int HealingAmount => healingAmount;

        private void OnValidate()
        {
            healingAmount = Mathf.Max(1, healingAmount);
        }

        public override bool CanUse(in ItemUseContext context, out string failureReason)
        {
            if (restoreEffect != null)
            {
                EffectExecutionResult result = restoreEffect.CanExecute(CreateEffectContext(context));
                failureReason = result.Succeeded ? string.Empty : result.Message;
                return result.Succeeded;
            }

            PlayerHealth health = FindHealth(context.User);
            if (health == null)
            {
                failureReason = "No player health component found.";
                return false;
            }

            if (health.IsAtMaximum)
            {
                failureReason = "Health is already full.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public override void Apply(in ItemUseContext context)
        {
            if (restoreEffect != null)
            {
                EffectExecutionResult result = restoreEffect.Execute(CreateEffectContext(context));
                Debug.Log(result.Message);
                return;
            }

            PlayerHealth health = FindHealth(context.User);
            if (health == null)
            {
                return;
            }

            int healed = health.Heal(healingAmount);
            Debug.Log($"Used {context.Item.ItemId}. Restored {healed} health.");
        }

        private static EffectExecutionContext CreateEffectContext(in ItemUseContext context)
        {
            Vector3 position = context.User == null ? Vector3.zero : context.User.transform.position;
            return new EffectExecutionContext(
                null,
                context.User,
                context.User,
                position,
                position,
                context.User == null ? Vector3.forward : context.User.transform.forward,
                context.Item);
        }

        private static PlayerHealth FindHealth(GameObject user)
        {
            if (user == null)
            {
                return null;
            }

            PlayerHealth health = user.GetComponentInParent<PlayerHealth>();
            return health != null ? health : user.GetComponentInChildren<PlayerHealth>();
        }
    }
}
