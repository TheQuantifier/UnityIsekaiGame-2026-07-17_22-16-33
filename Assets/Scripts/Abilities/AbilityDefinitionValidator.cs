using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Abilities
{
    public static class AbilityDefinitionValidator
    {
        public static void ValidateAbility(AbilityDefinition ability, IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (ability == null || report == null)
            {
                return;
            }

            if (ability.PrimaryCategory == null)
            {
                report.AddError($"Ability '{ability.DisplayName}' is missing a primary category.");
            }

            if (ability.ActivationTime < 0f)
            {
                report.AddError($"Ability '{ability.DisplayName}' has negative activation time.");
            }

            if (ability.Range < 0f)
            {
                report.AddError($"Ability '{ability.DisplayName}' has negative range.");
            }

            if (ability.CooldownDuration < 0f)
            {
                report.AddError($"Ability '{ability.DisplayName}' has negative cooldown.");
            }

            if (!System.Enum.IsDefined(typeof(AbilityTargetingMode), ability.TargetingMode))
            {
                report.AddError($"Ability '{ability.DisplayName}' has invalid targeting mode.");
            }

            if (!System.Enum.IsDefined(typeof(AbilityDeliveryMode), ability.DeliveryMode))
            {
                report.AddError($"Ability '{ability.DisplayName}' has invalid delivery mode.");
            }

            ValidateCosts(ability, report);
            ValidateEffects(ability, definitionsById, report);
            ValidateDelivery(ability, report);
        }

        public static void ValidateEffect(EffectDefinition effect, DefinitionValidationReport report)
        {
            effect?.ValidateDefinition(report);
        }

        private static void ValidateCosts(AbilityDefinition ability, DefinitionValidationReport report)
        {
            HashSet<AbilityResourceType> resourceTypes = new HashSet<AbilityResourceType>();
            foreach (AbilityResourceCost cost in ability.ResourceCosts)
            {
                if (cost.HasInvalidRawAmount)
                {
                    report.AddError($"Ability '{ability.DisplayName}' has invalid {cost.ResourceType} cost.");
                }

                if (!resourceTypes.Add(cost.ResourceType))
                {
                    report.AddError($"Ability '{ability.DisplayName}' has duplicate {cost.ResourceType} costs.");
                }
            }
        }

        private static void ValidateEffects(AbilityDefinition ability, IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (ability.Effects.Count == 0)
            {
                report.AddError($"Ability '{ability.DisplayName}' has no effects.");
                return;
            }

            for (int i = 0; i < ability.Effects.Count; i++)
            {
                EffectDefinition effect = ability.Effects[i];
                if (effect == null)
                {
                    report.AddError($"Ability '{ability.DisplayName}' has a missing effect reference at index {i}.");
                    continue;
                }

                if (definitionsById != null
                    && (!definitionsById.TryGetValue(effect.Id, out IGameDefinition found) || !ReferenceEquals(found, effect)))
                {
                    report.AddError($"Ability '{ability.DisplayName}' references effect '{effect.Id}', which is not in the configured catalog.");
                }
            }
        }

        private static void ValidateDelivery(AbilityDefinition ability, DefinitionValidationReport report)
        {
            if (ability.DeliveryMode == AbilityDeliveryMode.Projectile)
            {
                if (ability.ProjectileDelivery == null || ability.ProjectileDelivery.ProjectilePrefab == null)
                {
                    report.AddError($"Ability '{ability.DisplayName}' uses projectile delivery without a projectile prefab.");
                }

                return;
            }

            if (ability.DeliveryMode == AbilityDeliveryMode.Immediate && ability.ProjectileDelivery != null && ability.ProjectileDelivery.ProjectilePrefab != null)
            {
                report.AddWarning($"Ability '{ability.DisplayName}' uses immediate delivery but has projectile configuration.");
            }

            if (ability.TargetingMode == AbilityTargetingMode.Direction && ability.DeliveryMode == AbilityDeliveryMode.Immediate)
            {
                report.AddWarning($"Ability '{ability.DisplayName}' targets a direction but uses immediate delivery.");
            }
        }

        public static void ValidateSpellAdapter(UnityIsekaiGame.Magic.SpellDefinition spell, DefinitionValidationReport report)
        {
            if (spell == null || spell.Ability == null || report == null)
            {
                return;
            }

            AbilityDefinition ability = spell.Ability;
            AbilityResourceCost manaCost = ability.ResourceCosts.FirstOrDefault(cost => cost.ResourceType == AbilityResourceType.Mana);
            if (!UnityEngine.Mathf.Approximately(manaCost.Amount, spell.ManaCost))
            {
                report.AddWarning($"Spell '{spell.DisplayName}' has legacy mana cost {spell.ManaCost:0.###} but ability '{ability.Id}' has mana cost {manaCost.Amount:0.###}; ability data is authoritative.");
            }

            if (!UnityEngine.Mathf.Approximately(ability.CooldownDuration, spell.Cooldown))
            {
                report.AddWarning($"Spell '{spell.DisplayName}' has legacy cooldown {spell.Cooldown:0.###} but ability '{ability.Id}' has cooldown {ability.CooldownDuration:0.###}; ability data is authoritative.");
            }
        }
    }
}
