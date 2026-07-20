using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractDeliveryInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private PlayerContractJournal journal;
        [SerializeField] private string destinationId;
        [SerializeField] private string interactionPrompt = "Deliver contract items";

        public string InteractionPrompt => interactionPrompt;

        private void Awake()
        {
            if (journal == null)
            {
                journal = FindAnyObjectByType<PlayerContractJournal>();
            }
        }

        public bool CanInteract(in InteractionContext context)
        {
            return enabled && isActiveAndEnabled && journal != null && !string.IsNullOrWhiteSpace(destinationId) && !PrototypeGameplayModalState.IsModalActive;
        }

        public void Interact(in InteractionContext context)
        {
            ContractOperationResult result = journal == null
                ? ContractOperationResult.Failure("No contract journal found.")
                : journal.DeliverTo(destinationId);

            Debug.Log(result.Message);
            PrototypeHudMessageBus.Show(result.Message);
        }
    }
}
