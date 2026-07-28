namespace UnityIsekaiGame.Professions
{
    public enum ProfessionCategory
    {
        Custom = 0,
        Craft = 1,
        Trade = 2,
        Academic = 3,
        Medical = 4,
        Military = 5,
        Government = 6,
        Religious = 7,
        Agricultural = 8,
        Commercial = 9,
        Service = 10,
        Artistic = 11,
        Exploration = 12,
        Criminal = 13,
        Magical = 14,
        Technical = 15,
        Labor = 16
    }

    public enum ProfessionRecognitionForm
    {
        Either = 0,
        Formal = 1,
        Informal = 2
    }

    public enum ProfessionRelationshipState
    {
        Interested = 0,
        Aspiring = 1,
        StudentFoundation = 2,
        ApprenticeFoundation = 3,
        Practicing = 4,
        RecognizedPractitioner = 5,
        Inactive = 6,
        Suspended = 7,
        Revoked = 8,
        Abandoned = 9,
        Retired = 10,
        Former = 11,
        Secret = 12,
        Disputed = 13,
        Custom = 14
    }

    public enum ProfessionProjectionAudience
    {
        AuthoritativeInternal = 0,
        PrivilegedDebug = 1,
        Self = 2,
        PublicInspection = 3,
        Biography = 4,
        KnowledgeProjection = 5
    }

    public enum ProfessionOperationStatus
    {
        Succeeded = 0,
        Preview = 1,
        Duplicate = 2,
        MissingRuntime = 3,
        MissingDefinition = 4,
        MissingPerson = 5,
        MissingRelationship = 6,
        DuplicateRelationshipId = 7,
        DuplicateActiveRelationship = 8,
        InvalidRequest = 9,
        InvalidState = 10,
        InvalidSpecialization = 11,
        MissingRecognitionAuthority = 12,
        MultiplePrimary = 13,
        RestoreFailed = 14,
        ValidationFailed = 15
    }

    public enum ProfessionHistoryHookKind
    {
        BeganPracticing = 0,
        Recognized = 1,
        SpecializationAdopted = 2,
        MadePrimary = 3,
        Stopped = 4,
        Retired = 5,
        RecognitionSuspended = 6,
        RecognitionRevoked = 7,
        SecretRevealed = 8,
        Corrected = 9
    }

    public enum ProfessionEntryType
    {
        SelfDeclaredPractice = 0,
        InformalApprenticeship = 1,
        FormalStudy = 2,
        GuildApplication = 3,
        RecognitionApplication = 4,
        MilitaryOrGovernmentAppointment = 5,
        ReligiousVocation = 6,
        Specialization = 7,
        Reentry = 8,
        Custom = 9
    }

    public enum ProfessionEntryFormality
    {
        Informal = 0,
        Formal = 1,
        Either = 2
    }

    public enum ProfessionSelfDeclarationPolicy
    {
        Disallowed = 0,
        Allowed = 1,
        Required = 2
    }

    public enum ProfessionReentryPolicy
    {
        NotApplicable = 0,
        AllowFormerInactiveAbandonedRetired = 1,
        AllowSuspendedWithAuthority = 2,
        AllowRevokedWithExplicitReinstatement = 3
    }

    public enum ProfessionEntryRequestState
    {
        Draft = 0,
        Submitted = 1,
        UnderReview = 2,
        Approved = 3,
        Rejected = 4,
        Withdrawn = 5,
        Expired = 6,
        Cancelled = 7,
        Invalid = 8,
        Custom = 9
    }

    public enum ProfessionEligibilityStatus
    {
        Succeeded = 0,
        Preview = 1,
        MissingRuntime = 2,
        MissingDefinition = 3,
        MissingPerson = 4,
        MissingEntryPath = 5,
        ProfessionMismatch = 6,
        SpecializationMismatch = 7,
        FormalityMismatch = 8,
        SelfDeclarationBlocked = 9,
        MissingAuthority = 10,
        InvalidAuthority = 11,
        RequirementFailed = 12,
        MissingSkill = 13,
        MissingKnowledge = 14,
        MissingCapability = 15,
        MissingTrait = 16,
        MissingStatus = 17,
        MissingOrganization = 18,
        AccessDenied = 19,
        AgeOrLifeStageBlocked = 20,
        Conflict = 21,
        DuplicateActiveRelationship = 22,
        StaleEvaluation = 23,
        InvalidRequest = 24,
        RestoreFailed = 25,
        ValidationFailed = 26
    }

    public enum ProfessionEntryOperationStatus
    {
        Succeeded = 0,
        Preview = 1,
        Duplicate = 2,
        MissingRuntime = 3,
        MissingDefinition = 4,
        MissingRequest = 5,
        InvalidRequest = 6,
        InvalidState = 7,
        EligibilityFailed = 8,
        StaleEvaluation = 9,
        DuplicateRelationship = 10,
        MissingAuthority = 11,
        InvalidAuthority = 12,
        RestoreFailed = 13,
        ValidationFailed = 14
    }

    public enum ProfessionEntryProjectionAudience
    {
        AuthoritativeInternal = 0,
        PrivilegedDebug = 1,
        Applicant = 2,
        Sponsor = 3,
        Authority = 4,
        PublicInspection = 5
    }

    public enum ProfessionEntryHistoryHookKind
    {
        EligibilityEvaluated = 0,
        InformalEntry = 1,
        RequestSubmitted = 2,
        RequestApproved = 3,
        RequestRejected = 4,
        RequestWithdrawn = 5,
        SpecializationEntered = 6,
        ProfessionResumed = 7,
        RecognitionReinstated = 8,
        SecretRequirementRevealed = 9,
        Corrected = 10
    }
}
