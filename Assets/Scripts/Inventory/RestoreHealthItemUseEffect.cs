using UnityEngine;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.Inventory
{
    [CreateAssetMenu(fileName = "RestoreHealthEffect", menuName = "Unity Isekai Game/Inventory/Effects/Restore Health")]
    public sealed class RestoreHealthItemUseEffect : ItemUseEffect
    {
        [SerializeField, Min(1)] private int healingAmount = 25;

        public int HealingAmount => healingAmount;

        private void OnValidate()
        {
            healingAmount = Mathf.Max(1, healingAmount);
        }

        public override bool CanUse(in ItemUseContext context, out string failureReason)
        {
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
            PlayerHealth health = FindHealth(context.User);
            if (health == null)
            {
                return;
            }

            int healed = health.Heal(healingAmount);
            Debug.Log($"Used {context.Item.ItemId}. Restored {healed} health.");
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
