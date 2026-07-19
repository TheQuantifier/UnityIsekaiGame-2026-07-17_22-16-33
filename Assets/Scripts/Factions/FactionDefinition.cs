using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;

namespace UnityIsekaiGame.Factions
{
    [CreateAssetMenu(fileName = "Faction", menuName = "Unity Isekai Game/Factions/Faction")]
    public sealed class FactionDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string factionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private Sprite icon;
        [SerializeField] private FactionKind kind = FactionKind.Unspecified;
        [SerializeField] private FactionDefinition parentFaction;
        [SerializeField] private PlaceDefinition homePlace;
        [SerializeField] private PlaceDefinition headquartersPlace;
        [SerializeField] private PlaceDefinition defaultJurisdictionPlace;
        [SerializeField] private PlaceDefinition foundingPlace;
        [SerializeField] private PersonDefinition defaultLeader;
        [SerializeField] private FactionAuthorityFlags authority = FactionAuthorityFlags.None;
        [SerializeField] private FactionVisibility visibility = FactionVisibility.Public;
        [SerializeField] private Color presentationColor = Color.white;
        [SerializeField] private string culturePlaceholder;
        [SerializeField] private string foundingMetadataPlaceholder;

        public string FactionId => factionId;
        public string Id => factionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Faction;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public Sprite Icon => icon;
        public FactionKind Kind => kind;
        public FactionDefinition ParentFaction => parentFaction;
        public PlaceDefinition HomePlace => homePlace;
        public PlaceDefinition HeadquartersPlace => headquartersPlace;
        public PlaceDefinition DefaultJurisdictionPlace => defaultJurisdictionPlace;
        public PlaceDefinition FoundingPlace => foundingPlace;
        public PersonDefinition DefaultLeader => defaultLeader;
        public FactionAuthorityFlags Authority => authority;
        public FactionVisibility Visibility => visibility;
        public Color PresentationColor => presentationColor;
        public string CulturePlaceholder => culturePlaceholder;
        public string FoundingMetadataPlaceholder => foundingMetadataPlaceholder;

        public bool HasAuthority(FactionAuthorityFlags capability)
        {
            return capability != FactionAuthorityFlags.None && (authority & capability) == capability;
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || report == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Id) && !Id.StartsWith("faction."))
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' should use the 'faction.' namespace prefix.");
            }

            if (kind == FactionKind.Unspecified)
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' has unspecified faction kind metadata.");
            }

            if (!Enum.IsDefined(typeof(FactionKind), kind))
            {
                report.AddError($"FactionDefinition '{DisplayName}' has invalid faction kind '{kind}'.");
            }

            if (!Enum.IsDefined(typeof(FactionVisibility), visibility))
            {
                report.AddError($"FactionDefinition '{DisplayName}' has invalid visibility '{visibility}'.");
            }

            ValidateAuthority(report);
            ValidateParent(definitionsById, report);
            ValidatePlaceReference(homePlace, nameof(HomePlace), definitionsById, report);
            ValidatePlaceReference(headquartersPlace, nameof(HeadquartersPlace), definitionsById, report);
            ValidatePlaceReference(defaultJurisdictionPlace, nameof(DefaultJurisdictionPlace), definitionsById, report);
            ValidatePlaceReference(foundingPlace, nameof(FoundingPlace), definitionsById, report);
            ValidateLeader(definitionsById, report);
            ValidateKindCategoryConsistency(report);
            ValidateSuspiciousConfiguration(report);
        }

        private void ValidateAuthority(DefinitionValidationReport report)
        {
            int validFlags = 0;
            foreach (FactionAuthorityFlags value in Enum.GetValues(typeof(FactionAuthorityFlags)))
            {
                validFlags |= (int)value;
            }

            if (((int)authority & ~validFlags) != 0)
            {
                report.AddError($"FactionDefinition '{DisplayName}' has invalid authority flags '{authority}'.");
            }
        }

        private void ValidateParent(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (parentFaction == null)
            {
                return;
            }

            if (ReferenceEquals(this, parentFaction) || parentFaction.Id == Id)
            {
                report.AddError($"FactionDefinition '{DisplayName}' cannot parent itself.");
            }
            else if (!definitionsById.TryGetValue(parentFaction.Id, out IGameDefinition parent) || parent is not FactionDefinition)
            {
                report.AddError($"FactionDefinition '{DisplayName}' references parent faction '{parentFaction.Id}', which is not in the configured catalog.");
            }

            if (FactionHierarchyUtility.HasParentCycle(this))
            {
                report.AddError($"FactionDefinition '{DisplayName}' has a circular parent hierarchy.");
            }
        }

        private void ValidatePlaceReference(
            PlaceDefinition place,
            string label,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (place == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(place.Id, out IGameDefinition found) || found is not PlaceDefinition)
            {
                report.AddError($"FactionDefinition '{DisplayName}' references {label} '{place.Id}', which is not in the configured catalog.");
            }
        }

        private void ValidateLeader(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (defaultLeader == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(defaultLeader.Id, out IGameDefinition found) || found is not PersonDefinition)
            {
                report.AddError($"FactionDefinition '{DisplayName}' references default leader '{defaultLeader.Id}', which is not in the configured catalog.");
            }
        }

        private void ValidateKindCategoryConsistency(DefinitionValidationReport report)
        {
            if (primaryCategory == null || kind == FactionKind.Unspecified)
            {
                return;
            }

            string expectedCategoryId = FactionHierarchyUtility.GetExpectedCategoryId(kind);
            if (!string.IsNullOrWhiteSpace(expectedCategoryId)
                && !ClassificationUtility.IsInCategory(primaryCategory, expectedCategoryId))
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' kind '{kind}' is not under expected category '{expectedCategoryId}'.");
            }
        }

        private void ValidateSuspiciousConfiguration(DefinitionValidationReport report)
        {
            if (kind == FactionKind.Criminal
                && primaryCategory != null
                && ClassificationUtility.IsInCategory(primaryCategory, "faction-category.government"))
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' is a criminal group categorized as government.");
            }

            if (kind == FactionKind.Military
                && parentFaction != null
                && parentFaction.Kind == FactionKind.Company)
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' is a military group parented under a company.");
            }

            if ((kind == FactionKind.Guild || kind == FactionKind.Company)
                && defaultJurisdictionPlace != null
                && defaultJurisdictionPlace.PlaceKind == PlaceKind.World)
            {
                report.AddWarning($"FactionDefinition '{DisplayName}' has broad world-level default jurisdiction for a local organization.");
            }
        }
    }
}
