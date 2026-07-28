using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory.Composition;
using UnityIsekaiGame.Inventory.Production;

namespace UnityIsekaiGame.Inventory.Recipes
{
    [CreateAssetMenu(fileName = "RecipeDefinition", menuName = "Unity Isekai Game/Inventory/Recipe Definition")]
    public sealed class RecipeDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private RecipeCategory category = RecipeCategory.Unknown;
        [SerializeField] private RecipeLifecycleState state = RecipeLifecycleState.Active;
        [SerializeField] private string currentVersionId;
        [SerializeField] private string[] tags = Array.Empty<string>();
        [SerializeField] private RecipeVersionData[] versions = Array.Empty<RecipeVersionData>();
        [SerializeField] private RecipeVariantData[] variants = Array.Empty<RecipeVariantData>();
        [SerializeField] private RecipeInputSpecificationData[] inputs = Array.Empty<RecipeInputSpecificationData>();
        [SerializeField] private RecipeOutputSpecificationData[] outputs = Array.Empty<RecipeOutputSpecificationData>();
        [SerializeField] private RecipeTransferMappingData[] transferMappings = Array.Empty<RecipeTransferMappingData>();
        [SerializeField] private RecipeProcedureStepData[] procedureSteps = Array.Empty<RecipeProcedureStepData>();
        [SerializeField] private string[] recipeRequirementIds = Array.Empty<string>();
        [SerializeField] private RecipeBatchPolicyData batchPolicy = new RecipeBatchPolicyData();
        [SerializeField] private string compositionTransferPolicyId;
        [SerializeField] private string qualityGenerationPolicyId;
        [SerializeField] private string affixGenerationPolicyId;
        [SerializeField] private string durabilityInitializationPolicyId;
        [SerializeField] private int knowledgeDifficulty = 1;
        [SerializeField] private int teachingDifficulty = 1;
        [SerializeField] private string secrecyAccessPolicyId;
        [SerializeField] private string sourceId;
        [SerializeField] private int schemaVersion = 1;

        public string Id => recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public RecipeCategory Category => category;
        public RecipeLifecycleState State => state;
        public string CurrentVersionId => currentVersionId ?? string.Empty;
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();
        public IReadOnlyList<RecipeVersionData> Versions => (versions ?? Array.Empty<RecipeVersionData>()).Select(version => version.Clone()).ToArray();
        public IReadOnlyList<RecipeVariantData> Variants => (variants ?? Array.Empty<RecipeVariantData>()).Select(variant => variant.Clone()).ToArray();
        public IReadOnlyList<RecipeInputSpecificationData> Inputs => (inputs ?? Array.Empty<RecipeInputSpecificationData>()).Select(input => input.CloneScaled(1f)).ToArray();
        public IReadOnlyList<RecipeOutputSpecificationData> Outputs => (outputs ?? Array.Empty<RecipeOutputSpecificationData>()).Select(output => output.CloneScaled(1f)).ToArray();
        public IReadOnlyList<RecipeTransferMappingData> TransferMappings => (transferMappings ?? Array.Empty<RecipeTransferMappingData>()).Select(mapping => mapping.Clone()).ToArray();
        public IReadOnlyList<RecipeProcedureStepData> ProcedureSteps => (procedureSteps ?? Array.Empty<RecipeProcedureStepData>()).Select(step => step.Clone()).ToArray();
        public IReadOnlyList<string> RecipeRequirementIds => recipeRequirementIds ?? Array.Empty<string>();
        public RecipeBatchPolicyData BatchPolicy => batchPolicy == null ? new RecipeBatchPolicyData() : batchPolicy.Clone();
        public string CompositionTransferPolicyId => compositionTransferPolicyId ?? string.Empty;
        public string QualityGenerationPolicyId => qualityGenerationPolicyId ?? string.Empty;
        public string AffixGenerationPolicyId => affixGenerationPolicyId ?? string.Empty;
        public string DurabilityInitializationPolicyId => durabilityInitializationPolicyId ?? string.Empty;
        public int KnowledgeDifficulty => Math.Max(0, knowledgeDifficulty);
        public int TeachingDifficulty => Math.Max(0, teachingDifficulty);
        public string SecrecyAccessPolicyId => secrecyAccessPolicyId ?? string.Empty;
        public string SourceId => sourceId ?? string.Empty;
        public int SchemaVersion => Math.Max(1, schemaVersion);

