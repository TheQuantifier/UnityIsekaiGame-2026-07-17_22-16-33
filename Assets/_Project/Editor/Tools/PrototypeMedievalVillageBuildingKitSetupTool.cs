using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeMedievalVillageBuildingKitSetupTool
    {
        private const string KitRoot = "Assets/ThirdParty/MedievalVillageBuildingPack";
        private const string KitMaterialsRoot = KitRoot + "/Materials";
        private const string KitMeshesRoot = KitRoot + "/Meshes";
        private const string KitPreviewScenePath = KitRoot + "/Scene/Preview_Scene.unity";
        private const string KitPreviewLightingFolder = KitRoot + "/Scene/Preview_Scene";
        private const string KitPreviewLightingSettings = KitRoot + "/Scene/Preview_SceneSettings.lighting";

        [MenuItem("Tools/Prototype Scene/Building Kits/Prepare Medieval Village Building Pack")]
        public static void PrepareMedievalVillageBuildingPack()
        {
            if (!AssetDatabase.IsValidFolder(KitRoot))
            {
                throw new InvalidOperationException($"Medieval village building kit folder was not found at {KitRoot}.");
            }

            var changedAssetPaths = new List<string>();
            var importerCount = NormalizeModelImporters(changedAssetPaths);
            var materialCount = ConvertMaterialsToUrp(changedAssetPaths);
            var sceneFixCount = CleanPreviewSceneReferences();

            AssetDatabase.SaveAssets();
            if (changedAssetPaths.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(changedAssetPaths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log($"Prepared medieval village building pack. Importers={importerCount}, Materials={materialCount}, SceneFixes={sceneFixCount}.");
        }

        private static int NormalizeModelImporters(ICollection<string> changedAssetPaths)
        {
            var changed = 0;
            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { KitMeshesRoot });
            foreach (var guid in modelGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }

                var dirty = false;
                var serialized = new SerializedObject(importer);
                var materialLocation = serialized.FindProperty("m_Materials.m_MaterialLocation");
                if (materialLocation != null && materialLocation.propertyType == SerializedPropertyType.Integer && materialLocation.intValue != 1)
                {
                    materialLocation.intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    dirty = true;
                }

                if (!importer.generateSecondaryUV)
                {
                    importer.generateSecondaryUV = true;
                    dirty = true;
                }

                if (importer.importCameras)
                {
                    importer.importCameras = false;
                    dirty = true;
                }

                if (importer.importLights)
                {
                    importer.importLights = false;
                    dirty = true;
                }

                if (!dirty)
                {
                    continue;
                }

                changed++;
                changedAssetPaths.Add(path);
                importer.SaveAndReimport();
            }

            return changed;
        }

        private static int ConvertMaterialsToUrp(ICollection<string> changedAssetPaths)
        {
            var litShader = FindShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard");
            var transparentShader = litShader;
            var changed = 0;
            var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { KitMaterialsRoot });
            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                var baseMap = FirstTexture(material, "_BaseMap", "_MainTex");
                var normalMap = FirstTexture(material, "_BumpMap");
                var maskMap = FirstTexture(material, "_MaskMap", "_MetallicGlossMap", "_RMHA", "_Roughness");
                var baseColor = FirstColor(material, "_BaseColor", "_Color", "_Tint");
                var transparent = IsTransparentMaterial(material);

                material.shader = transparent ? transparentShader : litShader;
                ApplyTexture(material, "_BaseMap", baseMap);
                ApplyTexture(material, "_MainTex", baseMap);
                ApplyTexture(material, "_BumpMap", normalMap);
                ApplyTexture(material, "_MetallicGlossMap", maskMap);
                ApplyColor(material, "_BaseColor", baseColor);
                ApplyColor(material, "_Color", baseColor);
                ApplyFloat(material, "_Metallic", 0f);
                ApplyFloat(material, "_Smoothness", transparent ? 0.65f : 0.25f);
                ApplyFloat(material, "_BumpScale", normalMap == null ? 0f : 1f);

                if (normalMap != null)
                {
                    material.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    material.DisableKeyword("_NORMALMAP");
                }

                ConfigureSurface(material, transparent);
                EditorUtility.SetDirty(material);
                changedAssetPaths.Add(path);
                changed++;
            }

            return changed;
        }

        private static int CleanPreviewSceneReferences()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(KitPreviewScenePath))
            {
                return 0;
            }

            var activeScenePath = SceneManager.GetActiveScene().path;
            var activeSceneIsDirty = SceneManager.GetActiveScene().isDirty;
            if (!string.IsNullOrEmpty(activeScenePath) && activeSceneIsDirty)
            {
                throw new InvalidOperationException("Cannot clean medieval village preview scene while the active scene has unsaved changes.");
            }

            var previewScene = EditorSceneManager.OpenScene(KitPreviewScenePath, OpenSceneMode.Single);
            var changed = 0;

            foreach (var root in previewScene.GetRootGameObjects())
            {
                changed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            }

            if (LightmapSettings.lightmaps is { Length: > 0 })
            {
                LightmapSettings.lightmaps = Array.Empty<LightmapData>();
                changed++;
            }

            if (LightmapSettings.lightProbes != null)
            {
                LightmapSettings.lightProbes = null;
                changed++;
            }

            try
            {
                Lightmapping.ClearLightingDataAsset();
                changed++;
            }
            catch (MissingMethodException)
            {
                // Older editor API fallback: the saved scene still keeps runtime lightmaps cleared.
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(previewScene);
                EditorSceneManager.SaveScene(previewScene);
            }

            changed += DisablePreviewSceneBakedLighting();
            changed += DeleteAssetIfExists(KitPreviewLightingFolder);
            changed += DeleteAssetIfExists(KitPreviewLightingSettings);

            if (!string.IsNullOrEmpty(activeScenePath) && activeScenePath != KitPreviewScenePath)
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            return changed;
        }

        private static int DisablePreviewSceneBakedLighting()
        {
            var fullPath = Path.GetFullPath(KitPreviewScenePath);
            if (!File.Exists(fullPath))
            {
                return 0;
            }

            var text = File.ReadAllText(fullPath);
            var updated = text
                .Replace("    m_EnableBakedLightmaps: 1", "    m_EnableBakedLightmaps: 0")
                .Replace("    m_EnableRealtimeLightmaps: 1", "    m_EnableRealtimeLightmaps: 0");

            var lightingSettingsIndex = updated.IndexOf("  m_LightingSettings: {fileID:", StringComparison.Ordinal);
            if (lightingSettingsIndex >= 0)
            {
                var lineEnd = updated.IndexOf('\n', lightingSettingsIndex);
                if (lineEnd < 0)
                {
                    lineEnd = updated.Length;
                }

                updated = updated[..lightingSettingsIndex] + "  m_LightingSettings: {fileID: 0}" + updated[lineEnd..];
            }

            if (updated == text)
            {
                return 0;
            }

            File.WriteAllText(fullPath, updated);
            AssetDatabase.ImportAsset(KitPreviewScenePath, ImportAssetOptions.ForceSynchronousImport);
            return 1;
        }

        private static int DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null && !AssetDatabase.IsValidFolder(assetPath))
            {
                return 0;
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Failed to delete stale medieval village preview lighting asset at {assetPath}.");
            }

            return 1;
        }

        private static bool IsTransparentMaterial(Material material)
        {
            var name = material.name;
            return name.IndexOf("transparent", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("window", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Shader FindShader(params string[] names)
        {
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("No supported material shader was found for medieval village kit conversion.");
        }

        private static Texture FirstTexture(Material material, params string[] names)
        {
            foreach (var name in names)
            {
                if (material.HasProperty(name))
                {
                    var texture = material.GetTexture(name);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Color FirstColor(Material material, params string[] names)
        {
            foreach (var name in names)
            {
                if (material.HasProperty(name))
                {
                    return material.GetColor(name);
                }
            }

            return Color.white;
        }

        private static void ApplyTexture(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void ApplyColor(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void ApplyFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void ConfigureSurface(Material material, bool transparent)
        {
            if (transparent)
            {
                ApplyFloat(material, "_Surface", 1f);
                ApplyFloat(material, "_Blend", 0f);
                ApplyFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                ApplyFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                ApplyFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                return;
            }

            ApplyFloat(material, "_Surface", 0f);
            ApplyFloat(material, "_Blend", 0f);
            ApplyFloat(material, "_SrcBlend", (float)BlendMode.One);
            ApplyFloat(material, "_DstBlend", (float)BlendMode.Zero);
            ApplyFloat(material, "_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
