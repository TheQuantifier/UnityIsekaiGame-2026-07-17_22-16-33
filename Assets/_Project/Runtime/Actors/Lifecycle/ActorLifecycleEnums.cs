namespace UnityIsekaiGame.ActorLifecycle
{
    public enum ActorLifecycleState
    {
        Active,
        Defeated,
        Unconscious,
        Dead
    }

    public enum DefeatPolicyOutcome
    {
        BecomeUnconscious,
        DieImmediately,
        RemainDefeated,
        IgnoreDefeat
    }

    public enum LifecycleTransitionKind
    {
        None,
        Defeat,
        Unconsciousness,
        Death,
        Recovery,
        Revival
    }

    public enum LifecycleTriggerKind
    {
        HealthDepleted,
        ExplicitDefeat,
        ExplicitDeath,
        Scripted,
        Environmental,
        Recovery,
        Revival
    }
}
