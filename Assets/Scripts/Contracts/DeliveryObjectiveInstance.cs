using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Contracts
{
    public sealed class DeliveryObjectiveInstance : ContractObjectiveInstance
    {
        private readonly DeliveryObjectiveDefinition definition;
        private readonly PlayerInventory inventory;
        private int deliveredQuantity;

        public DeliveryObjectiveInstance(DeliveryObjectiveDefinition definition, PlayerInventory inventory)
            : base(definition)
        {
            this.definition = definition;
            this.inventory = inventory;
        }

        public string DestinationId => definition == null ? string.Empty : definition.DestinationId;
        public override int CurrentProgress => deliveredQuantity;
        public override int RequiredProgress => definition == null ? 1 : definition.RequiredQuantity;

        public ContractOperationResult TryDeliver(string destinationId)
        {
            if (IsComplete)
            {
                return ContractOperationResult.Failure("Delivery objective is already complete.");
            }

            if (definition == null || definition.Item == null || inventory == null)
            {
                return ContractOperationResult.Failure("Delivery objective is not configured.");
            }

            if (destinationId != definition.DestinationId)
            {
                return ContractOperationResult.Failure("This is not the correct delivery destination.");
            }

            int remainingQuantity = RequiredProgress - deliveredQuantity;
            if (inventory.CountItem(definition.Item) < remainingQuantity)
            {
                return ContractOperationResult.Failure($"Need {remainingQuantity} x {definition.Item.DisplayName} to deliver.");
            }

            if (!inventory.RemoveItem(definition.Item, remainingQuantity))
            {
                return ContractOperationResult.Failure("Could not remove delivery items.");
            }

            deliveredQuantity = RequiredProgress;
            NotifyProgressChanged();
            return ContractOperationResult.Success($"Delivered {remainingQuantity} x {definition.Item.DisplayName}.");
        }

        public override void RefreshProgress()
        {
            NotifyProgressChanged();
        }

        public override bool TryRestoreFromSaveData(ObjectiveProgressSaveData saveData, out string failureReason)
        {
            if (!ValidateCommonSaveData(saveData, out failureReason))
            {
                return false;
            }

            deliveredQuantity = System.Math.Min(saveData.currentProgress, RequiredProgress);
            RestoreCompleted(saveData.completed);
            return true;
        }
    }
}
