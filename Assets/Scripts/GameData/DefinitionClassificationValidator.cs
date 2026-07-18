using System.Collections.Generic;

namespace UnityIsekaiGame.GameData
{
    public static class DefinitionClassificationValidator
    {
        public static void ValidateCatalogDefinitions(
            IReadOnlyList<IGameDefinition> definitions,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (definitions == null || definitionsById == null || report == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                IGameDefinition definition = definitions[i];

                if (definition is CategoryDefinition category)
                {
                    ValidateCategory(category, definitionsById, report);
                }

                if (definition is TagDefinition tag)
                {
                    ValidateTag(tag, report);
                }

                ValidateDefinitionClassification(definition, definitionsById, report);
            }
        }

        private static void ValidateCategory(
            CategoryDefinition category,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (category == null)
            {
                return;
            }

            if (category.ParentCategory == null)
            {
                return;
            }

            if (ReferenceEquals(category, category.ParentCategory) || category.Id == category.ParentCategory.Id)
            {
                report.AddError($"Category '{category.Id}' cannot be its own parent.");
                return;
            }

            if (!definitionsById.TryGetValue(category.ParentCategory.Id, out IGameDefinition parentDefinition)
                || parentDefinition is not CategoryDefinition)
            {
                report.AddError($"Category '{category.Id}' references parent category '{category.ParentCategory.Id}', which is not in the configured catalog.");
            }

            if (ClassificationUtility.HasCategoryCycle(category))
            {
                report.AddError($"Category '{category.Id}' has a circular parent hierarchy.");
            }

            if (category.Domain != CategoryDomain.General
                && category.ParentCategory.Domain != CategoryDomain.General
                && category.Domain != category.ParentCategory.Domain)
            {
                report.AddWarning($"Category '{category.Id}' domain '{category.Domain}' differs from parent category '{category.ParentCategory.Id}' domain '{category.ParentCategory.Domain}'.");
            }
        }

        private static void ValidateTag(TagDefinition tag, DefinitionValidationReport report)
        {
            if (tag == null)
            {
                return;
            }

            if (!tag.Id.StartsWith("tag."))
            {
                report.AddWarning($"Tag '{tag.Id}' should use the 'tag.' namespace prefix for new content.");
            }
        }

        private static void ValidateDefinitionClassification(
            IGameDefinition definition,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (definition == null)
            {
                return;
            }

            CategoryDomain expectedDomain = definition is ICategorizableDefinition categorizable
                ? categorizable.ClassificationDomain
                : CategoryDomain.General;

            if (definition is ICategorizableDefinition categorized)
            {
                ValidateAssignedCategory(definition, categorized.PrimaryCategory, expectedDomain, definitionsById, report);
            }

            if (definition is ITaggedDefinition tagged)
            {
                ValidateAssignedTags(definition, tagged.Tags, expectedDomain, definitionsById, report);
            }

            if (definition is ILegacyStringTaggedDefinition legacyStringTagged)
            {
                ValidateLegacyTags(definition, legacyStringTagged, report);
            }
        }

        private static void ValidateAssignedCategory(
            IGameDefinition owner,
            CategoryDefinition category,
            CategoryDomain expectedDomain,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (category == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(category.Id, out IGameDefinition categoryDefinition)
                || categoryDefinition is not CategoryDefinition)
            {
                report.AddError($"{owner.GetType().Name} '{owner.DisplayName}' references category '{category.Id}', which is not in the configured catalog.");
                return;
            }

            if (expectedDomain != CategoryDomain.General
                && category.Domain != CategoryDomain.General
                && category.Domain != expectedDomain)
            {
                report.AddError($"{owner.GetType().Name} '{owner.DisplayName}' uses category '{category.Id}' with domain '{category.Domain}', expected '{expectedDomain}'.");
            }
        }

        private static void ValidateAssignedTags(
            IGameDefinition owner,
            IReadOnlyList<TagDefinition> tags,
            CategoryDomain expectedDomain,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (tags == null)
            {
                return;
            }

            HashSet<string> seenTagIds = new HashSet<string>();

            for (int i = 0; i < tags.Count; i++)
            {
                TagDefinition tag = tags[i];

                if (tag == null)
                {
                    report.AddWarning($"{owner.GetType().Name} '{owner.DisplayName}' has a null tag reference at index {i}.");
                    continue;
                }

                if (!seenTagIds.Add(tag.Id))
                {
                    report.AddWarning($"{owner.GetType().Name} '{owner.DisplayName}' has duplicate tag '{tag.Id}'.");
                }

                if (!definitionsById.TryGetValue(tag.Id, out IGameDefinition tagDefinition)
                    || tagDefinition is not TagDefinition)
                {
                    report.AddError($"{owner.GetType().Name} '{owner.DisplayName}' references tag '{tag.Id}', which is not in the configured catalog.");
                    continue;
                }

                if (expectedDomain != CategoryDomain.General
                    && tag.Domain != CategoryDomain.General
                    && tag.Domain != expectedDomain)
                {
                    report.AddWarning($"{owner.GetType().Name} '{owner.DisplayName}' uses tag '{tag.Id}' with domain '{tag.Domain}', expected '{expectedDomain}' or General.");
                }
            }
        }

        private static void ValidateLegacyTags(
            IGameDefinition owner,
            ILegacyStringTaggedDefinition legacyStringTagged,
            DefinitionValidationReport report)
        {
            IReadOnlyList<string> legacyTags = legacyStringTagged.LegacyTags;
            for (int i = 0; i < legacyTags.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(legacyTags[i]))
                {
                    report.AddWarning($"{owner.GetType().Name} '{owner.DisplayName}' still has legacy raw {legacyStringTagged.LegacyTagLabel} tag '{legacyTags[i]}'. Prefer typed TagDefinition references for new content.");
                }
            }
        }
    }
}
