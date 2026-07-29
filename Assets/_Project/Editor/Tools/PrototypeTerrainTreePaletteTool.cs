using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeTerrainTreePaletteTool
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string GeneratedVegetationRoot = "Assets/_Project/Prototype/Environment/Vegetation";
        private const string GeneratedMaterialRoot = GeneratedVegetationRoot + "/Materials";
        private const string GeneratedPrefabRoot = GeneratedVegetationRoot + "/Prefabs";
        private const float DefaultBendFactor = 0.35f;
        private const int TreeScaleRandomSalt = 0x278D13;

        private static readonly PrototypeTreePaletteEntry[] TreePalette =
        {
            new("Broadleaf Tree", "prototype-tree-broadleaf", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/trees/bigLeavesTreeL.prefab", PrototypeTreePaletteStyle.GreenTree, 0.25f, 6.5f, 0.85f, 1.35f),
            new("Simple Tree", "prototype-tree-simple", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/trees/simpleTreeL.prefab", PrototypeTreePaletteStyle.GreenTree, 0.3f, 7.25f, 0.8f, 1.4f),
            new("Classic Tree", "prototype-tree-classic", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/trees/tree1L.prefab", PrototypeTreePaletteStyle.GreenTree, 0.35f, 7.75f, 0.85f, 1.45f),
            new("Fallen Branch Tree", "prototype-tree-fallen-branch", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/trees/fallenBranchesTreeL.prefab", PrototypeTreePaletteStyle.GreenTree, 0.3f, 6.75f, 0.8f, 1.3f),
            new("Purple Accent Tree", "prototype-tree-purple-accent", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/trees/purpleTreeL.prefab", PrototypeTreePaletteStyle.PurpleTree, 0.2f, 6.25f, 0.85f, 1.25f),
            new("Dry Broadleaf Tree", "prototype-tree-dry-broadleaf", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/dryTree/dryBigLeavesTreeL.prefab", PrototypeTreePaletteStyle.DryTree, 0.2f, 6.0f, 0.75f, 1.25f),
            new("Dry Simple Tree", "prototype-tree-dry-simple", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/dryTree/drySimpleTreeL.prefab", PrototypeTreePaletteStyle.DryTree, 0.25f, 6.5f, 0.75f, 1.3f),
            new("Dry Tree", "prototype-tree-dry", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/dryTree/dryTreeL.prefab", PrototypeTreePaletteStyle.DryTree, 0.25f, 6.75f, 0.75f, 1.35f),
            new("Dead Tree", "prototype-tree-dead", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/dryBranchL/deadTreeL.prefab", PrototypeTreePaletteStyle.DeadTree, 0.15f, 5.75f, 0.7f, 1.2f),
            new("Big Leaf Shrub", "prototype-shrub-big-leaf", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/shurbs/bigLeavesShrubsL.prefab", PrototypeTreePaletteStyle.GreenShrub, 0.45f, 2.6f, 0.7f, 1.35f),
            new("Small Leaf Shrub", "prototype-shrub-small-leaf", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/shurbs/littleLeavesShrubL.prefab", PrototypeTreePaletteStyle.GreenShrub, 0.45f, 2.2f, 0.65f, 1.25f),
            new("Shrub", "prototype-shrub", "Assets/ThirdParty/polygonTrees/polygonTrees/prefabs/prefabLODs/shurbs/shrubL.prefab", PrototypeTreePaletteStyle.GreenShrub, 0.4f, 2.4f, 0.7f, 1.3f)
        };

        [MenuItem("Tools/Prototype Scene/Configure Prototype Terrain Tree Palette")]
        public static void ConfigurePrototypeTerrainTreePalette()
        {
            EnsurePrototypeTreeAssets();

            var scene = OpenPrototypeSceneIfNeeded();
            var terrains = FindPrototypeTerrains();

            var prototypes = LoadTreePrototypes();
            foreach (var terrain in terrains)
            {
                terrain.terrainData.treePrototypes = prototypes;
                terrain.treeDistance = 650f;
                terrain.treeBillboardDistance = 85f;
                terrain.treeCrossFadeLength = 12f;
                terrain.treeMaximumFullLODCount = 120;
                terrain.drawTreesAndFoliage = true;

                UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
                UnityEditor.EditorUtility.SetDirty(terrain);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Configured {prototypes.Length} prototype terrain tree/shrub paint entries across {terrains.Length} terrain tile(s): {string.Join(", ", TreePalette.Select(entry => entry.Name))}.");
        }

        [MenuItem("Tools/Prototype Scene/Rebuild Prototype Vegetation Prefabs")]
        public static void RebuildPrototypeVegetationPrefabs()
        {
            EnsurePrototypeTreeAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Rebuilt {TreePalette.Length} project-owned prototype vegetation prefab(s) in {GeneratedPrefabRoot}.");
        }

        [MenuItem("Tools/Prototype Scene/Randomize Painted Prototype Tree Heights")]
        public static void RandomizePaintedPrototypeTreeHeights()
        {
            var scene = OpenPrototypeSceneIfNeeded();
            var terrains = FindPrototypeTerrains();
            var changedInstances = 0;

            foreach (var terrain in terrains)
            {
                var instances = terrain.terrainData.treeInstances;
                for (var i = 0; i < instances.Length; i++)
                {
                    var instance = instances[i];
                    if (instance.prototypeIndex < 0 || instance.prototypeIndex >= TreePalette.Length)
                    {
                        continue;
                    }

                    var entry = TreePalette[instance.prototypeIndex];
                    var height = Mathf.Lerp(entry.MinimumPaintedHeightScale, entry.MaximumPaintedHeightScale, Deterministic01(instance, TreeScaleRandomSalt));
                    var width = height * Mathf.Lerp(0.82f, 1.16f, Deterministic01(instance, TreeScaleRandomSalt + 97));

                    instance.heightScale = height;
                    instance.widthScale = width;
                    instances[i] = instance;
                    changedInstances++;
                }

                terrain.terrainData.treeInstances = instances;
                EditorUtility.SetDirty(terrain.terrainData);
                EditorUtility.SetDirty(terrain);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Randomized prototype terrain tree scale for {changedInstances} painted tree instance(s) across {terrains.Length} terrain tile(s).");
        }

        private static void EnsurePrototypeTreeAssets()
        {
            EnsureFolder("Assets/_Project/Prototype");
            EnsureFolder("Assets/_Project/Prototype/Environment");
            EnsureFolder(GeneratedVegetationRoot);
            EnsureFolder(GeneratedMaterialRoot);
            EnsureFolder(GeneratedPrefabRoot);

            var materials = PrototypeTreeMaterials.LoadOrCreate(GeneratedMaterialRoot);
            foreach (var entry in TreePalette)
            {
                CreateOrUpdateGeneratedPrefab(entry, materials);
            }
        }

        private static void CreateOrUpdateGeneratedPrefab(PrototypeTreePaletteEntry entry, PrototypeTreeMaterials materials)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(entry.SourcePrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException($"Missing source vegetation prefab for {entry.Name}: {entry.SourcePrefabPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(source);
            }

            try
            {
                instance.name = entry.Name;
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.transform.localScale = Vector3.one * entry.PrefabScale;
                AssignProjectMaterials(instance, entry.Style, materials);
                RemoveImportedRuntimeColliders(instance);
                ValidateGeneratedPrefab(instance, entry);

                PrefabUtility.SaveAsPrefabAsset(instance, entry.GeneratedPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssignProjectMaterials(GameObject instance, PrototypeTreePaletteStyle style, PrototypeTreeMaterials materials)
        {
            var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                var sourceMaterials = renderer.sharedMaterials;
                var replacementMaterials = new Material[sourceMaterials.Length];
                for (var i = 0; i < sourceMaterials.Length; i++)
                {
                    replacementMaterials[i] = materials.Resolve(style, sourceMaterials[i], i, sourceMaterials.Length);
                }

                renderer.sharedMaterials = replacementMaterials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RemoveImportedRuntimeColliders(GameObject instance)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void ValidateGeneratedPrefab(GameObject instance, PrototypeTreePaletteEntry entry)
        {
            var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Generated vegetation prefab '{entry.Name}' has no MeshRenderer components.");
            }

            var meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0 || meshFilters.All(filter => filter == null || filter.sharedMesh == null))
            {
                throw new InvalidOperationException($"Generated vegetation prefab '{entry.Name}' has no assigned mesh.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(material => material == null || material.shader == null))
                {
                    throw new InvalidOperationException($"Generated vegetation prefab '{entry.Name}' has an invalid material assignment.");
                }
            }
        }

        private static TreePrototype[] LoadTreePrototypes()
        {
            var prototypes = new List<TreePrototype>(TreePalette.Length);
            var missing = new List<string>();

            foreach (var entry in TreePalette)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.GeneratedPrefabPath);
                if (prefab == null)
                {
                    missing.Add($"{entry.Name} ({entry.GeneratedPrefabPath})");
                    continue;
                }

                prototypes.Add(new TreePrototype
                {
                    prefab = prefab,
                    bendFactor = entry.BendFactor <= 0f ? DefaultBendFactor : entry.BendFactor
                });
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException("Missing prototype tree palette prefab(s): " + string.Join("; ", missing));
            }

            return prototypes.ToArray();
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

        private static Terrain[] FindPrototypeTerrains()
        {
            var ground = FindScenePath("PrototypeScene/Environment/Ground");
            if (ground == null)
            {
                throw new InvalidOperationException("PrototypeScene/Environment/Ground was not found.");
            }

            var terrains = ground.GetComponentsInChildren<Terrain>(true)
                .Where(terrain => terrain != null && terrain.terrainData != null)
                .OrderBy(terrain => terrain.transform.position.z)
                .ThenBy(terrain => terrain.transform.position.x)
                .ToArray();

            if (terrains.Length == 0)
            {
                throw new InvalidOperationException("No Terrain components were found under PrototypeScene/Environment/Ground.");
            }

            return terrains;
        }

        private static float Deterministic01(TreeInstance instance, int salt)
        {
            unchecked
            {
                var hash = 2166136261u;
                Mix(ref hash, instance.prototypeIndex);
                Mix(ref hash, Mathf.RoundToInt(instance.position.x * 100000f));
                Mix(ref hash, Mathf.RoundToInt(instance.position.y * 100000f));
                Mix(ref hash, Mathf.RoundToInt(instance.position.z * 100000f));
                Mix(ref hash, salt);
                return (hash & 0x00FFFFFF) / 16777216f;
            }
        }

        private static void Mix(ref uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
            }
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

        private readonly struct PrototypeTreePaletteEntry
        {
            public PrototypeTreePaletteEntry(
                string name,
                string assetName,
                string sourcePrefabPath,
                PrototypeTreePaletteStyle style,
                float bendFactor,
                float prefabScale,
                float minimumPaintedHeightScale,
                float maximumPaintedHeightScale)
            {
                Name = name;
                AssetName = assetName;
                SourcePrefabPath = sourcePrefabPath;
                Style = style;
                BendFactor = bendFactor;
                PrefabScale = prefabScale;
                MinimumPaintedHeightScale = minimumPaintedHeightScale;
                MaximumPaintedHeightScale = maximumPaintedHeightScale;
            }

            public string Name { get; }

            public string AssetName { get; }

            public string SourcePrefabPath { get; }

            public PrototypeTreePaletteStyle Style { get; }

            public float BendFactor { get; }

            public float PrefabScale { get; }

            public float MinimumPaintedHeightScale { get; }

            public float MaximumPaintedHeightScale { get; }

            public string GeneratedPrefabPath => $"{GeneratedPrefabRoot}/{AssetName}.prefab";
        }

        private enum PrototypeTreePaletteStyle
        {
            GreenTree,
            PurpleTree,
            DryTree,
            DeadTree,
            GreenShrub
        }

        private sealed class PrototypeTreeMaterials
        {
            private PrototypeTreeMaterials(Material bark, Material leaves, Material purpleLeaves, Material dryBark, Material dryLeaves, Material deadWood, Material shrubStem, Material shrubLeaves)
            {
                Bark = bark;
                Leaves = leaves;
                PurpleLeaves = purpleLeaves;
                DryBark = dryBark;
                DryLeaves = dryLeaves;
                DeadWood = deadWood;
                ShrubStem = shrubStem;
                ShrubLeaves = shrubLeaves;
            }

            private Material Bark { get; }

            private Material Leaves { get; }

            private Material PurpleLeaves { get; }

            private Material DryBark { get; }

            private Material DryLeaves { get; }

            private Material DeadWood { get; }

            private Material ShrubStem { get; }

            private Material ShrubLeaves { get; }

            public static PrototypeTreeMaterials LoadOrCreate(string root)
            {
                return new PrototypeTreeMaterials(
                    LoadOrCreateMaterial(root, "Prototype Tree Bark", new Color(0.34f, 0.22f, 0.12f, 1f), 0.22f),
                    LoadOrCreateMaterial(root, "Prototype Tree Leaves", new Color(0.22f, 0.50f, 0.18f, 1f), 0.18f),
                    LoadOrCreateMaterial(root, "Prototype Purple Leaves", new Color(0.43f, 0.26f, 0.52f, 1f), 0.2f),
                    LoadOrCreateMaterial(root, "Prototype Dry Bark", new Color(0.42f, 0.31f, 0.19f, 1f), 0.25f),
                    LoadOrCreateMaterial(root, "Prototype Dry Leaves", new Color(0.56f, 0.45f, 0.25f, 1f), 0.18f),
                    LoadOrCreateMaterial(root, "Prototype Dead Wood", new Color(0.36f, 0.32f, 0.27f, 1f), 0.3f),
                    LoadOrCreateMaterial(root, "Prototype Shrub Stem", new Color(0.31f, 0.25f, 0.14f, 1f), 0.24f),
                    LoadOrCreateMaterial(root, "Prototype Shrub Leaves", new Color(0.18f, 0.44f, 0.14f, 1f), 0.18f));
            }

            public Material Resolve(PrototypeTreePaletteStyle style, Material source, int materialIndex, int materialCount)
            {
                var sourceName = source == null ? string.Empty : source.name.ToLowerInvariant();
                var looksLikeLeaves = sourceName.Contains("leave") || sourceName.Contains("grass") || (materialCount > 1 && materialIndex > 0);

                switch (style)
                {
                    case PrototypeTreePaletteStyle.PurpleTree:
                        return looksLikeLeaves ? PurpleLeaves : Bark;
                    case PrototypeTreePaletteStyle.DryTree:
                        return looksLikeLeaves ? DryLeaves : DryBark;
                    case PrototypeTreePaletteStyle.DeadTree:
                        return DeadWood;
                    case PrototypeTreePaletteStyle.GreenShrub:
                        return looksLikeLeaves ? ShrubLeaves : ShrubStem;
                    default:
                        return looksLikeLeaves ? Leaves : Bark;
                }
            }

            private static Material LoadOrCreateMaterial(string root, string name, Color color, float smoothness)
            {
                var path = $"{root}/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(ResolveLitShader())
                    {
                        name = name
                    };
                    AssetDatabase.CreateAsset(material, path);
                }

                material.shader = ResolveLitShader();
                SetMaterialColor(material, color);
                SetMaterialFloat(material, "_Smoothness", smoothness);
                SetMaterialFloat(material, "_Metallic", 0f);
                EditorUtility.SetDirty(material);
                return material;
            }

            private static Shader ResolveLitShader()
            {
                return Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Diffuse");
            }

            private static void SetMaterialColor(Material material, Color color)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
            }

            private static void SetMaterialFloat(Material material, string property, float value)
            {
                if (material.HasProperty(property))
                {
                    material.SetFloat(property, value);
                }
            }
        }
    }
}
