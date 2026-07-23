using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Beings.Biology.Compatibility
{
    [CreateAssetMenu(fileName = "BiologicalInteraction", menuName = "Unity Isekai Game/Beings/Biology/Biological Interaction")]
    public sealed class BiologicalInteractionDefinition : ScriptableObject, IGameDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string interactionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private BiologicalInteractionCategory category = BiologicalInteractionCategory.Unknown;
        [SerializeField] private BiologicalInteractionDisposition disposition = BiologicalInteractionDisposition.Contextual;
        [SerializeField] private BiologicalCompatibilityState defaultCompatibility = BiologicalCompatibilityState.Compatible;
        [SerializeField, Min(0f)] private float defaultRateMultiplier = 1f;
        [SerializeField, Min(0f)] private float defaultSeverityMultiplier = 1f;
        [SerializeField, Min(0f)] private float defaultConsequenceMultiplier = 1f;
        [SerializeField, Min(0f)] private float defaultMaximumSeverity = float.PositiveInfinity;
        [SerializeField] private string[] relatedHazardIds;
        [SerializeField] private string[] relatedResourceIds;
        [SerializeField] private string[] relatedInjuryTypeIds;
        [SerializeField] private string[] relatedDamageTypeIds;
        [SerializeField] private string[] supportedModifierChannels;
        [SerializeField] private string[] supportedSpecialOutcomes;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private bool alphaEnabled = true;
        [SerializeField, TextArea(1, 3)] private string validationMetadata;

        public string Id => interactionId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public BiologicalInteractionCategory Category => category;
        public BiologicalInteractionDisposition Disposition => disposition;
        public BiologicalCompatibilityState DefaultCompatibility => defaultCompatibility;
        public float DefaultRateMultiplier => Sanitize(defaultRateMultiplier, 1f);
        public float DefaultSeverityMultiplier => Sanitize(defaultSeverityMultiplier, 1f);
        public float DefaultConsequenceMultiplier => Sanitize(defaultConsequenceMultiplier, 1f);
        public float DefaultMaximumSeverity => SanitizeMaximumSeverity(defaultMaximumSeverity);
        public IReadOnlyList<string> RelatedHazardIds => relatedHazardIds ?? Array.Empty<string>();
        public IReadOnlyList<string> RelatedResourceIds => relatedResourceIds ?? Array.Empty<string>();
        public IReadOnlyList<string> RelatedInjuryTypeIds => relatedInjuryTypeIds ?? Array.Empty<string>();
        public IReadOnlyList<string> RelatedDamageTypeIds => relatedDamageTypeIds ?? Array.Empty<string>();
        public IReadOnlyList<string> SupportedModifierChannels => supportedModifierChannels ?? Array.Empty<string>();
        public IReadOnlyList<string> SupportedSpecialOutcomes => supportedSpecialOutcomes ?? Array.Empty<string>();
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public bool AlphaEnabled => alphaEnabled;
        public string ValidationMetadata => validationMetadata ?? string.Empty;

        private void OnValidate()
        {
            interactionId = interactionId?.Trim();
            displayName = displayName?.Trim();
            defaultRateMultiplier = Sanitize(defaultRateMultiplier, 1f);
            defaultSeverityMultiplier = Sanitize(defaultSeverityMultiplier, 1f);
            defaultConsequenceMultiplier = Sanitize(defaultConsequenceMultiplier, 1f);
            defaultMaximumSeverity = SanitizeMaximumSeverity(defaultMaximumSeverity);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"BiologicalInteractionDefinition '{name}' is missing a stable ID.");
            }
            else if (!BiologicalInteractionIds.IsCanonicalAlphaInteraction(Id))
            {
                report.AddWarning($"BiologicalInteractionDefinition '{Id}' should use the 'interaction.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(BiologicalInteractionCategory), category))
            {
                report.AddError($"BiologicalInteractionDefinition '{DisplayName}' has an invalid category.");
            }

            if (!Enum.IsDefined(typeof(BiologicalInteractionDisposition), disposition))
            {
                report.AddError($"BiologicalInteractionDefinition '{DisplayName}' has an invalid disposition.");
            }

            if (!Enum.IsDefined(typeof(BiologicalCompatibilityState), defaultCompatibility))
            {
                report.AddError($"BiologicalInteractionDefinition '{DisplayName}' has an invalid default compatibility state.");
            }

            ValidateReferenceIds(definitionsById, report);
            ValidateCanonicalAlphaSet(definitionsById, report);
        }

        private void ValidateReferenceIds(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null)
            {
                return;
            }

            foreach (TagDefinition tag in Tags)
            {
                if (tag == null || !definitionsById.ContainsKey(tag.Id))
                {
                    report.AddError($"BiologicalInteractionDefinition '{DisplayName}' references a missing tag.");
                }
            }
        }

        private static void ValidateCanonicalAlphaSet(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || !definitionsById.ContainsKey("species.human"))
            {
                return;
            }

            BiologicalInteractionDefinition first = definitionsById.Values.OfType<BiologicalInteractionDefinition>().OrderBy(definition => definition.Id, StringComparer.Ordinal).FirstOrDefault();
            if (first == null || !ReferenceEquals(first, definitionsById.Values.OfType<BiologicalInteractionDefinition>().FirstOrDefault(definition => definition.Id == first.Id)))
            {
                return;
            }

            foreach (string id in BiologicalInteractionCanonicalSet.RequiredInteractionIds)
            {
                if (!definitionsById.TryGetValue(id, out IGameDefinition definition) || definition is not BiologicalInteractionDefinition)
                {
                    report.AddError($"Canonical BiologicalInteractionDefinition '{id}' must be registered in the alpha definition catalog.");
                }
            }
        }

        private static float Sanitize(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? fallback : Mathf.Min(value, 10f);
        }

        private static float SanitizeMaximumSeverity(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 999f : Mathf.Min(value, 999f);
        }
    }

    public static class BiologicalInteractionCanonicalSet
    {
        public static readonly string[] RequiredInteractionIds =
        {
            BiologicalInteractionIds.Bleeding,
            BiologicalInteractionIds.Suffocation,
            BiologicalInteractionIds.Overheating,
            BiologicalInteractionIds.Hypothermia,
            BiologicalInteractionIds.Starvation,
            BiologicalInteractionIds.Dehydration,
            BiologicalInteractionIds.ExtremeFatigue,
            BiologicalInteractionIds.SleepDeprivation,
            BiologicalInteractionIds.BluntTrauma,
            BiologicalInteractionIds.Laceration,
            BiologicalInteractionIds.Puncture,
            BiologicalInteractionIds.Fracture,
            BiologicalInteractionIds.Burn,
            BiologicalInteractionIds.Crush,
            BiologicalInteractionIds.Severing,
            BiologicalInteractionIds.OrganTrauma,
            BiologicalInteractionIds.CoreDamage,
            BiologicalInteractionIds.IncorporealDisruption,
            BiologicalInteractionIds.Blood,
            BiologicalInteractionIds.Breath,
            BiologicalInteractionIds.Temperature,
            BiologicalInteractionIds.Nutrition,
            BiologicalInteractionIds.Hydration,
            BiologicalInteractionIds.Sleep,
            BiologicalInteractionIds.Fatigue,
            BiologicalInteractionIds.Digestion,
            BiologicalInteractionIds.Intoxication,
            BiologicalInteractionIds.NaturalHealing,
            BiologicalInteractionIds.Regeneration,
            BiologicalInteractionIds.BiologicalHealing,
            BiologicalInteractionIds.ConstructRepair,
            BiologicalInteractionIds.SpiritRestoration,
            BiologicalInteractionIds.HolyHealing,
            BiologicalInteractionIds.NecroticRestoration,
            BiologicalInteractionIds.Disease,
            BiologicalInteractionIds.Infection,
            BiologicalInteractionIds.Parasite,
            BiologicalInteractionIds.Poison,
            BiologicalInteractionIds.Venom,
            BiologicalInteractionIds.Toxin,
            BiologicalInteractionIds.Alcohol,
            BiologicalInteractionIds.Polymorph,
            BiologicalInteractionIds.SpeciesChange,
            BiologicalInteractionIds.Possession,
            BiologicalInteractionIds.BodyReplacement,
            BiologicalInteractionIds.Reincarnation,
            BiologicalInteractionIds.Holy,
            BiologicalInteractionIds.Necrotic,
            BiologicalInteractionIds.Fire,
            BiologicalInteractionIds.Cold,
            BiologicalInteractionIds.Radiant,
            BiologicalInteractionIds.Corruption
        };
    }
}
