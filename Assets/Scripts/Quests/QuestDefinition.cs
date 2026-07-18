using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Unity Isekai Game/Quests/Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 4)] private string summary;
        [SerializeField, TextArea(3, 8)] private string detailedDescription;
        [SerializeField] private QuestCategory category = QuestCategory.SideQuest;
        [SerializeField] private string questGiverId;
        [SerializeField] private string questGiverDisplayName;
        [SerializeField] private QuestStageDefinition[] stages;
        [SerializeField] private ContractRewardDefinition reward;
        [SerializeField] private string[] prerequisiteQuestIds;
        [SerializeField] private bool repeatable;
        [SerializeField] private bool hiddenUntilDiscovered;
        [SerializeField] private bool canAbandon = true;

        public string QuestId => questId;
        public string Title => string.IsNullOrWhiteSpace(title) ? "Untitled Quest" : title;
        public string Summary => summary;
        public string DetailedDescription => detailedDescription;
        public QuestCategory Category => category;
        public string QuestGiverId => questGiverId;
        public string QuestGiverDisplayName => questGiverDisplayName;
        public IReadOnlyList<QuestStageDefinition> Stages => stages ?? Array.Empty<QuestStageDefinition>();
        public ContractRewardDefinition Reward => reward;
        public IReadOnlyList<string> PrerequisiteQuestIds => prerequisiteQuestIds ?? Array.Empty<string>();
        public bool Repeatable => repeatable;
        public bool HiddenUntilDiscovered => hiddenUntilDiscovered;
        public bool CanAbandon => canAbandon;
    }
}
