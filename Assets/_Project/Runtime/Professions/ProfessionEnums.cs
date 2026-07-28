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

    public enum TrainingProgramCategory
    {
        Custom = 0,
        AcademicEducation = 1,
        VocationalTraining = 2,
        Apprenticeship = 3,
        Mentorship = 4,
        MilitaryTraining = 5,
        ReligiousInstruction = 6,
        ProfessionalInduction = 7,
        SafetyTraining = 8,
        ContinuingEducation = 9,
        RemedialTraining = 10
    }

    public enum TrainingProgramFormality
    {
        Informal = 0,
        Formal = 1,
        Either = 2
    }

    public enum TrainingTeachingMethod
    {
        Custom = 0,
        Lecture = 1,
        Reading = 2,
        Demonstration = 3,
        GuidedPractice = 4,
        SupervisedWork = 5,
        Discussion = 6,
        ExaminationPreparation = 7,
        FieldStudy = 8,
        IndependentStudy = 9
    }

    public enum TrainingEnrollmentState
    {
        Applied = 0,
        Accepted = 1,
        Enrolled = 2,
        Active = 3,
        Paused = 4,
        Suspended = 5,
        Withdrawn = 6,
        Dismissed = 7,
        Failed = 8,
        Completed = 9,
        Cancelled = 10,
        Expired = 11,
        Custom = 12
    }

    public enum TrainingInstructorRoleKind
    {
        Instructor = 0,
        Mentor = 1,
        Master = 2,
        Supervisor = 3,
        Evaluator = 4,
        AssistantInstructor = 5,
        GuestInstructor = 6,
        Custom = 7
    }

    public enum TrainingAssignmentActivityCategory
    {
        Custom = 0,
        Crafting = 1,
        ProductionJob = 2,
        Repair = 3,
        Salvage = 4,
        Experiment = 5,
        ResearchRecord = 6,
        WorkOrder = 7,
        Combat = 8,
        Service = 9
    }

    public enum TrainingSupervisionLevel
    {
        Custom = 0,
        ObservationOnly = 1,
        DirectInstruction = 2,
        CloselySupervised = 3,
        PeriodicallySupervised = 4,
        IndependentWithReview = 5
    }

    public enum TrainingSessionCompletionState
    {
        Planned = 0,
        Attended = 1,
        Completed = 2,
        Partial = 3,
        Failed = 4,
        Cancelled = 5
    }

    public enum TrainingWorkOutcome
    {
        Unknown = 0,
        Succeeded = 1,
        Partial = 2,
        Failed = 3,
        Rejected = 4
    }

    public enum TrainingOperationStatus
    {
        Succeeded = 0,
        Preview = 1,
        Duplicate = 2,
        MissingRuntime = 3,
        MissingDefinition = 4,
        MissingPerson = 5,
        MissingEnrollment = 6,
        InvalidRequest = 7,
        InvalidState = 8,
        InvalidTransition = 9,
        InvalidInstructor = 10,
        InvalidModule = 11,
        InvalidLesson = 12,
        InvalidAssignment = 13,
        DuplicateActivity = 14,
        RequirementBlocked = 15,
        StaleProgress = 16,
        TeachingFailed = 17,
        RestoreFailed = 18,
        ValidationFailed = 19
    }

    public enum TrainingProjectionAudience
    {
        AuthoritativeInternal = 0,
        PrivilegedDebug = 1,
        Learner = 2,
        Instructor = 3,
        Mentor = 4,
        Supervisor = 5,
        Institution = 6,
        ProfessionAuthority = 7,
        PublicInspection = 8,
        PersonKnown = 9,
        PersonBelieved = 10
    }

    public enum TrainingHistoryHookKind
    {
        ProgramEntered = 0,
        ApprenticeshipBegun = 1,
        InstructorAssigned = 2,
        MajorAssignmentCompleted = 3,
        ProgramPaused = 4,
        LearnerDismissed = 5,
        ProgramFailed = 6,
        ProgramCompleted = 7,
        ApprenticeshipCompleted = 8,
        Corrected = 9
    }
}
