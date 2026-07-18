using UnityEngine;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    [CreateAssetMenu(fileName = "DeliveryObjective", menuName = "Unity Isekai Game/Contracts/Objectives/Delivery")]
    public sealed class DeliveryObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private string destinationId;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int requiredQuantity = 1;

        public string DestinationId => destinationId;
        public ItemDefinition Item => item;
        public int RequiredQuantity => Mathf.Max(1, requiredQuantity);

        public override ContractObjectiveInstance CreateInstance(ContractObjectiveContext context)
        {
            return new DeliveryObjectiveInstance(this, context.Inventory);
        }
    }
}
