using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Progression
{
    [CreateAssetMenu(fileName = "CurrencyDefinition", menuName = "Unity Isekai Game/Progression/Currency Definition")]
    public sealed class CurrencyDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string currencyId;
        [SerializeField] private string displayName;
        [SerializeField] private string symbol = "G";
        [SerializeField, TextArea] private string description;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField, Min(0)] private int minorUnitPrecision;
        [SerializeField] private string denominationRelationshipMetadata;
        [SerializeField] private string regionFactionMetadata;
        [SerializeField] private bool enabledForAlpha = true;
        [SerializeField] private string stackDisplayPolicy = "WholeUnits";
        [SerializeField] private string futureExchangeSupport;

        public string CurrencyId => currencyId;
        public string Id => currencyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Symbol => symbol ?? string.Empty;
        public string Description => description;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Currency;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public int MinorUnitPrecision => Mathf.Max(0, minorUnitPrecision);
        public string DenominationRelationshipMetadata => denominationRelationshipMetadata ?? string.Empty;
        public string RegionFactionMetadata => regionFactionMetadata ?? string.Empty;
        public bool EnabledForAlpha => enabledForAlpha;
        public string StackDisplayPolicy => stackDisplayPolicy ?? string.Empty;
        public string FutureExchangeSupport => futureExchangeSupport ?? string.Empty;

        private void OnValidate()
        {
            minorUnitPrecision = Mathf.Max(0, minorUnitPrecision);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (!Id.StartsWith("currency."))
            {
                report.AddWarning($"Currency '{DisplayName}' should use the 'currency.' namespace prefix.");
            }

            if (MinorUnitPrecision < 0)
            {
                report.AddError($"Currency '{DisplayName}' has invalid precision.");
            }
        }
    }
}
