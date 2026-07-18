using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    [CreateAssetMenu(fileName = "DefeatObjective", menuName = "Unity Isekai Game/Contracts/Objectives/Defeat")]
    public sealed class DefeatObjectiveDefinition : ContractObjectiveDefinition
    {
        [SerializeField] private string targetCategory;
        [SerializeField, Min(1)] private int requiredDefeats = 1;

        public string TargetCategory => targetCategory;
        public int RequiredDefeats => Mathf.Max(1, requiredDefeats);

        public override ContractObjectiveInstance CreateInstance(ContractObjectiveContext context)
        {
            return new DefeatObjectiveInstance(this);
        }
    }

    public sealed class DefeatObjectiveInstance : ContractObjectiveInstance
    {
        private readonly DefeatObjectiveDefinition definition;
        private int currentDefeats;

        public DefeatObjectiveInstance(DefeatObjectiveDefinition definition)
            : base(definition)
        {
            this.definition = definition;
        }

        public override int CurrentProgress => currentDefeats;
        public override int RequiredProgress => definition == null ? 1 : definition.RequiredDefeats;

        public void RecordDefeat(string targetCategory)
        {
            if (IsComplete || definition == null || string.IsNullOrWhiteSpace(targetCategory) || targetCategory != definition.TargetCategory)
            {
                return;
            }

            currentDefeats = System.Math.Min(RequiredProgress, currentDefeats + 1);
            NotifyProgressChanged();
        }

        public override void RefreshProgress()
        {
            NotifyProgressChanged();
        }
    }
}
