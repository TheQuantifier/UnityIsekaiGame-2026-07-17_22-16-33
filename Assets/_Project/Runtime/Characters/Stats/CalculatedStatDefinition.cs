using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Stats
{
    [CreateAssetMenu(fileName = "CalculatedStatDefinition", menuName = "Unity Isekai Game/Stats/Calculated Stat Definition")]
    public sealed class CalculatedStatDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string statId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private CalculatedStatFormulaDefinition formula;
        [SerializeField] private bool exposedOnCharacterMenu = true;
        [SerializeField] private int sortOrder;
        [SerializeField] private float optionalPresentationMaximum;
        [SerializeField] private CalculatedStatPurpose purpose = CalculatedStatPurpose.General;
        [SerializeField] private string linkedFutureResourceId;

        public string Id => statId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.CalculatedStat;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public CalculatedStatFormulaDefinition Formula => formula;
        public bool ExposedOnCharacterMenu => exposedOnCharacterMenu;
        public int SortOrder => sortOrder;
        public float OptionalPresentationMaximum => Mathf.Max(0f, optionalPresentationMaximum);
        public CalculatedStatPurpose Purpose => purpose;
        public string LinkedFutureResourceId => linkedFutureResourceId ?? string.Empty;
        public bool IsResourceMaximum => purpose == CalculatedStatPurpose.ResourceMaximum;

        private void OnValidate()
        {
            optionalPresentationMaximum = Mathf.Max(0f, optionalPresentationMaximum);
            if (linkedFutureResourceId != null)
            {
                linkedFutureResourceId = linkedFutureResourceId.Trim();
            }
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"Calculated stat '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("calculated-stat."))
            {
                report.AddWarning($"Calculated stat '{Id}' should use the 'calculated-stat.' namespace prefix.");
            }

            if (formula == null)
            {
                report.AddError($"Calculated stat '{DisplayName}' is missing a formula definition.");
                return;
            }

            if (definitionsById == null || !definitionsById.TryGetValue(formula.Id, out IGameDefinition found) || found is not CalculatedStatFormulaDefinition)
            {
                report.AddError($"Calculated stat '{DisplayName}' references formula '{formula.Id}', which is not in the configured catalog.");
            }

            ValidatePurposeMetadata(definitionsById, report);
        }

        private void ValidatePurposeMetadata(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (!Enum.IsDefined(typeof(CalculatedStatPurpose), purpose))
            {
                report.AddError($"Calculated stat '{DisplayName}' has an invalid purpose value '{(int)purpose}'.");
            }

            if (IsResourceMaximum)
            {
                if (string.IsNullOrWhiteSpace(LinkedFutureResourceId))
                {
                    report.AddError($"Calculated stat '{DisplayName}' is a resource maximum but does not declare a linked future resource ID.");
                }
                else if (!LinkedFutureResourceId.StartsWith("resource."))
                {
                    report.AddError($"Calculated stat '{DisplayName}' resource link '{LinkedFutureResourceId}' must use the 'resource.' namespace prefix.");
                }
                else if (!CalculatedStatIds.IsReservedFutureResourceId(LinkedFutureResourceId))
                {
                    report.AddError($"Calculated stat '{DisplayName}' links unsupported future resource ID '{LinkedFutureResourceId}'.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(LinkedFutureResourceId))
            {
                report.AddWarning($"Calculated stat '{DisplayName}' links resource '{LinkedFutureResourceId}' but is not marked as a resource maximum.");
            }

            if (definitionsById == null)
            {
                return;
            }

            List<CalculatedStatDefinition> stats = definitionsById.Values
                .OfType<CalculatedStatDefinition>()
                .Where(stat => stat != null)
                .OrderBy(stat => stat.Id, StringComparer.Ordinal)
                .ToList();
            if (stats.Count == 0)
            {
                return;
            }

            if (IsResourceMaximum && !string.IsNullOrWhiteSpace(LinkedFutureResourceId))
            {
                foreach (CalculatedStatDefinition duplicate in stats)
                {
                    if (duplicate == this
                        || !duplicate.IsResourceMaximum
                        || !string.Equals(duplicate.LinkedFutureResourceId, LinkedFutureResourceId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    report.AddError($"Calculated stat '{DisplayName}' duplicates resource-maximum mapping '{LinkedFutureResourceId}' already used by '{duplicate.Id}'.");
                }
            }

            if (!ReferenceEquals(stats[0], this))
            {
                return;
            }

            ValidateRequiredResourceMapping(stats, CalculatedStatIds.FutureResourceHealth, CalculatedStatIds.MaximumHealth, report);
            ValidateRequiredResourceMapping(stats, CalculatedStatIds.FutureResourceStamina, CalculatedStatIds.MaximumStamina, report);
            ValidateRequiredResourceMapping(stats, CalculatedStatIds.FutureResourceMana, CalculatedStatIds.MaximumMana, report);
        }

        private static void ValidateRequiredResourceMapping(
            IReadOnlyList<CalculatedStatDefinition> stats,
            string resourceId,
            string expectedStatId,
            DefinitionValidationReport report)
        {
            List<CalculatedStatDefinition> mapped = stats
                .Where(stat => stat.IsResourceMaximum && string.Equals(stat.LinkedFutureResourceId, resourceId, StringComparison.Ordinal))
                .ToList();
            if (mapped.Count == 0)
            {
                report.AddError($"No calculated stat maps the future resource maximum '{resourceId}'. Expected '{expectedStatId}'.");
                return;
            }

            if (mapped.Count > 1)
            {
                report.AddError($"Future resource maximum '{resourceId}' is mapped by multiple calculated stats: {string.Join(", ", mapped.Select(stat => stat.Id))}.");
                return;
            }

            if (!string.Equals(mapped[0].Id, expectedStatId, StringComparison.Ordinal))
            {
                report.AddError($"Future resource maximum '{resourceId}' must be mapped by '{expectedStatId}', but is mapped by '{mapped[0].Id}'.");
            }
        }
    }
}
