using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Beings.Biology.Compatibility;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Beings.Biology.Transformation
{
    [CreateAssetMenu(fileName = "TransformationMethod", menuName = "Unity Isekai Game/Beings/Biology/Transformation Method")]
    public sealed class TransformationMethodDefinition : ScriptableObject, IGameDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string methodId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private string biologicalInteractionDefinitionId;
        [SerializeField] private TransformationCategory category = TransformationCategory.Unknown;
        [SerializeField] private bool temporary;
        [SerializeField, Min(0f)] private float defaultDurationSeconds;
        [SerializeField] private string[] allowedTargetSpeciesIds = Array.Empty<string>();
        [SerializeField] private string[] allowedTargetBodyFormIds = Array.Empty<string>();
        [SerializeField] private string[] requiredRuntimeCapabilityKeys = Array.Empty<string>();
        [SerializeField] private string[] blockingRuntimeCapabilityKeys = Array.Empty<string>();
        [SerializeField] private TransformationTransferPolicy transferPolicy = TransformationTransferPolicy.TransferPersonOwnedOnly;
        [SerializeField] private TransformationReconciliationPolicy anatomyPolicy = TransformationReconciliationPolicy.Rebuild;
        [SerializeField] private TransformationReconciliationPolicy conditionPolicy = TransformationReconciliationPolicy.Clear;
        [SerializeField] private TransformationReconciliationPolicy vitalPolicy = TransformationReconciliationPolicy.InitializeClean;
        [SerializeField] private TransformationReconciliationPolicy hazardPolicy = TransformationReconciliationPolicy.Clear;
        [SerializeField] private TransformationReconciliationPolicy recoveryPolicy = TransformationReconciliationPolicy.Cancel;
        [SerializeField] private TransformationEquipmentPolicy equipmentPolicy = TransformationEquipmentPolicy.PreserveIfCompatible;
        [SerializeField] private TransformationLifecyclePolicy lifecyclePolicy = TransformationLifecyclePolicy.Preserve;
        [SerializeField] private TransformationControllerPolicy controllerPolicy = TransformationControllerPolicy.PreserveController;
        [SerializeField] private TransformationAssociationPolicy associationPolicy = TransformationAssociationPolicy.PreserveBody;
        [SerializeField] private TransformationReversionPolicy reversionPolicy = TransformationReversionPolicy.None;
        [SerializeField] private bool allowSameSpecies;
        [SerializeField] private bool alphaExecutionEnabled = true;
        [SerializeField] private TagDefinition[] tags = Array.Empty<TagDefinition>();
        [SerializeField, TextArea(1, 3)] private string validationMetadata;

        public string Id => methodId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public string BiologicalInteractionDefinitionId => biologicalInteractionDefinitionId ?? string.Empty;
        public TransformationCategory Category => category;
        public bool Temporary => temporary;
        public float DefaultDurationSeconds => Mathf.Max(0f, defaultDurationSeconds);
        public IReadOnlyList<string> AllowedTargetSpeciesIds => allowedTargetSpeciesIds ?? Array.Empty<string>();
        public IReadOnlyList<string> AllowedTargetBodyFormIds => allowedTargetBodyFormIds ?? Array.Empty<string>();
        public IReadOnlyList<string> RequiredRuntimeCapabilityKeys => requiredRuntimeCapabilityKeys ?? Array.Empty<string>();
        public IReadOnlyList<string> BlockingRuntimeCapabilityKeys => blockingRuntimeCapabilityKeys ?? Array.Empty<string>();
        public TransformationTransferPolicy TransferPolicy => transferPolicy;
        public TransformationReconciliationPolicy AnatomyPolicy => anatomyPolicy;
        public TransformationReconciliationPolicy ConditionPolicy => conditionPolicy;
        public TransformationReconciliationPolicy VitalPolicy => vitalPolicy;
        public TransformationReconciliationPolicy HazardPolicy => hazardPolicy;
        public TransformationReconciliationPolicy RecoveryPolicy => recoveryPolicy;
        public TransformationEquipmentPolicy EquipmentPolicy => equipmentPolicy;
        public TransformationLifecyclePolicy LifecyclePolicy => lifecyclePolicy;
        public TransformationControllerPolicy ControllerPolicy => controllerPolicy;
        public TransformationAssociationPolicy AssociationPolicy => associationPolicy;
        public TransformationReversionPolicy ReversionPolicy => reversionPolicy;
        public bool AllowSameSpecies => allowSameSpecies;
        public bool AlphaExecutionEnabled => alphaExecutionEnabled;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public string ValidationMetadata => validationMetadata ?? string.Empty;

        private void OnValidate()
        {
            methodId = methodId?.Trim();
            displayName = displayName?.Trim();
            biologicalInteractionDefinitionId = biologicalInteractionDefinitionId?.Trim();
            defaultDurationSeconds = Mathf.Max(0f, defaultDurationSeconds);
            allowedTargetSpeciesIds = Normalize(allowedTargetSpeciesIds);
            allowedTargetBodyFormIds = Normalize(allowedTargetBodyFormIds);
            requiredRuntimeCapabilityKeys = Normalize(requiredRuntimeCapabilityKeys);
            blockingRuntimeCapabilityKeys = Normalize(blockingRuntimeCapabilityKeys);
        }

        public bool AllowsTargetSpecies(string speciesId)
        {
            return AllowedTargetSpeciesIds.Count == 0 || AllowedTargetSpeciesIds.Contains(speciesId ?? string.Empty, StringComparer.Ordinal);
        }

        public bool AllowsTargetBodyForm(string bodyFormId)
        {
            return AllowedTargetBodyFormIds.Count == 0 || AllowedTargetBodyFormIds.Contains(bodyFormId ?? string.Empty, StringComparer.Ordinal);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"TransformationMethodDefinition '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("transformation.", StringComparison.Ordinal))
            {
                report.AddWarning($"TransformationMethodDefinition '{Id}' should use the 'transformation.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(TransformationCategory), category) || category == TransformationCategory.Unknown)
            {
                report.AddError($"TransformationMethodDefinition '{DisplayName}' has an invalid category.");
            }

            if (string.IsNullOrWhiteSpace(BiologicalInteractionDefinitionId)
                || definitionsById == null
                || !definitionsById.TryGetValue(BiologicalInteractionDefinitionId, out IGameDefinition interaction)
                || interaction is not BiologicalInteractionDefinition)
            {
                report.AddError($"TransformationMethodDefinition '{DisplayName}' references missing Biological Interaction '{BiologicalInteractionDefinitionId}'.");
            }

            foreach (string speciesId in AllowedTargetSpeciesIds)
            {
                if (definitionsById == null || !definitionsById.TryGetValue(speciesId, out IGameDefinition species) || species is not SpeciesDefinition)
                {
                    report.AddError($"TransformationMethodDefinition '{DisplayName}' references missing target Species '{speciesId}'.");
                }
            }

            foreach (string bodyFormId in AllowedTargetBodyFormIds)
            {
                if (definitionsById == null || !definitionsById.TryGetValue(bodyFormId, out IGameDefinition bodyForm) || bodyForm is not BodyFormDefinition)
                {
                    report.AddError($"TransformationMethodDefinition '{DisplayName}' references missing target body form '{bodyFormId}'.");
                }
            }

            foreach (string capabilityKey in RequiredRuntimeCapabilityKeys.Concat(BlockingRuntimeCapabilityKeys))
            {
                if (capabilityKey.StartsWith("capability.", StringComparison.Ordinal))
                {
                    report.AddError($"TransformationMethodDefinition '{DisplayName}' runtime capability key '{capabilityKey}' must use a runtime key, not a CapabilityDefinition ID.");
                }
            }

            if (temporary && reversionPolicy == TransformationReversionPolicy.None)
            {
                report.AddError($"TransformationMethodDefinition '{DisplayName}' is temporary but has no reversion policy.");
            }

            ValidateCanonicalAlphaSet(definitionsById, report);
        }

        private static void ValidateCanonicalAlphaSet(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || !definitionsById.ContainsKey("species.human"))
            {
                return;
            }

            TransformationMethodDefinition first = definitionsById.Values.OfType<TransformationMethodDefinition>().OrderBy(definition => definition.Id, StringComparer.Ordinal).FirstOrDefault();
            if (first == null || !ReferenceEquals(first, definitionsById.Values.OfType<TransformationMethodDefinition>().FirstOrDefault(definition => definition.Id == first.Id)))
            {
                return;
            }

            string[] required =
            {
                "transformation.polymorph.temporary",
                "transformation.species-change.permanent",
                "transformation.body-form-change",
                "transformation.body-replacement",
                "transformation.body-swap",
                "transformation.possession",
                "transformation.reincarnation",
                "transformation.resurrection-body",
                "transformation.spirit-embodiment",
                "transformation.structure-replacement",
                "transformation.organ-replacement",
                "transformation.limb-replacement",
                "transformation.construct-component-replacement"
            };

            foreach (string id in required)
            {
                if (!definitionsById.TryGetValue(id, out IGameDefinition definition) || definition is not TransformationMethodDefinition)
                {
                    report.AddError($"Canonical TransformationMethodDefinition '{id}' must be registered in the alpha definition catalog.");
                }
            }
        }

        private static string[] Normalize(string[] values)
        {
            return values == null ? Array.Empty<string>() : values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
    }
}
