using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Beings.Biology.BiologicalConditions
{
    [CreateAssetMenu(fileName = "BiologicalTransmissionProfile", menuName = "Unity Isekai Game/Beings/Biology/Biological Transmission Profile")]
    public sealed class BiologicalTransmissionProfileDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string transmissionProfileId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private string conditionDefinitionId;
        [SerializeField] private BiologicalConditionTransmissionMode transmissionMode = BiologicalConditionTransmissionMode.Contact;
        [SerializeField] private BiologicalExposureRoute exposureRoute = BiologicalExposureRoute.Contact;
        [SerializeField] private float transferredDose = 1f;
        [SerializeField] private bool alphaEnabled = true;
        [SerializeField, TextArea(1, 3)] private string validationMetadata;

        public string Id => transmissionProfileId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public string ConditionDefinitionId => conditionDefinitionId ?? string.Empty;
        public BiologicalConditionTransmissionMode TransmissionMode => transmissionMode;
        public BiologicalExposureRoute ExposureRoute => exposureRoute;
        public float TransferredDose => Mathf.Max(0f, transferredDose);
        public bool AlphaEnabled => alphaEnabled;
        public string ValidationMetadata => validationMetadata ?? string.Empty;

        private void OnValidate()
        {
            transmissionProfileId = transmissionProfileId?.Trim();
            conditionDefinitionId = conditionDefinitionId?.Trim();
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"BiologicalTransmissionProfileDefinition '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("transmission.biology.", StringComparison.Ordinal))
            {
                report.AddWarning($"BiologicalTransmissionProfileDefinition '{Id}' should use the 'transmission.biology.' namespace prefix.");
            }

            if (string.IsNullOrWhiteSpace(ConditionDefinitionId)
                || definitionsById == null
                || !definitionsById.TryGetValue(ConditionDefinitionId, out IGameDefinition condition)
                || condition is not BiologicalConditionDefinition)
            {
                report.AddError($"BiologicalTransmissionProfileDefinition '{DisplayName}' references missing Biological Condition '{ConditionDefinitionId}'.");
            }

            if (TransmissionMode == BiologicalConditionTransmissionMode.None)
            {
                report.AddError($"BiologicalTransmissionProfileDefinition '{DisplayName}' has no transmission mode.");
            }
        }
    }
}
