using UnityEngine;
using UnityIsekaiGame.CharacterSystem;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Abilities
{
    [CreateAssetMenu(fileName = "NewRestoreVitalEffect", menuName = "Unity Isekai Game/Abilities/Effects/Restore Vital")]
    public sealed class RestoreVitalEffectDefinition : EffectDefinition
    {
        [SerializeField] private VitalType vitalType = VitalType.Health;
        [SerializeField, Min(0f)] private float amount = 25f;

        public VitalType VitalType => vitalType;
        public float Amount => amount;

        private void OnValidate()
        {
            amount = Mathf.Max(0f, amount);
        }

        public override EffectExecutionResult CanExecute(in EffectExecutionContext context)
        {
            if (amount <= 0f)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"{DisplayName} has no positive restore amount.");
            }

            if (context.Target == null)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.InvalidTarget, $"{DisplayName} has no target.");
            }

            return vitalType switch
            {
                VitalType.Health => CanRestoreHealth(context.Target),
                VitalType.Mana => CanRestoreMana(context.Target),
                VitalType.Stamina => CanRestoreStamina(context.Target),
                _ => EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"{DisplayName} has an invalid vital type.")
            };
        }

        public override EffectExecutionResult Execute(in EffectExecutionContext context)
        {
            EffectExecutionResult canExecute = CanExecute(in context);
            if (!canExecute.Succeeded)
            {
                return canExecute;
            }

            return vitalType switch
            {
                VitalType.Health => RestoreHealth(context.Target, context.MagnitudeMultiplier),
                VitalType.Mana => RestoreMana(context.Target, context.MagnitudeMultiplier),
                VitalType.Stamina => RestoreStamina(context.Target, context.MagnitudeMultiplier),
                _ => EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"{DisplayName} has an invalid vital type.")
            };
        }

        public override void ValidateDefinition(UnityIsekaiGame.GameData.DefinitionValidationReport report)
        {
            base.ValidateDefinition(report);
            if (amount <= 0f)
            {
                report?.AddError($"Restore effect '{DisplayName}' must have a positive amount.");
            }
        }

        private EffectExecutionResult CanRestoreHealth(GameObject target)
        {
            if (CanUseHealingPipeline(target))
            {
                HealingApplicationResult preview = new DamageHealingService().PreviewHealing(CreateHealingRequest(target, amount, string.Empty));
                if (preview.Succeeded)
                {
                    return preview.FinalHealingAmount > CharacterResourceCollection.Epsilon
                        ? EffectExecutionResult.Success("Health can be restored.")
                        : EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, "Health is already full.");
                }
            }

            PlayerHealth health = target.GetComponentInParent<PlayerHealth>();
            if (health == null)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{target.name} has no health component.");
            }

            return health.IsAtMaximum
                ? EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, "Health is already full.")
                : EffectExecutionResult.Success("Health can be restored.");
        }

        private EffectExecutionResult CanRestoreMana(GameObject target)
        {
            PlayerMana mana = target.GetComponentInParent<PlayerMana>();
            if (mana == null)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{target.name} has no mana component.");
            }

            return mana.CurrentMana >= mana.MaximumMana
                ? EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, "Mana is already full.")
                : EffectExecutionResult.Success("Mana can be restored.");
        }

        private EffectExecutionResult CanRestoreStamina(GameObject target)
        {
            PlayerStamina stamina = target.GetComponentInParent<PlayerStamina>();
            if (stamina == null)
            {
                return EffectExecutionResult.Failure(EffectExecutionStatus.UnsupportedTarget, $"{target.name} has no stamina component.");
            }

            return stamina.CurrentStamina >= stamina.MaximumStamina
                ? EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, "Stamina is already full.")
                : EffectExecutionResult.Success("Stamina can be restored.");
        }

        private EffectExecutionResult RestoreHealth(GameObject target, float multiplier)
        {
            if (CanUseHealingPipeline(target))
            {
                float restoreAmount = amount * Mathf.Max(0f, multiplier);
                HealingApplicationResult healingResult = new DamageHealingService().ApplyHealing(CreateHealingRequest(target, restoreAmount, DisplayName));
                return healingResult.Succeeded && healingResult.HealthChanged
                    ? EffectExecutionResult.Success(healingResult.Message, healingResult.FinalHealingAmount)
                    : EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, healingResult.Message);
            }

            PlayerHealth health = target.GetComponentInParent<PlayerHealth>();
            int healed = health.Heal(Mathf.RoundToInt(amount * Mathf.Max(0f, multiplier)));
            return healed > 0
                ? EffectExecutionResult.Success($"Restored {healed} health.", healed)
                : EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, "Health is already full.");
        }

        private EffectExecutionResult RestoreMana(GameObject target, float multiplier)
        {
            VitalChangeResult result = target.GetComponentInParent<PlayerMana>().Restore(amount * Mathf.Max(0f, multiplier));
            return result.Succeeded
                ? EffectExecutionResult.Success(result.Message, result.ChangedAmount)
                : EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, result.Message);
        }

        private EffectExecutionResult RestoreStamina(GameObject target, float multiplier)
        {
            VitalChangeResult result = target.GetComponentInParent<PlayerStamina>().Restore(amount * Mathf.Max(0f, multiplier));
            return result.Succeeded
                ? EffectExecutionResult.Success(result.Message, result.ChangedAmount)
                : EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, result.Message);
        }

        private static bool CanUseHealingPipeline(GameObject target)
        {
            return target != null
                && target.GetComponentInParent<CharacterResourceCollection>() != null
                && !string.IsNullOrWhiteSpace(ResolveActorId(target));
        }

        private static HealingApplicationRequest CreateHealingRequest(GameObject target, float restoreAmount, string reason)
        {
            return new HealingApplicationRequest(
                string.Empty,
                ResolveActorId(target),
                target,
                ResolveActorId(target),
                target,
                restoreAmount,
                reason);
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
    }
}
