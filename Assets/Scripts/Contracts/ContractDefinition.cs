using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Contracts
{
    [CreateAssetMenu(fileName = "Contract", menuName = "Unity Isekai Game/Contracts/Contract")]
    public sealed class ContractDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string contractId;
        [SerializeField] private string displayTitle;
        [SerializeField, TextArea] private string description;
        [SerializeField] private PersonDefinition requesterPerson;
        [SerializeField] private FactionDefinition requesterFaction;
        [SerializeField] private FactionDefinition postingFaction;
        [SerializeField] private string approvingOrganizationPlaceholder;
        [SerializeField] private string requesterName;
        [SerializeField] private string recommendedRank;
        [SerializeField] private ContractObjectiveDefinition[] objectives;
        [SerializeField] private ContractRewardDefinition reward;
        [SerializeField] private string expirationPlaceholder;

        public string ContractId => contractId;
        public string Id => contractId;
        public string DisplayName => DisplayTitle;
        public string DisplayTitle => displayTitle;
        public string Description => description;
        public PersonDefinition RequesterPerson => requesterPerson;
        public FactionDefinition RequesterFaction => requesterFaction;
        public FactionDefinition PostingFaction => postingFaction;
        public string ApprovingOrganizationPlaceholder => approvingOrganizationPlaceholder;
        public string RequesterName => requesterName;
        public string RequesterDisplayName => requesterPerson != null
            ? requesterPerson.DisplayName
            : requesterFaction != null
                ? requesterFaction.DisplayName
                : requesterName;
        public string PostingFactionDisplayName => postingFaction == null ? string.Empty : postingFaction.DisplayName;
        public string RecommendedRank => recommendedRank;
        public IReadOnlyList<ContractObjectiveDefinition> Objectives => objectives ?? System.Array.Empty<ContractObjectiveDefinition>();
        public ContractRewardDefinition Reward => reward;
        public string ExpirationPlaceholder => expirationPlaceholder;

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || report == null)
            {
                return;
            }

            ValidatePersonReference(requesterPerson, nameof(RequesterPerson), definitionsById, report);
            ValidateFactionReference(requesterFaction, nameof(RequesterFaction), definitionsById, report);
            ValidateFactionReference(postingFaction, nameof(PostingFaction), definitionsById, report);

            if (requesterPerson != null && requesterFaction != null)
            {
                report.AddWarning($"ContractDefinition '{DisplayTitle}' has both requester person and requester faction. The person label will be displayed first.");
            }

            if (requesterPerson == null
                && requesterFaction == null
                && !string.IsNullOrWhiteSpace(requesterName))
            {
                report.AddWarning($"ContractDefinition '{DisplayTitle}' still uses legacy requester text '{requesterName}'. Prefer a typed requester person or faction for new content.");
            }
        }

        private void ValidatePersonReference(
            PersonDefinition person,
            string label,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (person == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(person.Id, out IGameDefinition found) || found is not PersonDefinition)
            {
                report.AddError($"ContractDefinition '{DisplayTitle}' references {label} '{person.Id}', which is not in the configured catalog.");
            }
        }

        private void ValidateFactionReference(
            FactionDefinition faction,
            string label,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (faction == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(faction.Id, out IGameDefinition found) || found is not FactionDefinition)
            {
                report.AddError($"ContractDefinition '{DisplayTitle}' references {label} '{faction.Id}', which is not in the configured catalog.");
            }
        }
    }
}
