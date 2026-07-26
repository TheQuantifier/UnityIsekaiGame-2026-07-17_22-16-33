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
        public void PrototypeSceneKeepsOnlyMenuAndTestingShell()
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
            AssertSceneContains(scene, "Ground - Main Prototype");
            Assert.That(scene, Does.Contain("guid: e3dc3f70f41944be9ee51eac14956a39"), "Ground should use PrototypeGround material.");

            AssertSceneDoesNotContain(scene, "Prototype Ground");
            AssertSceneDoesNotContain(scene, "Prototype Systems World");
            AssertSceneDoesNotContain(scene, "Systems World Safety Floor");
            AssertSceneDoesNotContain(scene, "Hub - Systems World");
            AssertSceneDoesNotContain(scene, "Zone - ");
            AssertSceneDoesNotContain(scene, "Pickup - ");
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
