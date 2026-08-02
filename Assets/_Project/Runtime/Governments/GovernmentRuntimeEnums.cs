using System;

namespace UnityIsekaiGame.Governments
{
    public enum PoliticalVisibility
    {
        Public = 0,
        Restricted = 1,
        Confidential = 2,
        Secret = 3,
        Hidden = 4,
        DevelopmentOnly = 5
    }

    public enum PoliticalOperationCode
    {
        Unknown = 0,
        Succeeded = 1,
        Preview = 2,
        Duplicate = 3,
        InvalidRequest = 4,
        MissingDefinition = 5,
        MissingPolity = 6,
        MissingGovernment = 7,
        MissingTerritory = 8,
        MissingClaim = 9,
        MissingJurisdiction = 10,
        InvalidReference = 11,
        InvalidState = 12,
        InvalidAuthority = 13,
        CycleRejected = 14,
        Conflict = 15,
        ValidationFailed = 16,
        Disposed = 17,
        AccessDenied = 18
    }

    public enum PoliticalOperationKind
    {
        Unknown = 0,
        CreatePolity = 1,
        RegisterGovernment = 2,
        CreateTerritory = 3,
        AssertClaim = 4,
        RecordControl = 5,
        RecordAdministration = 6,
        RegisterSeat = 7,
        AssertSovereignty = 8,
        CreateJurisdiction = 9,
        ResolveJurisdiction = 10,
        TransferTerritory = 11,
        TransitionGovernment = 12,
        SplitOrMergePolity = 13
    }

    public enum PolityCategory
    {
        Unknown = 0,
        Kingdom = 1,
        Empire = 2,
        Republic = 3,
        CityState = 4,
        Principality = 5,
        Duchy = 6,
        CountyRealm = 7,
        Confederation = 8,
        Federation = 9,
        TribalPolity = 10,
        ReligiousPolity = 11,
        MilitaryPolity = 12,
        ColonialPolity = 13,
        AutonomousPolity = 14,
        DisputedPolity = 15,
        StatelessPoliticalCommunity = 16,
        Custom = 100
    }

    public enum PolityLifecycleState
    {
        Unknown = 0,
        Forming = 1,
        Active = 2,
        Fragmented = 3,
        Disputed = 4,
        Occupied = 5,
        Stateless = 6,
        Dissolved = 7,
        Merged = 8,
        Split = 9,
        Historical = 10,
        Archived = 11,
        Invalid = 12
    }

    public enum PoliticalNameCategory
    {
        Unknown = 0,
        Official = 1,
        Short = 2,
        Common = 3,
        Historical = 4,
        Foreign = 5,
        Claimed = 6,
        Disputed = 7,
        Abbreviation = 8,
        DemonymPlaceholder = 9,
        Secret = 10,
        Provisional = 11
    }

    public enum GovernmentCategory
    {
        Unknown = 0,
        MonarchicalGovernment = 1,
        ImperialGovernment = 2,
        RepublicanGovernment = 3,
        CouncilGovernment = 4,
        ParliamentaryPlaceholder = 5,
        ExecutiveGovernment = 6,
        TribalCouncil = 7,
        ReligiousGovernment = 8,
        MilitaryGovernment = 9,
        MunicipalGovernment = 10,
        RegionalGovernment = 11,
        ProvincialGovernment = 12,
        ColonialAdministration = 13,
        OccupationAdministration = 14,
        ProvisionalGovernment = 15,
        GovernmentInExile = 16,
        RevolutionaryGovernment = 17,
        ClaimantGovernment = 18,
        Custom = 100
    }

    public enum GovernmentLevel
    {
        Unknown = 0,
        Central = 1,
        Regional = 2,
        Provincial = 3,
        County = 4,
        Municipal = 5,
        Local = 6,
        Special = 7,
        NonTerritorial = 8
    }

    public enum GovernmentLifecycleState
    {
        Unknown = 0,
        Forming = 1,
        Active = 2,
        Provisional = 3,
        Contested = 4,
        OccupationAdministration = 5,
        InExile = 6,
        Suspended = 7,
        Collapsed = 8,
        Succeeded = 9,
        Dissolved = 10,
        Historical = 11,
        Archived = 12
    }

    public enum GovernmentInstitutionRoleCategory
    {
        Unknown = 0,
        Executive = 1,
        Council = 2,
        Ministry = 3,
        CourtPlaceholder = 4,
        MilitaryAdministration = 5,
        Treasury = 6,
        LocalAdministration = 7,
        DiplomaticMission = 8,
        ReligiousAdministration = 9,
        Custom = 100
    }

    public enum PoliticalTerritoryCategory
    {
        Unknown = 0,
        Realm = 1,
        Province = 2,
        Region = 3,
        County = 4,
        City = 5,
        Town = 6,
        Village = 7,
        District = 8,
        Colony = 9,
        OccupiedArea = 10,
        AutonomousRegion = 11,
        ReligiousJurisdiction = 12,
        MilitaryDistrict = 13,
        UnincorporatedLand = 14,
        Custom = 100
    }

