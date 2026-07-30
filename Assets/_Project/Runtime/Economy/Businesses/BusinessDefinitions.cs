using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Progression;

namespace UnityIsekaiGame.Economy.Businesses
{
    [Serializable]
    public sealed class BusinessSharePolicyData
    {
        public bool requireTotalActiveOwnership = true;
        public long requiredTotalNumerator = 10000L;
        public long requiredTotalDenominator = 10000L;
        public bool allowControlDifferentFromOwnership = true;

        public BusinessSharePolicyData Clone()
        {
            return new BusinessSharePolicyData
            {
                requireTotalActiveOwnership = requireTotalActiveOwnership,
                requiredTotalNumerator = Math.Max(0L, requiredTotalNumerator),
                requiredTotalDenominator = Math.Max(1L, requiredTotalDenominator),
                allowControlDifferentFromOwnership = allowControlDifferentFromOwnership
            };
        }
    }

    [CreateAssetMenu(fileName = "BusinessDefinition", menuName = "Unity Isekai Game/Economy/Business Definition")]
    public sealed class BusinessDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string businessDefinitionId;
        [SerializeField] private string displayName;
        [SerializeField] private BusinessCategory category = BusinessCategory.MerchantShop;
        [SerializeField] private BusinessOwnerSubjectKind[] permittedOwnerTypes = { BusinessOwnerSubjectKind.Person, BusinessOwnerSubjectKind.Organization };
        [SerializeField] private BusinessEstablishmentType[] permittedEstablishmentTypes = { BusinessEstablishmentType.Shop, BusinessEstablishmentType.Stall, BusinessEstablishmentType.Workshop };
        [SerializeField] private string[] permittedGoodsAndServiceCategories = Array.Empty<string>();
        [SerializeField] private string[] requiredProfessionOrCredentialIds = Array.Empty<string>();
        [SerializeField] private string[] requiredRoleOrPositionIds = Array.Empty<string>();
        [SerializeField] private BusinessAccountPurpose[] defaultAccountPurposes = { BusinessAccountPurpose.OperatingFunds };
        [SerializeField] private BusinessInventoryPurpose[] defaultInventoryPurposes = { BusinessInventoryPurpose.RetailStock };
        [SerializeField] private ProductionOutputOwnerPolicy defaultOutputOwnerPolicy = ProductionOutputOwnerPolicy.BusinessOwnsOutputs;
        [SerializeField] private BusinessRevenueCategory[] defaultRevenueCategories = { BusinessRevenueCategory.RetailSale, BusinessRevenueCategory.ServiceIncome };
        [SerializeField] private BusinessExpenseCategory[] defaultExpenseCategories = { BusinessExpenseCategory.InventoryPurchase, BusinessExpenseCategory.PayrollExpense };
        [SerializeField] private BusinessSharePolicyData ownershipPolicy = new BusinessSharePolicyData();
        [SerializeField] private string defaultControlPolicyId;
        [SerializeField] private string accessPolicyId;
        [SerializeField, Min(1)] private int definitionVersion = 1;

        public string Id => businessDefinitionId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public BusinessCategory Category => category;
        public IReadOnlyList<BusinessOwnerSubjectKind> PermittedOwnerTypes => BusinessModelHelpers.NormalizeEnums(permittedOwnerTypes);
        public IReadOnlyList<BusinessEstablishmentType> PermittedEstablishmentTypes => BusinessModelHelpers.NormalizeEnums(permittedEstablishmentTypes);
        public IReadOnlyList<string> PermittedGoodsAndServiceCategories => BusinessModelHelpers.CleanIds(permittedGoodsAndServiceCategories);
        public IReadOnlyList<string> RequiredProfessionOrCredentialIds => BusinessModelHelpers.CleanIds(requiredProfessionOrCredentialIds);
        public IReadOnlyList<string> RequiredRoleOrPositionIds => BusinessModelHelpers.CleanIds(requiredRoleOrPositionIds);
        public IReadOnlyList<BusinessAccountPurpose> DefaultAccountPurposes => BusinessModelHelpers.NormalizeEnums(defaultAccountPurposes);
        public IReadOnlyList<BusinessInventoryPurpose> DefaultInventoryPurposes => BusinessModelHelpers.NormalizeEnums(defaultInventoryPurposes);
        public ProductionOutputOwnerPolicy DefaultOutputOwnerPolicy => defaultOutputOwnerPolicy;
        public IReadOnlyList<BusinessRevenueCategory> DefaultRevenueCategories => BusinessModelHelpers.NormalizeEnums(defaultRevenueCategories);
        public IReadOnlyList<BusinessExpenseCategory> DefaultExpenseCategories => BusinessModelHelpers.NormalizeEnums(defaultExpenseCategories);
        public BusinessSharePolicyData OwnershipPolicy => ownershipPolicy?.Clone() ?? new BusinessSharePolicyData();
        public string DefaultControlPolicyId => defaultControlPolicyId ?? string.Empty;
        public string AccessPolicyId => accessPolicyId ?? string.Empty;
        public int DefinitionVersion => Math.Max(1, definitionVersion);

        public void Initialize(string id, string display, BusinessCategory businessCategory)
        {
            businessDefinitionId = id ?? string.Empty;
            displayName = display ?? string.Empty;
            category = businessCategory;
            definitionVersion = Math.Max(1, definitionVersion);
        }

        private void OnValidate()
        {
            definitionVersion = Math.Max(1, definitionVersion);
            ownershipPolicy ??= new BusinessSharePolicyData();
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id) || !Id.StartsWith("business.", StringComparison.Ordinal))
            {
                report.AddError($"Business definition '{DisplayName}' must use the 'business.' namespace.");
            }

            if (!Enum.IsDefined(typeof(BusinessCategory), category) || category == BusinessCategory.Unknown)
            {
                report.AddError($"Business definition '{DisplayName}' has an invalid business category.");
            }

            if (PermittedOwnerTypes.Count == 0 || PermittedOwnerTypes.Contains(BusinessOwnerSubjectKind.Unknown))
            {
                report.AddError($"Business definition '{DisplayName}' must declare permitted owner types.");
            }

            if (PermittedEstablishmentTypes.Count == 0 || PermittedEstablishmentTypes.Contains(BusinessEstablishmentType.Unknown))
            {
                report.AddError($"Business definition '{DisplayName}' must declare permitted establishment types.");
            }

            BusinessSharePolicyData policy = OwnershipPolicy;
            if (policy.requiredTotalDenominator <= 0L || policy.requiredTotalNumerator < 0L || DefinitionVersion <= 0)
            {
                report.AddError($"Business definition '{DisplayName}' has an invalid ownership policy or version.");
            }

            foreach (string id in RequiredProfessionOrCredentialIds)
            {
                if (definitionsById != null && !definitionsById.ContainsKey(id))
                {
                    report.AddWarning($"Business definition '{DisplayName}' references optional profession or credential '{id}' that is not in the current catalog.");
                }
            }
        }
    }
}
