using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    public sealed class CollectItemObjectiveInstance : ContractObjectiveInstance
    {
        private readonly CollectItemObjectiveDefinition definition;
        private readonly PlayerInventory inventory;
        private int currentQuantity;

        public CollectItemObjectiveInstance(CollectItemObjectiveDefinition definition, PlayerInventory inventory)
            : base(definition)
        {
            this.definition = definition;
            this.inventory = inventory;
        }

        public override int CurrentProgress => currentQuantity;
        public override int RequiredProgress => definition == null ? 1 : definition.RequiredQuantity;

        public override void Activate()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += RefreshProgress;
            }

            base.Activate();
        }

        public override void Deactivate()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshProgress;
            }
        }

        public override void RefreshProgress()
        {
            currentQuantity = definition == null || inventory == null ? 0 : inventory.CountItem(definition.Item);
            NotifyProgressChanged();
        }
    }
}
