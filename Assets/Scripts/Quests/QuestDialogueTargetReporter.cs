using UnityEngine;
using UnityIsekaiGame.Dialogue;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestDialogueTargetReporter : MonoBehaviour
    {
        [SerializeField] private NpcDialogueInteractable dialogueInteractable;
        [SerializeField] private PlayerQuestLog questLog;
        [SerializeField] private string talkTargetId;
        [SerializeField] private QuestDefinition offeredQuest;
        [SerializeField] private bool offerQuestOnTalk = true;

        private void Awake()
        {
            if (dialogueInteractable == null)
            {
                dialogueInteractable = GetComponent<NpcDialogueInteractable>();
            }

            if (questLog == null)
            {
                questLog = FindAnyObjectByType<PlayerQuestLog>();
            }
        }

        private void OnEnable()
        {
            if (dialogueInteractable != null)
            {
                dialogueInteractable.DialogueStarted += OnDialogueStarted;
            }
        }

        private void OnDisable()
        {
            if (dialogueInteractable != null)
            {
                dialogueInteractable.DialogueStarted -= OnDialogueStarted;
            }
        }

        private void OnDialogueStarted(NpcDialogueInteractable source)
        {
            if (offerQuestOnTalk && questLog != null && offeredQuest != null)
            {
                QuestInstance existingQuest = questLog.FindQuest(offeredQuest.QuestId);
                if (existingQuest == null || offeredQuest.Repeatable)
                {
                    QuestOperationResult result = questLog.StartQuest(offeredQuest);
                    Debug.Log(result.Message);
                    if (!result.Succeeded)
                    {
                        PrototypeHudMessageBus.Show(result.Message);
                    }
                }
            }

            QuestObjectiveSignalBus.ReportTalk(talkTargetId);
        }
    }
}
