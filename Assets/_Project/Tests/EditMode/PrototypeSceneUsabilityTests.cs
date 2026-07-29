using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityIsekaiGame.Tests
{
    public sealed class PrototypeSceneUsabilityTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string PrototypeVegetationPrefabRoot = "Assets/_Project/Prototype/Environment/Vegetation/Prefabs";

        [Test]
        public void PrototypeSceneKeepsCleanPlayableTestingShell()
        {
            string scene = File.ReadAllText(ScenePath);

            AssertSceneContains(scene, "EventSystem");
            AssertSceneContains(scene, "HUD Canvas");
            AssertSceneContains(scene, "Inventory Canvas");
            AssertSceneContains(scene, "Interaction Prompt Canvas");
            AssertSceneContains(scene, "PrototypeScene");
            AssertSceneContains(scene, "Environment");
            AssertSceneContains(scene, "Ground");
            AssertSceneContains(scene, "Boundaries");
            AssertSceneContains(scene, "Lighting");
            AssertSceneContains(scene, "Landmarks");
            AssertSceneContains(scene, "Player");
            AssertSceneContains(scene, "Prototype Player");
            AssertSceneContains(scene, "Spawn Points");
            AssertSceneContains(scene, "Prototype Player Spawn");
            AssertSceneContains(scene, "Gameplay");
            AssertSceneContains(scene, "Items");
            AssertSceneContains(scene, "Combat");
            AssertSceneContains(scene, "NPCs");
            AssertSceneContains(scene, "Quests");
            AssertSceneContains(scene, "Knowledge");
            AssertSceneContains(scene, "Biology");
            AssertSceneContains(scene, "UI");
            AssertSceneContains(scene, "Test Infrastructure");
            AssertSceneContains(scene, "Prototype Persistence Service");

            AssertSceneDoesNotContain(scene, "Prototype Ground");
            AssertSceneDoesNotContain(scene, "Ground - Main Prototype");
            AssertSceneDoesNotContain(scene, "Prototype Systems World");
            AssertSceneDoesNotContain(scene, "Systems World Safety Floor");
            AssertSceneDoesNotContain(scene, "Hub - Systems World");
            AssertSceneDoesNotContain(scene, "Zone - ");
            AssertSceneDoesNotContain(scene, "Prototype Enemy");
            AssertSceneDoesNotContain(scene, "Prototype Damage Dummy");
            AssertSceneDoesNotContain(scene, "Prototype Dialogue NPC");
            AssertSceneDoesNotContain(scene, "Prototype Quest Investigation Area");
            AssertSceneDoesNotContain(scene, "Prototype Contract Board");
            AssertSceneDoesNotContain(scene, "Prototype Delivery Crate");
            AssertSceneDoesNotContain(scene, "Status Applicator - ");
            AssertSceneDoesNotContain(scene, "Sign - ");
        }

        [Test]
        public void PrototypeSceneTestPointIdsAreUniqueAndComplete()
        {
            string scene = File.ReadAllText(ScenePath);
            MatchCollection matches = Regex.Matches(scene, @"testPointId:\s*(test-point\.[^\r\n]+)");
            HashSet<string> ids = new HashSet<string>();

            foreach (Match match in matches)
            {
                Assert.That(ids.Add(match.Groups[1].Value.Trim()), Is.True, $"Duplicate test point ID: {match.Groups[1].Value}");
            }

            Assert.That(ids, Does.Contain("test-point.spawn"));
            Assert.That(ids, Does.Contain("test-point.items"));
            Assert.That(ids, Does.Contain("test-point.equipment"));
            Assert.That(ids, Does.Contain("test-point.combat"));
            Assert.That(ids, Does.Contain("test-point.magic-status"));
            Assert.That(ids, Does.Contain("test-point.npc-quest"));
            Assert.That(ids, Does.Contain("test-point.contract-board"));
            Assert.That(ids, Does.Contain("test-point.investigation-area"));
        }

        [Test]
        public void PrototypeSceneHasSingleEventSystem()
        {
            string scene = File.ReadAllText(ScenePath);
            MatchCollection matches = Regex.Matches(scene, @"m_Name:\s*EventSystem\b");

            Assert.That(matches.Count, Is.EqualTo(1));
        }

        [Test]
        public void PrototypeVegetationPaintPrefabsHaveRenderableMeshesAndMaterials()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrototypeVegetationPrefabRoot });

            Assert.That(prefabGuids.Length, Is.GreaterThanOrEqualTo(12));

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                Assert.That(prefab, Is.Not.Null, path);

                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
                MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);

                Assert.That(prefab.transform.localScale.x, Is.GreaterThan(1.5f), path);
                Assert.That(prefab.transform.localScale.y, Is.GreaterThan(1.5f), path);
                Assert.That(prefab.transform.localScale.z, Is.GreaterThan(1.5f), path);
                Assert.That(meshFilters.Any(meshFilter => meshFilter != null && meshFilter.sharedMesh != null), Is.True, path);
                Assert.That(renderers.Length, Is.GreaterThan(0), path);

                foreach (MeshRenderer renderer in renderers)
                {
                    Assert.That(renderer.sharedMaterials.Length, Is.GreaterThan(0), path);
                    Assert.That(renderer.sharedMaterials.All(material => material != null && material.shader != null), Is.True, path);
                }
            }
        }

        [Test]
        public void PrototypeTerrainsUseGeneratedVegetationPaintPalette()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath);
            Terrain[] terrains = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
                .ToArray();

            Assert.That(terrains.Length, Is.GreaterThanOrEqualTo(1));

            foreach (Terrain terrain in terrains)
            {
                Assert.That(terrain.drawTreesAndFoliage, Is.True, terrain.name);
                Assert.That(terrain.terrainData, Is.Not.Null, terrain.name);
                Assert.That(terrain.terrainData.treePrototypes.Length, Is.GreaterThanOrEqualTo(12), terrain.name);

                foreach (TreePrototype treePrototype in terrain.terrainData.treePrototypes)
                {
                    Assert.That(treePrototype.prefab, Is.Not.Null, terrain.name);

                    string prefabPath = AssetDatabase.GetAssetPath(treePrototype.prefab);
                    Assert.That(prefabPath, Does.StartWith(PrototypeVegetationPrefabRoot), terrain.name);
                }
            }
        }

        private static void AssertSceneContains(string scene, string expectedName)
        {
            Assert.That(scene, Does.Contain($"m_Name: {expectedName}"), expectedName);
        }

        private static void AssertSceneDoesNotContain(string scene, string removedName)
        {
            Assert.That(scene, Does.Not.Contain($"m_Name: {removedName}"), removedName);
        }
    }
}
