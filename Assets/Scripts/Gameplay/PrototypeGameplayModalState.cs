using System;

namespace UnityIsekaiGame.Gameplay
{
    public static class PrototypeGameplayModalState
    {
        private static bool dialogueActive;

        public static bool DialogueActive => dialogueActive;
        public static bool IsModalActive => dialogueActive;

        public static event Action<bool> DialogueActiveChanged;

        public static void SetDialogueActive(bool active)
        {
            if (dialogueActive == active)
            {
                return;
            }

            dialogueActive = active;
            DialogueActiveChanged?.Invoke(dialogueActive);
        }
    }
}
