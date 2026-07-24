using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Knowledge;

namespace UnityIsekaiGame.Knowledge.History
{
    [Serializable]
    public sealed class HistoricalEventPayloadData
    {
        public HistoricalEventPayloadKind kind = HistoricalEventPayloadKind.Generic;
        public string fromBodyId;
        public string toBodyId;
        public string speciesId;
        public string locationId;
        public string organizationId;
        public string itemId;
        public string conditionId;
        public string diseaseId;
        public string diagnosisId;
        public string claimValueId;
        public string qualitativeValue;
        public string note;

        public HistoricalEventPayloadData Clone()
        {
            return new HistoricalEventPayloadData
            {
                kind = kind,
                fromBodyId = fromBodyId,
                toBodyId = toBodyId,
                speciesId = speciesId,
                locationId = locationId,
                organizationId = organizationId,
                itemId = itemId,
                conditionId = conditionId,
                diseaseId = diseaseId,
                diagnosisId = diagnosisId,
                claimValueId = claimValueId,
                qualitativeValue = qualitativeValue,
                note = note
            };
        }
    }

    [Serializable]
    public sealed class HistoricalEventRecordData
    {
        public string eventId;
        public string eventDefinitionId;
        public double occurredAtWorldTime;
        public double recordedAtWorldTime;
        public long sequence;
        public string primaryPersonId;
        public string[] participantPersonIds;
        public string[] bodyIds;
        public string locationId;
        public string organizationId;
        public string[] relatedEntityIds;
        public HistoricalEventCategory category;
        public KnowledgeVisibility visibility;
        public HistoricalEventStatus status;
        public string sourceSystem;
        public string provenance;
        public string supersedesEventId;
        public string correctedByEventId;
        public string correlationId;
        public string[] tags;
        public HistoricalEventPayloadData payload;

        public HistoricalEventRecordData Clone()
        {
            return new HistoricalEventRecordData
            {
                eventId = eventId,
                eventDefinitionId = eventDefinitionId,
                occurredAtWorldTime = occurredAtWorldTime,
                recordedAtWorldTime = recordedAtWorldTime,
                sequence = sequence,
                primaryPersonId = primaryPersonId,
                participantPersonIds = participantPersonIds == null ? Array.Empty<string>() : participantPersonIds.ToArray(),
                bodyIds = bodyIds == null ? Array.Empty<string>() : bodyIds.ToArray(),
                locationId = locationId,
                organizationId = organizationId,
                relatedEntityIds = relatedEntityIds == null ? Array.Empty<string>() : relatedEntityIds.ToArray(),
                category = category,
                visibility = visibility,
                status = status,
                sourceSystem = sourceSystem,
                provenance = provenance,
                supersedesEventId = supersedesEventId,
                correctedByEventId = correctedByEventId,
                correlationId = correlationId,
                tags = tags == null ? Array.Empty<string>() : tags.ToArray(),
                payload = payload?.Clone()
            };
        }
    }

    [Serializable]
    public sealed class HistoryMemoryRecordData
    {
        public string memoryId;
        public string ownerPersonId;
        public string historicalEventId;
        public string beliefId;
        public string[] evidenceIds;
        public HistoryMemorySource source;
        public double formedAtWorldTime;
        public double rememberedOccurredAtWorldTime;
        public double lastRecalledWorldTime;
        public int confidence;
        public int clarity;
        public int salience;
        public bool firstHand;
        public MemoryState state;
        public KnowledgeVisibility visibility;
        public string identityAtTimeId;
        public string bodyAtTimeId;
        public string correctedByMemoryId;
        public string correctionOfMemoryId;
        public string[] tags;
        public string debugDescription;

