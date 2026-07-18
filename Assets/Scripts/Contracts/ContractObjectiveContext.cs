using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Contracts
{
    public readonly struct ContractObjectiveContext
    {
        public ContractObjectiveContext(PlayerInventory inventory)
        {
            Inventory = inventory;
        }

        public PlayerInventory Inventory { get; }
    }
}
