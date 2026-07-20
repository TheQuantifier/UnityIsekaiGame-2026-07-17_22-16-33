using UnityEngine;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Places;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "ReachLocationObjective", menuName = "Unity Isekai Game/Quests/Objectives/Reach Location")]
    public sealed class ReachLocationObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private PlaceDefinition targetPlace;
        [SerializeField] private string locationId;

        public PlaceDefinition TargetPlace => targetPlace;
        public string LocationId => locationId;
        public string TargetLocationId => targetPlace == null ? locationId : targetPlace.Id;
        public string TargetDisplayName => targetPlace == null ? locationId : targetPlace.DisplayName;

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

        public override void ActivateForRestore()
        {
            QuestObjectiveSignalBus.ReachedLocation += OnReachedLocation;
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
            if (IsComplete || definition == null || string.IsNullOrWhiteSpace(locationId) || locationId != definition.TargetLocationId)
            {
                return;
            }

            currentProgress = 1;
            NotifyProgressChanged();
        }

        public override bool TryRestoreFromSaveData(ObjectiveProgressSaveData saveData, out string failureReason)
        {
            if (!ValidateCommonSaveData(saveData, out failureReason))
            {
                return false;
            }

            currentProgress = Mathf.Clamp(saveData.currentProgress, 0, RequiredProgress);
            RestoreCompleted(saveData.completed);
            return true;
        }
    }
}