        public HistoryMemoryRecordData Clone()
        {
            return new HistoryMemoryRecordData
            {
                memoryId = memoryId,
                ownerPersonId = ownerPersonId,
                historicalEventId = historicalEventId,
                beliefId = beliefId,
                evidenceIds = evidenceIds == null ? Array.Empty<string>() : evidenceIds.ToArray(),
                source = source,
                formedAtWorldTime = formedAtWorldTime,
                rememberedOccurredAtWorldTime = rememberedOccurredAtWorldTime,
                lastRecalledWorldTime = lastRecalledWorldTime,
                confidence = HistoryMath.ClampMetric(confidence),
                clarity = HistoryMath.ClampMetric(clarity),
                salience = HistoryMath.ClampMetric(salience),
                firstHand = firstHand,
                state = state,
                visibility = visibility,
                identityAtTimeId = identityAtTimeId,
                bodyAtTimeId = bodyAtTimeId,
                correctedByMemoryId = correctedByMemoryId,
                correctionOfMemoryId = correctionOfMemoryId,
                tags = tags == null ? Array.Empty<string>() : tags.ToArray(),
                debugDescription = debugDescription
            };
        }
    }

    [Serializable]
    public sealed class BodyOccupationRecordData
    {
        public string occupationId;
        public string personId;
        public string bodyId;
        public double startedAtWorldTime;
        public double endedAtWorldTime = -1d;
        public string startEventId;
        public string endEventId;
        public string reason;

        public BodyOccupationRecordData Clone()
        {
            return (BodyOccupationRecordData)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class AuthoritativeHistorySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public string worldId;
        public long nextSequence;
        public long historyRevision;
        public HistoricalEventRecordData[] events;
        public BodyOccupationRecordData[] bodyOccupations;
        public string[] processedTransactions;
    }

    [Serializable]
    public sealed class PersonMemorySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public string personId;
        public long memoryRevision;
        public HistoryMemoryRecordData[] memories;
        public string[] processedTransactions;
    }

    public sealed class HistoricalEventRecord
    {
        public HistoricalEventRecord(HistoricalEventRecordData data, HistoricalEventDefinition definition)
        {
            Data = data == null ? new HistoricalEventRecordData() : data.Clone();
            Definition = definition;
        }

        public HistoricalEventRecordData Data { get; }
        public HistoricalEventDefinition Definition { get; }
        public string EventId => Data.eventId ?? string.Empty;
        public string EventDefinitionId => Data.eventDefinitionId ?? string.Empty;
        public double OccurredAtWorldTime => Data.occurredAtWorldTime;
        public double RecordedAtWorldTime => Data.recordedAtWorldTime;
        public long Sequence => Data.sequence;
        public string PrimaryPersonId => Data.primaryPersonId ?? string.Empty;
        public IReadOnlyList<string> ParticipantPersonIds => Data.participantPersonIds ?? Array.Empty<string>();
        public IReadOnlyList<string> BodyIds => Data.bodyIds ?? Array.Empty<string>();
        public string LocationId => Data.locationId ?? string.Empty;
        public string OrganizationId => Data.organizationId ?? string.Empty;
        public HistoricalEventCategory Category => Data.category;
        public KnowledgeVisibility Visibility => Data.visibility;
        public HistoricalEventStatus Status => Data.status;
        public string SupersedesEventId => Data.supersedesEventId ?? string.Empty;
        public string CorrectedByEventId => Data.correctedByEventId ?? string.Empty;
        public IReadOnlyList<string> Tags => Data.tags ?? Array.Empty<string>();
        public HistoricalEventPayloadData Payload => Data.payload?.Clone() ?? new HistoricalEventPayloadData();
    }

    public sealed class HistoryMemoryRecord
    {
        public HistoryMemoryRecord(HistoryMemoryRecordData data)
        {
            Data = data == null ? new HistoryMemoryRecordData() : data.Clone();
        }

