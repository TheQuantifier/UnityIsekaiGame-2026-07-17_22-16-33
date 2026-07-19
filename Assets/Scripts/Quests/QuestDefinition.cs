using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Unity Isekai Game/Quests/Quest")]
    public sealed class QuestDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 4)] private string summary;
        [SerializeField, TextArea(3, 8)] private string detailedDescription;
        [SerializeField] private QuestCategory category = QuestCategory.SideQuest;
        [SerializeField] private PersonDefinition questGiver;
        [SerializeField] private FactionDefinition questSourceFaction;
        [SerializeField] private FactionDefinition relatedFaction;
        [Tooltip("Legacy fallback used only when Quest Giver is not assigned.")]
        [SerializeField] private string questGiverId;
        [Tooltip("Legacy fallback used only when Quest Giver is not assigned.")]
        [SerializeField] private string questGiverDisplayName;
        [SerializeField] private QuestStageDefinition[] stages;
        [SerializeField] private ContractRewardDefinition reward;
        [SerializeField] private string[] prerequisiteQuestIds;
        [SerializeField] private bool repeatable;
        [SerializeField] private bool hiddenUntilDiscovered;
        [SerializeField] private bool canAbandon = true;

        public string QuestId => questId;
        public string Id => questId;
        public string DisplayName => Title;
        public string Title => string.IsNullOrWhiteSpace(title) ? "Untitled Quest" : title;
        public string Summary => summary;
        public string DetailedDescription => detailedDescription;
        public QuestCategory Category => category;
        public PersonDefinition QuestGiver => questGiver;
        public FactionDefinition QuestSourceFaction => questSourceFaction;
        public FactionDefinition RelatedFaction => relatedFaction;
        public string QuestSourceDisplayName => questSourceFaction == null ? QuestGiverDisplayName : questSourceFaction.DisplayName;
        public string QuestGiverId => questGiver == null ? questGiverId : questGiver.PersonId;
        public string QuestGiverDisplayName => questGiver == null
            ? questGiverDisplayName
            : string.IsNullOrWhiteSpace(questGiver.Title)
                ? questGiver.DisplayName
                : $"{questGiver.DisplayName}, {questGiver.Title}";
        public IReadOnlyList<QuestStageDefinition> Stages => stages ?? Array.Empty<QuestStageDefinition>();
        public ContractRewardDefinition Reward => reward;
        public IReadOnlyList<string> PrerequisiteQuestIds => prerequisiteQuestIds ?? Array.Empty<string>();
        public bool Repeatable => repeatable;
        public bool HiddenUntilDiscovered => hiddenUntilDiscovered;
        public bool CanAbandon => canAbandon;

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || report == null)
            {
                return;
            }

            ValidatePersonReference(questGiver, nameof(QuestGiver), definitionsById, report);
            ValidateFactionReference(questSourceFaction, nameof(QuestSourceFaction), definitionsById, report);
            ValidateFactionReference(relatedFaction, nameof(RelatedFaction), definitionsById, report);
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
                report.AddError($"QuestDefinition '{Title}' references {label} '{person.Id}', which is not in the configured catalog.");
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
                report.AddError($"QuestDefinition '{Title}' references {label} '{faction.Id}', which is not in the configured catalog.");
            }
        }
    }
}
