using UnityEngine;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    [CreateAssetMenu(fileName = "CollectItemObjective", menuName = "Unity Isekai Game/Contracts/Objectives/Collect Item")]
    public sealed class CollectItemObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int requiredQuantity = 1;

        public ItemDefinition Item => item;
        public int RequiredQuantity => Mathf.Max(1, requiredQuantity);

        public override ContractObjectiveInstance CreateInstance(ContractObjectiveContext context)
        {
            return new CollectItemObjectiveInstance(this, context.Inventory);
        }
    }
}
