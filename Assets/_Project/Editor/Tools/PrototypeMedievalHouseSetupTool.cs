using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeMedievalHouseSetupTool
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string ImportedPackageRoot = "Assets/Medieval_house_lite";
        private const string PrototypeBuildingRoot = "Assets/_Project/Prototype/Environment/Buildings";
        private const string MedievalHouseRoot = PrototypeBuildingRoot + "/MedievalHouseLite";
        private const string MedievalHousePrefabPath = MedievalHouseRoot + "/Prefabs/medieval_house_lite_v2.prefab";
        private const string MedievalHouseFallbackMaterialPath = MedievalHouseRoot + "/Materials/Prototype House Fallback.mat";
        private const string SceneBuildingName = "Prototype Medieval House";
        private static readonly Vector3 PreferredPosition = new Vector3(22f, 0f, 28f);

        [MenuItem("Tools/Prototype Scene/Setup Medieval House")]
        public static void SetupMedievalHouse()
        {
            MoveImportedHousePackageIntoPrototype();
            EnsurePrefabRendererMaterials();
            EnsureDecorativePunctualLightsUseLowResolutionShadows();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MedievalHousePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Medieval house prefab was not found at {MedievalHousePrefabPath}.");
            }

            var scene = OpenPrototypeSceneIfNeeded();
            var landmarks = FindScenePath("PrototypeScene/Environment/Landmarks");
            if (landmarks == null)
            {
                throw new InvalidOperationException("PrototypeScene/Environment/Landmarks was not found.");
            }

            var buildings = FindOrCreateChild(landmarks.transform, "Buildings");
            var house = FindChildByName(buildings.transform, SceneBuildingName);
            if (house == null)
            {
                house = PrefabUtility.InstantiatePrefab(prefab, buildings.transform) as GameObject;
                if (house == null)
                {
                    house = UnityEngine.Object.Instantiate(prefab, buildings.transform);
                }

                house.name = SceneBuildingName;
            }

            ConfigureTransform(house);
            EnsureFootprintCollider(house);

            EditorUtility.SetDirty(buildings);
            EditorUtility.SetDirty(house);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Placed {SceneBuildingName} in PrototypeScene at {Format(house.transform.position)}.");
        }

        private static void EnsurePrefabRendererMaterials()
        {
            var fallbackMaterial = LoadOrCreateFallbackMaterial();
            var prefabRoot = PrefabUtility.LoadPrefabContents(MedievalHousePrefabPath);
            var changed = false;

            try
            {
                foreach (var renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    var rendererChanged = false;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].shader != null)
                        {
                            continue;
                        }

                        materials[i] = fallbackMaterial;
                        rendererChanged = true;
                    }

                    if (!rendererChanged)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, MedievalHousePrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureDecorativePunctualLightsUseLowResolutionShadows()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { MedievalHouseRoot });
            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                var changed = false;

                try
                {
                    foreach (var light in prefabRoot.GetComponentsInChildren<Light>(true))
                    {
                        if (light == null || light.type == LightType.Directional)
                        {
                            continue;
                        }

                        light.shadows = LightShadows.Soft;
                        light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
                        EditorUtility.SetDirty(light);
                        changed = true;
                    }

                    foreach (var behaviour in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (behaviour == null)
                        {
                            continue;
                        }

                        var serialized = new SerializedObject(behaviour);
                        var tierProperty = serialized.FindProperty("m_AdditionalLightsShadowResolutionTier");
                        if (tierProperty == null || tierProperty.propertyType != SerializedPropertyType.Integer)
                        {
                            continue;
                        }

                        tierProperty.intValue = 0;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(behaviour);
                        changed = true;
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static Material LoadOrCreateFallbackMaterial()
        {
            EnsureFolder(MedievalHouseRoot + "/Materials");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MedievalHouseFallbackMaterialPath);
            if (material == null)
            {
                material = new Material(FindLitShader());
                AssetDatabase.CreateAsset(material, MedievalHouseFallbackMaterialPath);
            }

            material.name = "Prototype House Fallback";
            material.color = new Color(0.58f, 0.48f, 0.36f, 1f);
            SetFloatIfPresent(material, "_Smoothness", 0f);
            SetFloatIfPresent(material, "_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");

            if (shader == null)
            {
                throw new InvalidOperationException("Could not find a supported Lit shader for the medieval house fallback material.");
            }

            return shader;
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void MoveImportedHousePackageIntoPrototype()
        {
            EnsureFolder("Assets/_Project/Prototype");
            EnsureFolder("Assets/_Project/Prototype/Environment");
            EnsureFolder(PrototypeBuildingRoot);

            if (AssetDatabase.IsValidFolder(MedievalHouseRoot))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ImportedPackageRoot))
            {
                throw new InvalidOperationException($"Imported medieval house package folder was not found at {ImportedPackageRoot}.");
            }

            var failure = AssetDatabase.MoveAsset(ImportedPackageRoot, MedievalHouseRoot);
            if (!string.IsNullOrWhiteSpace(failure))
            {
                throw new InvalidOperationException($"Failed to move medieval house package into prototype assets: {failure}");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureTransform(GameObject house)
        {
            var position = PreferredPosition;
            position.y = SampleGroundHeight(position) + 0.02f;
            house.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            house.transform.localScale = Vector3.one;
            SetStaticRecursive(house, true);
        }

        private static float SampleGroundHeight(Vector3 position)
        {
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                var terrainPosition = terrain.transform.position;
                var size = terrain.terrainData.size;
                if (position.x < terrainPosition.x || position.z < terrainPosition.z
                    || position.x > terrainPosition.x + size.x || position.z > terrainPosition.z + size.z)
                {
                    continue;
                }

                return terrain.SampleHeight(position) + terrainPosition.y;
            }

            if (Physics.Raycast(position + Vector3.up * 1000f, Vector3.down, out var hit, 2000f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return position.y;
        }

        private static void EnsureFootprintCollider(GameObject house)
        {
            var colliders = house.GetComponentsInChildren<Collider>(true);
            if (colliders.Any(collider => collider != null && !collider.isTrigger))
            {
                return;
            }

            var renderers = house.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer != null)
                .ToArray();
            if (renderers.Length == 0)
            {
                return;
            }

            var worldBounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            var colliderObject = FindOrCreateChild(house.transform, "Prototype House Footprint Collider");
            var collider = colliderObject.GetComponent<BoxCollider>() ?? colliderObject.AddComponent<BoxCollider>();
            collider.isTrigger = false;

            colliderObject.transform.SetPositionAndRotation(house.transform.position, house.transform.rotation);
            colliderObject.transform.localScale = Vector3.one;

            var localCenter = house.transform.InverseTransformPoint(worldBounds.center);
            localCenter.y = Mathf.Max(0.75f, localCenter.y * 0.5f);
            collider.center = localCenter;
            collider.size = new Vector3(
                Mathf.Max(1f, worldBounds.size.x * 0.9f),
                Mathf.Max(1.5f, worldBounds.size.y * 0.5f),
                Mathf.Max(1f, worldBounds.size.z * 0.9f));
            EditorUtility.SetDirty(colliderObject);
            EditorUtility.SetDirty(collider);
        }

        private static void SetStaticRecursive(GameObject root, bool isStatic)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.isStatic = isStatic;
                EditorUtility.SetDirty(transform.gameObject);
            }
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

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            var gameObject = new GameObject(childName);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject FindChildByName(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(child => child != null && child.name == name)
                .Select(child => child.gameObject)
                .FirstOrDefault();
        }

        private static GameObject FindScenePath(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0])
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException($"Cannot create invalid asset folder path '{path}'.");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static string Format(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }
    }
}
