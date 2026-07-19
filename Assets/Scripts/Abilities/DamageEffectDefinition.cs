using UnityEngine;
using UnityIsekaiGame.Combat;

namespace UnityIsekaiGame.Abilities
{
    [CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Unity Isekai Game/Abilities/Effects/Damage")]
    public sealed class DamageEffectDefinition : EffectDefinition
    {
        [SerializeField, Min(0f)] private float baseAmount = 10f;
        [SerializeField] private DamageType damageType = DamageType.Magic;
        [SerializeField] private AttackPowerScalingPolicy attackPowerScaling = AttackPowerScalingPolicy.IgnoreSourceAttackPower;

        public float BaseAmount => baseAmount;
        public DamageType DamageType => damageType;
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

            return context.Target.GetComponentInParent<IDamageable>() == null
                ? EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{context.Target.name} cannot take damage.")
                : EffectExecutionResult.Success($"{DisplayName} can damage target.");
        }

        public override EffectExecutionResult Execute(in EffectExecutionContext context)
        {
            EffectExecutionResult canExecute = CanExecute(in context);
            if (!canExecute.Succeeded)
            {
                return canExecute;
            }

            float scaledBaseAmount = baseAmount * Mathf.Max(0f, context.MagnitudeMultiplier);
            float amount = CombatStatUtility.CalculatePreMitigationDamage(scaledBaseAmount, context.Source, attackPowerScaling);
            DamageInfo damageInfo = new DamageInfo(amount, context.Source, context.TargetPosition, context.Direction, damageType);
            DamageResult damageResult = context.Target.GetComponentInParent<IDamageable>().ApplyDamage(in damageInfo);
            return damageResult.Applied
                ? EffectExecutionResult.Success(damageResult.Message, damageResult.AppliedAmount)
                : EffectExecutionResult.Failure(EffectExecutionStatus.BlockedOrImmune, damageResult.Message);
        }

        public override void ValidateDefinition(UnityIsekaiGame.GameData.DefinitionValidationReport report)
        {
            base.ValidateDefinition(report);
            if (baseAmount <= 0f)
            {
                report?.AddError($"Damage effect '{DisplayName}' must have a positive base amount.");
            }
        }
    }
}
