namespace UnityIsekaiGame.Capabilities
{
    public enum CapabilityValueType
    {
        Boolean,
        Numeric
    }

    public enum CapabilityAggregationPolicy
    {
        BooleanAny,
        Sum,
        Highest,
        PriorityOverride,
        Blocker
    }

    public enum CapabilitySourceCategory
    {
        Trait,
        Species,
        BiologicalClassification,
        Skill,
        Role,
        SocialStatus,
        Equipment,
        Status,
        Development,
        Other
    }
}
