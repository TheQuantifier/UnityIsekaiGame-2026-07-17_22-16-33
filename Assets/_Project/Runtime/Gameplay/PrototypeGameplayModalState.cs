using System;

namespace UnityIsekaiGame.Gameplay
{
    public static class PrototypeGameplayModalState
    {
        private static bool dialogueActive;
        private static bool contractMenuActive;

        public static bool DialogueActive => dialogueActive;
        public static bool ContractMenuActive => contractMenuActive;
        public static bool IsModalActive => dialogueActive || contractMenuActive;

        public static event Action<bool> DialogueActiveChanged;
        public static event Action<bool> ContractMenuActiveChanged;

        public static void SetDialogueActive(bool active)
        {
            if (dialogueActive == active)
            {
                return;
            }

            dialogueActive = active;
            DialogueActiveChanged?.Invoke(dialogueActive);
        }

        public static void SetContractMenuActive(bool active)
        {
            if (contractMenuActive == active)
            {
                return;
            }

            contractMenuActive = active;
            ContractMenuActiveChanged?.Invoke(contractMenuActive);
        }
    }
}
