using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    internal static class ClassificationTestFactory
    {
        public static CategoryDefinition CreateCategory(
            string id,
            string displayName,
            CategoryDomain domain = CategoryDomain.General,
            CategoryDefinition parentCategory = null)
        {
            CategoryDefinition category = ScriptableObject.CreateInstance<CategoryDefinition>();
            SerializedObject serializedCategory = new SerializedObject(category);
            serializedCategory.FindProperty("categoryId").stringValue = id;
            serializedCategory.FindProperty("displayName").stringValue = displayName;
            serializedCategory.FindProperty("domain").enumValueIndex = (int)domain;
            serializedCategory.FindProperty("parentCategory").objectReferenceValue = parentCategory;
            serializedCategory.ApplyModifiedPropertiesWithoutUndo();
            return category;
        }

        public static TagDefinition CreateTag(
            string id,
            string displayName,
            CategoryDomain domain = CategoryDomain.General)
        {
            TagDefinition tag = ScriptableObject.CreateInstance<TagDefinition>();
            SerializedObject serializedTag = new SerializedObject(tag);
            serializedTag.FindProperty("tagId").stringValue = id;
            serializedTag.FindProperty("displayName").stringValue = displayName;
            serializedTag.FindProperty("domain").enumValueIndex = (int)domain;
            serializedTag.ApplyModifiedPropertiesWithoutUndo();
            return tag;
        }

        public static RarityDefinition CreateRarity(
            string id,
            string displayName,
            int rank,
            bool isDefault = false)
        {
            RarityDefinition rarity = ScriptableObject.CreateInstance<RarityDefinition>();
            SerializedObject serializedRarity = new SerializedObject(rarity);
            serializedRarity.FindProperty("rarityId").stringValue = id;
            serializedRarity.FindProperty("displayName").stringValue = displayName;
            serializedRarity.FindProperty("rank").intValue = rank;
            serializedRarity.FindProperty("defaultRarity").boolValue = isDefault;
            serializedRarity.ApplyModifiedPropertiesWithoutUndo();
            return rarity;
        }

        public static QualityDefinition CreateQuality(
            string id,
            string displayName,
            int rank,
            bool isDefault = false)
        {
            QualityDefinition quality = ScriptableObject.CreateInstance<QualityDefinition>();
            SerializedObject serializedQuality = new SerializedObject(quality);
            serializedQuality.FindProperty("qualityId").stringValue = id;
            serializedQuality.FindProperty("displayName").stringValue = displayName;
            serializedQuality.FindProperty("rank").intValue = rank;
            serializedQuality.FindProperty("defaultQuality").boolValue = isDefault;
            serializedQuality.ApplyModifiedPropertiesWithoutUndo();
            return quality;
        }

        public static ConditionDefinition CreateCondition(
            string id,
            string displayName,
            int rank,
            float minimumNormalized,
            float maximumNormalized,
            bool unusable = false,
            bool isDefault = false)
        {
            ConditionDefinition condition = ScriptableObject.CreateInstance<ConditionDefinition>();
            SerializedObject serializedCondition = new SerializedObject(condition);
            serializedCondition.FindProperty("conditionId").stringValue = id;
            serializedCondition.FindProperty("displayName").stringValue = displayName;
            serializedCondition.FindProperty("rank").intValue = rank;
            serializedCondition.FindProperty("minimumNormalized").floatValue = minimumNormalized;
            serializedCondition.FindProperty("maximumNormalized").floatValue = maximumNormalized;
            serializedCondition.FindProperty("unusable").boolValue = unusable;
            serializedCondition.FindProperty("defaultCondition").boolValue = isDefault;
            serializedCondition.ApplyModifiedPropertiesWithoutUndo();
            return condition;
        }

        public static DefinitionCatalog CreateCatalog(params ScriptableObject[] definitions)
        {
            DefinitionCatalog catalog = ScriptableObject.CreateInstance<DefinitionCatalog>();
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.FindProperty("catalogId").stringValue = "catalog.test";
            SerializedProperty definitionsProperty = serializedCatalog.FindProperty("definitions");
            definitionsProperty.arraySize = definitions.Length;

            for (int i = 0; i < definitions.Length; i++)
            {
                definitionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }
    }
}
