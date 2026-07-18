using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractBoardInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private PlayerContractJournal journal;
        [SerializeField] private ContractDefinition[] availableContracts;
        [SerializeField] private string interactionPrompt = "Review contracts";

        private int nextContractIndex;

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
            return enabled && isActiveAndEnabled && journal != null && availableContracts != null && availableContracts.Length > 0 && !PrototypeGameplayModalState.IsModalActive;
        }

        public void Interact(in InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            for (int attempts = 0; attempts < availableContracts.Length; attempts++)
            {
                ContractDefinition contract = availableContracts[nextContractIndex];
                nextContractIndex = (nextContractIndex + 1) % availableContracts.Length;
                ContractOperationResult result = journal.AcceptContract(contract);
                Debug.Log(result.Message);
                PrototypeHudMessageBus.Show(result.Message);

                if (result.Succeeded)
                {
                    return;
                }
            }
        }
    }
}
