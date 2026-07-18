using UnityEngine;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "TalkObjective", menuName = "Unity Isekai Game/Quests/Objectives/Talk")]
    public sealed class TalkObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private PersonDefinition talkTargetPerson;
        [Tooltip("Legacy fallback used only when Talk Target Person is not assigned.")]
        [SerializeField] private string talkTargetId;

        public string TalkTargetId => talkTargetPerson == null ? talkTargetId : talkTargetPerson.PersonId;
        public PersonDefinition TalkTargetPerson => talkTargetPerson;

        public override ContractObjectiveInstance CreateInstance(ContractObjectiveContext context)
        {
            return new TalkObjectiveInstance(this);
        }
    }

    public sealed class TalkObjectiveInstance : ContractObjectiveInstance
    {
        private readonly TalkObjectiveDefinition definition;
        private int currentProgress;

        public TalkObjectiveInstance(TalkObjectiveDefinition definition)
            : base(definition)
        {
            this.definition = definition;
        }

        public override int CurrentProgress => currentProgress;
        public override int RequiredProgress => 1;

        public override void Activate()
        {
            QuestObjectiveSignalBus.TalkedTo += OnTalkedTo;
            base.Activate();
        }

        public override void Deactivate()
        {
            QuestObjectiveSignalBus.TalkedTo -= OnTalkedTo;
        }

        public override void RefreshProgress()
        {
            NotifyProgressChanged();
        }

        private void OnTalkedTo(string targetId)
        {
            if (IsComplete || definition == null || targetId != definition.TalkTargetId)
            {
                return;
            }

            currentProgress = 1;
            NotifyProgressChanged();
        }
    }
}
