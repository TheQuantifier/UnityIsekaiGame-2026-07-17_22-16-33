using System;
using UnityEngine;
using UnityIsekaiGame.Beings.Biology.Anatomy;

namespace UnityIsekaiGame.Beings.Biology.Compatibility
{
    [Serializable]
    public sealed class BiologicalInteractionRuleDefinition
    {
        [SerializeField] private string entryId;
        [SerializeField] private BiologicalCompatibilitySourceKind sourceKind = BiologicalCompatibilitySourceKind.System;
        [SerializeField] private string sourceId;
        [SerializeField] private string interactionDefinitionId;
        [SerializeField] private BiologicalInteractionCategory category = BiologicalInteractionCategory.Unknown;
        [SerializeField] private BiologicalInteractionRuleKind ruleKind = BiologicalInteractionRuleKind.Resistance;
        [SerializeField] private BiologicalCompatibilityState compatibilityState = BiologicalCompatibilityState.Compatible;
        [SerializeField, Min(0f)] private float rateMultiplier = 1f;
        [SerializeField, Min(0f)] private float severityMultiplier = 1f;
        [SerializeField, Min(0f)] private float consequenceMultiplier = 1f;
        [SerializeField, Min(0f)] private float minimumEffectFloor;
        [SerializeField, Min(0f)] private float maximumSeverity = float.PositiveInfinity;
        [SerializeField] private int priority;
        [SerializeField] private string convertedInteractionDefinitionId;
        [SerializeField] private string[] requiredRuntimeCapabilityKeys;
        [SerializeField] private string[] blockingRuntimeCapabilityKeys;
        [SerializeField] private string[] requiredAnatomyTagIds;
        [SerializeField] private AnatomyStructuralCategory[] requiredNodeCategories;
        [SerializeField] private string requiredNodeId;
        [SerializeField, TextArea(1, 3)] private string explanation;
        [SerializeField] private bool alphaEnabled = true;

        public string EntryId => entryId ?? string.Empty;
        public BiologicalCompatibilitySourceKind SourceKind => sourceKind;
        public string SourceId => sourceId ?? string.Empty;
        public string InteractionDefinitionId => interactionDefinitionId ?? string.Empty;
        public BiologicalInteractionCategory Category => category;
        public BiologicalInteractionRuleKind RuleKind => ruleKind;
        public BiologicalCompatibilityState CompatibilityState => compatibilityState;
        public float RateMultiplier => Mathf.Max(0f, rateMultiplier);
        public float SeverityMultiplier => Mathf.Max(0f, severityMultiplier);
        public float ConsequenceMultiplier => Mathf.Max(0f, consequenceMultiplier);
        public float MinimumEffectFloor => Mathf.Max(0f, minimumEffectFloor);
        public float MaximumSeverity => float.IsNaN(maximumSeverity) ? float.PositiveInfinity : Mathf.Max(0f, maximumSeverity);
        public int Priority => priority;
        public string ConvertedInteractionDefinitionId => convertedInteractionDefinitionId ?? string.Empty;
        public string[] RequiredRuntimeCapabilityKeys => requiredRuntimeCapabilityKeys ?? Array.Empty<string>();
        public string[] BlockingRuntimeCapabilityKeys => blockingRuntimeCapabilityKeys ?? Array.Empty<string>();
        public string[] RequiredAnatomyTagIds => requiredAnatomyTagIds ?? Array.Empty<string>();
        public AnatomyStructuralCategory[] RequiredNodeCategories => requiredNodeCategories ?? Array.Empty<AnatomyStructuralCategory>();
        public string RequiredNodeId => requiredNodeId ?? string.Empty;
        public string Explanation => explanation ?? string.Empty;
        public bool AlphaEnabled => alphaEnabled;

        public RuntimeBiologicalInteractionRule ToRuntimeRule(string fallbackSourceId)
        {
            return new RuntimeBiologicalInteractionRule(
                EntryId,
                SourceKind,
                string.IsNullOrWhiteSpace(SourceId) ? fallbackSourceId : SourceId,
                InteractionDefinitionId,
                Category,
                RuleKind,
                CompatibilityState,
                RateMultiplier,
                SeverityMultiplier,
                ConsequenceMultiplier,
                MinimumEffectFloor,
                MaximumSeverity,
                Priority,
                ConvertedInteractionDefinitionId,
                RequiredRuntimeCapabilityKeys,
                BlockingRuntimeCapabilityKeys,
                RequiredAnatomyTagIds,
                RequiredNodeCategories,
                RequiredNodeId,
                Explanation,
                AlphaEnabled);
        }
    }
}
