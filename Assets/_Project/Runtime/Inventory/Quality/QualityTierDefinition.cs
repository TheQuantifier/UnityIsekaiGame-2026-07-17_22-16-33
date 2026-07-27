using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Inventory.Quality
{
    [CreateAssetMenu(fileName = "QualityTierDefinition", menuName = "Unity Isekai Game/Inventory/Quality Tier Definition")]
    public sealed class QualityTierDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string tierId;
        [SerializeField] private string displayName;
        [SerializeField, Range(0f, 1f)] private float minimumQuality;
        [SerializeField, Range(0f, 1f)] private float maximumQuality = 1f;
        [SerializeField] private int sortOrder;
        [SerializeField] private string gameplayClassification;
        [SerializeField] private string defaultModifierPolicyId;
        [SerializeField] private int appraisalDifficulty;
        [SerializeField] private string accessPolicyId;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private int version = 1;

        public string Id => tierId;
        public string DisplayName => displayName;
        public float MinimumQuality => minimumQuality;
        public float MaximumQuality => maximumQuality;
        public int SortOrder => sortOrder;
        public string GameplayClassification => gameplayClassification ?? string.Empty;
        public string DefaultModifierPolicyId => defaultModifierPolicyId ?? string.Empty;
        public int AppraisalDifficulty => appraisalDifficulty;
        public string AccessPolicyId => accessPolicyId ?? string.Empty;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public int Version => Math.Max(1, version);

        public bool Contains(float quality)
        {
            return quality >= minimumQuality && quality <= maximumQuality;
        }

        private void OnValidate()
        {
            minimumQuality = Mathf.Clamp01(minimumQuality);
            maximumQuality = Mathf.Clamp01(maximumQuality);
            if (maximumQuality < minimumQuality)
            {
                maximumQuality = minimumQuality;
            }

            version = Mathf.Max(1, version);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (minimumQuality < 0f || maximumQuality > 1f || maximumQuality < minimumQuality)
            {
                report.AddError($"Quality tier '{DisplayName}' has an invalid quality range {minimumQuality:0.###}..{maximumQuality:0.###}.");
            }

            foreach (TagDefinition tag in Tags)
            {
                if (tag == null)
                {
                    report.AddError($"Quality tier '{DisplayName}' has a missing tag.");
                }
            }
        }

        public static bool ValidateTierRanges(IEnumerable<QualityTierDefinition> tiers, DefinitionValidationReport report, bool requireGapless = false)
        {
            List<QualityTierDefinition> ordered = new List<QualityTierDefinition>();
            if (tiers != null)
            {
                foreach (QualityTierDefinition tier in tiers)
                {
                    if (tier != null)
                    {
                        ordered.Add(tier);
                    }
                }
            }

            ordered.Sort((left, right) =>
            {
                int order = left.MinimumQuality.CompareTo(right.MinimumQuality);
                return order != 0 ? order : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });

            bool valid = true;
            float previousMax = -1f;
            string previousId = string.Empty;
            for (int i = 0; i < ordered.Count; i++)
            {
                QualityTierDefinition tier = ordered[i];
                if (tier.MinimumQuality < previousMax)
                {
                    valid = false;
                    report?.AddError($"Quality tier '{tier.Id}' overlaps previous tier '{previousId}'.");
                }

                if (requireGapless && previousMax >= 0f && tier.MinimumQuality > previousMax + 0.0001f)
                {
                    valid = false;
                    report?.AddError($"Quality tiers have a gap between '{previousId}' and '{tier.Id}'.");
                }

                previousMax = tier.MaximumQuality;
                previousId = tier.Id;
            }

            return valid;
        }
    }
}
