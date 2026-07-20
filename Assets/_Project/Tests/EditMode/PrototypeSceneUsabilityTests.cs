using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnityIsekaiGame.Tests
{
    public sealed class PrototypeSceneUsabilityTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";

        [Test]
        public void PrototypeSceneContainsRequiredUsabilityZones()
        {
            string scene = File.ReadAllText(ScenePath);

            AssertSceneContains(scene, "Sign - Central Hub");
            AssertSceneContains(scene, "Zone - Inventory and Items");
            AssertSceneContains(scene, "Zone - Equipment");
            AssertSceneContains(scene, "Zone - Combat");
            AssertSceneContains(scene, "Zone - Magic and Status");
            AssertSceneContains(scene, "Zone - Dialogue and Quests");
            AssertSceneContains(scene, "Zone - Contracts");
            AssertSceneContains(scene, "Zone - Investigation Area");
            AssertSceneContains(scene, "Zone - Persistence and Test Lab");
            AssertSceneContains(scene, "Prototype Layout Collision Floor");
        }

        [Test]
        public void PrototypeSceneContainsRequiredPickupGroups()
        {
            string scene = File.ReadAllText(ScenePath);

            AssertSceneContains(scene, "Pickup - Health Potion Single A");
            AssertSceneContains(scene, "Pickup - Health Potion Bundle");
            AssertSceneContains(scene, "Pickup - Health Potion Full Inventory Set");
            AssertSceneContains(scene, "Pickup - Prototype Iron Ore A");
            AssertSceneContains(scene, "Pickup - Prototype Iron Ore Stack");
            AssertSceneContains(scene, "Pickup - Prototype Sword Instance A");
            AssertSceneContains(scene, "Pickup - Prototype Sword Instance B");
            AssertSceneContains(scene, "Pickup - Prototype Helmet Instance A");
            AssertSceneContains(scene, "Pickup - Prototype Helmet Instance B");
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

        private static void AssertSceneContains(string scene, string expectedName)
        {
            Assert.That(scene, Does.Contain($"m_Name: {expectedName}"), expectedName);
        }
    }
}