        public HistoryMemoryRecordData Data { get; }
        public string MemoryId => Data.memoryId ?? string.Empty;
        public string OwnerPersonId => Data.ownerPersonId ?? string.Empty;
        public string HistoricalEventId => Data.historicalEventId ?? string.Empty;
        public string BeliefId => Data.beliefId ?? string.Empty;
        public IReadOnlyList<string> EvidenceIds => Data.evidenceIds ?? Array.Empty<string>();
        public HistoryMemorySource Source => Data.source;
        public MemoryState State => Data.state;
        public int Confidence => HistoryMath.ClampMetric(Data.confidence);
        public int Clarity => HistoryMath.ClampMetric(Data.clarity);
        public int Salience => HistoryMath.ClampMetric(Data.salience);
        public bool Accessible => State != MemoryState.Forgotten && State != MemoryState.Inaccessible;
        public string BodyAtTimeId => Data.bodyAtTimeId ?? string.Empty;
    }

    public sealed class BodyOccupationRecord
    {
        public BodyOccupationRecord(BodyOccupationRecordData data)
        {
            Data = data == null ? new BodyOccupationRecordData() : data.Clone();
        }

        public BodyOccupationRecordData Data { get; }
        public string OccupationId => Data.occupationId ?? string.Empty;
        public string PersonId => Data.personId ?? string.Empty;
        public string BodyId => Data.bodyId ?? string.Empty;
        public double StartedAtWorldTime => Data.startedAtWorldTime;
        public double EndedAtWorldTime => Data.endedAtWorldTime;
        public bool IsOpenEnded => EndedAtWorldTime < 0d;
    }

    public sealed class HistorySnapshot
    {
        public HistorySnapshot(string worldId, long revision, IReadOnlyList<HistoricalEventRecord> events, IReadOnlyList<BodyOccupationRecord> bodyOccupations)
        {
            WorldId = worldId ?? string.Empty;
            Revision = revision;
            Events = (events ?? Array.Empty<HistoricalEventRecord>()).OrderBy(HistoryOrdering.Key).ToArray();
            BodyOccupations = (bodyOccupations ?? Array.Empty<BodyOccupationRecord>()).OrderBy(record => record.StartedAtWorldTime).ThenBy(record => record.OccupationId, StringComparer.Ordinal).ToArray();
        }

        public string WorldId { get; }
        public long Revision { get; }
        public IReadOnlyList<HistoricalEventRecord> Events { get; }
        public IReadOnlyList<BodyOccupationRecord> BodyOccupations { get; }
    }

    public sealed class PersonMemorySnapshot
    {
        public PersonMemorySnapshot(string personId, long revision, IReadOnlyList<HistoryMemoryRecord> memories)
        {
            PersonId = personId ?? string.Empty;
            Revision = revision;
            Memories = (memories ?? Array.Empty<HistoryMemoryRecord>()).OrderBy(record => record.MemoryId, StringComparer.Ordinal).ToArray();
        }

        public string PersonId { get; }
        public long Revision { get; }
        public IReadOnlyList<HistoryMemoryRecord> Memories { get; }
        public IReadOnlyList<HistoryMemoryRecord> AccessibleMemories => Memories.Where(memory => memory.Accessible).ToArray();
    }

    public sealed class RecordHistoricalEventRequest
    {
        public string TransactionId { get; set; }
        public string EventId { get; set; }
        public string EventDefinitionId { get; set; }
        public double OccurredAtWorldTime { get; set; }
        public double RecordedAtWorldTime { get; set; }
        public long? Sequence { get; set; }
        public string PrimaryPersonId { get; set; }
        public string[] ParticipantPersonIds { get; set; }
        public string[] BodyIds { get; set; }
        public string LocationId { get; set; }
        public string OrganizationId { get; set; }
        public string[] RelatedEntityIds { get; set; }
        public HistoricalEventCategory? Category { get; set; }
        public KnowledgeVisibility? Visibility { get; set; }
        public string SourceSystem { get; set; }
        public string Provenance { get; set; }
        public string SupersedesEventId { get; set; }
        public string CorrelationId { get; set; }
        public string[] Tags { get; set; }
        public HistoricalEventPayloadData Payload { get; set; }
    }

