using System.Collections.Generic;
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

        private void OnValidate()
        {
            optionalPresentationMaximum = Mathf.Max(0f, optionalPresentationMaximum);
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
        }
    }
}
