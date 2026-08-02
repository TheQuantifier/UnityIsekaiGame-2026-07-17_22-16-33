using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Governments;

namespace UnityIsekaiGame.Laws
{
    public abstract class LegalDefinitionBase : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string definitionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private PoliticalVisibility visibility = PoliticalVisibility.Public;
        [SerializeField] private string[] tags = Array.Empty<string>();
        [SerializeField] private int version = 1;
        public string Id => definitionId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        public string Description => description ?? string.Empty;
        public PoliticalVisibility Visibility => visibility;
        public IReadOnlyList<string> Tags => PoliticalModelUtility.Clean(tags);
        public int Version => Math.Max(1, version);
        protected void ConfigureBase(string id, string name, PoliticalVisibility access, IEnumerable<string> tagIds) { definitionId = PoliticalModelUtility.Normalize(id); displayName = string.IsNullOrWhiteSpace(name) ? definitionId : name.Trim(); description = string.Empty; visibility = access; tags = PoliticalModelUtility.Clean(tagIds); version = 1; }
        public virtual void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report) { if (report == null) return; if (string.IsNullOrWhiteSpace(Id)) report.AddError($"{GetType().Name} has no stable ID."); }
    }

    [CreateAssetMenu(fileName = "LegalAuthorityDefinition", menuName = "Unity Isekai Game/Laws/Legal Authority Definition")]
    public sealed class LegalAuthorityDefinition : LegalDefinitionBase
    {
        [SerializeField] private LegalAuthorityCategory category;
        [SerializeField] private GovernmentLevel[] governmentLevels = Array.Empty<GovernmentLevel>();
        [SerializeField] private JurisdictionCategory[] jurisdictionCategories = Array.Empty<JurisdictionCategory>();
        [SerializeField] private LegalInstrumentCategory[] instrumentCategories = Array.Empty<LegalInstrumentCategory>();
        [SerializeField] private string[] requiredPermissionIds = Array.Empty<string>();
        [SerializeField] private bool allowsDelegation;
        [SerializeField] private bool allowsEmergencyLaw;
        public LegalAuthorityCategory Category => category;
        public IReadOnlyList<GovernmentLevel> GovernmentLevels => governmentLevels ?? Array.Empty<GovernmentLevel>();
        public IReadOnlyList<JurisdictionCategory> JurisdictionCategories => jurisdictionCategories ?? Array.Empty<JurisdictionCategory>();
        public IReadOnlyList<LegalInstrumentCategory> InstrumentCategories => instrumentCategories ?? Array.Empty<LegalInstrumentCategory>();
        public IReadOnlyList<string> RequiredPermissionIds => PoliticalModelUtility.Clean(requiredPermissionIds);
        public bool AllowsDelegation => allowsDelegation;
        public bool AllowsEmergencyLaw => allowsEmergencyLaw;
        public void DevelopmentConfigure(string id, string name, LegalAuthorityCategory authorityCategory, IEnumerable<GovernmentLevel> levels, IEnumerable<JurisdictionCategory> jurisdictions, IEnumerable<LegalInstrumentCategory> instruments, IEnumerable<string> permissions = null, bool delegation = true, bool emergency = false) { ConfigureBase(id, name, PoliticalVisibility.Public, new[] { "law", "authority" }); category = authorityCategory; governmentLevels = (levels ?? Array.Empty<GovernmentLevel>()).Distinct().ToArray(); jurisdictionCategories = (jurisdictions ?? Array.Empty<JurisdictionCategory>()).Distinct().ToArray(); instrumentCategories = (instruments ?? Array.Empty<LegalInstrumentCategory>()).Distinct().ToArray(); requiredPermissionIds = PoliticalModelUtility.Clean(permissions); allowsDelegation = delegation; allowsEmergencyLaw = emergency; }
        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitions, DefinitionValidationReport report) { base.ValidateCatalogDefinition(definitions, report); if (category == LegalAuthorityCategory.Unknown) report?.AddError($"Legal Authority '{DisplayName}' has no category."); if (InstrumentCategories.Count == 0) report?.AddError($"Legal Authority '{DisplayName}' supports no instruments."); }
    }

    [CreateAssetMenu(fileName = "LegalInstrumentDefinition", menuName = "Unity Isekai Game/Laws/Legal Instrument Definition")]
    public sealed class LegalInstrumentDefinition : LegalDefinitionBase
    {
        [SerializeField] private LegalInstrumentCategory category;
        [SerializeField] private int precedence;
        [SerializeField] private LegalConflictPolicy conflictPolicy = LegalConflictPolicy.HigherPrecedenceWins;
        [SerializeField] private bool requiresPublication = true;
        [SerializeField] private bool allowsSuspension = true;
        [SerializeField] private bool allowsAmendment = true;
        [SerializeField] private bool allowsRepeal = true;
        [SerializeField] private double maximumEmergencyDuration = -1d;
        public LegalInstrumentCategory Category => category; public int Precedence => precedence; public LegalConflictPolicy ConflictPolicy => conflictPolicy; public bool RequiresPublication => requiresPublication; public bool AllowsSuspension => allowsSuspension; public bool AllowsAmendment => allowsAmendment; public bool AllowsRepeal => allowsRepeal; public double MaximumEmergencyDuration => maximumEmergencyDuration;
        public void DevelopmentConfigure(string id, string name, LegalInstrumentCategory instrumentCategory, int legalPrecedence, LegalConflictPolicy policy, bool publication = true, double emergencyDuration = -1d) { ConfigureBase(id, name, PoliticalVisibility.Public, new[] { "law", "instrument" }); category = instrumentCategory; precedence = legalPrecedence; conflictPolicy = policy; requiresPublication = publication; allowsSuspension = true; allowsAmendment = true; allowsRepeal = true; maximumEmergencyDuration = emergencyDuration; }
        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitions, DefinitionValidationReport report) { base.ValidateCatalogDefinition(definitions, report); if (category == LegalInstrumentCategory.Unknown) report?.AddError($"Legal Instrument '{DisplayName}' has no category."); }
    }

    [CreateAssetMenu(fileName = "LegalProvisionDefinition", menuName = "Unity Isekai Game/Laws/Legal Provision Definition")]
    public sealed class LegalProvisionDefinition : LegalDefinitionBase
    {
        [SerializeField] private LegalEffectCategory effectCategory;
        [SerializeField] private LegalInstrumentCategory[] supportedInstruments = Array.Empty<LegalInstrumentCategory>();
        public LegalEffectCategory EffectCategory => effectCategory; public IReadOnlyList<LegalInstrumentCategory> SupportedInstruments => supportedInstruments ?? Array.Empty<LegalInstrumentCategory>();
        public void DevelopmentConfigure(string id, string name, LegalEffectCategory effect, IEnumerable<LegalInstrumentCategory> instruments) { ConfigureBase(id, name, PoliticalVisibility.Public, new[] { "law", "provision" }); effectCategory = effect; supportedInstruments = (instruments ?? Array.Empty<LegalInstrumentCategory>()).Distinct().ToArray(); }
        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitions, DefinitionValidationReport report) { base.ValidateCatalogDefinition(definitions, report); if (effectCategory == LegalEffectCategory.Unknown) report?.AddError($"Legal Provision '{DisplayName}' has no effect category."); }
    }

    [CreateAssetMenu(fileName = "LegalStatusDefinition", menuName = "Unity Isekai Game/Laws/Legal Status Definition")]
    public sealed class LegalStatusDefinition : LegalDefinitionBase
    {
        [SerializeField] private LegalStatusCategory category;
        [SerializeField] private bool requiresPolity;
        [SerializeField] private bool allowsMultiple = true;
        [SerializeField] private string[] rightDefinitionIds = Array.Empty<string>();
        [SerializeField] private string[] dutyDefinitionIds = Array.Empty<string>();
        public LegalStatusCategory Category => category; public bool RequiresPolity => requiresPolity; public bool AllowsMultiple => allowsMultiple; public IReadOnlyList<string> RightDefinitionIds => PoliticalModelUtility.Clean(rightDefinitionIds); public IReadOnlyList<string> DutyDefinitionIds => PoliticalModelUtility.Clean(dutyDefinitionIds);
        public void DevelopmentConfigure(string id, string name, LegalStatusCategory statusCategory, bool polityRequired, bool multiple = true) { ConfigureBase(id, name, PoliticalVisibility.Restricted, new[] { "law", "status" }); category = statusCategory; requiresPolity = polityRequired; allowsMultiple = multiple; }
        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitions, DefinitionValidationReport report) { base.ValidateCatalogDefinition(definitions, report); if (category == LegalStatusCategory.Unknown) report?.AddError($"Legal Status '{DisplayName}' has no category."); }
    }

    [CreateAssetMenu(fileName = "CitizenshipDefinition", menuName = "Unity Isekai Game/Laws/Citizenship Definition")]
    public sealed class CitizenshipDefinition : LegalDefinitionBase
    {
        [SerializeField] private CitizenshipAcquisitionRoute[] routes = Array.Empty<CitizenshipAcquisitionRoute>();
        [SerializeField] private bool requiresConsent = true;
        [SerializeField] private bool allowsMultiple = true;
        public IReadOnlyList<CitizenshipAcquisitionRoute> Routes => routes ?? Array.Empty<CitizenshipAcquisitionRoute>(); public bool RequiresConsent => requiresConsent; public bool AllowsMultiple => allowsMultiple;
        public void DevelopmentConfigure(string id, string name, IEnumerable<CitizenshipAcquisitionRoute> acquisitionRoutes, bool consent, bool multiple) { ConfigureBase(id, name, PoliticalVisibility.Restricted, new[] { "law", "citizenship" }); routes = (acquisitionRoutes ?? Array.Empty<CitizenshipAcquisitionRoute>()).Distinct().ToArray(); requiresConsent = consent; allowsMultiple = multiple; }
        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitions, DefinitionValidationReport report) { base.ValidateCatalogDefinition(definitions, report); if (Routes.Count == 0) report?.AddError($"Citizenship Definition '{DisplayName}' has no acquisition route."); }
    }
}
