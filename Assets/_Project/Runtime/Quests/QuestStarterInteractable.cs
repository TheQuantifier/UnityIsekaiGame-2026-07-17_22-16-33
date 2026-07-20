using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestStarterInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private PlayerQuestLog questLog;
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private string interactionPrompt = "Start quest";

        public string InteractionPrompt => interactionPrompt;

        private void Awake()
        {
            if (questLog == null)
            {
                questLog = FindAnyObjectByType<PlayerQuestLog>();
            }
        }

        public bool CanInteract(in InteractionContext context)
        {
            return enabled && isActiveAndEnabled && questLog != null && quest != null && !PrototypeGameplayModalState.IsModalActive;
        }

        public void Interact(in InteractionContext context)
        {
            QuestOperationResult result = questLog == null
                ? QuestOperationResult.Failure("No quest log found.")
                : questLog.StartQuest(quest);
            Debug.Log(result.Message);
            PrototypeHudMessageBus.Show(result.Message);
        }
    }
}