    public sealed class FormMemoryRequest
    {
        public string TransactionId { get; set; }
        public string MemoryId { get; set; }
        public string OwnerPersonId { get; set; }
        public string HistoricalEventId { get; set; }
        public string BeliefId { get; set; }
        public string[] EvidenceIds { get; set; }
        public HistoryMemorySource Source { get; set; } = HistoryMemorySource.DirectObservation;
        public double FormedAtWorldTime { get; set; }
        public double RememberedOccurredAtWorldTime { get; set; }
        public int Confidence { get; set; } = 700;
        public int Clarity { get; set; } = 700;
        public int Salience { get; set; } = 500;
        public bool FirstHand { get; set; }
        public KnowledgeVisibility Visibility { get; set; } = KnowledgeVisibility.Private;
        public string IdentityAtTimeId { get; set; }
        public string BodyAtTimeId { get; set; }
        public string DebugDescription { get; set; }
        public string[] Tags { get; set; }
        public bool CreateKnowledgeEvidence { get; set; }
    }

    public sealed class HistoryOperationResult
    {
        private HistoryOperationResult(bool succeeded, HistoryResultCode code, string message, string transactionId, bool preview, bool duplicate, HistoricalEventRecord eventRecord, HistoryMemoryRecord memory, KnowledgeOperationResult knowledgeResult, long priorRevision, long resultingRevision)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message ?? string.Empty;
            TransactionId = transactionId ?? string.Empty;
            Preview = preview;
            Duplicate = duplicate;
            Event = eventRecord;
            Memory = memory;
            KnowledgeResult = knowledgeResult;
            PriorRevision = priorRevision;
            ResultingRevision = resultingRevision;
        }

        public bool Succeeded { get; }
        public HistoryResultCode Code { get; }
        public string Message { get; }
        public string TransactionId { get; }
        public bool Preview { get; }
        public bool Duplicate { get; }
        public HistoricalEventRecord Event { get; }
        public HistoryMemoryRecord Memory { get; }
        public KnowledgeOperationResult KnowledgeResult { get; }
        public long PriorRevision { get; }
        public long ResultingRevision { get; }

        public static HistoryOperationResult Success(string message, string transactionId, HistoricalEventRecord eventRecord, HistoryMemoryRecord memory, KnowledgeOperationResult knowledgeResult, long priorRevision, long resultingRevision, bool preview = false, bool duplicate = false)
        {
            return new HistoryOperationResult(true, duplicate ? HistoryResultCode.Duplicate : preview ? HistoryResultCode.Preview : HistoryResultCode.Success, message, transactionId, preview, duplicate, eventRecord, memory, knowledgeResult, priorRevision, resultingRevision);
        }

        public static HistoryOperationResult Failure(HistoryResultCode code, string message, string transactionId = "", bool preview = false, long revision = 0L)
        {
            return new HistoryOperationResult(false, code, message, transactionId, preview, false, null, null, null, revision, revision);
        }
    }

    public static class HistoryMath
    {
        public static int ClampMetric(int value)
        {
            return Math.Max(0, Math.Min(1000, value));
        }
    }

    public static class HistoryOrdering
    {
        public static Tuple<double, long, string> Key(HistoricalEventRecord record)
        {
            return Tuple.Create(record?.OccurredAtWorldTime ?? 0d, record?.Sequence ?? 0L, record?.EventId ?? string.Empty);
        }

        public static int Compare(HistoricalEventRecord left, HistoricalEventRecord right)
        {
            int time = (left?.OccurredAtWorldTime ?? 0d).CompareTo(right?.OccurredAtWorldTime ?? 0d);
            if (time != 0)
            {
                return time;
            }

            int sequence = (left?.Sequence ?? 0L).CompareTo(right?.Sequence ?? 0L);
            return sequence != 0 ? sequence : string.Compare(left?.EventId, right?.EventId, StringComparison.Ordinal);
        }
    }
}
