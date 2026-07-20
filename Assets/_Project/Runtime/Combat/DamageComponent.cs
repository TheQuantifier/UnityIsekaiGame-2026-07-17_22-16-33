namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageComponent
    {
        public DamageComponent(DamageTypeDefinition damageType, float amount, AttackPowerScalingPolicy attackPowerScaling = AttackPowerScalingPolicy.IgnoreSourceAttackPower)
            : this(damageType, UnityIsekaiGame.Combat.DamageType.Physical, amount, attackPowerScaling, false)
        {
        }

        private DamageComponent(DamageTypeDefinition damageType, DamageType legacyDamageType, float amount, AttackPowerScalingPolicy attackPowerScaling, bool legacy)
        {
            DamageType = damageType;
            LegacyDamageType = legacyDamageType;
            Amount = amount;
            AttackPowerScaling = attackPowerScaling;
            IsLegacyUntyped = legacy;
        }

        public DamageTypeDefinition DamageType { get; }
        public DamageType LegacyDamageType { get; }
        public float Amount { get; }
        public AttackPowerScalingPolicy AttackPowerScaling { get; }
        public bool IsLegacyUntyped { get; }
        public bool IsValid => Amount > 0f && (DamageType != null || IsLegacyUntyped);

        public static DamageComponent Legacy(DamageType legacyDamageType, float amount, AttackPowerScalingPolicy attackPowerScaling = AttackPowerScalingPolicy.IgnoreSourceAttackPower)
        {
            return new DamageComponent(null, legacyDamageType, amount, attackPowerScaling, true);
        }
    }
}
