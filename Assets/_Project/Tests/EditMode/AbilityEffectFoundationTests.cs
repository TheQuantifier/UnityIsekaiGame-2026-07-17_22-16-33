using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class AbilityEffectFoundationTests
    {
        [Test]
        public void CooldownTracker_StartRejectsAndResetClearsCooldown()
        {
            ScriptableObject ability = CreateAbility("ability.test-cooldown", 2f);
            object tracker = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.AbilityCooldownTracker"));

            Invoke(tracker, "StartCooldown", ability, 10f);
            object[] args = { ability, 11f, 0f };
            bool onCooldown = (bool)tracker.GetType().GetMethod("IsOnCooldown").Invoke(tracker, args);

            Assert.That(onCooldown, Is.True);
            Assert.That((float)args[2], Is.EqualTo(1f));

            Invoke(tracker, "Reset");
            args = new object[] { ability, 11f, 0f };
            Assert.That((bool)tracker.GetType().GetMethod("IsOnCooldown").Invoke(tracker, args), Is.False);
        }

        [Test]
        public void ResourceValidation_DoesNotSpendManaWhenTargetValidationFails()
        {
            ScriptableObject effect = CreateDamageEffect("effect.test-damage", 5f);
            ScriptableObject ability = CreateAbility("ability.test-invalid-target", 0f, 20f, effect);
            GameObject source = new GameObject("Source");
            Component mana = source.AddComponent(RequiredType("UnityIsekaiGame.Gameplay.PlayerMana"));
            Invoke(mana, "RestoreToMaximum");
            GameObject target = new GameObject("Invalid Target");
            object context = CreateAbilityContext(ability, source, target);
            object tracker = Activator.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.AbilityCooldownTracker"));

            Assert.That(Get<float>(mana, "CurrentMana"), Is.EqualTo(100f));

            object result = InvokeStatic("UnityIsekaiGame.Abilities.AbilityExecutor", "Execute", context, tracker, 0f);

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            Assert.That(Get<float>(mana, "CurrentMana"), Is.EqualTo(100f));

            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void RestoreVitalEffect_FailsWhenHealthIsFull()
        {
            ScriptableObject restore = CreateRestoreEffect("effect.test-restore", 25f);
            GameObject target = new GameObject("Target");
            target.AddComponent(RequiredType("UnityIsekaiGame.Gameplay.PlayerHealth"));
            object context = CreateEffectContext(null, target, target);

            object result = Invoke(restore, "CanExecute", context);

            Assert.That(Get<bool>(result, "Succeeded"), Is.False);
            Assert.That(Get<object>(result, "Status").ToString(), Is.EqualTo("NoStateChange"));

            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void DefinitionValidation_FlagsAbilityWithNoEffects()
        {
            ScriptableObject ability = CreateAbility("ability.no-effects", 0f);
            DefinitionValidationReport report = DefinitionCatalogValidator.Validate(ClassificationTestFactory.CreateCatalog(ability));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.GetSummary(), Does.Contain("has no effects"));
        }

        [Test]
        public void SpellDefinition_CanReferenceAbilityAdapter()
        {
            ScriptableObject effect = CreateDamageEffect("effect.spell-adapter", 5f);
            ScriptableObject ability = CreateAbility("ability.spell-adapter", 0.5f, 10f, effect);
            ScriptableObject spell = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.Magic.SpellDefinition"));
            SerializedObject serializedSpell = new SerializedObject(spell);
            serializedSpell.FindProperty("spellId").stringValue = "spell.adapter";
            serializedSpell.FindProperty("displayName").stringValue = "Adapter";
            serializedSpell.FindProperty("ability").objectReferenceValue = ability;
            serializedSpell.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(Get<object>(spell, "Ability"), Is.SameAs(ability));
        }

        private static ScriptableObject CreateAbility(string id, float cooldown, float manaCost = 0f, ScriptableObject effect = null)
        {
            ScriptableObject ability = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.AbilityDefinition"));
            SerializedObject serializedAbility = new SerializedObject(ability);
            serializedAbility.FindProperty("abilityId").stringValue = id;
            serializedAbility.FindProperty("displayName").stringValue = id;
            serializedAbility.FindProperty("cooldownDuration").floatValue = cooldown;
            serializedAbility.FindProperty("targetingMode").enumValueIndex = 1;
            serializedAbility.FindProperty("deliveryMode").enumValueIndex = 0;

            SerializedProperty costs = serializedAbility.FindProperty("resourceCosts");
            costs.arraySize = manaCost > 0f ? 1 : 0;
            if (manaCost > 0f)
            {
                costs.GetArrayElementAtIndex(0).FindPropertyRelative("resourceType").enumValueIndex = 1;
                costs.GetArrayElementAtIndex(0).FindPropertyRelative("amount").floatValue = manaCost;
            }

            SerializedProperty effects = serializedAbility.FindProperty("effects");
            effects.arraySize = effect == null ? 0 : 1;
            if (effect != null)
            {
                effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            }

            serializedAbility.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }

        private static ScriptableObject CreateDamageEffect(string id, float amount)
        {
            ScriptableObject effect = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.DamageEffectDefinition"));
            SerializedObject serializedEffect = new SerializedObject(effect);
            serializedEffect.FindProperty("effectId").stringValue = id;
            serializedEffect.FindProperty("displayName").stringValue = id;
            serializedEffect.FindProperty("baseAmount").floatValue = amount;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static ScriptableObject CreateRestoreEffect(string id, float amount)
        {
            ScriptableObject effect = ScriptableObject.CreateInstance(RequiredType("UnityIsekaiGame.Abilities.RestoreVitalEffectDefinition"));
            SerializedObject serializedEffect = new SerializedObject(effect);
            serializedEffect.FindProperty("effectId").stringValue = id;
            serializedEffect.FindProperty("displayName").stringValue = id;
            serializedEffect.FindProperty("vitalType").enumValueIndex = 0;
            serializedEffect.FindProperty("amount").floatValue = amount;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static object CreateAbilityContext(ScriptableObject ability, GameObject source, GameObject target)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Abilities.AbilityExecutionContext"),
                ability, source, target, source.transform, source.transform.position, target.transform.position, source.transform.forward, false, null, null, 1f, null);
        }

        private static object CreateEffectContext(ScriptableObject ability, GameObject source, GameObject target)
        {
            return Activator.CreateInstance(
                RequiredType("UnityIsekaiGame.Abilities.EffectExecutionContext"),
                ability, source, target, source.transform.position, target.transform.position, source.transform.forward, null, null, 1f);
        }

        private static Type RequiredType(string fullName)
        {
            Type type = TestTypeResolver.RequiredType(fullName);
            Assert.That(type, Is.Not.Null, $"Expected runtime type {fullName} to exist in loaded project assemblies.");
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

        private static T Get<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName).GetValue(target);
        }
    }
}
