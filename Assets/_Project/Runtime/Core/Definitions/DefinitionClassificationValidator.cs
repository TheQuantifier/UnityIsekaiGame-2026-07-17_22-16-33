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

            ValidateRankedDefinitions(definitions, report);
            ValidateConditionRanges(definitions, report);
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

            if (definition is IInventoryItemDefinition item)
            {
                ValidateInventoryItem(item, report);
            }

            if (definition is IHasRarity rarityOwner)
            {
                ValidateRarityReference(definition, rarityOwner, definitionsById, report);
            }

            if (definition is ILegacyStringTaggedDefinition legacyStringTagged)
            {
                ValidateLegacyTags(definition, legacyStringTagged, report);
            }
        }

        private static void ValidateRarityReference(
            IGameDefinition owner,
            IHasRarity rarityOwner,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (owner == null || rarityOwner == null)
            {
                return;
            }

            RarityDefinition rarity = rarityOwner.Rarity;
            if (rarity == null)
            {
                report.AddWarning($"{owner.GetType().Name} '{owner.DisplayName}' has no rarity assigned. Rarity is optional for compatibility but should be set for new content.");
                return;
            }

            if (!definitionsById.TryGetValue(rarity.Id, out IGameDefinition rarityDefinition)
                || rarityDefinition is not RarityDefinition)
            {
                report.AddError($"{owner.GetType().Name} '{owner.DisplayName}' references rarity '{rarity.Id}', which is not in the configured catalog.");
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

        private static void ValidateInventoryItem(IInventoryItemDefinition item, DefinitionValidationReport report)
        {
            if (item.PrimaryCategory == null)
            {
                report.AddError($"Item definition '{item.DisplayName}' is missing a primary item category.");
            }

            if (item.Stackable && item.MaximumStackSize < 1)
            {
                report.AddError($"Item definition '{item.DisplayName}' is stackable but has a maximum stack size below 1.");
            }

            if (!item.Stackable && item.MaximumStackSize != 1)
            {
                report.AddWarning($"Item definition '{item.DisplayName}' is non-stackable but reports a maximum stack size of {item.MaximumStackSize}; non-stackable items should behave as size 1 stacks.");
            }

            if (item is IItemInstancePolicy instancePolicy)
            {
                ValidateItemInstancePolicy(item, instancePolicy, report);
            }

            bool inEquipmentCategory = ItemTaxonomyUtility.IsEquipment(item);
            bool inWeaponCategory = ItemTaxonomyUtility.IsWeapon(item);
            bool inArmorCategory = ItemTaxonomyUtility.IsArmor(item);
            bool inConsumableCategory = ItemTaxonomyUtility.IsConsumable(item);

            if ((inEquipmentCategory || inWeaponCategory || inArmorCategory)
                && item is IEquippableItemDefinition equippableInCategory
                && !equippableInCategory.IsEquippable)
            {
                report.AddWarning($"Item definition '{item.DisplayName}' is categorized as equipment but has no equip capability.");
            }

            if (item is IEquippableItemDefinition equippable
                && equippable.IsEquippable
                && !inEquipmentCategory
                && !inWeaponCategory
                && !inArmorCategory)
            {
                report.AddWarning($"Item definition '{item.DisplayName}' is equippable but is not in the item.equipment, item.weapon, or item.armor category hierarchy.");
            }

            if (item is IUsableItemDefinition usable)
            {
                if (usable.IsUsable && usable.UseEffectCount <= 0)
                {
                    report.AddError($"Item definition '{item.DisplayName}' is usable but has no use effects.");
                }

                if (usable.HasMissingUseEffect)
                {
                    report.AddError($"Item definition '{item.DisplayName}' has a missing use effect reference.");
                }

                if (usable.IsUsable && !inConsumableCategory)
                {
                    report.AddWarning($"Item definition '{item.DisplayName}' has use effects but is not categorized under item.consumable.");
                }

                if (inConsumableCategory && !usable.IsUsable)
                {
                    report.AddWarning($"Item definition '{item.DisplayName}' is categorized as consumable but has no use effects.");
                }
            }
        }

        private static void ValidateItemInstancePolicy(
            IInventoryItemDefinition item,
            IItemInstancePolicy instancePolicy,
            DefinitionValidationReport report)
        {
            if (item == null || instancePolicy == null)
            {
                return;
            }

            if (!System.Enum.IsDefined(typeof(ItemInstanceMode), instancePolicy.InstanceMode))
            {
                report.AddError($"Item definition '{item.DisplayName}' has an invalid item instance mode '{instancePolicy.InstanceMode}'.");
                return;
            }

            if (instancePolicy.InstanceMode == ItemInstanceMode.AlwaysInstanced && item.Stackable)
            {
                report.AddWarning($"Item definition '{item.DisplayName}' is always-instanced but is configured as stackable. Stateful items should normally use size 1 stacks until inventory instances are implemented.");
            }

            if (instancePolicy.InstanceMode != ItemInstanceMode.DefinitionOnly && item.MaximumStackSize > 1)
            {
                report.AddWarning($"Item definition '{item.DisplayName}' allows item instances with maximum stack size {item.MaximumStackSize}. Future stateful instances should not share indistinguishable stacks.");
            }
        }

        private static void ValidateRankedDefinitions(IReadOnlyList<IGameDefinition> definitions, DefinitionValidationReport report)
        {
            ValidateRarityDefinitions(definitions, report);
            ValidateQualityDefinitions(definitions, report);
            ValidateConditionDefinitions(definitions, report);
        }

        private static void ValidateRarityDefinitions(IReadOnlyList<IGameDefinition> definitions, DefinitionValidationReport report)
        {
            Dictionary<int, RarityDefinition> ranks = new Dictionary<int, RarityDefinition>();
            bool defaultSeen = false;

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is not RarityDefinition rarity)
                {
                    continue;
                }

                if (!rarity.Id.StartsWith("rarity."))
                {
                    report.AddWarning($"Rarity '{rarity.Id}' should use the 'rarity.' namespace prefix.");
                }

                if (rarity.Rank < 0)
                {
                    report.AddError($"Rarity '{rarity.Id}' has a negative rank.");
                }

                if (ranks.TryGetValue(rarity.Rank, out RarityDefinition existing))
                {
                    report.AddError($"Rarity rank {rarity.Rank} is used by both '{existing.Id}' and '{rarity.Id}'.");
                }
                else
                {
                    ranks.Add(rarity.Rank, rarity);
                }

                if (rarity.IsDefault)
                {
                    if (defaultSeen)
                    {
                        report.AddError($"Multiple rarity definitions are marked as default; '{rarity.Id}' is an extra default.");
                    }

                    defaultSeen = true;
                }
            }
        }

        private static void ValidateQualityDefinitions(IReadOnlyList<IGameDefinition> definitions, DefinitionValidationReport report)
        {
            Dictionary<int, QualityDefinition> ranks = new Dictionary<int, QualityDefinition>();
            bool defaultSeen = false;

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is not QualityDefinition quality)
                {
                    continue;
                }

                if (!quality.Id.StartsWith("quality."))
                {
                    report.AddWarning($"Quality '{quality.Id}' should use the 'quality.' namespace prefix.");
                }

                if (quality.Rank < 0)
                {
                    report.AddError($"Quality '{quality.Id}' has a negative rank.");
                }

                if (ranks.TryGetValue(quality.Rank, out QualityDefinition existing))
                {
                    report.AddError($"Quality rank {quality.Rank} is used by both '{existing.Id}' and '{quality.Id}'.");
                }
                else
                {
                    ranks.Add(quality.Rank, quality);
                }

                if (quality.IsDefault)
                {
                    if (defaultSeen)
                    {
                        report.AddError($"Multiple quality definitions are marked as default; '{quality.Id}' is an extra default.");
                    }

                    defaultSeen = true;
                }
            }
        }

        private static void ValidateConditionDefinitions(IReadOnlyList<IGameDefinition> definitions, DefinitionValidationReport report)
        {
            Dictionary<int, ConditionDefinition> ranks = new Dictionary<int, ConditionDefinition>();
            bool defaultSeen = false;

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is not ConditionDefinition condition)
                {
                    continue;
                }

                if (!condition.Id.StartsWith("condition."))
                {
                    report.AddWarning($"Condition '{condition.Id}' should use the 'condition.' namespace prefix.");
                }

                if (condition.Rank < 0)
                {
                    report.AddError($"Condition '{condition.Id}' has a negative rank.");
                }

                if (ranks.TryGetValue(condition.Rank, out ConditionDefinition existing))
                {
                    report.AddError($"Condition rank {condition.Rank} is used by both '{existing.Id}' and '{condition.Id}'.");
                }
                else
                {
                    ranks.Add(condition.Rank, condition);
                }

                if (condition.IsDefault)
                {
                    if (defaultSeen)
                    {
                        report.AddError($"Multiple condition definitions are marked as default; '{condition.Id}' is an extra default.");
                    }

                    defaultSeen = true;
                }
            }
        }

        private static void ValidateConditionRanges(IReadOnlyList<IGameDefinition> definitions, DefinitionValidationReport report)
        {
            List<ConditionDefinition> conditions = new List<ConditionDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is ConditionDefinition condition)
                {
                    conditions.Add(condition);
                    ValidateConditionRange(condition, report);
                }
            }

            if (conditions.Count == 0)
            {
                return;
            }

            conditions.Sort(CompareConditionRanges);
            const float epsilon = 0.0001f;
            float expectedMinimum = 0f;

            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionDefinition condition = conditions[i];

                if (condition.MinimumNormalized > expectedMinimum + epsilon)
                {
                    report.AddError($"Condition ranges have a gap from {expectedMinimum:0.###} to {condition.MinimumNormalized:0.###} before '{condition.Id}'.");
                }

                if (condition.MinimumNormalized < expectedMinimum - epsilon)
                {
                    report.AddError($"Condition range '{condition.Id}' overlaps a previous condition range near {condition.MinimumNormalized:0.###}.");
                }

                expectedMinimum = condition.MaximumNormalized;
            }

            if (expectedMinimum < 1f - epsilon)
            {
                report.AddError($"Condition ranges have a gap from {expectedMinimum:0.###} to 1.");
            }
        }

        private static void ValidateConditionRange(ConditionDefinition condition, DefinitionValidationReport report)
        {
            if (condition.MinimumNormalized < 0f || condition.MinimumNormalized > 1f)
            {
                report.AddError($"Condition '{condition.Id}' minimum normalized value must be between 0 and 1.");
            }

            if (condition.MaximumNormalized < 0f || condition.MaximumNormalized > 1f)
            {
                report.AddError($"Condition '{condition.Id}' maximum normalized value must be between 0 and 1.");
            }

            if (condition.MinimumNormalized > condition.MaximumNormalized)
            {
                report.AddError($"Condition '{condition.Id}' minimum normalized value is greater than its maximum.");
            }
        }

        private static int CompareConditionRanges(ConditionDefinition left, ConditionDefinition right)
        {
            int minimumComparison = left.MinimumNormalized.CompareTo(right.MinimumNormalized);
            return minimumComparison != 0 ? minimumComparison : left.Rank.CompareTo(right.Rank);
        }
    }
}
