using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Beings.Biology.VitalProcesses;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Beings.Biology.Hazards
{
    [CreateAssetMenu(fileName = "BiologicalHazard", menuName = "Unity Isekai Game/Beings/Biology/Biological Hazard")]
    public sealed class BiologicalHazardDefinition : ScriptableObject, IGameDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string hazardId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private BiologicalHazardStackingPolicy stackingPolicy = BiologicalHazardStackingPolicy.MergeSources;
        [SerializeField] private BiologicalHazardSeverity defaultSeverity = BiologicalHazardSeverity.Minor;
        [SerializeField] private string targetResourceId;
        [SerializeField] private VitalResourceMutationOperation resourceOperation = VitalResourceMutationOperation.Consume;
        [SerializeField, Min(0f)] private float baseResourceRatePerHour;
        [SerializeField, Min(0f)] private float baseDamagePerHour;
        [SerializeField] private DamageTypeDefinition damageType;
        [SerializeField] private BiologicalHazardLifecycleRequestKind lifecycleRequest = BiologicalHazardLifecycleRequestKind.EvaluatePressure;
        [SerializeField] private string[] requiredRuntimeCapabilityKeys;
        [SerializeField] private string[] blockingRuntimeCapabilityKeys;
        [SerializeField] private bool alphaEnabled = true;
        [SerializeField] private string futureResistanceMetadata;
        [SerializeField] private TagDefinition[] tags;

        public string Id => hazardId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public BiologicalHazardStackingPolicy StackingPolicy => stackingPolicy;
        public BiologicalHazardSeverity DefaultSeverity => defaultSeverity;
        public string TargetResourceId => targetResourceId ?? string.Empty;
        public VitalResourceMutationOperation ResourceOperation => resourceOperation;
        public float BaseResourceRatePerHour => Mathf.Max(0f, baseResourceRatePerHour);
        public float BaseDamagePerHour => Mathf.Max(0f, baseDamagePerHour);
        public DamageTypeDefinition DamageType => damageType;
        public BiologicalHazardLifecycleRequestKind LifecycleRequest => lifecycleRequest;
        public IReadOnlyList<string> RequiredRuntimeCapabilityKeys => requiredRuntimeCapabilityKeys ?? Array.Empty<string>();
        public IReadOnlyList<string> BlockingRuntimeCapabilityKeys => blockingRuntimeCapabilityKeys ?? Array.Empty<string>();
        public bool AlphaEnabled => alphaEnabled;
        public string FutureResistanceMetadata => futureResistanceMetadata ?? string.Empty;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();

        private void OnValidate()
        {
            hazardId = hazardId?.Trim();
            displayName = displayName?.Trim();
            targetResourceId = targetResourceId?.Trim();
            baseResourceRatePerHour = Mathf.Max(0f, baseResourceRatePerHour);
            baseDamagePerHour = Mathf.Max(0f, baseDamagePerHour);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"BiologicalHazardDefinition '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("hazard.biology.", StringComparison.Ordinal) && !Id.StartsWith("hazard.environment.", StringComparison.Ordinal))
            {
                report.AddWarning($"BiologicalHazardDefinition '{Id}' should use the 'hazard.biology.' or 'hazard.environment.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(BiologicalHazardStackingPolicy), stackingPolicy))
            {
                report.AddError($"BiologicalHazardDefinition '{DisplayName}' has an invalid stacking policy.");
            }

            if (!Enum.IsDefined(typeof(BiologicalHazardSeverity), defaultSeverity))
            {
                report.AddError($"BiologicalHazardDefinition '{DisplayName}' has an invalid severity.");
            }

            if (!string.IsNullOrWhiteSpace(TargetResourceId)
                && definitionsById != null
                && (!definitionsById.TryGetValue(TargetResourceId, out IGameDefinition resource) || resource is not BiologicalResourceDefinition))
            {
                report.AddError($"BiologicalHazardDefinition '{DisplayName}' references unknown Biological Resource '{TargetResourceId}'.");
            }

            if (damageType != null && definitionsById != null && (!definitionsById.TryGetValue(damageType.Id, out IGameDefinition damageDefinition) || damageDefinition is not DamageTypeDefinition))
            {
                report.AddError($"BiologicalHazardDefinition '{DisplayName}' references unknown Damage Type '{damageType.Id}'.");
            }

            foreach (TagDefinition tag in Tags)
            {
                if (tag == null)
                {
                    report.AddError($"BiologicalHazardDefinition '{DisplayName}' has a missing tag reference.");
                }
                else if (definitionsById != null && (!definitionsById.TryGetValue(tag.Id, out IGameDefinition found) || found is not TagDefinition))
                {
                    report.AddError($"BiologicalHazardDefinition '{DisplayName}' references tag '{tag.Id}', which is not in the configured catalog.");
                }
            }

            ValidateCanonicalAlphaSet(definitionsById, report);
        }

        private static void ValidateCanonicalAlphaSet(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || !definitionsById.ContainsKey("species.human"))
            {
                return;
            }

            string[] required =
            {
                BiologicalHazardIds.Bleeding,
                BiologicalHazardIds.Suffocation,
                BiologicalHazardIds.Overheating,
                BiologicalHazardIds.Hypothermia,
                BiologicalHazardIds.Starvation,
                BiologicalHazardIds.Dehydration,
                BiologicalHazardIds.ExtremeFatigue,
                BiologicalHazardIds.SleepDeprivation,
                BiologicalHazardIds.EnvironmentalExposure
            };

            foreach (string id in required)
            {
                if (!definitionsById.TryGetValue(id, out IGameDefinition definition) || definition is not BiologicalHazardDefinition)
                {
                    report.AddError($"Canonical BiologicalHazardDefinition '{id}' must be registered in the alpha definition catalog.");
                }
            }
        }
    }
}
