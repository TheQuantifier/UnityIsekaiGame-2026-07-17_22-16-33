using UnityEngine;
using System;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Interaction;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Dialogue
{
    public sealed class NpcDialogueInteractable : MonoBehaviour, IInteractable, IDialogueParticipant
    {
        [SerializeField] private string interactionPrompt = "Talk";
        [SerializeField] private PersonIdentity personIdentity;
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private DialogueNodeDefinition startingNode;

        public string InteractionPrompt => personIdentity != null && personIdentity.HasValidIdentity
            ? $"Talk to {personIdentity.DisplayName}"
            : interactionPrompt;
        public PersonIdentity PersonIdentity => personIdentity;
        public string DialogueDisplayName => personIdentity == null ? string.Empty : personIdentity.DisplayName;
        public event Action<NpcDialogueInteractable> DialogueStarted;

        private void Awake()
        {
            if (personIdentity == null)
            {
                personIdentity = GetComponent<PersonIdentity>();
            }

            if (dialogueController == null)
            {
                dialogueController = FindAnyObjectByType<DialogueController>();
            }
        }

        public bool CanInteract(in InteractionContext context)
        {
            if (!enabled || !isActiveAndEnabled || startingNode == null || dialogueController == null || dialogueController.IsActive)
            {
                return false;
            }

            PlayerInputReader input = context.Interactor == null ? null : context.Interactor.GetComponentInParent<PlayerInputReader>();
            if (input != null && input.GameplayInputBlocked)
            {
                return false;
            }

            PlayerHealth health = context.Interactor == null ? null : context.Interactor.GetComponentInParent<PlayerHealth>();
            return health == null || !health.IsDefeated;
        }

        public void Interact(in InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            DialogueOperationResult result = dialogueController.StartDialogue(startingNode, DialogueDisplayName, personIdentity == null ? null : personIdentity.Portrait);
            Debug.Log(result.Message);
            if (result.Succeeded)
            {
                DialogueStarted?.Invoke(this);
            }

            if (!result.Succeeded)
            {
                PrototypeHudMessageBus.Show(result.Message);
            }
        }
    }
}
