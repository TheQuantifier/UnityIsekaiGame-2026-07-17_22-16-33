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
