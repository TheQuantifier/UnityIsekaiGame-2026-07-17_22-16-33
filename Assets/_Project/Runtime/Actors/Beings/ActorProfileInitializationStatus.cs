namespace UnityIsekaiGame.Beings
{
    public enum ActorProfileInitializationStatus
    {
        NotInitialized,
        InitializedFromProfile,
        InitializedFromLegacyFallback,
        MissingProfile,
        InvalidProfile,
        AlreadyInitialized
    }
}
