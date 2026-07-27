using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory.Quality;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Inventory
{
    public sealed class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private bool disableOnCollected;

        public string InteractionPrompt => item == null ? "Pick up" : $"Pick up {quantity} x {item.DisplayName}";

        public int Quantity => quantity;
        public ItemDefinition Item => item;
        public bool DisableOnCollected => disableOnCollected;

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
        }

        public void Configure(ItemDefinition itemDefinition, int pickupQuantity, bool disableWhenCollected = false)
        {
            item = itemDefinition;
            quantity = Mathf.Max(1, pickupQuantity);
            disableOnCollected = disableWhenCollected;
        }

        public void ResetPickupState(int pickupQuantity, bool active)
        {
            quantity = Mathf.Max(1, pickupQuantity);
            gameObject.SetActive(active);
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

            if (TryCollectSceneAuthoredInstance(context, inventory))
            {
                return;
            }

            InventoryAddResult result = inventory.AddItemOrInstances(item, quantity);

            if (result.AddedAll)
            {
                Debug.Log($"Collected all {result.AddedQuantity} x {item.ItemId} from {name}.");
                PrototypeHudMessageBus.Show($"Picked up {result.AddedQuantity} x {item.DisplayName}");
                CompletePickup();
                return;
            }

            if (result.AddedAny)
            {
                quantity = result.RemainingQuantity;
                Debug.Log($"Partial pickup from {name}. {quantity} x {item.ItemId} remain in the world.");
                PrototypeHudMessageBus.Show($"Picked up {result.AddedQuantity} x {item.DisplayName}. Inventory full.");
                return;
            }

            Debug.Log($"Inventory full. {name} remains in the world with {quantity} x {item.ItemId}.");
            PrototypeHudMessageBus.Show("Inventory full");
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

        private bool TryCollectSceneAuthoredInstance(in InteractionContext context, PlayerInventory inventory)
        {
            WorldItemQualityAffixPreset preset = GetComponent<WorldItemQualityAffixPreset>();
            if (preset == null || quantity != 1)
            {
                return false;
            }

            IItemQualityAffixRuntimeProvider provider = FindQualityProvider(context.Interactor);
            if (!preset.TryPreparePickupInstance(item, provider, out string itemInstanceId, out string failureReason))
            {
                Debug.LogWarning($"{name} could not prepare scene-authored item quality: {failureReason}");
                return false;
            }

            if (!inventory.CanAddExistingItemIdentity(item, itemInstanceId))
            {
                Debug.Log($"Inventory full. {name} remains in the world with {quantity} x {item.ItemId}.");
                PrototypeHudMessageBus.Show("Inventory full");
                return true;
            }

            InventoryInstanceOperationResult result = inventory.AddExistingItemIdentity(item, itemInstanceId);
            if (!result.Succeeded)
            {
                Debug.LogWarning($"{name} could not add scene-authored item instance '{itemInstanceId}' to inventory: {result.Message}");
                PrototypeHudMessageBus.Show("Inventory full");
                return true;
            }

            Debug.Log($"Collected scene-authored {item.ItemId} instance {itemInstanceId} from {name}.");
            PrototypeHudMessageBus.Show($"Picked up {item.DisplayName}");
            CompletePickup();
            return true;
        }

        private static IItemQualityAffixRuntimeProvider FindQualityProvider(GameObject interactor)
        {
            if (interactor != null)
            {
                IItemQualityAffixRuntimeProvider provider = interactor.GetComponentInParent<IItemQualityAffixRuntimeProvider>();
                if (provider != null)
                {
                    return provider;
                }

                provider = interactor.GetComponentInChildren<IItemQualityAffixRuntimeProvider>();
                if (provider != null)
                {
                    return provider;
                }
            }

            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IItemQualityAffixRuntimeProvider found)
                {
                    return found;
                }
            }

            return null;
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
