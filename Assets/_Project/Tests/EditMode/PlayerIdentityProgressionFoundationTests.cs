using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.Tests
{
    public sealed class PlayerIdentityProgressionFoundationTests
    {
        private const string PrototypeCatalogPath = "Assets/_Project/Prototype/Content/GameData/PrototypeDefinitionCatalog.asset";

        private GameObject runtimeRoot;

        [TearDown]
        public void TearDown()
        {
            if (runtimeRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            }
        }

        [Test]
        public void PrototypeCatalogRegistersFeature51Definitions()
        {
            DefinitionRegistry registry = LoadRegistry();

            AssertResolve(registry, "origin-family.native-born", "UnityIsekaiGame.Progression.OriginFamilyDefinition");
            AssertResolve(registry, "origin-family.summoned-otherworlder", "UnityIsekaiGame.Progression.OriginFamilyDefinition");
            AssertResolve(registry, "origin-family.reincarnated-local", "UnityIsekaiGame.Progression.OriginFamilyDefinition");
            AssertResolve(registry, "origin.native-born.farmer-child", "UnityIsekaiGame.Progression.OriginDefinition");
            AssertResolve(registry, "origin.summoned-otherworlder.accidental", "UnityIsekaiGame.Progression.OriginDefinition");
            AssertResolve(registry, "origin.reincarnated-local.scholar-mage", "UnityIsekaiGame.Progression.OriginDefinition");
            AssertResolve(registry, "birth-gift.sturdy-soul", "UnityIsekaiGame.Progression.BirthGiftDefinition");
            AssertResolve(registry, "birth-gift.latent-arcane-bolt", "UnityIsekaiGame.Progression.BirthGiftDefinition");
            AssertResolve(registry, "role.commoner", "UnityIsekaiGame.Progression.RoleDefinition");
            AssertResolve(registry, "role.noble", "UnityIsekaiGame.Progression.RoleDefinition");
            AssertResolve(registry, "social-status.citizen", "UnityIsekaiGame.Progression.SocialStatusDefinition");
            AssertResolve(registry, "social-status.wanted", "UnityIsekaiGame.Progression.SocialStatusDefinition");
            AssertResolve(registry, "currency.gold", "UnityIsekaiGame.Progression.CurrencyDefinition");
            AssertResolve(registry, "title.lord", "UnityIsekaiGame.Progression.TitleDefinition");
            AssertResolve(registry, "overall-level.prototype", "UnityIsekaiGame.Progression.OverallLevelConfiguration");

            Assert.That(registry.Contains("Gold"), Is.False);
            Assert.That(registry.Contains("NativeFarmer"), Is.False);
            Assert.That(registry.Contains("origin.NativeFarmer"), Is.False);
        }

        [Test]
        public void OriginAssignmentIsOnceOnlyAndAppliesStartingRewardsOnce()
        {
            DefinitionRegistry registry = LoadRegistry();
            Component progression = CreateProgression(registry);
            object family = Definition(registry, "origin-family.native-born");
            object origin = Definition(registry, "origin.native-born.farmer-child");
            object gift = Definition(registry, "birth-gift.sturdy-soul");
            object generated = InvokeStatic(
                RequiredType("UnityIsekaiGame.Progression.CharacterOriginGenerationResult"),
                "Success",
                family,
                origin,
                gift,
                true,
                33L);

            object assigned = Invoke(progression, "AssignGeneratedOrigin", generated, 1234, "test", false);

            Assert.That(GetProperty<bool>(assigned, "Succeeded"), Is.True, GetProperty<string>(assigned, "Message"));
            string saveJson = JsonUtility.ToJson(Invoke(progression, "CreateSaveData"));
            Assert.That(saveJson, Does.Contain("\"assigned\":true"));
            Assert.That(saveJson, Does.Contain("\"currencyDefinitionId\":\"currency.gold\""));
            Assert.That(saveJson, Does.Contain("\"amount\":33"));
            Assert.That(saveJson, Does.Contain("\"roleDefinitionId\":\"role.commoner\""));
            Assert.That(saveJson, Does.Contain("\"socialStatusDefinitionId\":\"social-status.citizen\""));
            Assert.That(saveJson, Does.Contain("\"definitionId\":\"origin.native-born.farmer-child\""));
            Assert.That(saveJson, Does.Contain("\"definitionId\":\"birth-gift.sturdy-soul\""));

            object duplicate = Invoke(progression, "AssignGeneratedOrigin", generated, 5678, "test", false);

            Assert.That(GetProperty<bool>(duplicate, "Succeeded"), Is.False);
            Assert.That(GetProperty<string>(duplicate, "Code"), Is.EqualTo("OriginAlreadyAssigned"));
            Assert.That(JsonUtility.ToJson(Invoke(progression, "CreateSaveData")), Does.Contain("\"amount\":33"));
            Assert.That(JsonUtility.ToJson(Invoke(progression, "CreateSaveData")), Does.Not.Contain("\"amount\":999"));
        }

        [Test]
        public void RoleConflictRequiresExplicitAcceptanceAndPreservesHistory()
        {
            DefinitionRegistry registry = LoadRegistry();
            Component progression = CreateProgression(registry);
            object commoner = Definition(registry, "role.commoner");
            object noble = Definition(registry, "role.noble");

            object commonerResult = Invoke(progression, "AddRole", commoner, "development", string.Empty, false, false, false);
            Assert.That(GetProperty<bool>(commonerResult, "Succeeded"), Is.True, GetProperty<string>(commonerResult, "Message"));

            object rejected = Invoke(progression, "AddRole", noble, "development", string.Empty, false, false, false);
            Assert.That(GetProperty<bool>(rejected, "Succeeded"), Is.False);
            Assert.That(GetProperty<string>(rejected, "Code"), Is.EqualTo("RoleConflict"));

            object accepted = Invoke(progression, "AddRole", noble, "development", string.Empty, false, true, false);
            Assert.That(GetProperty<bool>(accepted, "Succeeded"), Is.True, GetProperty<string>(accepted, "Message"));
            string saveJson = JsonUtility.ToJson(Invoke(progression, "CreateSaveData"));
            Assert.That(saveJson, Does.Contain("\"roleDefinitionId\":\"role.commoner\""));
            Assert.That(saveJson, Does.Contain("\"lifecycleState\":3"));
            Assert.That(saveJson, Does.Contain("\"roleDefinitionId\":\"role.noble\""));
            Assert.That(saveJson, Does.Contain("\"lifecycleState\":0"));
        }

        [Test]
        public void IdentityProgressionParticipantRejectsWrongPlayerPayload()
        {
            DefinitionRegistry registry = LoadRegistry();
            Component progression = CreateProgression(registry);
            Type participantType = RequiredType("UnityIsekaiGame.Progression.PlayerIdentityProgressionPersistenceParticipant");
            Func<DefinitionRegistry> provider = () => registry;
            object participant = Activator.CreateInstance(
                participantType,
                progression,
                provider,
                PersistenceService.LocalPlayerId,
                PersistenceService.LocalAccountId);
            object saveData = Invoke(progression, "CreateSaveData");
            saveData.GetType().GetField("playerId").SetValue(saveData, "other-player");

            PersistenceParticipantPrepareResult prepare = (PersistenceParticipantPrepareResult)Invoke(
                participant,
                "PreparePayload",
                JsonUtility.ToJson(saveData),
                GetStatic<int>(participantType, "CurrentParticipantSchemaVersion"));

            Assert.That(prepare.Succeeded, Is.False);
            Assert.That(prepare.Message, Does.Contain("does not match participant owner"));
        }

        private static DefinitionRegistry LoadRegistry()
        {
            DefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(PrototypeCatalogPath);
            Assert.That(catalog, Is.Not.Null, "Prototype catalog failed to load.");
            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(catalog);
            Assert.That(report.ErrorCount, Is.EqualTo(0), report.ToString());
            return catalog.CreateRegistry();
        }

        private Component CreateProgression(DefinitionRegistry registry)
        {
            runtimeRoot = new GameObject("Identity Progression Test Runtime");
            Component stats = runtimeRoot.AddComponent(RequiredType("UnityIsekaiGame.Stats.ActorStats"));
            Component progression = runtimeRoot.AddComponent(RequiredType("UnityIsekaiGame.Progression.PlayerIdentityProgression"));
            Invoke(progression, "ConfigureIdentity", PersistenceService.LocalAccountId, PersistenceService.LocalPlayerId, "person.test");
            Invoke(progression, "ConfigureRuntimeReferences", stats, null, null, Definition(registry, "overall-level.prototype"));
            Invoke(progression, "RegisterDefinitionCache", registry);
            object[] validateArgs = { null };
            bool valid = (bool)progression.GetType().GetMethod("ValidateIdentity").Invoke(progression, validateArgs);
            Assert.That(valid, Is.True, validateArgs[0] as string);
            return progression;
        }

        private static object Definition(DefinitionRegistry registry, string id)
        {
            Assert.That(registry.TryGet(id, out IGameDefinition definition), Is.True, id);
            return definition;
        }

        private static void AssertResolve(DefinitionRegistry registry, string id, string expectedType)
        {
            Assert.That(registry.TryGet(id, out IGameDefinition definition), Is.True, id);
            Assert.That(definition.GetType().FullName, Is.EqualTo(expectedType), id);
        }

        private static Type RequiredType(string typeName)
        {
            Type type = TestTypeResolver.RequiredType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, args);
            Assert.That(method, Is.Not.Null, $"{target.GetType().FullName}.{methodName}");
            return method.Invoke(target, args);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(type, methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, args);
            Assert.That(method, Is.Not.Null, $"{type.FullName}.{methodName}");
            return method.Invoke(null, args);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName).GetValue(target);
        }

        private static T GetStatic<T>(Type type, string fieldName)
        {
            return (T)type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, object[] args)
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool matches = true;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                    {
                        continue;
                    }

                    if (!parameters[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
