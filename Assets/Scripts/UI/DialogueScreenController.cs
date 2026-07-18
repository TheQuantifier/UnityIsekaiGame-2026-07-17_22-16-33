using UnityEngine;
using UnityIsekaiGame.Dialogue;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.UI
{
    public sealed class DialogueScreenController : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private DialogueView view;
        [SerializeField] private PlayerInputReader input;

        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private bool hasCursorState;

        private void Awake()
        {
            if (dialogueController == null)
            {
                dialogueController = FindAnyObjectByType<DialogueController>();
            }

            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
            }

            if (view != null)
            {
                view.Initialize(Choose);
                view.Hide();
            }
        }

        private void OnEnable()
        {
            if (dialogueController != null)
            {
                dialogueController.DialogueStarted += OnDialogueStarted;
                dialogueController.NodeChanged += OnNodeChanged;
                dialogueController.DialogueEnded += OnDialogueEnded;
            }
        }

        private void OnDisable()
        {
            if (dialogueController != null)
            {
                dialogueController.DialogueStarted -= OnDialogueStarted;
                dialogueController.NodeChanged -= OnNodeChanged;
                dialogueController.DialogueEnded -= OnDialogueEnded;
            }

            if (dialogueController != null && dialogueController.IsActive)
            {
                dialogueController.EndDialogue();
            }

            RestoreGameplayState();
        }

        private void Update()
        {
            if (dialogueController == null || !dialogueController.IsActive || input == null)
            {
                return;
            }

            if (input.ConsumeDialogueCancel())
            {
                DialogueOperationResult result = dialogueController.Cancel();
                if (!result.Succeeded)
                {
                    PrototypeHudMessageBus.Show(result.Message);
                }

                return;
            }

            if (!dialogueController.IsAwaitingChoice && input.ConsumeDialogueAdvance())
            {
                if (dialogueController.CurrentNode != null && dialogueController.CurrentNode.EndsConversation)
                {
                    return;
                }

                DialogueOperationResult result = dialogueController.Advance();
                if (!result.Succeeded)
                {
                    PrototypeHudMessageBus.Show(result.Message);
                }
            }
        }

        private void Choose(int choiceIndex)
        {
            if (dialogueController == null || !dialogueController.IsActive)
            {
                return;
            }

            DialogueOperationResult result = dialogueController.Choose(choiceIndex);
            input?.ClearDialogueActions();
            if (!result.Succeeded)
            {
                PrototypeHudMessageBus.Show(result.Message);
            }
        }

        private void OnDialogueStarted(DialogueNodeDefinition node)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            hasCursorState = true;

            input?.SetGameplayInputBlocked(true);
            input?.ClearCancel();
            input?.ClearInventoryUiActions();
            input?.ClearDialogueActions();
            PrototypeGameplayModalState.SetDialogueActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            view?.Show();
            view?.Render(node, dialogueController.GetSpeakerName(node), dialogueController.GetPortrait(node));
        }

        private void OnNodeChanged(DialogueNodeDefinition node)
        {
            view?.Render(node, dialogueController.GetSpeakerName(node), dialogueController.GetPortrait(node));
        }

        private void OnDialogueEnded()
        {
            view?.Hide();
            RestoreGameplayState();
        }

        private void RestoreGameplayState()
        {
            PrototypeGameplayModalState.SetDialogueActive(false);
            input?.ClearGameplayActionQueues();
            input?.ClearInventoryUiActions();
            input?.ClearDialogueActions();
            input?.SetGameplayInputBlocked(false);

            if (hasCursorState)
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisible;
                hasCursorState = false;
            }
        }
    }
}
