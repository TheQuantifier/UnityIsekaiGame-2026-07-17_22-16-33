namespace UnityIsekaiGame.Stats
{
    public interface IRuntimeStatReceiver
    {
        bool HasStat(StatType statType);
        float GetStatValue(StatType statType);
        bool AddModifier(RuntimeStatModifier modifier);
        bool RemoveModifiersFromSource(StatModifierSource source);
    }
}
