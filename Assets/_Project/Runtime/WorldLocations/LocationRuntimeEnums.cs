namespace UnityIsekaiGame.WorldLocations
{
    public enum LocationCategory
    {
        Unknown,
        World,
        Region,
        Settlement,
        District,
        Building,
        Room,
        FunctionalArea,
        Wilderness,
        Dungeon,
        RouteAnchor,
        InteractionPoint,
        Custom
    }

    public enum LocationLifecycleState
    {
        Unknown,
        Proposed,
        Active,
        Inactive,
        Closed,
        Destroyed,
        Historical,
        Removed
    }

    public enum LocationVisibility
    {
        Public,
        Restricted,
        Secret,
        Hidden
    }

    public enum LocationNameCategory
    {
        Official,
        Common,
        Alias,
        Historical,
        LocalLanguage,
        Internal,
        Secret
    }

    public enum LocationAssociationKind
    {
        Unknown,
        Property,
        Organization,
        Government,
        Territory,
        SceneBinding,
        PrototypeMarker,
        Provenance,
        Custom
    }

    public enum LocationOperationStatus
    {
        Succeeded,
        Preview,
        Duplicate,
        InvalidRequest,
        MissingDefinition,
        MissingLocation,
        DuplicateLocationId,
        WrongWorld,
        InvalidName,
        InvalidLifecycleTransition,
        InvalidReference,
        UnsupportedByDefinition,
        RevisionConflict,
        PersistenceInvalid,
        RestoreFailed,
        Disposed
    }

    public enum LocationReferenceResolutionStatus
    {
        Resolved,
        MissingLocation,
        WrongWorld,
        Destroyed,
        InvalidRequest
    }
}
