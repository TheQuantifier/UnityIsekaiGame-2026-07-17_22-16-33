using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeSceneScaleCalibrationTool
    {
        public const float TargetCharacterHeight = 1.8f;
        public const float TargetMedievalHouseHeight = 9.3f;
        public const float MinimumTreeHeight = 6.0f;
        public const float MaximumTreeHeight = 9.9f;
        public const float MaximumTreeFootprint = 13.0f;
        public const float MinimumShrubHeight = 2.0f;
        public const float MaximumShrubHeight = 3.3f;
        public const float MaximumShrubFootprint = 3.8f;

        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string MedievalHouseScenePath = "PrototypeScene/Environment/Landmarks/Buildings/Prototype Medieval House";
        private const string MedievalHousePrefabPath = "Assets/_Project/Prototype/Environment/Buildings/MedievalHouseLite/Prefabs/medieval_house_lite_v2.prefab";
        private const string VegetationPrefabRoot = "Assets/_Project/Prototype/Environment/Vegetation/Prefabs";

        [MenuItem("Tools/Prototype Scene/Calibrate Prototype World Scale")]
        public static void CalibratePrototypeWorldScale()
        {
            var scene = OpenPrototypeSceneIfNeeded();
            var playerHeight = MeasurePlayerHeight(scene);

            CalibrateMedievalHouse();
            CalibrateVegetationPrefabs();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Prototype world scale calibrated. Player={playerHeight:0.00}m, reference={TargetCharacterHeight:0.00}m, houseTarget={TargetMedievalHouseHeight:0.00}m.");
        }

        [MenuItem("Tools/Prototype Scene/Report Prototype World Scale")]
        public static void ReportPrototypeWorldScale()
        {
            var scene = OpenPrototypeSceneIfNeeded();
            var lines = new List<string>
            {
                $"Prototype scale report: Player={MeasurePlayerHeight(scene):0.00}m target={TargetCharacterHeight:0.00}m"
            };

            var house = FindScenePath(scene, MedievalHouseScenePath);
            if (house != null && TryCalculateRendererBounds(house, out var houseBounds))
            {
                lines.Add($"House={houseBounds.size.x:0.00}x{houseBounds.size.y:0.00}x{houseBounds.size.z:0.00}m scale={Format(house.transform.localScale)}");
            }

            foreach (var prefab in LoadVegetationPrefabs())
            {
                if (TryCalculatePrefabBounds(prefab, out var bounds))
                {
                    lines.Add($"{prefab.name}={bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00}m scale={Format(prefab.transform.localScale)}");
                }
            }

            Debug.Log(string.Join("\n", lines));
        }

        private static void CalibrateMedievalHouse()
        {
            var housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MedievalHousePrefabPath);
            if (housePrefab == null || !TryCalculatePrefabBounds(housePrefab, out var prefabBounds))
            {
                throw new InvalidOperationException($"Cannot calibrate medieval house because prefab bounds could not be measured at {MedievalHousePrefabPath}.");
            }

            var targetScale = Mathf.Clamp(TargetMedievalHouseHeight / Mathf.Max(0.01f, prefabBounds.size.y), 0.25f, 3f);
            var scene = SceneManager.GetActiveScene();
            var house = FindScenePath(scene, MedievalHouseScenePath);
            if (house == null)
            {
                throw new InvalidOperationException($"Cannot calibrate medieval house because scene object was not found at {MedievalHouseScenePath}.");
            }

            house.transform.localScale = Vector3.one * targetScale;
            EditorUtility.SetDirty(house);

            foreach (var collider in house.GetComponentsInChildren<BoxCollider>(true))
            {
                if (collider == null || !collider.name.Contains("Footprint", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                collider.size = new Vector3(
                    Mathf.Max(1f, prefabBounds.size.x * 0.9f),
                    Mathf.Max(1.5f, prefabBounds.size.y * 0.5f),
                    Mathf.Max(1f, prefabBounds.size.z * 0.9f));
                EditorUtility.SetDirty(collider);
            }
        }

        private static void CalibrateVegetationPrefabs()
        {
            foreach (var prefab in LoadVegetationPrefabs())
            {
                if (!TryCalculatePrefabBounds(prefab, out var bounds))
                {
                    continue;
                }

                var isTree = prefab.name.StartsWith("prototype-tree-", StringComparison.Ordinal);
                var maximumFootprint = isTree ? MaximumTreeFootprint : MaximumShrubFootprint;
                var maximumHeight = isTree ? MaximumTreeHeight : MaximumShrubHeight;
                var minimumHeight = isTree ? MinimumTreeHeight : MinimumShrubHeight;
                var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
                var targetHeightByFootprint = maximumFootprint * bounds.size.y / Mathf.Max(0.01f, footprint);
                var targetHeight = Mathf.Min(maximumHeight, targetHeightByFootprint);
                var minimumFootprintAtHeight = footprint * minimumHeight / Mathf.Max(0.01f, bounds.size.y);
                if (minimumFootprintAtHeight <= maximumFootprint)
                {
                    targetHeight = Mathf.Max(targetHeight, minimumHeight);
                }

                var targetScale = prefab.transform.localScale.x * targetHeight / Mathf.Max(0.01f, bounds.size.y);
                if (Mathf.Abs(targetScale - prefab.transform.localScale.x) < 0.001f)
                {
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                var editable = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    editable.transform.localScale = Vector3.one * targetScale;
                    PrefabUtility.SaveAsPrefabAsset(editable, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(editable);
                }
            }
        }

        private static float MeasurePlayerHeight(Scene scene)
        {
            var player = FindScenePath(scene, "PrototypeScene/Player/Prototype Player");
            if (player == null)
            {
                return TargetCharacterHeight;
            }

            var controller = player.GetComponent<CharacterController>();
            if (controller != null && controller.height > 0f)
            {
                return controller.height * Mathf.Abs(player.transform.lossyScale.y);
            }

            var capsule = player.GetComponent<CapsuleCollider>();
            if (capsule != null && capsule.height > 0f)
            {
                return capsule.height * Mathf.Abs(player.transform.lossyScale.y);
            }

            return TryCalculateRendererBounds(player, out var bounds) && bounds.size.y > 0f
                ? bounds.size.y
                : TargetCharacterHeight;
        }

        private static GameObject[] LoadVegetationPrefabs()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { VegetationPrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(prefab => prefab != null)
                .OrderBy(prefab => prefab.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool TryCalculatePrefabBounds(GameObject prefab, out Bounds bounds)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab);
            }

            try
            {
                return TryCalculateRendererBounds(instance, out bounds);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static bool TryCalculateRendererBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer != null)
                .ToArray();

            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static Scene OpenPrototypeSceneIfNeeded()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(scene.path, PrototypeScenePath, StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            }

            return scene;
        }

        private static GameObject FindScenePath(Scene scene, string path)
        {
            var parts = path.Split('/');
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!string.Equals(root.name, parts[0], StringComparison.Ordinal))
                {
                    continue;
                }

                var current = root.transform;
                for (var i = 1; i < parts.Length; i++)
                {
                    current = current.Find(parts[i]);
                    if (current == null)
                    {
                        return null;
                    }
                }

                return current.gameObject;
            }

            return null;
        }

        private static string Format(Vector3 scale)
        {
            return $"{scale.x:0.###},{scale.y:0.###},{scale.z:0.###}";
        }
    }
}
