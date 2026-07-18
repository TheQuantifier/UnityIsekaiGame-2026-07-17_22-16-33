using UnityEngine;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "ReachLocationObjective", menuName = "Unity Isekai Game/Quests/Objectives/Reach Location")]
    public sealed class ReachLocationObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private string locationId;

        public string LocationId => locationId;

        public override ContractObjectiveInstance CreateInstance(ContractObjectiveContext context)
        {
            return new ReachLocationObjectiveInstance(this);
        }
    }

    public sealed class ReachLocationObjectiveInstance : ContractObjectiveInstance
    {
        private readonly ReachLocationObjectiveDefinition definition;
        private int currentProgress;

        public ReachLocationObjectiveInstance(ReachLocationObjectiveDefinition definition)
            : base(definition)
        {
            this.definition = definition;
        }

        public override int CurrentProgress => currentProgress;
        public override int RequiredProgress => 1;

        public override void Activate()
        {
            QuestObjectiveSignalBus.ReachedLocation += OnReachedLocation;
            base.Activate();
        }

        public override void Deactivate()
        {
            QuestObjectiveSignalBus.ReachedLocation -= OnReachedLocation;
        }

        public override void RefreshProgress()
        {
            NotifyProgressChanged();
        }

        private void OnReachedLocation(string locationId)
        {
            if (IsComplete || definition == null || locationId != definition.LocationId)
            {
                return;
            }

            currentProgress = 1;
            NotifyProgressChanged();
        }
    }
}
