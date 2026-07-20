namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageComponentResult
    {
        public DamageComponentResult(
            DamageTypeDefinition damageType,
            DamageType legacyDamageType,
            float originalAmount,
            float defenseMitigation,
            float effectiveResistance,
            float resistanceDelta,
            float finalAmount,
            bool immune)
        {
            DamageType = damageType;
            LegacyDamageType = legacyDamageType;
            OriginalAmount = originalAmount;
            DefenseMitigation = defenseMitigation;
            EffectiveResistance = effectiveResistance;
            ResistanceDelta = resistanceDelta;
            FinalAmount = finalAmount;
            Immune = immune;
        }

        public DamageTypeDefinition DamageType { get; }
        public DamageType LegacyDamageType { get; }
        public string DamageTypeId => DamageType == null ? LegacyDamageType.ToString() : DamageType.Id;
        public float OriginalAmount { get; }
        public float DefenseMitigation { get; }
        public float EffectiveResistance { get; }
        public float ResistanceDelta { get; }
        public float ResistanceMitigation => ResistanceDelta > 0f ? ResistanceDelta : 0f;
        public float WeaknessAmplification => ResistanceDelta < 0f ? -ResistanceDelta : 0f;
        public float FinalAmount { get; }
        public bool Immune { get; }
    }
}
