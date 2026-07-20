using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Gameplay;

namespace UnityIsekaiGame.Abilities
{
    public static class AbilityResourceCostProcessor
    {
        public static AbilityExecutionResult ValidateCosts(AbilityDefinition ability, GameObject source)
        {
            if (ability == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.MissingAbility, "Missing ability.");
            }

            if (source == null)
            {
                return AbilityExecutionResult.Failure(AbilityExecutionStatus.InvalidSource, "Missing ability source.");
            }

            HashSet<AbilityResourceType> resourceTypes = new HashSet<AbilityResourceType>();
            foreach (AbilityResourceCost cost in ability.ResourceCosts)
            {
                if (!resourceTypes.Add(cost.ResourceType))
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.InsufficientResource, $"Duplicate {cost.ResourceType} costs are not supported.");
                }

                if (cost.Amount <= 0f)
                {
                    continue;
                }

                if (!CanPay(source, cost))
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.InsufficientResource, $"Not enough {cost.ResourceType.ToString().ToLowerInvariant()}.");
                }
            }

            return AbilityExecutionResult.Success("Costs can be paid.");
        }

        public static AbilityExecutionResult CommitCosts(AbilityDefinition ability, GameObject source)
        {
            AbilityExecutionResult validation = ValidateCosts(ability, source);
            if (!validation.Succeeded)
            {
                return validation;
            }

            foreach (AbilityResourceCost cost in ability.ResourceCosts)
            {
                if (cost.Amount <= 0f)
                {
                    continue;
                }

                VitalChangeResult result = Spend(source, cost);
                if (!result.Succeeded)
                {
                    return AbilityExecutionResult.Failure(AbilityExecutionStatus.ResourceCommitFailed, result.Message);
                }
            }

            return AbilityExecutionResult.Success("Costs paid.");
        }

        private static bool CanPay(GameObject source, AbilityResourceCost cost)
        {
            return cost.ResourceType switch
            {
                AbilityResourceType.Mana => source.GetComponentInParent<PlayerMana>()?.CanSpend(cost.Amount) == true,
                AbilityResourceType.Stamina => source.GetComponentInParent<PlayerStamina>()?.CanSpend(cost.Amount) == true,
                AbilityResourceType.Health => false,
                _ => false
            };
        }

        private static VitalChangeResult Spend(GameObject source, AbilityResourceCost cost)
        {
            return cost.ResourceType switch
            {
                AbilityResourceType.Mana => source.GetComponentInParent<PlayerMana>().Spend(cost.Amount),
                AbilityResourceType.Stamina => source.GetComponentInParent<PlayerStamina>().Spend(cost.Amount, "Ability"),
                _ => VitalChangeResult.Failure(cost.Amount, $"{cost.ResourceType} costs are not supported yet.")
            };
        }
    }
}
