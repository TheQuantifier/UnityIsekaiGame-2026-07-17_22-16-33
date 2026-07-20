namespace UnityIsekaiGame.Stats
{
    public readonly struct RuntimeStatModifier
    {
        public RuntimeStatModifier(
            StatType statType,
            StatModifierOperation operation,
            float value,
            StatModifierSource source,
            int priority = 0)
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            Source = source;
            Priority = priority;
        }

        public StatType StatType { get; }
        public StatModifierOperation Operation { get; }
        public float Value { get; }
        public StatModifierSource Source { get; }
        public int Priority { get; }
        public bool IsValid => Source.IsValid && !float.IsNaN(Value) && !float.IsInfinity(Value);
    }
}
