using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Knowledge.History
{
    [CreateAssetMenu(fileName = "HistoricalEventDefinition", menuName = "Unity Isekai Game/Knowledge/Historical Event Definition")]
    public sealed class HistoricalEventDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string eventDefinitionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private HistoricalEventCategory category = HistoricalEventCategory.CustomWorldEvent;
        [SerializeField] private KnowledgeVisibility defaultVisibility = KnowledgeVisibility.Public;
        [SerializeField] private HistoricalEventPayloadKind payloadKind = HistoricalEventPayloadKind.Generic;
        [SerializeField] private string[] tags;
        [SerializeField] private string validationMetadata;

        public string Id => eventDefinitionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public HistoricalEventCategory Category => category;
        public KnowledgeVisibility DefaultVisibility => defaultVisibility;
        public HistoricalEventPayloadKind PayloadKind => payloadKind;
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();
        public string ValidationMetadata => validationMetadata ?? string.Empty;

        private void OnValidate()
        {
            eventDefinitionId = eventDefinitionId?.Trim();
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"Historical Event '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("history-event.", StringComparison.Ordinal))
            {
                report.AddWarning($"Historical Event '{Id}' should use the 'history-event.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(HistoricalEventCategory), category))
            {
                report.AddError($"Historical Event '{DisplayName}' has an invalid category.");
            }

            if (!Enum.IsDefined(typeof(KnowledgeVisibility), defaultVisibility))
            {
                report.AddError($"Historical Event '{DisplayName}' has an invalid default visibility.");
            }

            if (!Enum.IsDefined(typeof(HistoricalEventPayloadKind), payloadKind))
            {
                report.AddError($"Historical Event '{DisplayName}' has an invalid payload kind.");
            }
        }
    }
}
