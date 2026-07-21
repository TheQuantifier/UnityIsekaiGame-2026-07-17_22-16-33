using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.Combat.Defense;

namespace UnityIsekaiGame.Combat.Execution
{
    public interface ICombatExecutionHandler
    {
        string HandlerId { get; }
        bool CanHandle(CombatExecutionDefinition definition, object payload);
        CombatExecutionHandlerResult Preview(CombatExecutionDefinition definition, object payload, string transactionId);
        CombatExecutionHandlerResult Execute(CombatExecutionDefinition definition, object payload, string transactionId);
    }

    public sealed class CombatExecutionHandlerResult
    {
        private CombatExecutionHandlerResult(bool succeeded, string code, string message, object payloadResult)
        {
            Succeeded = succeeded;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            PayloadResult = payloadResult;
        }

        public bool Succeeded { get; }
        public string Code { get; }
        public string Message { get; }
        public object PayloadResult { get; }

        public static CombatExecutionHandlerResult Success(string message, object payloadResult = null)
        {
            return new CombatExecutionHandlerResult(true, CombatExecutionResultCode.Success, message, payloadResult);
        }

        public static CombatExecutionHandlerResult Failure(string code, string message, object payloadResult = null)
        {
            return new CombatExecutionHandlerResult(false, code, message, payloadResult);
        }
    }

    public sealed class NoOpCombatExecutionHandler : ICombatExecutionHandler
    {
        public string HandlerId => "combat-execution.handler.noop";

        public bool CanHandle(CombatExecutionDefinition definition, object payload)
        {
            return payload == null;
        }

        public CombatExecutionHandlerResult Preview(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            return CombatExecutionHandlerResult.Success($"Previewed {definition.DisplayName} with no mutating payload.");
        }

        public CombatExecutionHandlerResult Execute(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            return CombatExecutionHandlerResult.Success($"Executed {definition.DisplayName} with no mutating payload.");
        }
    }

    public sealed class AttackCombatExecutionHandler : ICombatExecutionHandler
    {
        private readonly IAttackResolutionService attackResolutionService;

        public AttackCombatExecutionHandler(IAttackResolutionService attackResolutionService)
        {
            this.attackResolutionService = attackResolutionService;
        }

        public string HandlerId => "combat-execution.handler.attack";

        public bool CanHandle(CombatExecutionDefinition definition, object payload)
        {
            return payload is AttackResolutionRequest;
        }

        public CombatExecutionHandlerResult Preview(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            AttackResolutionRequest request = (AttackResolutionRequest)payload;
            AttackResolutionResult result = attackResolutionService.PreviewAttack(request);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Code, result.Message, result);
        }

        public CombatExecutionHandlerResult Execute(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            AttackResolutionRequest request = (AttackResolutionRequest)payload;
            AttackResolutionResult result = attackResolutionService.ExecuteAttack(request);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Code, result.Message, result);
        }
    }

    public sealed class AbilityCombatExecutionHandler : ICombatExecutionHandler
    {
        public string HandlerId => "combat-execution.handler.ability";

        public bool CanHandle(CombatExecutionDefinition definition, object payload)
        {
            return payload is AbilityExecutionContext;
        }

        public CombatExecutionHandlerResult Preview(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            AbilityExecutionContext context = (AbilityExecutionContext)payload;
            AbilityExecutionResult result = ValidateWithoutLegacyCosts(context);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Status.ToString(), result.Message, result);
        }

        public CombatExecutionHandlerResult Execute(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            AbilityExecutionContext context = (AbilityExecutionContext)payload;
            AbilityExecutionResult validation = ValidateWithoutLegacyCosts(context);
            if (!validation.Succeeded)
            {
                return CombatExecutionHandlerResult.Failure(validation.Status.ToString(), validation.Message, validation);
            }

            AbilityExecutionResult result = AbilityExecutor.ExecuteEffects(context.ToEffectContext(), context.Ability.Effects);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Status.ToString(), result.Message, result);
        }

        private static AbilityExecutionResult ValidateWithoutLegacyCosts(AbilityExecutionContext context)
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

            if (context.Ability.Effects.Count == 0)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.NoEffects, $"{context.Ability.DisplayName} has no effects.");
            }

            if (context.Ability.TargetingMode == AbilityTargetingMode.DirectTarget && context.Target == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.InvalidTarget, $"{context.Ability.DisplayName} requires a target.");
            }

            EffectExecutionContext effectContext = context.ToEffectContext();
            for (int i = 0; i < context.Ability.Effects.Count; i++)
            {
                EffectDefinition effect = context.Ability.Effects[i];
                if (effect == null)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectValidationFailure, $"Missing effect at index {i}.", i);
                }

                EffectExecutionResult result = effect.CanExecute(effectContext);
                if (!result.Succeeded)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.EffectValidationFailure, result.Message, i, result);
                }
            }

            return AbilityExecutionResult.Success("Ability can execute through combat execution.");
        }
    }

    public sealed class DefenseActivationCombatExecutionHandler : ICombatExecutionHandler
    {
        private readonly IDefensiveActionService defensiveActionService;

        public DefenseActivationCombatExecutionHandler(IDefensiveActionService defensiveActionService)
        {
            this.defensiveActionService = defensiveActionService;
        }

        public string HandlerId => "combat-execution.handler.defense";

        public bool CanHandle(CombatExecutionDefinition definition, object payload)
        {
            return payload is DefenseActivationRequest;
        }

        public CombatExecutionHandlerResult Preview(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            DefenseActivationResult result = defensiveActionService.PreviewActivate((DefenseActivationRequest)payload);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Code, result.Message, result);
        }

        public CombatExecutionHandlerResult Execute(CombatExecutionDefinition definition, object payload, string transactionId)
        {
            DefenseActivationResult result = defensiveActionService.Activate((DefenseActivationRequest)payload);
            return result.Succeeded
                ? CombatExecutionHandlerResult.Success(result.Message, result)
                : CombatExecutionHandlerResult.Failure(result.Code, result.Message, result);
        }
    }
}
