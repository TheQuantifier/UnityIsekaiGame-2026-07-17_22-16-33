using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.CharacterSystem;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Abilities
{
    [CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Unity Isekai Game/Abilities/Effects/Damage")]
    public sealed class DamageEffectDefinition : EffectDefinition
    {
        [SerializeField, Min(0f)] private float baseAmount = 10f;
        [SerializeField] private DamageType damageType = DamageType.Magic;
        [SerializeField] private DamageTypeDefinition typedDamageType;
        [SerializeField] private DamageComponentDefinition[] typedComponents;
        [SerializeField] private AttackPowerScalingPolicy attackPowerScaling = AttackPowerScalingPolicy.IgnoreSourceAttackPower;

        public float BaseAmount => baseAmount;
        public DamageType DamageType => damageType;
        public DamageTypeDefinition TypedDamageType => typedDamageType;
        public IReadOnlyList<DamageComponentDefinition> TypedComponents => typedComponents ?? System.Array.Empty<DamageComponentDefinition>();
        public AttackPowerScalingPolicy AttackPowerScaling => attackPowerScaling;

        private void OnValidate()
        {
            baseAmount = Mathf.Max(0f, baseAmount);
        }

        public override EffectExecutionResult CanExecute(in EffectExecutionContext context)
        {
            if (baseAmount <= 0f)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"{DisplayName} has no positive damage amount.");
            }

            if (context.Target == null)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.InvalidTarget, $"{DisplayName} has no target.");
            }

            if (attackPowerScaling == AttackPowerScalingPolicy.AddSourceAttackPower &&
                !CombatStatUtility.TryGetAttackPower(context.Source, out _))
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{DisplayName} requires a source with attack power.");
            }

            return CanUseDamagePipeline(in context) || context.Target.GetComponentInParent<IDamageable>() != null
                ? EffectExecutionResult.Success($"{DisplayName} can damage target.")
                : EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{context.Target.name} cannot take damage.");
        }

        public override EffectExecutionResult Execute(in EffectExecutionContext context)
        {
            EffectExecutionResult canExecute = CanExecute(in context);
            if (!canExecute.Succeeded)
            {
                return canExecute;
            }

            if (TryExecuteDamagePipeline(in context, out EffectExecutionResult pipelineResult))
            {
                return pipelineResult;
            }

            DamagePacket packet = CreateDamagePacket(in context, out float amount);
            DamageInfo damageInfo = new DamageInfo(amount, context.Source, context.TargetPosition, context.Direction, damageType, packet);
            DamageResult damageResult = context.Target.GetComponentInParent<IDamageable>().ApplyDamage(in damageInfo);
            return damageResult.Applied
                ? EffectExecutionResult.Success(damageResult.Message, damageResult.AppliedAmount)
                : EffectExecutionResult.Failure(EffectExecutionStatus.BlockedOrImmune, damageResult.Message);
        }

        private bool CanUseDamagePipeline(in EffectExecutionContext context)
        {
            if (typedDamageType == null || typedComponents != null && typedComponents.Length > 0)
            {
                return false;
            }

            return context.Target.GetComponentInParent<CharacterResourceCollection>() != null
                && !string.IsNullOrWhiteSpace(ResolveActorId(context.Target));
        }

        private bool TryExecuteDamagePipeline(in EffectExecutionContext context, out EffectExecutionResult result)
        {
            result = default;
            if (!CanUseDamagePipeline(in context))
            {
                return false;
            }

            float scaledBaseAmount = baseAmount * Mathf.Max(0f, context.MagnitudeMultiplier);
            float amount = CombatStatUtility.CalculatePreMitigationDamage(scaledBaseAmount, context.Source, attackPowerScaling);
            DamageHealingService service = new DamageHealingService();
            DamageApplicationRequest request = new DamageApplicationRequest(
                string.Empty,
                ResolveActorId(context.Source),
                context.Source,
                ResolveActorId(context.Target),
                context.Target,
                typedDamageType,
                amount,
                DisplayName);
            DamageApplicationResult damageResult = service.ApplyDamage(request);
            result = damageResult.Succeeded && damageResult.HealthChanged
                ? EffectExecutionResult.Success(damageResult.Message, damageResult.FinalDamageAmount)
                : damageResult.Succeeded
                    ? EffectExecutionResult.Failure(EffectExecutionStatus.BlockedOrImmune, damageResult.Message)
                    : EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, damageResult.Message);
            return true;
        }

        private static string ResolveActorId(GameObject actor)
        {
            if (actor == null)
            {
                return string.Empty;
            }

            CharacterSystemCoordinator character = actor.GetComponentInParent<CharacterSystemCoordinator>();
            if (character != null && !string.IsNullOrWhiteSpace(character.ActorId))
            {
                return character.ActorId;
            }

            WorldEntityIdentity identity = actor.GetComponentInParent<WorldEntityIdentity>();
            return identity == null ? string.Empty : identity.EntityId;
        }

        public override void ValidateDefinition(UnityIsekaiGame.GameData.DefinitionValidationReport report)
        {
            base.ValidateDefinition(report);
            if (baseAmount <= 0f)
            {
                report?.AddError($"Damage effect '{DisplayName}' must have a positive base amount.");
            }
        }

        public override void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            ValidateDefinition(report);

            bool hasTypedComponents = typedComponents != null && typedComponents.Length > 0;
            if (typedDamageType != null && hasTypedComponents)
            {
                report.AddWarning($"Damage effect '{DisplayName}' has both a typed damage type and typed components; typed components are authoritative.");
            }

            if (typedDamageType != null)
            {
                ValidateDamageTypeReference(typedDamageType, definitionsById, report);
            }

            if (typedComponents == null)
            {
                return;
            }

            HashSet<string> seenComponents = new HashSet<string>();
            for (int i = 0; i < typedComponents.Length; i++)
            {
                DamageComponentDefinition component = typedComponents[i];
                if (component == null)
                {
                    report.AddError($"Damage effect '{DisplayName}' has a null typed damage component at index {i}.");
                    continue;
                }

                if (component.DamageType == null)
                {
                    report.AddError($"Damage effect '{DisplayName}' has a typed component with no damage type at index {i}.");
                    continue;
                }

                if (component.BaseAmount <= 0f)
                {
                    report.AddWarning($"Damage effect '{DisplayName}' has zero damage for '{component.DamageType.Id}' at index {i}.");
                }

                if (!seenComponents.Add(component.DamageType.Id))
                {
                    report.AddWarning($"Damage effect '{DisplayName}' has multiple components for '{component.DamageType.Id}'.");
                }

                ValidateDamageTypeReference(component.DamageType, definitionsById, report);
            }
        }

        private DamagePacket CreateDamagePacket(in EffectExecutionContext context, out float totalAmount)
        {
            List<DamageComponent> components = new List<DamageComponent>();
            totalAmount = 0f;
            if (typedComponents != null && typedComponents.Length > 0)
            {
                for (int i = 0; i < typedComponents.Length; i++)
                {
                    DamageComponentDefinition componentDefinition = typedComponents[i];
                    if (componentDefinition == null)
                    {
                        continue;
                    }

                    DamageComponent component = componentDefinition.CreateRuntimeComponent(context.Source, context.MagnitudeMultiplier);
                    if (component.IsValid)
                    {
                        components.Add(component);
                        totalAmount += component.Amount;
                    }
                }
            }
            else
            {
                float scaledBaseAmount = baseAmount * Mathf.Max(0f, context.MagnitudeMultiplier);
                float amount = CombatStatUtility.CalculatePreMitigationDamage(scaledBaseAmount, context.Source, attackPowerScaling);
                DamageComponent component = typedDamageType == null
                    ? DamageComponent.Legacy(damageType, amount, attackPowerScaling)
                    : new DamageComponent(typedDamageType, amount, attackPowerScaling);
                components.Add(component);
                totalAmount = amount;
            }

            return new DamagePacket(context.Source, context.Ability, null, components);
        }

        private static void ValidateDamageTypeReference(DamageTypeDefinition damageTypeDefinition, IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (damageTypeDefinition == null || report == null)
            {
                return;
            }

            if (definitionsById == null
                || !definitionsById.TryGetValue(damageTypeDefinition.Id, out IGameDefinition found)
                || found is not DamageTypeDefinition)
            {
                report.AddError($"Damage effect references damage type '{damageTypeDefinition.Id}', which is not in the configured catalog.");
            }
        }
    }
}
