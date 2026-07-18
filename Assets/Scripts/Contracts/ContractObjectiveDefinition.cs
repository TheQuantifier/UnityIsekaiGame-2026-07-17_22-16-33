using UnityEngine;

namespace UnityIsekaiGame.Contracts
{
    public abstract class ContractObjectiveDefinition : ScriptableObject
    {
        [SerializeField, TextArea] private string description;

        public string Description => description;

        public abstract ContractObjectiveInstance CreateInstance(ContractObjectiveContext context);
    }
}
