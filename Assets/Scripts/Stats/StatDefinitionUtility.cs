namespace UnityIsekaiGame.Stats
{
    public static class StatDefinitionUtility
    {
        public static string GetStableId(StatType statType)
        {
            return statType switch
            {
                StatType.MaximumHealth => "stat.max-health",
                StatType.MaximumStamina => "stat.max-stamina",
                StatType.MaximumMana => "stat.max-mana",
                StatType.AttackPower => "stat.attack-power",
                StatType.Defense => "stat.defense",
                StatType.MovementSpeed => "stat.movement-speed",
                _ => string.Empty
            };
        }
    }
}
