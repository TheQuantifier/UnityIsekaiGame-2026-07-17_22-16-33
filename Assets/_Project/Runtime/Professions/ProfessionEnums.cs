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
}
