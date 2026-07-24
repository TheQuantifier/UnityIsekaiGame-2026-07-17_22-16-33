namespace UnityIsekaiGame.Knowledge.History
{
    public enum HistoricalEventCategory
    {
        Unknown,
        Identity,
        BirthOrCreation,
        BodyTransition,
        Location,
        Travel,
        Relationship,
        Affiliation,
        EmploymentOrRole,
        Combat,
        Injury,
        Recovery,
        Disease,
        Diagnosis,
        Treatment,
        Crime,
        Discovery,
        Ownership,
        Reputation,
        QuestRelevant,
        Political,
        Social,
        DeathOrDisappearance,
        CustomWorldEvent
    }

    public enum HistoricalEventPayloadKind
    {
        None,
        Generic,
        BodyTransition,
        Location,
        Organization,
        Condition,
        Discovery,
        Correction
    }

    public enum HistoricalEventStatus
    {
        Active,
        Superseded,
        Correction
    }

    public enum HistoryMemorySource
    {
        Unknown,
        DirectParticipation,
        DirectObservation,
        WitnessTestimony,
        WrittenRecord,
        Investigation,
        Examination,
        Diagnosis,
        Inference,
        KnowledgeSharing,
        PreviousBody,
        ScriptedSetup,
        DevelopmentFixture
    }

    public enum MemoryState
    {
        Accessible,
        Inaccessible,
        Uncertain,
        Disputed,
        Corrected,
        Forgotten
    }

    public enum HistoryResultCode
    {
        Success,
        Preview,
        Duplicate,
        InvalidRequest,
        MissingDefinition,
        MissingEvent,
        MissingPerson,
        MissingBody,
        MissingMemory,
        InvalidTimeRange,
        InvalidCorrection,
        CircularCorrection,
        PrivateHistoryBlocked,
        KnowledgeRejected,
        RestoreFailed
    }
}
