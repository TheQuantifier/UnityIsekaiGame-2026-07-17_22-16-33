using UnityEngine;
using System;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Dialogue
{
    public sealed class NpcDialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Talk";
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private DialogueNodeDefinition startingNode;

        public string InteractionPrompt => interactionPrompt;
        public event Action<NpcDialogueInteractable> DialogueStarted;

        private void Awake()
        {
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

            DialogueOperationResult result = dialogueController.StartDialogue(startingNode);
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
