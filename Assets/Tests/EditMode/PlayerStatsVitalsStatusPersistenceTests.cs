using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.Tests
{
    public sealed class PlayerStatsVitalsStatusPersistenceTests
    {
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(Path.GetTempPath(), "UnityIsekaiGameStatsVitalsStatusPersistenceTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(testRoot) && Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [Test]
        public void ParticipantDeclaresPlayerScopeOwnerAndStableIdentity()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            object participant = CreateParticipant(fixture);

            Assert.That(Get<string>(participant, "ParticipantKey"), Is.EqualTo("player.stats-vitals-status"));
            Assert.That(Get<int>(participant, "ParticipantSchemaVersion"), Is.EqualTo(1));
            Assert.That(Get<bool>(participant, "IsRequired"), Is.True);
            Assert.That(Get<PersistenceScope>(participant, "Scope"), Is.EqualTo(PersistenceScope.Player));
            Assert.That(Get<string>(participant, "OwnerId"), Is.EqualTo(PersistenceService.LocalPlayerId));
            Assert.That(Get<PersistenceLoadPhase>(participant, "LoadPhase"), Is.EqualTo(PersistenceLoadPhase.Statuses));
        }

        [Test]
        public void SaveLoadRoundTripRestoresVitalsStatusAndModifiers()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            ScriptableObject might = CreateStatus("status.test-might", "Might", "SaveRemainingDuration", "Timed", 30f, 1, "AttackPower", "FlatAdd", 5f);
            fixture.SetRegistry(new IGameDefinition[] { (IGameDefinition)might });
            Invoke(fixture.Health, "Damage", 30);
            Invoke(fixture.Mana, "Spend", 20f);
            Invoke(fixture.Stamina, "Spend", 15f, "test");
            Invoke(fixture.StatusController, "ApplyStatus", CreateRequest(might, fixture.Root, "source.test", "app-might", 0f, 0f));

            PersistenceService service = CreateService(fixture);
            Assert.That(service.Save("slot-0001").Succeeded, Is.True);

            Invoke(fixture.Health, "Heal", 20);
            Invoke(fixture.Mana, "Restore", 20f);
            Invoke(fixture.Stamina, "Restore", 15f);
            Invoke(fixture.StatusController, "ClearAllStatuses");

            PersistenceLoadResult load = service.Load("slot-0001");

            Assert.That(load.Succeeded, Is.True, load.Message);
            Assert.That(Get<int>(fixture.Health, "CurrentHealth"), Is.EqualTo(70));
            Assert.That(Get<float>(fixture.Mana, "CurrentMana"), Is.EqualTo(80f).Within(0.001f));
            Assert.That(Get<float>(fixture.Stamina, "CurrentStamina"), Is.EqualTo(85f).Within(0.001f));
            Assert.That(Get<float>(fixture.Stats, "AttackPower"), Is.EqualTo(10f).Within(0.001f));
            object restored = FirstStatus(fixture.StatusController);
            Assert.That(Get<string>(restored, "ApplicationId"), Is.EqualTo("app-might"));
            Assert.That(Get<float>(restored, "RemainingDuration"), Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void CaptureExcludesDoNotSaveStatuses()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            ScriptableObject saved = CreateStatus("status.saved", "Saved", "SaveRemainingDuration", "Timed", 30f, 1, "AttackPower", "FlatAdd", 1f);
            ScriptableObject excluded = CreateStatus("status.excluded", "Excluded", "DoNotSave", "Timed", 30f, 1, "Defense", "FlatAdd", 1f);
            fixture.SetRegistry(new IGameDefinition[] { (IGameDefinition)saved, (IGameDefinition)excluded });
            object participant = CreateParticipant(fixture);
            Invoke(fixture.StatusController, "ApplyStatus", CreateRequest(saved, fixture.Root, "source.saved", "app-saved", 0f, 0f));
            Invoke(fixture.StatusController, "ApplyStatus", CreateRequest(excluded, fixture.Root, "source.excluded", "app-excluded", 0f, 0f));

            object capture = Invoke(participant, "CapturePayload");
            string payload = Get<string>(capture, "PayloadJson");

            Assert.That(Get<bool>(capture, "Succeeded"), Is.True);
            Assert.That(payload, Does.Contain("status.saved"));
            Assert.That(payload, Does.Not.Contain("status.excluded"));
        }

        [Test]
        public void PrepareRejectsDuplicateApplicationIdWithoutMutation()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            ScriptableObject might = CreateStatus("status.test-might", "Might", "SaveRemainingDuration", "Timed", 30f, 1, "AttackPower", "FlatAdd", 5f);
            fixture.SetRegistry(new IGameDefinition[] { (IGameDefinition)might });
            Invoke(fixture.StatusController, "ApplyStatus", CreateRequest(might, fixture.Root, "source.live", "app-live", 0f, 0f));
            object participant = CreateParticipant(fixture);
            object payload = CreatePayload(90, 90f, 90f);
            AddStatusEntry(payload, "status.test-might", "dup", "SaveRemainingDuration", "Timed", 10f, 1);
            AddStatusEntry(payload, "status.test-might", "dup", "SaveRemainingDuration", "Timed", 10f, 1);

            object result = Invoke(participant, "PreparePayload", JsonUtility.ToJson(payload), 1);

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            Assert.That(Get<float>(fixture.Stats, "AttackPower"), Is.EqualTo(10f).Within(0.001f));
            Assert.That(ActiveStatusCount(fixture.StatusController), Is.EqualTo(1));
        }

        [Test]
        public void PrepareRejectsUnsupportedSchemaVersionAndDefeatedHealth()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            object participant = CreateParticipant(fixture);
            object payload = CreatePayload(90, 90f, 90f);

            object future = Invoke(participant, "PreparePayload", JsonUtility.ToJson(payload), 2);
            SetField(payload, "schemaVersion", 0);
            object old = Invoke(participant, "PreparePayload", JsonUtility.ToJson(payload), 1);
            SetField(payload, "schemaVersion", 1);
            SetField(payload, "currentHealth", 0);
            object defeated = Invoke(participant, "PreparePayload", JsonUtility.ToJson(payload), 1);

            Assert.That(Get<bool>(future, "Succeeded"), Is.False);
            Assert.That(Get<bool>(old, "Succeeded"), Is.False);
            Assert.That(Get<bool>(defeated, "Succeeded"), Is.False);
        }

        [Test]
        public void LoadRejectsOwnerMismatchBeforeCommit()
        {
            using RuntimeFixture fixture = RuntimeFixture.Create();
            PersistenceService service = CreateService(fixture);
            Assert.That(service.Save("slot-0001").Succeeded, Is.True);

            SaveSlotPaths paths = Paths("slot-0001");
            GameSaveEnvelope envelope = JsonUtility.FromJson<GameSaveEnvelope>(File.ReadAllText(paths.PrimaryPath));
            envelope.participants[0].ownerId = "other-player";
            envelope.contentChecksum = PersistenceService.ComputeChecksum(envelope);
            File.WriteAllText(paths.PrimaryPath, JsonUtility.ToJson(envelope, true));
            Invoke(fixture.Health, "Damage", 25);

            PersistenceLoadResult load = service.Load("slot-0001");

            Assert.That(load.Succeeded, Is.False);
            Assert.That(load.Status, Is.EqualTo(PersistenceLoadStatus.ParticipantPrepareFailed));
            Assert.That(Get<int>(fixture.Health, "CurrentHealth"), Is.EqualTo(75));
        }

        private PersistenceService CreateService(RuntimeFixture fixture)
        {
            PersistenceService service = new PersistenceService(new PersistencePathProvider(testRoot));
            Assert.That(service.RegisterParticipant((IPersistenceParticipant)CreateParticipant(fixture), out string failureReason), Is.True, failureReason);
            return service;
        }

        private SaveSlotPaths Paths(string slotId)
        {
            PersistencePathProvider provider = new PersistencePathProvider(testRoot);
            Assert.That(provider.TryGetPaths(slotId, out SaveSlotPaths paths, out string failure), Is.True, failure);
            return paths;
        }

        private static object CreateParticipant(RuntimeFixture fixture)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Persistence.PlayerStatsVitalsStatusPersistenceParticipant"),
                fixture.Stats,
                fixture.Health,
                fixture.Mana,
                fixture.Stamina,
                fixture.StatusController,
                (Func<DefinitionRegistry>)(() => fixture.Registry),
                PersistenceService.LocalPlayerId);
        }

        private static object CreatePayload(int health, float mana, float stamina)
        {
            object payload = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Persistence.PlayerStatsVitalsStatusSaveData"));
            SetField(payload, "schemaVersion", 1);
            SetField(payload, "currentHealth", health);
            SetField(payload, "currentMana", mana);
            SetField(payload, "currentStamina", stamina);
            return payload;
        }

        private static void AddStatusEntry(object payload, string definitionId, string applicationId, string persistencePolicy, string durationModel, float remainingDuration, int stackCount)
        {
            IList statuses = (IList)payload.GetType().GetField("statuses").GetValue(payload);
            object entry = Activator.CreateInstance(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectSaveData"));
            SetField(entry, "statusDefinitionId", definitionId);
            SetField(entry, "applicationId", applicationId);
            SetField(entry, "sourceId", "source.restore");
            SetField(entry, "remainingDuration", remainingDuration);
            SetField(entry, "stackCount", stackCount);
            SetField(entry, "durationModel", EnumValue("UnityIsekaiGame.StatusEffects.StatusDurationModel", durationModel));
            SetField(entry, "persistencePolicy", EnumValue("UnityIsekaiGame.StatusEffects.StatusPersistencePolicy", persistencePolicy));
            statuses.Add(entry);
        }

        private static ScriptableObject CreateStatus(string id, string displayName, string persistencePolicy, string durationModel, float duration, int maxStacks, string stat, string operation, float value)
        {
            ScriptableObject status = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectDefinition"));
            SerializedObject serialized = new SerializedObject(status);
            serialized.FindProperty("statusId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("durationModel").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusDurationModel", durationModel);
            serialized.FindProperty("defaultDuration").floatValue = duration;
            serialized.FindProperty("stackingPolicy").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusStackingPolicy", "IndependentInstances");
            serialized.FindProperty("refreshPolicy").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusRefreshPolicy", "ResetToFullDuration");
            serialized.FindProperty("persistencePolicy").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusPersistencePolicy", persistencePolicy);
            serialized.FindProperty("maximumStacks").intValue = maxStacks;
            SerializedProperty modifiers = serialized.FindProperty("statModifiers");
            modifiers.arraySize = 1;
            SerializedProperty modifier = modifiers.GetArrayElementAtIndex(0);
            modifier.FindPropertyRelative("statType").enumValueIndex = EnumIndex("UnityIsekaiGame.Stats.StatType", stat);
            modifier.FindPropertyRelative("operation").enumValueIndex = EnumIndex("UnityIsekaiGame.Stats.StatModifierOperation", operation);
            modifier.FindPropertyRelative("value").floatValue = value;
            modifier.FindPropertyRelative("scaleWithStacks").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return status;
        }

        private static object CreateRequest(ScriptableObject status, GameObject target, string sourceId, string applicationId, float durationOverride, float now)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectApplicationRequest"),
                status, target, sourceId, durationOverride, applicationId, now);
        }

        private static object FirstStatus(Component controller)
        {
            foreach (object status in (IEnumerable)Get<object>(controller, "ActiveStatuses"))
            {
                return status;
            }

            return null;
        }

        private static int ActiveStatusCount(Component controller)
        {
            int count = 0;
            foreach (object _ in (IEnumerable)Get<object>(controller, "ActiveStatuses"))
            {
                count++;
            }

            return count;
        }

        private static object EnumValue(string fullName, string name)
        {
            return Enum.Parse(RequiredType(fullName), name);
        }

        private static int EnumIndex(string fullName, string name)
        {
            return Convert.ToInt32(EnumValue(fullName, name));
        }

        private static Type RequiredType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, $"Expected runtime type {fullName} to exist in Assembly-CSharp.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName).Invoke(target, args);
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, Array.Empty<object>());
        }

        private static T Get<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName).SetValue(target, value);
        }

        private sealed class RuntimeFixture : IDisposable
        {
            public GameObject Root { get; private set; }
            public Component Stats { get; private set; }
            public Component Health { get; private set; }
            public Component Mana { get; private set; }
            public Component Stamina { get; private set; }
            public Component StatusController { get; private set; }
            public DefinitionRegistry Registry { get; private set; } = new DefinitionRegistry(Array.Empty<IGameDefinition>());

            public static RuntimeFixture Create()
            {
                RuntimeFixture fixture = new RuntimeFixture();
                fixture.Root = new GameObject("Stats Vitals Status Persistence Test");
                fixture.Stats = fixture.Root.AddComponent(RequiredType("UnityIsekaiGame.Equipment.PlayerStats"));
                fixture.Health = fixture.Root.AddComponent(RequiredType("UnityIsekaiGame.Gameplay.PlayerHealth"));
                fixture.Mana = fixture.Root.AddComponent(RequiredType("UnityIsekaiGame.Gameplay.PlayerMana"));
                fixture.Stamina = fixture.Root.AddComponent(RequiredType("UnityIsekaiGame.Gameplay.PlayerStamina"));
                fixture.StatusController = fixture.Root.AddComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));
                InvokeNonPublic(fixture.Stats, "Awake");
                InvokeNonPublic(fixture.Stats, "OnEnable");
                InvokeNonPublic(fixture.Health, "Awake");
                InvokeNonPublic(fixture.Health, "OnEnable");
                InvokeNonPublic(fixture.Mana, "Awake");
                InvokeNonPublic(fixture.Mana, "OnEnable");
                InvokeNonPublic(fixture.Stamina, "Awake");
                InvokeNonPublic(fixture.Stamina, "OnEnable");
                InvokeNonPublic(fixture.StatusController, "Awake");
                return fixture;
            }

            public void SetRegistry(IEnumerable<IGameDefinition> definitions)
            {
                Registry = new DefinitionRegistry(definitions);
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }
        }
    }
}
