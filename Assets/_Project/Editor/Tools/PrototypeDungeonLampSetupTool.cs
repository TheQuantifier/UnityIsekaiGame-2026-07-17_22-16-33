using UnityEditor;
using UnityEngine;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeDungeonLampSetupTool
    {
        private static readonly string[] LampPrefabPaths =
        {
            "Assets/_Project/Prototype/Prefabs/Building Parts/HandPaintedDungeon/Lamp_02 Variant.prefab"
        };

        private static readonly string[] UnlitLampPrefabPaths =
        {
            "Assets/_Project/Prototype/Prefabs/Building Parts/HandPaintedDungeon/Lamp_01 Variant.prefab"
        };

        private static readonly string[] DungeonPrefabPaths =
        {
            "Assets/_Project/Prototype/Prefabs/Buildings/PrototypeDungeon/Dungeon1.prefab"
        };

        [MenuItem("Tools/Prototype Scene/Dungeon Kits/Setup Dungeon Lamp Lights")]
        public static void SetupDungeonLampLights()
        {
            var updated = 0;
            foreach (var prefabPath in LampPrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    if (EnsureLampLight(root))
                    {
                        updated++;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var prefabPath in UnlitLampPrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    RemoveLampLight(root);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var prefabPath in DungeonPrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    CleanupDungeonLampInstances(root);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Configured dungeon lamp lights on {updated} project lamp prefab(s).");
        }

        private static bool EnsureLampLight(GameObject root)
        {
            RemoveRootLight(root);

            var lightObject = root.transform.Find("Prototype Lamp Point Light");
            var createdLightObject = false;
            if (lightObject == null)
            {
                var created = new GameObject("Prototype Lamp Point Light");
                created.transform.SetParent(root.transform, false);
                lightObject = created.transform;
                createdLightObject = true;
            }

            if (createdLightObject)
            {
                lightObject.localPosition = ResolveLightPosition(root);
            }

            lightObject.localRotation = Quaternion.identity;
            lightObject.localScale = Vector3.one;

            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.gameObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.42f, 1f);
            light.intensity = 5.5f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            light.shadowStrength = 0f;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            light.renderMode = LightRenderMode.Auto;
            light.bounceIntensity = 0.35f;

            RemoveOrphanUniversalAdditionalLightData(root);

            EditorUtility.SetDirty(lightObject.gameObject);
            EditorUtility.SetDirty(light);
            return true;
        }

        private static Vector3 ResolveLightPosition(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Vector3(0f, 1.7f, 0f);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var localCenter = root.transform.InverseTransformPoint(bounds.center);
            var localTop = root.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            return new Vector3(localCenter.x, Mathf.Lerp(localCenter.y, localTop.y, 0.72f), localCenter.z);
        }

        private static void RemoveLegacyLightChild(GameObject root)
        {
            var existing = root.transform.Find("Prototype Lamp Point Light");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void RemoveRootLight(GameObject root)
        {
            foreach (var light in root.GetComponents<Light>())
            {
                if (PrefabUtility.IsAddedComponentOverride(light))
                {
                    PrefabUtility.RevertAddedComponent(light, InteractionMode.AutomatedAction);
                }
                else
                {
                    Object.DestroyImmediate(light);
                }
            }

            RemoveUniversalAdditionalLightData(root);
        }

        private static void RemoveLampLight(GameObject root)
        {
            RemoveLegacyLightChild(root);
            RemoveAllLights(root);

            EditorUtility.SetDirty(root);
        }

        private static void CleanupDungeonLampInstances(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var gameObject = transform.gameObject;
                if (gameObject.name.StartsWith("Lamp_01 Variant"))
                {
                    RemoveAllLights(gameObject);
                }
                else if (gameObject.name.StartsWith("Lamp_02 Variant"))
                {
                    RemoveRootLight(gameObject);
                    RemoveOrphanUniversalAdditionalLightData(gameObject);
                }
            }
        }

        private static void RemoveAllLights(GameObject root)
        {
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                RemoveComponent(light);
            }

            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name != "UniversalAdditionalLightData")
                {
                    continue;
                }

                RemoveComponent(component);
            }
        }

        private static void RemoveOrphanUniversalAdditionalLightData(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name != "UniversalAdditionalLightData")
                {
                    continue;
                }

                if (component.GetComponent<Light>() == null)
                {
                    RemoveComponent(component);
                }
            }
        }

        private static void RemoveUniversalAdditionalLightData(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null || component.GetType().Name != "UniversalAdditionalLightData")
                {
                    continue;
                }

                RemoveComponent(component);
            }
        }

        private static void RemoveComponent(Component component)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(component))
            {
                try
                {
                    PrefabUtility.RevertAddedComponent(component, InteractionMode.AutomatedAction);
                    return;
                }
                catch
                {
                    // Source prefab components and non-added instance components cannot be reverted this way.
                }
            }

            Object.DestroyImmediate(component);
        }
    }
}
