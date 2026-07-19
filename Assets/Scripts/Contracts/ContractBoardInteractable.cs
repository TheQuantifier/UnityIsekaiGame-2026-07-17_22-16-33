using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Interaction;
using UnityIsekaiGame.UI.Contracts;

namespace UnityIsekaiGame.Contracts
{
    public sealed class ContractBoardInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private PlayerContractJournal journal;
        [SerializeField] private ContractDefinition[] availableContracts;
        [SerializeField] private ContractBoardMenuView menuView;
        [SerializeField] private string interactionPrompt = "Review contracts";

        public string InteractionPrompt => interactionPrompt;

        private void Awake()
        {
            if (journal == null)
            {
                journal = FindAnyObjectByType<PlayerContractJournal>();
            }

            if (menuView == null)
            {
                menuView = FindAnyObjectByType<ContractBoardMenuView>(FindObjectsInactive.Include);
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

            if (menuView == null)
            {
                menuView = ContractBoardMenuView.FindOrCreate();
            }

            menuView.Open(journal, availableContracts);
        }
    }
}