        private void OnValidate()
        {
            recipeId = recipeId?.Trim();
            currentVersionId = currentVersionId?.Trim();
            knowledgeDifficulty = Math.Max(0, knowledgeDifficulty);
            teachingDifficulty = Math.Max(0, teachingDifficulty);
            schemaVersion = Math.Max(1, schemaVersion);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"Recipe definition '{name}' is missing an ID.");
            }
            else if (!Id.StartsWith("recipe.", StringComparison.Ordinal))
            {
                report.AddWarning($"Recipe definition '{Id}' should use the 'recipe.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(RecipeCategory), category) || category == RecipeCategory.Unknown)
            {
                report.AddError($"Recipe '{DisplayName}' must declare a concrete category.");
            }

            ValidateVersions(report);
            ValidateInputs(report, definitionsById);
            ValidateOutputs(report, definitionsById);
            ValidateTransferMappings(report);
            ValidateProcedure(report);
            ValidateRequirements(report, definitionsById);
            ValidateBatch(report);
        }

        private void ValidateVersions(DefinitionValidationReport report)
        {
            RecipeVersionData[] values = versions ?? Array.Empty<RecipeVersionData>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeVersionData version in values)
            {
                if (version == null || string.IsNullOrWhiteSpace(version.versionId))
                {
                    report.AddError($"Recipe '{DisplayName}' has a version without an ID.");
                    continue;
                }

                if (!ids.Add(version.versionId))
                {
                    report.AddError($"Recipe '{DisplayName}' has duplicate version '{version.versionId}'.");
                }
            }

            if (values.Length == 0)
            {
                report.AddError($"Recipe '{DisplayName}' must declare at least one version.");
            }
            else if (string.IsNullOrWhiteSpace(CurrentVersionId) || !ids.Contains(CurrentVersionId))
            {
                report.AddError($"Recipe '{DisplayName}' references missing current version '{CurrentVersionId}'.");
            }

            foreach (RecipeVersionData version in values.Where(value => value != null))
            {
                if (!string.IsNullOrWhiteSpace(version.priorVersionId) && !ids.Contains(version.priorVersionId))
                {
                    report.AddError($"Recipe version '{version.versionId}' references missing prior version '{version.priorVersionId}'.");
                }

                if (HasLineageCycle(version.versionId, values))
                {
                    report.AddError($"Recipe version lineage for '{version.versionId}' contains a cycle.");
                }
            }
        }

        private static bool HasLineageCycle(string start, IEnumerable<RecipeVersionData> versions)
        {
            Dictionary<string, string> priorById = (versions ?? Array.Empty<RecipeVersionData>())
                .Where(version => version != null && !string.IsNullOrWhiteSpace(version.versionId))
                .ToDictionary(version => version.versionId, version => version.priorVersionId ?? string.Empty, StringComparer.Ordinal);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            string cursor = start;
            while (!string.IsNullOrWhiteSpace(cursor) && priorById.TryGetValue(cursor, out string prior))
            {
                if (!seen.Add(cursor))
                {
                    return true;
                }

                cursor = prior;
            }

            return false;
        }

        private void ValidateInputs(DefinitionValidationReport report, IReadOnlyDictionary<string, IGameDefinition> definitionsById)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeInputSpecificationData input in inputs ?? Array.Empty<RecipeInputSpecificationData>())
            {
                if (input == null || string.IsNullOrWhiteSpace(input.inputId))
                {
                    report.AddError($"Recipe '{DisplayName}' has an input without an ID.");
                    continue;
                }

                if (!ids.Add(input.inputId))
                {
                    report.AddError($"Recipe '{DisplayName}' has duplicate input '{input.inputId}'.");
                }

                if (input.role == RecipeInputRole.Unknown)
                {
                    report.AddError($"Recipe input '{input.inputId}' must declare a concrete role.");
                }

                if (input.quantity <= 0f && input.requirementState == RecipeRequirementState.Required)
                {
                    report.AddError($"Recipe input '{input.inputId}' must have a positive quantity.");
                }

                if (!string.IsNullOrWhiteSpace(input.materialDefinitionId) && (definitionsById == null || !definitionsById.TryGetValue(input.materialDefinitionId, out IGameDefinition material) || material is not MaterialDefinition))
                {
                    report.AddError($"Recipe input '{input.inputId}' references missing Material definition '{input.materialDefinitionId}'.");
                }

                if (!string.IsNullOrWhiteSpace(input.itemDefinitionId) && (definitionsById == null || !definitionsById.TryGetValue(input.itemDefinitionId, out IGameDefinition item) || item is not UnityIsekaiGame.Inventory.ItemDefinition))
                {
                    report.AddError($"Recipe input '{input.inputId}' references missing Item definition '{input.itemDefinitionId}'.");
                }
            }
        }

        private void ValidateOutputs(DefinitionValidationReport report, IReadOnlyDictionary<string, IGameDefinition> definitionsById)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeOutputSpecificationData output in outputs ?? Array.Empty<RecipeOutputSpecificationData>())
            {
                if (output == null || string.IsNullOrWhiteSpace(output.outputId))
                {
                    report.AddError($"Recipe '{DisplayName}' has an output without an ID.");
                    continue;
                }

                if (!ids.Add(output.outputId))
                {
                    report.AddError($"Recipe '{DisplayName}' has duplicate output '{output.outputId}'.");
                }

                if (output.role == RecipeOutputRole.Unknown)
                {
                    report.AddError($"Recipe output '{output.outputId}' must declare a concrete role.");
                }

                if (output.quantity <= 0f)
                {
                    report.AddError($"Recipe output '{output.outputId}' must have a positive quantity.");
                }

                if (!string.IsNullOrWhiteSpace(output.itemDefinitionId) && (definitionsById == null || !definitionsById.TryGetValue(output.itemDefinitionId, out IGameDefinition item) || item is not UnityIsekaiGame.Inventory.ItemDefinition))
                {
                    report.AddError($"Recipe output '{output.outputId}' references missing Item definition '{output.itemDefinitionId}'.");
                }
            }

            if ((outputs ?? Array.Empty<RecipeOutputSpecificationData>()).All(output => output == null || output.role != RecipeOutputRole.PrimaryOutput))
            {
                report.AddError($"Recipe '{DisplayName}' must declare at least one primary output.");
            }
        }

        private void ValidateTransferMappings(DefinitionValidationReport report)
        {
            HashSet<string> inputIds = new HashSet<string>((inputs ?? Array.Empty<RecipeInputSpecificationData>()).Where(input => input != null).Select(input => input.inputId), StringComparer.Ordinal);
            HashSet<string> outputIds = new HashSet<string>((outputs ?? Array.Empty<RecipeOutputSpecificationData>()).Where(output => output != null).Select(output => output.outputId), StringComparer.Ordinal);
            HashSet<string> trackedTargets = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeTransferMappingData mapping in transferMappings ?? Array.Empty<RecipeTransferMappingData>())
            {
                if (mapping == null || string.IsNullOrWhiteSpace(mapping.mappingId))
                {
                    report.AddError($"Recipe '{DisplayName}' has a transfer mapping without an ID.");
                    continue;
                }

                if (!inputIds.Contains(mapping.sourceInputId ?? string.Empty))
                {
                    report.AddError($"Recipe transfer '{mapping.mappingId}' references missing input '{mapping.sourceInputId}'.");
                }

                if (!outputIds.Contains(mapping.targetOutputId ?? string.Empty))
                {
                    report.AddError($"Recipe transfer '{mapping.mappingId}' references missing output '{mapping.targetOutputId}'.");
                }

                if (mapping.preserveTrackedComponent && !trackedTargets.Add(mapping.sourceInputId ?? string.Empty))
                {
                    report.AddError($"Recipe transfer '{mapping.mappingId}' duplicates tracked component source '{mapping.sourceInputId}'.");
                }
            }
        }

        private void ValidateProcedure(DefinitionValidationReport report)
        {
            RecipeProcedureStepData[] steps = procedureSteps ?? Array.Empty<RecipeProcedureStepData>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeProcedureStepData step in steps)
            {
                if (step == null || string.IsNullOrWhiteSpace(step.stepId))
                {
                    report.AddError($"Recipe '{DisplayName}' has a procedure step without an ID.");
                    continue;
                }

                if (!ids.Add(step.stepId))
                {
                    report.AddError($"Recipe '{DisplayName}' has duplicate procedure step '{step.stepId}'.");
                }

                if (step.stepKind == RecipeProcedureStepKind.Unknown)
                {
                    report.AddError($"Recipe step '{step.stepId}' must declare a concrete step kind.");
                }
            }

            foreach (RecipeProcedureStepData step in steps.Where(step => step != null))
            {
                foreach (string dependency in step.dependsOnStepIds ?? Array.Empty<string>())
                {
                    if (!ids.Contains(dependency))
                    {
                        report.AddError($"Recipe step '{step.stepId}' references missing dependency '{dependency}'.");
                    }
                }

                if (HasStepCycle(step.stepId, steps))
                {
                    report.AddError($"Recipe procedure step '{step.stepId}' participates in a dependency cycle.");
                }
            }
        }

        private static bool HasStepCycle(string start, IEnumerable<RecipeProcedureStepData> steps)
        {
            Dictionary<string, string[]> dependencies = (steps ?? Array.Empty<RecipeProcedureStepData>())
                .Where(step => step != null && !string.IsNullOrWhiteSpace(step.stepId))
                .ToDictionary(step => step.stepId, step => step.dependsOnStepIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
            return Visit(start);

            bool Visit(string current)
            {
                if (!visiting.Add(current))
                {
                    return true;
                }

                foreach (string dependency in dependencies.TryGetValue(current, out string[] values) ? values : Array.Empty<string>())
                {
                    if (dependencies.ContainsKey(dependency) && Visit(dependency))
                    {
                        return true;
                    }
                }

                visiting.Remove(current);
                return false;
            }
        }

        private void ValidateRequirements(DefinitionValidationReport report, IReadOnlyDictionary<string, IGameDefinition> definitionsById)
        {
            foreach (string requirementId in recipeRequirementIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(requirementId))
                {
                    continue;
                }

                if (definitionsById == null || !definitionsById.TryGetValue(requirementId, out IGameDefinition requirement) || requirement is not ProductionRequirementDefinition)
                {
                    report.AddError($"Recipe '{DisplayName}' references missing Production Requirement '{requirementId}'.");
                }
            }
        }

        private void ValidateBatch(DefinitionValidationReport report)
        {
            RecipeBatchPolicyData policy = BatchPolicy;
            if (policy.baseBatchSize <= 0f || policy.minimumBatchSize <= 0f || policy.maximumBatchSize < policy.minimumBatchSize || policy.batchIncrement <= 0f)
            {
                report.AddError($"Recipe '{DisplayName}' has invalid batch policy bounds.");
            }
        }
    }
}
