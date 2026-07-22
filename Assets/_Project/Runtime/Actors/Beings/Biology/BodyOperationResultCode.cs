namespace UnityIsekaiGame.Beings.Biology
{
    public enum BodyOperationResultCode
    {
        Success,
        PreviewSucceeded,
        DuplicateAssignment,
        MissingActorBody,
        StaleActorBody,
        MissingPerson,
        MissingSpecies,
        MissingClassification,
        MissingBodyForm,
        RuntimeNotReady,
        ContributionApplicationFailure,
        TraitGrantFailure,
        CapabilityApplicationFailure,
        CalculatedStatContributionFailure,
        RestoreResolutionFailure,
        IncompatibleRestoredIdentity,
        InvalidSnapshotCoherence,
        InvalidConfiguration,
        InvalidRequest
    }
}