    public enum TerritoryLifecycleState
    {
        Unknown = 0,
        Proposed = 1,
        Active = 2,
        Disputed = 3,
        Occupied = 4,
        Transferred = 5,
        Dissolved = 6,
        Historical = 7,
        Archived = 8
    }

    public enum TerritoryMembershipKind
    {
        Unknown = 0,
        ContainsPlace = 1,
        SeatPlace = 2,
        CapitalPlace = 3,
        AdministrativeCenter = 4,
        ClaimedPlace = 5,
        ControlledPlace = 6
    }

    public enum TerritorialClaimCategory
    {
        Unknown = 0,
        Sovereignty = 1,
        Administration = 2,
        Control = 3,
        HistoricalEntitlement = 4,
        TreatyBased = 5,
        Occupation = 6,
        Autonomy = 7,
        NonTerritorialJurisdiction = 8,
        Custom = 100
    }

    public enum TerritorialClaimLifecycleState
    {
        Unknown = 0,
        Asserted = 1,
        Recognized = 2,
        Disputed = 3,
        Contested = 4,
        Suspended = 5,
        Transferred = 6,
        Abandoned = 7,
        Superseded = 8,
        Historical = 9
    }

    public enum TerritorialControlState
    {
        Unknown = 0,
        Controlled = 1,
        PartiallyControlled = 2,
        Occupied = 3,
        Contested = 4,
        Lost = 5,
        Historical = 6
    }

    public enum AdministrationState
    {
        Unknown = 0,
        Administered = 1,
        Delegated = 2,
        Autonomous = 3,
        OccupiedAdministration = 4,
        Suspended = 5,
        Ended = 6,
        Historical = 7
    }

    public enum SeatCategory
    {
        Unknown = 0,
        Capital = 1,
        AdministrativeSeat = 2,
        ExileSeat = 3,
        CeremonialSeat = 4,
        TemporarySeat = 5
    }

    public enum SovereigntyClaimCategory
    {
        Unknown = 0,
        FullSovereignty = 1,
        SharedSovereignty = 2,
        AutonomousSelfGovernment = 3,
        Suzerainty = 4,
        Protectorate = 5,
        OccupiedButClaimed = 6,
        GovernmentInExile = 7,
        LegitimacyClaim = 8,
        Custom = 100
    }

    public enum SovereigntyClaimState
    {
        Unknown = 0,
        Claimed = 1,
        Recognized = 2,
        Disputed = 3,
        Contested = 4,
        Suspended = 5,
        Abandoned = 6,
        Superseded = 7,
        Historical = 8
    }

    public enum JurisdictionCategory
    {
        Unknown = 0,
        GeneralGovernment = 1,
        Municipal = 2,
        Military = 3,
        Religious = 4,
        Commercial = 5,
        Property = 6,
        Professional = 7,
        Diplomatic = 8,
        Emergency = 9,
        Custom = 100
    }

    [Flags]
    public enum JurisdictionScopeDimension
    {
        None = 0,
        Territory = 1 << 0,
        Place = 1 << 1,
        Person = 1 << 2,
        Organization = 1 << 3,
        Property = 1 << 4,
        SubjectMatter = 1 << 5,
        Office = 1 << 6,
        Status = 1 << 7
    }

    public enum JurisdictionSubjectMatter
    {
        Unknown = 0,
        GeneralAdministration = 1,
        MunicipalServices = 2,
        MilitaryDiscipline = 3,
        ReligiousInternalAffairs = 4,
        TradeRegulation = 5,
        PropertyAdministration = 6,
        PublicOrderPlaceholder = 7,
        BorderAdministrationPlaceholder = 8,
        EmergencyAdministration = 9,
        Custom = 100
    }

    public enum JurisdictionLifecycleState
    {
        Unknown = 0,
        Proposed = 1,
        Active = 2,
        Delegated = 3,
        Suspended = 4,
        Contested = 5,
        Ended = 6,
        Superseded = 7,
        Historical = 8
    }

    public enum JurisdictionConflictPolicy
    {
        Unknown = 0,
        Shared = 1,
        SpecificOverridesGeneral = 2,
        Exclusive = 3,
        DelegatedOverridesSource = 4,
        HigherPriorityWins = 5,
        Contested = 6
    }

    public enum JurisdictionResolutionStatus
    {
        Unknown = 0,
        NoApplicableJurisdiction = 1,
        Applicable = 2,
        Shared = 3,
        Contested = 4,
        ExclusiveConflict = 5,
        DeniedByLifecycle = 6,
        InvalidRequest = 7
    }

    public enum PoliticalTransitionKind
    {
        Unknown = 0,
        BoundaryChange = 1,
        TerritorialTransfer = 2,
        GovernmentSuccession = 3,
        GovernmentCollapse = 4,
        PolitySplit = 5,
        PolityMerger = 6,
        IndependenceOrSecession = 7,
        OccupationAdministration = 8
    }
}
