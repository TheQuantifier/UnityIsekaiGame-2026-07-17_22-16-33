namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageCalculation
    {
        public DamageCalculation(float preMitigationAmount, float defense, float mitigatedAmount, float finalAmount)
        {
            PreMitigationAmount = preMitigationAmount;
            Defense = defense;
            MitigatedAmount = mitigatedAmount;
            FinalAmount = finalAmount;
        }

        public float PreMitigationAmount { get; }
        public float Defense { get; }
        public float MitigatedAmount { get; }
        public float FinalAmount { get; }
    }
}
