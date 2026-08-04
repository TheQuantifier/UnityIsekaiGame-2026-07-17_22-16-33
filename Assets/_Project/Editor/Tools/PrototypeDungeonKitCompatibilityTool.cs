using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeDungeonKitCompatibilityTool
    {
        private static readonly string[] DungeonRoots =
        {
            "Assets/ThirdParty/Dungeon/FreeDungeon",
            "Assets/ThirdParty/Dungeon/StylizedHandPaintedDungeonFree"
        };

        [MenuItem("Tools/Prototype Scene/Dungeon Kits/Normalize Dungeon Kit Materials")]
        public static void NormalizeDungeonKitMaterials()
        {
            var shader = FindLitShader();
            var changedMaterials = 0;
            var changedImporters = 0;

            foreach (var root in DungeonRoots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { root }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (NormalizeModelImporter(path))
                    {
                        changedImporters++;
                    }
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { root }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }

                    if (NormalizeMaterial(material, shader))
                    {
                        changedMaterials++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Normalized {changedMaterials} dungeon kit material(s) and {changedImporters} model importer(s) for the active render pipeline.");
        }

        private static bool NormalizeModelImporter(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var changedMetadata = NormalizeModelImporterMetadata(assetPath + ".meta");
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return changedMetadata;
            }

            var serialized = new SerializedObject(importer);
            var materialLocation = serialized.FindProperty("m_Materials.m_MaterialLocation");
            if (materialLocation == null)
            {
                if (changedMetadata)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }

                return changedMetadata;
            }

            const int supportedMaterialLocation = 1;
            if (materialLocation.intValue == supportedMaterialLocation)
            {
                if (changedMetadata)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }

                return changedMetadata;
            }

            materialLocation.intValue = supportedMaterialLocation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            return true;
        }

        private static bool NormalizeModelImporterMetadata(string metaPath)
        {
            if (string.IsNullOrWhiteSpace(metaPath) || !File.Exists(metaPath))
            {
                return false;
            }

            var contents = File.ReadAllText(metaPath);
            var normalized = contents.Replace("    materialLocation: 0", "    materialLocation: 1");
            if (string.Equals(contents, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(metaPath, normalized);
            return true;
        }

        private static bool NormalizeMaterial(Material material, Shader shader)
        {
            var changed = false;
            var mainTexture = GetTexture(material, "_BaseMap") ?? GetTexture(material, "_MainTex");
            var baseColor = GetColor(material, "_BaseColor", "_Color", Color.white);

            if (material.shader != shader)
            {
                material.shader = shader;
                changed = true;
            }

            if (mainTexture != null)
            {
                changed |= SetTexture(material, "_BaseMap", "_MainTex", mainTexture);
            }

            changed |= SetColor(material, "_BaseColor", "_Color", baseColor);
            changed |= SetFloat(material, "_Metallic", 0f);
            changed |= SetFloat(material, "_Smoothness", 0.15f);
            changed |= SetFloat(material, "_Glossiness", 0.15f);

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return changed;
        }

        private static Shader FindLitShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");

            if (shader == null)
            {
                throw new InvalidOperationException("Could not find a supported Lit shader for dungeon kit materials.");
            }

            return shader;
        }

        private static Texture GetTexture(Material material, string propertyName)
        {
            return material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        }

        private static Color GetColor(Material material, string primaryProperty, string fallbackProperty, Color fallback)
        {
            if (material.HasProperty(primaryProperty))
            {
                return material.GetColor(primaryProperty);
            }

            if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            {
                return material.GetColor(fallbackProperty);
            }

            return fallback;
        }

        private static bool SetTexture(Material material, string primaryProperty, string fallbackProperty, Texture texture)
        {
            if (material.HasProperty(primaryProperty))
            {
                if (material.GetTexture(primaryProperty) == texture)
                {
                    return false;
                }

                material.SetTexture(primaryProperty, texture);
                return true;
            }

            if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            {
                if (material.GetTexture(fallbackProperty) == texture)
                {
                    return false;
                }

                material.SetTexture(fallbackProperty, texture);
                return true;
            }

            return false;
        }

        private static bool SetColor(Material material, string primaryProperty, string fallbackProperty, Color color)
        {
            if (material.HasProperty(primaryProperty))
            {
                if (material.GetColor(primaryProperty) == color)
                {
                    return false;
                }

                material.SetColor(primaryProperty, color);
                return true;
            }

            if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            {
                if (material.GetColor(fallbackProperty) == color)
                {
                    return false;
                }

                material.SetColor(fallbackProperty, color);
                return true;
            }

            return false;
        }

        private static bool SetFloat(Material material, string propertyName, float value)
        {
            if (!material.HasProperty(propertyName))
            {
                return false;
            }

            if (Mathf.Approximately(material.GetFloat(propertyName), value))
            {
                return false;
            }

            material.SetFloat(propertyName, value);
            return true;
        }
    }
}
