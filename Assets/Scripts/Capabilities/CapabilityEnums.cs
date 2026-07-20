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
        Skill,
        Role,
        SocialStatus,
        Equipment,
        Status,
        Development,
        Other
    }
}
