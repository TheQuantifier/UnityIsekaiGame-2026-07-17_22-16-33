using UnityEngine;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Inventory
{
    public sealed class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private bool disableOnCollected;

        public string InteractionPrompt => item == null ? "Pick up" : $"Pick up {quantity} x {item.DisplayName}";

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
        }

        public bool CanInteract(in InteractionContext context)
        {
            return enabled && isActiveAndEnabled && item != null && quantity > 0;
        }

        public void Interact(in InteractionContext context)
        {
            PlayerInventory inventory = FindInventory(context.Interactor);
            if (inventory == null)
            {
                Debug.LogWarning($"{name} could not find a PlayerInventory on the interactor.");
                return;
            }

            InventoryAddResult result = inventory.AddItem(item, quantity);

            if (result.AddedAll)
            {
                Debug.Log($"Collected all {result.AddedQuantity} x {item.ItemId} from {name}.");
                CompletePickup();
                return;
            }

            if (result.AddedAny)
            {
                quantity = result.RemainingQuantity;
                Debug.Log($"Partial pickup from {name}. {quantity} x {item.ItemId} remain in the world.");
                return;
            }

            Debug.Log($"Inventory full. {name} remains in the world with {quantity} x {item.ItemId}.");
        }

        private static PlayerInventory FindInventory(GameObject interactor)
        {
            if (interactor == null)
            {
                return null;
            }

            PlayerInventory inventory = interactor.GetComponentInParent<PlayerInventory>();
            return inventory != null ? inventory : interactor.GetComponentInChildren<PlayerInventory>();
        }

        private void CompletePickup()
        {
            if (disableOnCollected)
            {
                gameObject.SetActive(false);
                return;
            }

            Destroy(gameObject);
        }
    }
}
