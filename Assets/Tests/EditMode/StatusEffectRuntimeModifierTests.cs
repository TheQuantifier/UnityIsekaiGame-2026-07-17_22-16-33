using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class StatusEffectRuntimeModifierTests
    {
        [Test]
        public void RuntimeStatAggregation_AppliesFlatThenPercent()
        {
            object collection = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Stats.RuntimeStatCollection"));
            object attack = EnumValue("UnityIsekaiGame.Stats.StatType", "AttackPower");
            object flat = EnumValue("UnityIsekaiGame.Stats.StatModifierOperation", "FlatAdd");
            object percent = EnumValue("UnityIsekaiGame.Stats.StatModifierOperation", "PercentAdd");
            object sourceA = CreateSource("Equipment", "equipment.slot.MainHand");
            object sourceB = CreateSource("StatusEffect", "status-1");

            Invoke(collection, "SetBaseValue", attack, 10f);
            Invoke(collection, "AddModifier", CreateRuntimeModifier(attack, flat, 5f, sourceA));
            Invoke(collection, "AddModifier", CreateRuntimeModifier(attack, percent, 0.2f, sourceB));

            Assert.That((float)Invoke(collection, "GetValue", attack), Is.EqualTo(18f).Within(0.001f));
        }

        [Test]
        public void PlayerStats_GettersInitializeBaseVitalsBeforeAwake()
        {
            GameObject target = new GameObject("Stats Target");
            Component stats = target.AddComponent(RequiredType("UnityIsekaiGame.Equipment.PlayerStats"));

            Assert.That(Get<float>(stats, "MaximumHealth"), Is.EqualTo(100f));
            Assert.That(Get<float>(stats, "MaximumStamina"), Is.EqualTo(100f));
            Assert.That(Get<float>(stats, "MaximumMana"), Is.EqualTo(100f));

            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void TimedStatus_ExpiresAndRemovesModifier()
        {
            GameObject target = CreateStatTarget("Target");
            ScriptableObject status = CreateStatus("status.test-might", "Might", "RefreshDuration", 1f, 1, "AttackPower", "FlatAdd", 5f);
            object controller = target.GetComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));

            object result = Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.test", string.Empty, 0f, 0f));

            Assert.That(Get<bool>(result, "Succeeded"), Is.True);
            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(10f));

            Invoke(controller, "UpdateStatuses", 1.1f);

            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(5f));
            Assert.That(Get<int>(Get<object>(controller, "ActiveStatuses"), "Count"), Is.EqualTo(0));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void ExplicitRemoval_RemovesOnlyThatStatusModifier()
        {
            GameObject target = CreateStatTarget("Target");
            ScriptableObject might = CreateStatus("status.test-might", "Might", "IndependentInstances", 10f, 1, "AttackPower", "FlatAdd", 5f);
            ScriptableObject weak = CreateStatus("status.test-weak", "Weak", "IndependentInstances", 10f, 1, "AttackPower", "FlatAdd", -2f);
            object controller = target.GetComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));

            object mightResult = Invoke(controller, "ApplyStatus", CreateRequest(might, target, "source.might", "app-might", 0f, 0f));
            Invoke(controller, "ApplyStatus", CreateRequest(weak, target, "source.weak", "app-weak", 0f, 0f));

            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(8f));
            Invoke(controller, "RemoveStatus", Get<string>(Get<object>(mightResult, "StatusEffect"), "ApplicationId"));

            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(3f));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void StackingPolicy_AddStackCapsAtMaximum()
        {
            GameObject target = CreateStatTarget("Target");
            ScriptableObject status = CreateStatus("status.test-stack", "Stack", "AddStack", 10f, 2, "AttackPower", "FlatAdd", 2f);
            object controller = target.GetComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));

            object first = Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.stack", string.Empty, 0f, 0f));
            object second = Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.stack", string.Empty, 0f, 1f));
            object third = Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.stack", string.Empty, 0f, 2f));

            Assert.That(Get<bool>(first, "Succeeded"), Is.True);
            Assert.That(Get<bool>(second, "Succeeded"), Is.True);
            Assert.That(Get<bool>(third, "Succeeded"), Is.False);
            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(9f));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void RefreshPolicy_ResetsTimedDuration()
        {
            GameObject target = CreateStatTarget("Target");
            ScriptableObject status = CreateStatus("status.test-refresh", "Refresh", "RefreshDuration", 5f, 1, "AttackPower", "FlatAdd", 1f);
            object controller = target.GetComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));

            object first = Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.refresh", string.Empty, 0f, 0f));
            object runtime = Get<object>(first, "StatusEffect");
            Invoke(controller, "UpdateStatuses", 3f);
            Invoke(controller, "ApplyStatus", CreateRequest(status, target, "source.refresh", string.Empty, 0f, 3f));

            Assert.That(Get<float>(runtime, "RemainingDuration"), Is.EqualTo(5f).Within(0.001f));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void ApplyStatusEffectDefinition_FailsOnInvalidTarget()
        {
            ScriptableObject status = CreateStatus("status.test-target", "Target", "RefreshDuration", 5f, 1, "AttackPower", "FlatAdd", 1f);
            ScriptableObject effect = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.ApplyStatusEffectDefinition"));
            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("effectId").stringValue = "effect.apply-status";
            serialized.FindProperty("displayName").stringValue = "Apply Status";
            serialized.FindProperty("statusEffect").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject source = new GameObject("Source");
            GameObject invalidTarget = new GameObject("Invalid");
            object context = CreateEffectContext(null, source, invalidTarget);

            object result = Invoke(effect, "CanExecute", context);

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(invalidTarget);
        }

        [Test]
        public void Restore_DuplicateApplicationIdFailsAtomically()
        {
            GameObject target = CreateStatTarget("Target");
            ScriptableObject status = CreateStatus("status.test-restore", "Restore", "IndependentInstances", 10f, 1, "AttackPower", "FlatAdd", 2f);
            DefinitionRegistry registry = new DefinitionRegistry(new[] { (IGameDefinition)status });
            object controller = target.GetComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));
            Array saveEntries = Array.CreateInstance(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectSaveData"), 2);
            saveEntries.SetValue(CreateSaveEntry("status.test-restore", "dup-app", 5f, 1), 0);
            saveEntries.SetValue(CreateSaveEntry("status.test-restore", "dup-app", 5f, 1), 1);

            object result = InvokeStatic("UnityIsekaiGame.StatusEffects.StatusEffectRestoreUtility", "Restore", controller, saveEntries, registry, target, 0f);

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            Assert.That(GetStat(target, "AttackPower"), Is.EqualTo(5f));
            Assert.That(Get<int>(Get<object>(controller, "ActiveStatuses"), "Count"), Is.EqualTo(0));
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static GameObject CreateStatTarget(string name)
        {
            GameObject target = new GameObject(name);
            Component stats = target.AddComponent(RequiredType("UnityIsekaiGame.Equipment.PlayerStats"));
            InvokeNonPublic(stats, "Awake");
            Component controller = target.AddComponent(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectController"));
            InvokeNonPublic(controller, "Awake");
            return target;
        }

        private static ScriptableObject CreateStatus(string id, string displayName, string stackingPolicy, float duration, int maxStacks, string stat, string operation, float value)
        {
            ScriptableObject status = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectDefinition"));
            SerializedObject serialized = new SerializedObject(status);
            serialized.FindProperty("statusId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("durationModel").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusDurationModel", "Timed");
            serialized.FindProperty("defaultDuration").floatValue = duration;
            serialized.FindProperty("stackingPolicy").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusStackingPolicy", stackingPolicy);
            serialized.FindProperty("refreshPolicy").enumValueIndex = EnumIndex("UnityIsekaiGame.StatusEffects.StatusRefreshPolicy", "ResetToFullDuration");
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

        private static object CreateSaveEntry(string definitionId, string applicationId, float remainingDuration, int stackCount)
        {
            object entry = Activator.CreateInstance(RequiredType("UnityIsekaiGame.StatusEffects.StatusEffectSaveData"));
            SetField(entry, "statusDefinitionId", definitionId);
            SetField(entry, "applicationId", applicationId);
            SetField(entry, "sourceId", "source.restore");
            SetField(entry, "remainingDuration", remainingDuration);
            SetField(entry, "stackCount", stackCount);
            SetField(entry, "durationModel", EnumValue("UnityIsekaiGame.StatusEffects.StatusDurationModel", "Timed"));
            return entry;
        }

        private static object CreateEffectContext(ScriptableObject ability, GameObject source, GameObject target)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Abilities.EffectExecutionContext"),
                ability, source, target, source.transform.position, target.transform.position, source.transform.forward, null, null, 1f);
        }

        private static float GetStat(GameObject target, string stat)
        {
            object stats = target.GetComponent(RequiredType("UnityIsekaiGame.Equipment.PlayerStats"));
            return (float)Invoke(stats, "GetStatValue", EnumValue("UnityIsekaiGame.Stats.StatType", stat));
        }

        private static object CreateSource(string sourceType, string sourceId)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Stats.StatModifierSource"),
                EnumValue("UnityIsekaiGame.Stats.StatModifierSourceType", sourceType),
                sourceId);
        }

        private static object CreateRuntimeModifier(object stat, object operation, float value, object source)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Stats.RuntimeStatModifier"),
                stat, operation, value, source, 0);
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

        private static object InvokeStatic(string typeName, string methodName, params object[] args)
        {
            return RequiredType(typeName).GetMethod(methodName).Invoke(null, args);
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
    }
}
