using UnityEngine;
using UnityIsekaiGame.Dialogue;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestDialogueTargetReporter : MonoBehaviour, IQuestProvider
    {
        [SerializeField] private NpcDialogueInteractable dialogueInteractable;
        [SerializeField] private PersonIdentity personIdentity;
        [SerializeField] private PlayerQuestLog questLog;
        [Tooltip("Legacy fallback used only when Person Identity is missing or invalid.")]
        [SerializeField] private string talkTargetId;
        [SerializeField] private QuestDefinition offeredQuest;
        [SerializeField] private bool offerQuestOnTalk = true;

        public PersonIdentity PersonIdentity => personIdentity;
        public string QuestProviderId => ResolvePersonId();

        private void Awake()
        {
            if (dialogueInteractable == null)
            {
                dialogueInteractable = GetComponent<NpcDialogueInteractable>();
            }

            if (personIdentity == null)
            {
                personIdentity = GetComponent<PersonIdentity>();
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

            string personId = ResolvePersonId();
            if (string.IsNullOrWhiteSpace(personId))
            {
                Debug.LogWarning($"{name} cannot report quest dialogue because it has no person ID.");
                return;
            }

            QuestObjectiveSignalBus.ReportTalk(personId);
        }

        private string ResolvePersonId()
        {
            if (personIdentity != null && personIdentity.HasValidIdentity)
            {
                return personIdentity.PersonId;
            }

            return talkTargetId;
        }
    }
}
