using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    public abstract class ContractObjectiveDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used when this objective is persisted as part of a quest stage. Unique within the owning quest stage.")]
        [SerializeField] private string objectiveId;
        [SerializeField, TextArea] private string description;

        public string ObjectiveId => objectiveId;
        public string Description => description;

        public abstract ContractObjectiveInstance CreateInstance(ContractObjectiveContext context);
    }
}
