using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    public static class DefinitionCatalogValidator
    {
        public static DefinitionValidationReport Validate(DefinitionCatalog catalog)
        {
            DefinitionValidationReport report = new DefinitionValidationReport();

            if (catalog == null)
            {
                report.AddError("Definition catalog is missing.");
                return report;
            }

            DefinitionIdValidationResult catalogIdResult = DefinitionIdValidator.Validate(catalog.CatalogId, $"{catalog.name} catalog ID");
            foreach (DefinitionIdValidationMessage message in catalogIdResult.Messages)
            {
                report.Add(message.Severity, message.Message);
            }

            HashSet<ScriptableObject> seenAssets = new HashSet<ScriptableObject>();
            Dictionary<string, IGameDefinition> definitionsById = new Dictionary<string, IGameDefinition>();
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            IReadOnlyList<ScriptableObject> assets = catalog.DefinitionAssets;

            for (int i = 0; i < assets.Count; i++)
            {
                ScriptableObject asset = assets[i];
                string slotName = $"{catalog.name} definition slot {i}";

                if (asset == null)
                {
                    report.AddError($"{slotName} is missing a reference.");
                    continue;
                }

                if (!seenAssets.Add(asset))
                {
                    report.AddError($"{slotName} duplicates asset reference '{asset.name}'.");
                }

                if (asset is not IGameDefinition definition)
                {
                    report.AddError($"{slotName} references '{asset.name}', which does not implement IGameDefinition.");
                    continue;
                }

                definitions.Add(definition);

                DefinitionIdValidationResult idResult = DefinitionIdValidator.Validate(definition.Id, $"{definition.GetType().Name} '{asset.name}' ID");
                foreach (DefinitionIdValidationMessage message in idResult.Messages)
                {
                    report.Add(message.Severity, message.Message);
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    report.AddWarning($"{definition.GetType().Name} '{asset.name}' has an empty display name.");
                }

                if (idResult.IsValid)
                {
                    if (definitionsById.TryGetValue(definition.Id, out IGameDefinition existingDefinition))
                    {
                        report.AddError($"Duplicate definition ID '{definition.Id}' found on {existingDefinition.GetType().Name} and {definition.GetType().Name}.");
                    }
                    else
                    {
                        definitionsById.Add(definition.Id, definition);
                    }
                }
            }

            DefinitionClassificationValidator.ValidateCatalogDefinitions(definitions, definitionsById, report);
            ValidateParticipantDefinitions(definitions, definitionsById, report);

            return report;
        }

        private static void ValidateParticipantDefinitions(
            IReadOnlyList<IGameDefinition> definitions,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is IDefinitionCatalogValidationParticipant participant)
                {
                    participant.ValidateCatalogDefinition(definitionsById, report);
                }
            }
        }
    }
}
