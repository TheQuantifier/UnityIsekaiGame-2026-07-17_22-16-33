using UnityEngine;
using UnityIsekaiGame.Magic;

namespace UnityIsekaiGame.Abilities
{
    public static class AbilityExecutor
    {
        public static AbilityExecutionResult Execute(in AbilityExecutionContext context, AbilityCooldownTracker cooldowns = null, float now = -1f)
        {
            AbilityExecutionResult validation = Validate(in context, cooldowns, now);
            if (!validation.Succeeded)
            {
                return validation;
            }

            AbilityExecutionResult costCommit = AbilityResourceCostProcessor.CommitCosts(context.Ability, context.Source);
            if (!costCommit.Succeeded)
            {
                return costCommit;
            }

            AbilityExecutionResult result = context.Ability.DeliveryMode == AbilityDeliveryMode.Projectile
                ? SpawnProjectile(in context)
                : ExecuteImmediate(in context);

            if (!result.Succeeded)
            {
                return result;
            }

            cooldowns?.StartCooldown(context.Ability, ResolveNow(now));
            return result;
        }

        public static AbilityExecutionResult Validate(in AbilityExecutionContext context, AbilityCooldownTracker cooldowns = null, float now = -1f)
        {
            if (context.Ability == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.MissingAbility, "Missing ability.");
            }

            if (context.Source == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.InvalidSource, "Missing ability source.");
            }

            if (context.GameplayBlocked)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.BlockedGameplayState, "Gameplay input is blocked.");
            }

            if (cooldowns != null && cooldowns.IsOnCooldown(context.Ability, ResolveNow(now), out _))
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.CooldownActive, $"{context.Ability.DisplayName} is on cooldown.");
            }

            AbilityExecutionResult configuration = ValidateConfiguration(in context);
            if (!configuration.Succeeded)
            {
                return configuration;
            }

            AbilityExecutionResult range = ValidateRange(in context);
            if (!range.Succeeded)
            {
                return range;
            }

            AbilityExecutionResult costs = AbilityResourceCostProcessor.ValidateCosts(context.Ability, context.Source);
            if (!costs.Succeeded)
            {
                return costs;
            }

            if (context.Ability.DeliveryMode == AbilityDeliveryMode.Immediate)
            {
                return ValidateEffects(in context);
            }

            return AbilityExecutionResult.Success("Ability can execute.");
        }

        public static AbilityExecutionResult ExecuteEffects(in EffectExecutionContext context, System.Collections.Generic.IReadOnlyList<EffectDefinition> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.NoEffects, "Ability has no effects.");
            }

            for (int i = 0; i < effects.Count; i++)
            {
                EffectDefinition effect = effects[i];
                if (effect == null)
                {
                    EffectExecutionResult missing = EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"Missing effect at index {i}.");
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectValidationFailure, missing.Message, i, missing);
                }

                EffectExecutionResult result = effect.Execute(in context);
                if (!result.Succeeded)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectExecutionFailure, result.Message, i, result);
                }
            }

            return AbilityExecutionResult.Success("Ability effects executed.");
        }

        private static AbilityExecutionResult ValidateConfiguration(in AbilityExecutionContext context)
        {
            if (context.Ability.Effects.Count == 0)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.NoEffects, $"{context.Ability.DisplayName} has no effects.");
            }

            if (context.Ability.TargetingMode == AbilityTargetingMode.DirectTarget && context.Target == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.InvalidTarget, $"{context.Ability.DisplayName} requires a target.");
            }

            if (context.Ability.DeliveryMode == AbilityDeliveryMode.Projectile)
            {
                AbilityProjectileDelivery projectile = context.Ability.ProjectileDelivery;
                if (projectile == null || projectile.ProjectilePrefab == null || context.DeliveryOrigin == null)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.InvalidDeliveryConfiguration, "Invalid projectile configuration.");
                }
            }

            return AbilityExecutionResult.Success("Ability configuration is valid.");
        }

        private static AbilityExecutionResult ValidateRange(in AbilityExecutionContext context)
        {
            if (context.Target == null || context.Ability.Range <= 0f)
            {
                return AbilityExecutionResult.Success("Range valid.");
            }

            float distance = Vector3.Distance(context.SourcePosition, context.TargetPosition);
            return distance <= context.Ability.Range
                ? AbilityExecutionResult.Success("Range valid.")
                : AbilityExecutionResult.Failure(AbilityExecutionStatus.OutOfRange, $"{context.Ability.DisplayName} is out of range.");
        }

        private static AbilityExecutionResult ValidateEffects(in AbilityExecutionContext context)
        {
            EffectExecutionContext effectContext = context.ToEffectContext();
            for (int i = 0; i < context.Ability.Effects.Count; i++)
            {
                EffectDefinition effect = context.Ability.Effects[i];
                if (effect == null)
                {
                    EffectExecutionResult missing = EffectExecutionResult.Failure(EffectExecutionStatus.InvalidConfiguration, $"Missing effect at index {i}.");
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectValidationFailure, missing.Message, i, missing);
                }

                EffectExecutionResult result = effect.CanExecute(in effectContext);
                if (!result.Succeeded)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectValidationFailure, result.Message, i, result);
                }
            }

            return AbilityExecutionResult.Success("Effects can execute.");
        }

        private static AbilityExecutionResult ExecuteImmediate(in AbilityExecutionContext context)
        {
            return ExecuteEffects(context.ToEffectContext(), context.Ability.Effects);
        }

        private static AbilityExecutionResult SpawnProjectile(in AbilityExecutionContext context)
        {
            AbilityProjectileDelivery delivery = context.Ability.ProjectileDelivery;
            Vector3 spawnPosition = context.DeliveryOrigin.TransformPoint(delivery.CastPointOffset);
            Quaternion spawnRotation = Quaternion.LookRotation(context.Direction, Vector3.up);
            SpellProjectile projectile = Object.Instantiate(delivery.ProjectilePrefab, spawnPosition, spawnRotation);
            projectile.Initialize(context.Source, context.Direction, delivery.ProjectileSpeed, context.Ability, delivery.MaximumLifetime);
            context.ProjectileSpawned?.Invoke(projectile);
            return AbilityExecutionResult.Success($"Cast {context.Ability.DisplayName}.");
        }

        private static float ResolveNow(float now)
        {
            return now >= 0f ? now : Time.time;
        }
    }
}
