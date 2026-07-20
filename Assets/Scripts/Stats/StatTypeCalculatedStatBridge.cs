using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Stats
{
    public static class StatTypeCalculatedStatBridge
    {
        public static bool TryGetCalculatedStatId(StatType statType, out string calculatedStatId)
        {
            switch (statType)
            {
                case StatType.MaximumHealth:
                    calculatedStatId = CalculatedStatIds.MaximumHealth;
                    return true;
                case StatType.MaximumStamina:
                    calculatedStatId = CalculatedStatIds.MaximumStamina;
                    return true;
                case StatType.MaximumMana:
                    calculatedStatId = CalculatedStatIds.MaximumMana;
                    return true;
                case StatType.AttackPower:
                    calculatedStatId = CalculatedStatIds.PhysicalPower;
                    return true;
                case StatType.Defense:
                    calculatedStatId = CalculatedStatIds.PhysicalDefense;
                    return true;
                case StatType.MovementSpeed:
                    calculatedStatId = CalculatedStatIds.MovementSpeed;
                    return true;
                default:
                    calculatedStatId = string.Empty;
                    return false;
            }
        }

        public static CalculatedStatContributionSourceCategory MapSourceCategory(StatModifierSourceType sourceType)
        {
            switch (sourceType)
            {
                case StatModifierSourceType.Equipment:
                    return CalculatedStatContributionSourceCategory.Equipment;
                case StatModifierSourceType.StatusEffect:
                    return CalculatedStatContributionSourceCategory.CombatStatus;
                case StatModifierSourceType.Role:
                    return CalculatedStatContributionSourceCategory.Role;
                case StatModifierSourceType.SocialStatus:
                    return CalculatedStatContributionSourceCategory.SocialStatus;
                case StatModifierSourceType.Origin:
                    return CalculatedStatContributionSourceCategory.Origin;
                case StatModifierSourceType.BirthGift:
                    return CalculatedStatContributionSourceCategory.BirthGift;
                case StatModifierSourceType.Progression:
                case StatModifierSourceType.Debug:
                    return CalculatedStatContributionSourceCategory.Development;
                default:
                    return CalculatedStatContributionSourceCategory.Other;
            }
        }

        public static bool TryMapPermanentGrantToAttribute(StatType statType, out string attributeId)
        {
            switch (statType)
            {
                case StatType.MaximumHealth:
                    attributeId = AttributeIds.Vitality;
                    return true;
                case StatType.MaximumStamina:
                    attributeId = AttributeIds.Endurance;
                    return true;
                case StatType.MaximumMana:
                    attributeId = AttributeIds.ManaCapacity;
                    return true;
                case StatType.AttackPower:
                    attributeId = AttributeIds.Strength;
                    return true;
                case StatType.Defense:
                    attributeId = AttributeIds.Vitality;
                    return true;
                case StatType.MovementSpeed:
                    attributeId = AttributeIds.Agility;
                    return true;
                default:
                    attributeId = string.Empty;
                    return false;
            }
        }
    }
}
