namespace UnityIsekaiGame.WorldLocations
{
    public enum InteractionPointCategory
    {
        Unknown,
        ServiceCounter,
        AdministrationDesk,
        GuildCounter,
        GovernmentDesk,
        RecordsDesk,
        QuestBoard,
        MerchantCounter,
        SalesCounter,
        StorageAccess,
        Workstation,
        CraftingStation,
        MeetingPoint,
        Seat,
        Bed,
        DetentionPoint,
        PrisonCellPoint,
        CourtDesk,
        RegistrationPoint,
        InformationPoint,
        InventoryDisplayPoint,
        ContainerAccess,
        JobStation,
        Custom
    }

    public enum InteractionServiceCategory
    {
        Unknown,
        Information,
        Registration,
        MembershipAdministration,
        RankAdministration,
        OfficeAdministrationPlaceholder,
        QuestAccessPlaceholder,
        QuestSubmissionPlaceholder,
        GovernmentService,
        RecordAccess,
        MerchantService,
        PurchasePlaceholder,
        SalePlaceholder,
        PermitAdministration,
        StorageAccess,
        Crafting,
        EmploymentServicePlaceholder,
        CourtService,
        DetentionService,
        MeetingService,
        RestPlaceholder,
        Custom
    }

    public enum InteractionPointLifecycleState
    {
        Unknown,
        Proposed,
        Active,
        Inactive,
        Disabled,
        Closed,
        BrokenPlaceholder,
        Destroyed,
        Historical,
        Invalid
    }

    public enum InteractionPointVisibility
    {
        Public,
        LocallyKnown,
        OrganizationKnown,
        MemberKnown,
        StaffKnown,
        GovernmentKnown,
        Restricted,
        Secret,
        Hidden,
        Diagnostic
    }

    public enum InteractionPointOperationStatus
    {
        Succeeded,
        Preview,
        Duplicate,
        InvalidRequest,
        MissingDefinition,
        MissingService,
        MissingPoint,
        MissingHostLocation,
        WrongWorld,
        InvalidHostLocation,
        InvalidLifecycleTransition,
        InvalidServiceBinding,
        InvalidSubjectLink,
        InvalidProvider,
        ProviderAbsent,
        ConsumerAbsent,
        PresenceFailed,
        CapacityFull,
        ReservationConflict,
        SessionConflict,
        RevisionConflict,
        DestinationRuntimeUnavailable,
        MissingAuthorization,
        LegalRestriction,
        RequirementFailed,
        VisibilityDenied,
        PersistenceInvalid,
        RestoreFailed,
        Disposed
    }

    public enum InteractionPointUseState
    {
        Unknown,
        Free,
        InUse,
        Reserved,
        Blocked,
        Disabled,
        Unavailable,
        Full,
        Custom
    }

    public enum InteractionUseSessionLifecycle
    {
        Unknown,
        Proposed,
        Active,
        Suspended,
        Completed,
        Cancelled,
        Expired,
        Historical,
        Invalid
    }

    public enum InteractionReservationLifecycle
    {
        Unknown,
        Proposed,
        Active,
        Consumed,
        Released,
        Expired,
        Cancelled,
        Historical
    }

    public enum InteractionSubjectLinkRole
    {
        Unknown,
        RepresentedOrganization,
        RepresentedGovernment,
        RepresentedOffice,
        RepresentedBusiness,
        AssociatedProperty,
        AssociatedInventory,
        AssociatedTreasury,
        AssociatedCourt,
        AssociatedCustodyLocation,
        AssociatedRecordsCollection,
        AssociatedQuestSourcePlaceholder,
        ServiceProviderOrganization,
        Custom
    }

    public enum InteractionProviderRequirementKind
    {
        Unknown,
        NoProvider,
        AnyAuthorizedMember,
        SpecificOfficeholder,
        SpecificRank,
        EmployeeWithPosition,
        BusinessOwner,
        AssignedClerk,
        AssignedPerson,
        AutomatedService,
        Custom
    }

    public enum InteractionPhysicalPresencePolicy
    {
        Unknown,
        NotRequired,
        SameExactLocation,
        WithinHostLocation,
        WithinImmediateParent,
        ProviderAndConsumerSameLocation,
        RemoteAllowed,
        Custom
    }

    public enum InteractionDestinationRuntime
    {
        Unknown,
        InteractionPoint,
        OrganizationMembership,
        OrganizationAuthority,
        Legal,
        KnowledgeRecords,
        ItemInventory,
        BusinessTrade,
        Justice,
        QuestPlaceholder,
        Crafting,
        Profession,
        Social,
        Custom
    }

    public enum InteractionSceneBindingCategory
    {
        None,
        PrototypeMarker,
        ServiceCounterMarker,
        DeskMarker,
        BoardMarker,
        StorageMarker,
        WorkstationMarker,
        SeatMarker,
        BedMarker,
        Custom
    }
}
