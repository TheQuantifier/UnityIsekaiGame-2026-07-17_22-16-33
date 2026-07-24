using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Knowledge;

namespace UnityIsekaiGame.Knowledge.History
{
    public sealed class AuthoritativeHistoryRuntime
    {
        private readonly Dictionary<string, HistoricalEventRecordData> eventsById = new Dictionary<string, HistoricalEventRecordData>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> eventIdsByPerson = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> eventIdsByBody = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> eventIdsByLocation = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> eventIdsByOrganization = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> eventIdsByTag = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, BodyOccupationRecordData> occupationsById = new Dictionary<string, BodyOccupationRecordData>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> occupationIdsByPerson = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly HashSet<string> processedTransactions = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownPersonIds = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownBodyIds = new HashSet<string>(StringComparer.Ordinal);
        private DefinitionRegistry registry;
        private bool suppressEvents;

        public event Action<AuthoritativeHistoryRuntime, HistoryOperationResult> HistoryChanged;

        public string WorldId { get; private set; } = "world.local";
        public long HistoryRevision { get; private set; }
        public long NextSequence { get; private set; } = 1L;

        public void Configure(DefinitionRegistry definitionRegistry, string worldId, IEnumerable<string> knownPersons = null, IEnumerable<string> knownBodies = null)
        {
            registry = definitionRegistry ?? registry;
            WorldId = string.IsNullOrWhiteSpace(worldId) ? WorldId : worldId;
            knownPersonIds = new HashSet<string>((knownPersons ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
            knownBodyIds = new HashSet<string>((knownBodies ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
        }

        public HistoryOperationResult PreviewRecordEvent(RecordHistoricalEventRequest request)
        {
            return RecordEvent(request, preview: true, restoring: false);
        }

        public HistoryOperationResult RecordEvent(RecordHistoricalEventRequest request, bool preview = false, bool restoring = false)
        {
            long priorRevision = HistoryRevision;
            if (!ValidateRecordRequest(request, out HistoricalEventDefinition definition, out string failure, out HistoryResultCode code))
            {
                return HistoryOperationResult.Failure(code, failure, request?.TransactionId, preview, HistoryRevision);
            }

            string transactionKey = TransactionKey(request.TransactionId);
            if (!preview && processedTransactions.Contains(transactionKey))
            {
                HistoricalEventRecord existingById = TryGetEvent(request.EventId, out HistoricalEventRecord duplicateRecord)
                    ? duplicateRecord
                    : null;
                return HistoryOperationResult.Success("Historical event transaction already processed.", request.TransactionId, existingById, null, null, priorRevision, HistoryRevision, duplicate: true);
            }

            HistoricalEventRecordData data = CreateEventData(request, definition);
            HistoricalEventRecord snapshot = Wrap(data);
            if (preview)
            {
                return HistoryOperationResult.Success("Historical event preview succeeded.", request.TransactionId, snapshot, null, null, priorRevision, HistoryRevision, preview: true);
            }

            if (!string.IsNullOrWhiteSpace(data.supersedesEventId))
            {
                eventsById[data.supersedesEventId].status = HistoricalEventStatus.Superseded;
                eventsById[data.supersedesEventId].correctedByEventId = data.eventId;
                data.status = HistoricalEventStatus.Correction;
            }

            eventsById[data.eventId] = data;
            AddIndexes(data);
            processedTransactions.Add(transactionKey);
            HistoryRevision++;
            HistoryOperationResult result = HistoryOperationResult.Success("Historical event recorded.", request.TransactionId, Wrap(data), null, null, priorRevision, HistoryRevision);
            RaiseChanged(result, restoring);
            return result;
        }

        public HistoryOperationResult RecordBodyTransition(string transactionId, string eventId, string personId, string fromBodyId, string toBodyId, double occurredAtWorldTime, double recordedAtWorldTime, string reason, bool preview = false, bool restoring = false)
        {
            RecordHistoricalEventRequest request = new RecordHistoricalEventRequest
            {
                TransactionId = transactionId,
                EventId = eventId,
                EventDefinitionId = "history-event.body-transition",
                OccurredAtWorldTime = occurredAtWorldTime,
                RecordedAtWorldTime = recordedAtWorldTime,
                PrimaryPersonId = personId,
                ParticipantPersonIds = new[] { personId },
                BodyIds = new[] { fromBodyId, toBodyId }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray(),
                Category = HistoricalEventCategory.BodyTransition,
                Visibility = KnowledgeVisibility.Private,
                SourceSystem = "BodyTransition",
                Provenance = string.IsNullOrWhiteSpace(reason) ? "Body transition recorded." : reason,
                Payload = new HistoricalEventPayloadData
                {
                    kind = HistoricalEventPayloadKind.BodyTransition,
                    fromBodyId = fromBodyId,
                    toBodyId = toBodyId,
                    note = reason
                },
                Tags = new[] { "body-transition", "identity-continuity" }
            };
            HistoryOperationResult result = RecordEvent(request, preview, restoring);
            if (!result.Succeeded || preview)
            {
                return result;
            }

            CloseOpenOccupation(personId, fromBodyId, occurredAtWorldTime, eventId);
            AddOccupation(new BodyOccupationRecordData
            {
                occupationId = $"occupation.{personId}.{toBodyId}.{eventId}",
                personId = personId,
                bodyId = toBodyId,
                startedAtWorldTime = occurredAtWorldTime,
                endedAtWorldTime = -1d,
                startEventId = eventId,
                reason = reason
            });
            return result;
        }

        public bool TryGetEvent(string eventId, out HistoricalEventRecord record)
        {
            if (eventsById.TryGetValue(eventId ?? string.Empty, out HistoricalEventRecordData data))
            {
                record = Wrap(data);
                return true;
            }

            record = null;
            return false;
        }

        public bool TryGetAcceptedEvent(string eventId, out HistoricalEventRecord record)
        {
            record = null;
            if (!eventsById.TryGetValue(eventId ?? string.Empty, out HistoricalEventRecordData data))
            {
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrWhiteSpace(data.correctedByEventId) && seen.Add(data.eventId) && eventsById.TryGetValue(data.correctedByEventId, out HistoricalEventRecordData corrected))
            {
                data = corrected;
            }

            record = Wrap(data);
            return true;
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByPerson(string personId)
        {
            return RecordsFromIndex(eventIdsByPerson, personId);
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByBody(string bodyId)
        {
            return RecordsFromIndex(eventIdsByBody, bodyId);
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByLocation(string locationId)
        {
            return RecordsFromIndex(eventIdsByLocation, locationId);
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByOrganization(string organizationId)
        {
            return RecordsFromIndex(eventIdsByOrganization, organizationId);
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByTag(string tag)
        {
            return RecordsFromIndex(eventIdsByTag, tag);
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByTimeRange(double inclusiveStart, double inclusiveEnd)
        {
            if (inclusiveEnd < inclusiveStart)
            {
                return Array.Empty<HistoricalEventRecord>();
            }

            return eventsById.Values
                .Where(data => data.occurredAtWorldTime >= inclusiveStart && data.occurredAtWorldTime <= inclusiveEnd)
                .Select(Wrap)
                .OrderBy(HistoryOrdering.Key)
                .ToArray();
        }

        public IReadOnlyList<HistoricalEventRecord> QueryByCategory(HistoricalEventCategory category)
        {
            return eventsById.Values
                .Where(data => data.category == category)
                .Select(Wrap)
                .OrderBy(HistoryOrdering.Key)
                .ToArray();
        }

        public IReadOnlyList<HistoricalEventRecord> QueryPersonAccessible(string personId, PersonMemoryRuntime memoryRuntime = null, bool privileged = false)
        {
            if (privileged)
            {
                return CreateSnapshot().Events;
            }

            HashSet<string> remembered = new HashSet<string>((memoryRuntime?.CreateSnapshot().AccessibleMemories ?? Array.Empty<HistoryMemoryRecord>()).Select(memory => memory.HistoricalEventId), StringComparer.Ordinal);
            return eventsById.Values
                .Where(data => IsVisibleToPerson(data, personId, remembered))
                .Select(Wrap)
                .OrderBy(HistoryOrdering.Key)
                .ToArray();
        }

        public IReadOnlyList<BodyOccupationRecord> QueryBodyOccupations(string personId)
        {
            return OccupationsFromIndex(personId);
        }

        public HistorySnapshot CreateSnapshot()
        {
            return new HistorySnapshot(WorldId, HistoryRevision, eventsById.Values.Select(Wrap).ToArray(), occupationsById.Values.Select(data => new BodyOccupationRecord(data)).ToArray());
        }

        public AuthoritativeHistorySaveData CreateSaveData()
        {
            return new AuthoritativeHistorySaveData
            {
                schemaVersion = AuthoritativeHistorySaveData.CurrentSchemaVersion,
                worldId = WorldId,
                nextSequence = NextSequence,
                historyRevision = HistoryRevision,
                events = eventsById.Values.OrderBy(data => data.occurredAtWorldTime).ThenBy(data => data.sequence).ThenBy(data => data.eventId, StringComparer.Ordinal).Select(data => data.Clone()).ToArray(),
                bodyOccupations = occupationsById.Values.OrderBy(data => data.startedAtWorldTime).ThenBy(data => data.occupationId, StringComparer.Ordinal).Select(data => data.Clone()).ToArray(),
                processedTransactions = processedTransactions.OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }

        public HistoryOperationResult RestoreFromSaveData(AuthoritativeHistorySaveData saveData, DefinitionRegistry definitionRegistry, IEnumerable<string> knownPersons, IEnumerable<string> knownBodies, bool restoring = true)
        {
            if (!ValidateSaveData(saveData, definitionRegistry, knownPersons, knownBodies, out string failureReason))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.RestoreFailed, failureReason, revision: HistoryRevision);
            }

            AuthoritativeHistorySaveData rollback = CreateSaveData();
            try
            {
                suppressEvents = restoring;
                Configure(definitionRegistry, saveData.worldId, knownPersons, knownBodies);
                eventsById.Clear();
                ClearIndexes();
                occupationsById.Clear();
                occupationIdsByPerson.Clear();
                processedTransactions.Clear();

                foreach (HistoricalEventRecordData data in saveData.events ?? Array.Empty<HistoricalEventRecordData>())
                {
                    HistoricalEventRecordData clone = data.Clone();
                    eventsById[clone.eventId] = clone;
                    AddIndexes(clone);
                }

                foreach (BodyOccupationRecordData data in saveData.bodyOccupations ?? Array.Empty<BodyOccupationRecordData>())
                {
                    AddOccupation(data.Clone());
                }

                foreach (string transaction in saveData.processedTransactions ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(transaction))
                    {
                        processedTransactions.Add(transaction);
                    }
                }

                NextSequence = Math.Max(1L, saveData.nextSequence);
                HistoryRevision = Math.Max(0L, saveData.historyRevision);
                return HistoryOperationResult.Success("Authoritative history restored.", string.Empty, null, null, null, HistoryRevision, HistoryRevision);
            }
            catch (Exception exception)
            {
                RestoreFromSaveData(rollback, registry, knownPersonIds, knownBodyIds, restoring: true);
                return HistoryOperationResult.Failure(HistoryResultCode.RestoreFailed, exception.Message, revision: HistoryRevision);
            }
            finally
            {
                suppressEvents = false;
            }
        }

        public static bool ValidateSaveData(AuthoritativeHistorySaveData saveData, DefinitionRegistry definitionRegistry, IEnumerable<string> knownPersons, IEnumerable<string> knownBodies, out string failureReason)
        {
            failureReason = string.Empty;
            if (saveData == null)
            {
                failureReason = "Authoritative history save data is missing.";
                return false;
            }

            if (saveData.schemaVersion != AuthoritativeHistorySaveData.CurrentSchemaVersion)
            {
                failureReason = $"Unsupported Authoritative History schema version {saveData.schemaVersion}.";
                return false;
            }

            AuthoritativeHistoryRuntime validator = new AuthoritativeHistoryRuntime();
            validator.Configure(definitionRegistry, saveData.worldId, knownPersons, knownBodies);
            foreach (HistoricalEventRecordData eventData in saveData.events ?? Array.Empty<HistoricalEventRecordData>())
            {
                RecordHistoricalEventRequest request = RequestFromData(eventData);
                request.SupersedesEventId = string.Empty;
                if (!validator.ValidateRecordRequest(request, out _, out failureReason, out _))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(eventData.correctedByEventId) && !saveData.events.Any(data => string.Equals(data.eventId, eventData.correctedByEventId, StringComparison.Ordinal)))
                {
                    failureReason = $"Historical event '{eventData.eventId}' references missing correctedBy event '{eventData.correctedByEventId}'.";
                    return false;
                }

                validator.eventsById[eventData.eventId] = eventData.Clone();
            }

            foreach (HistoricalEventRecordData eventData in saveData.events ?? Array.Empty<HistoricalEventRecordData>())
            {
                if (!validator.ValidateSavedCorrectionLink(eventData, out failureReason))
                {
                    return false;
                }
            }

            HashSet<string> occupationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (BodyOccupationRecordData occupation in saveData.bodyOccupations ?? Array.Empty<BodyOccupationRecordData>())
            {
                if (!ValidateOccupation(occupation, validator.knownPersonIds, validator.knownBodyIds, out failureReason) || !occupationIds.Add(occupation.occupationId ?? string.Empty))
                {
                    failureReason = string.IsNullOrWhiteSpace(failureReason) ? $"Missing or duplicate body occupation ID '{occupation?.occupationId}'." : failureReason;
                    return false;
                }
            }

            return true;
        }

        private bool ValidateSavedCorrectionLink(HistoricalEventRecordData eventData, out string failure)
        {
            failure = string.Empty;
            if (eventData == null)
            {
                failure = "Historical event save entry is missing.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(eventData.correctedByEventId))
            {
                if (!eventsById.TryGetValue(eventData.correctedByEventId, out HistoricalEventRecordData correction))
                {
                    failure = $"Historical event '{eventData.eventId}' references missing correctedBy event '{eventData.correctedByEventId}'.";
                    return false;
                }

                if (!string.Equals(correction.supersedesEventId, eventData.eventId, StringComparison.Ordinal))
                {
                    failure = $"Historical event '{eventData.eventId}' correctedBy event '{eventData.correctedByEventId}' does not supersede it.";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(eventData.supersedesEventId))
            {
                return true;
            }

            if (string.Equals(eventData.eventId, eventData.supersedesEventId, StringComparison.Ordinal))
            {
                failure = "Historical event cannot supersede itself.";
                return false;
            }

            if (!eventsById.TryGetValue(eventData.supersedesEventId, out HistoricalEventRecordData target))
            {
                failure = $"Superseded historical event '{eventData.supersedesEventId}' is missing.";
                return false;
            }

            if (!string.Equals(target.correctedByEventId, eventData.eventId, StringComparison.Ordinal))
            {
                failure = $"Superseded historical event '{eventData.supersedesEventId}' does not point back to correction '{eventData.eventId}'.";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal) { eventData.supersedesEventId };
            string nextId = eventData.correctedByEventId;
            while (!string.IsNullOrWhiteSpace(nextId))
            {
                if (!seen.Add(nextId))
                {
                    failure = $"Historical event correction chain for '{eventData.supersedesEventId}' is circular.";
                    return false;
                }

                if (!eventsById.TryGetValue(nextId, out HistoricalEventRecordData next))
                {
                    failure = $"Historical event correction chain references missing event '{nextId}'.";
                    return false;
                }

                nextId = next.correctedByEventId;
            }

            return true;
        }

        private bool ValidateRecordRequest(RecordHistoricalEventRequest request, out HistoricalEventDefinition definition, out string failure, out HistoryResultCode code)
        {
            definition = null;
            failure = string.Empty;
            code = HistoryResultCode.InvalidRequest;
            if (request == null)
            {
                failure = "Historical event request is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.TransactionId) || string.IsNullOrWhiteSpace(request.EventId))
            {
                failure = "Historical event requires transaction and event IDs.";
                return false;
            }

            if (eventsById.ContainsKey(request.EventId))
            {
                failure = $"Historical event '{request.EventId}' already exists.";
                return false;
            }

            if (registry == null || !registry.TryGet(request.EventDefinitionId, out definition))
            {
                code = HistoryResultCode.MissingDefinition;
                failure = $"Historical event definition '{request.EventDefinitionId}' is missing.";
                return false;
            }

            if (double.IsNaN(request.OccurredAtWorldTime) || double.IsInfinity(request.OccurredAtWorldTime) || request.RecordedAtWorldTime < request.OccurredAtWorldTime)
            {
                code = HistoryResultCode.InvalidTimeRange;
                failure = "Historical event has invalid occurrence or recorded time.";
                return false;
            }

            if (!ValidateKnownPerson(request.PrimaryPersonId, required: false, out failure))
            {
                code = HistoryResultCode.MissingPerson;
                return false;
            }

            foreach (string person in Distinct(request.ParticipantPersonIds))
            {
                if (!ValidateKnownPerson(person, required: true, out failure))
                {
                    code = HistoryResultCode.MissingPerson;
                    return false;
                }
            }

            foreach (string body in Distinct(request.BodyIds))
            {
                if (!ValidateKnownBody(body, out failure))
                {
                    code = HistoryResultCode.MissingBody;
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SupersedesEventId) && !CanSupersede(request.EventId, request.SupersedesEventId, out failure))
            {
                code = failure.Contains("circular", StringComparison.OrdinalIgnoreCase) ? HistoryResultCode.CircularCorrection : HistoryResultCode.InvalidCorrection;
                return false;
            }

            return true;
        }

        private static RecordHistoricalEventRequest RequestFromData(HistoricalEventRecordData data)
        {
            return new RecordHistoricalEventRequest
            {
                TransactionId = $"restore.{data?.eventId}",
                EventId = data?.eventId,
                EventDefinitionId = data?.eventDefinitionId,
                OccurredAtWorldTime = data?.occurredAtWorldTime ?? 0d,
                RecordedAtWorldTime = data?.recordedAtWorldTime ?? 0d,
                Sequence = data?.sequence,
                PrimaryPersonId = data?.primaryPersonId,
                ParticipantPersonIds = data?.participantPersonIds,
                BodyIds = data?.bodyIds,
                LocationId = data?.locationId,
                OrganizationId = data?.organizationId,
                RelatedEntityIds = data?.relatedEntityIds,
                Category = data?.category,
                Visibility = data?.visibility,
                SourceSystem = data?.sourceSystem,
                Provenance = data?.provenance,
                SupersedesEventId = data?.supersedesEventId,
                CorrelationId = data?.correlationId,
                Tags = data?.tags,
                Payload = data?.payload
            };
        }

        private HistoricalEventRecordData CreateEventData(RecordHistoricalEventRequest request, HistoricalEventDefinition definition)
        {
            string[] persons = Distinct((request.ParticipantPersonIds ?? Array.Empty<string>()).Concat(new[] { request.PrimaryPersonId })).ToArray();
            HistoricalEventCategory category = request.Category ?? definition.Category;
            KnowledgeVisibility visibility = request.Visibility ?? definition.DefaultVisibility;
            long sequence = request.Sequence ?? NextSequence++;
            return new HistoricalEventRecordData
            {
                eventId = request.EventId,
                eventDefinitionId = definition.Id,
                occurredAtWorldTime = request.OccurredAtWorldTime,
                recordedAtWorldTime = request.RecordedAtWorldTime,
                sequence = sequence,
                primaryPersonId = request.PrimaryPersonId,
                participantPersonIds = persons,
                bodyIds = Distinct(request.BodyIds).ToArray(),
                locationId = request.LocationId,
                organizationId = request.OrganizationId,
                relatedEntityIds = Distinct(request.RelatedEntityIds).ToArray(),
                category = category,
                visibility = visibility,
                status = string.IsNullOrWhiteSpace(request.SupersedesEventId) ? HistoricalEventStatus.Active : HistoricalEventStatus.Correction,
                sourceSystem = request.SourceSystem,
                provenance = request.Provenance,
                supersedesEventId = request.SupersedesEventId,
                correlationId = request.CorrelationId,
                tags = Distinct((request.Tags ?? Array.Empty<string>()).Concat(definition.Tags)).ToArray(),
                payload = request.Payload?.Clone() ?? new HistoricalEventPayloadData { kind = definition.PayloadKind }
            };
        }

        private bool CanSupersede(string eventId, string targetEventId, out string failure)
        {
            failure = string.Empty;
            if (string.Equals(eventId, targetEventId, StringComparison.Ordinal))
            {
                failure = "Historical event cannot supersede itself.";
                return false;
            }

            if (!eventsById.TryGetValue(targetEventId ?? string.Empty, out HistoricalEventRecordData target))
            {
                failure = $"Superseded historical event '{targetEventId}' is missing.";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal) { eventId };
            while (!string.IsNullOrWhiteSpace(target.correctedByEventId))
            {
                if (!seen.Add(target.correctedByEventId))
                {
                    failure = $"Historical event correction chain for '{targetEventId}' is circular.";
                    return false;
                }

                if (!eventsById.TryGetValue(target.correctedByEventId, out target))
                {
                    break;
                }
            }

            return true;
        }

        private bool ValidateKnownPerson(string personId, bool required, out string failure)
        {
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(personId))
            {
                if (!required)
                {
                    return true;
                }

                failure = "Historical event references an empty Person ID.";
                return false;
            }

            if (knownPersonIds.Count > 0 && !knownPersonIds.Contains(personId))
            {
                failure = $"Historical event references unknown Person '{personId}'.";
                return false;
            }

            return true;
        }

        private bool ValidateKnownBody(string bodyId, out string failure)
        {
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(bodyId))
            {
                return true;
            }

            if (knownBodyIds.Count > 0 && !knownBodyIds.Contains(bodyId))
            {
                failure = $"Historical event references unknown Body '{bodyId}'.";
                return false;
            }

            return true;
        }

        private static bool ValidateOccupation(BodyOccupationRecordData occupation, HashSet<string> knownPersons, HashSet<string> knownBodies, out string failure)
        {
            failure = string.Empty;
            if (occupation == null || string.IsNullOrWhiteSpace(occupation.occupationId) || string.IsNullOrWhiteSpace(occupation.personId) || string.IsNullOrWhiteSpace(occupation.bodyId))
            {
                failure = "Body occupation record is missing required IDs.";
                return false;
            }

            if (occupation.endedAtWorldTime >= 0d && occupation.endedAtWorldTime < occupation.startedAtWorldTime)
            {
                failure = $"Body occupation '{occupation.occupationId}' has an invalid time range.";
                return false;
            }

            if (knownPersons.Count > 0 && !knownPersons.Contains(occupation.personId))
            {
                failure = $"Body occupation '{occupation.occupationId}' references unknown Person '{occupation.personId}'.";
                return false;
            }

            if (knownBodies.Count > 0 && !knownBodies.Contains(occupation.bodyId))
            {
                failure = $"Body occupation '{occupation.occupationId}' references unknown Body '{occupation.bodyId}'.";
                return false;
            }

            return true;
        }

        private void AddIndexes(HistoricalEventRecordData data)
        {
            foreach (string person in Distinct(data.participantPersonIds))
            {
                AddIndex(eventIdsByPerson, person, data.eventId);
            }

            foreach (string body in Distinct(data.bodyIds))
            {
                AddIndex(eventIdsByBody, body, data.eventId);
            }

            AddIndex(eventIdsByLocation, data.locationId, data.eventId);
            AddIndex(eventIdsByOrganization, data.organizationId, data.eventId);
            foreach (string tag in Distinct(data.tags))
            {
                AddIndex(eventIdsByTag, tag, data.eventId);
            }
        }

        private void AddOccupation(BodyOccupationRecordData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.occupationId))
            {
                return;
            }

            occupationsById[data.occupationId] = data.Clone();
            AddIndex(occupationIdsByPerson, data.personId, data.occupationId);
        }

        private void CloseOpenOccupation(string personId, string bodyId, double endTime, string endEventId)
        {
            foreach (BodyOccupationRecordData occupation in occupationsById.Values.Where(record => string.Equals(record.personId, personId, StringComparison.Ordinal) && record.IsOpenEnded() && (string.IsNullOrWhiteSpace(bodyId) || string.Equals(record.bodyId, bodyId, StringComparison.Ordinal))).ToArray())
            {
                occupation.endedAtWorldTime = endTime;
                occupation.endEventId = endEventId;
            }
        }

        private IReadOnlyList<BodyOccupationRecord> OccupationsFromIndex(string personId)
        {
            if (!occupationIdsByPerson.TryGetValue(personId ?? string.Empty, out List<string> ids))
            {
                return Array.Empty<BodyOccupationRecord>();
            }

            return ids.Where(id => occupationsById.ContainsKey(id)).Select(id => new BodyOccupationRecord(occupationsById[id])).OrderBy(record => record.StartedAtWorldTime).ThenBy(record => record.OccupationId, StringComparer.Ordinal).ToArray();
        }

        private IReadOnlyList<HistoricalEventRecord> RecordsFromIndex(Dictionary<string, List<string>> index, string key)
        {
            if (!index.TryGetValue(key ?? string.Empty, out List<string> ids))
            {
                return Array.Empty<HistoricalEventRecord>();
            }

            return ids.Where(id => eventsById.ContainsKey(id)).Select(id => Wrap(eventsById[id])).OrderBy(HistoryOrdering.Key).ToArray();
        }

        private static void AddIndex(Dictionary<string, List<string>> index, string key, string eventId)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            if (!index.TryGetValue(key, out List<string> ids))
            {
                ids = new List<string>();
                index[key] = ids;
            }

            if (!ids.Contains(eventId, StringComparer.Ordinal))
            {
                ids.Add(eventId);
            }
        }

        private bool IsVisibleToPerson(HistoricalEventRecordData data, string personId, HashSet<string> rememberedEventIds)
        {
            if (data == null)
            {
                return false;
            }

            if (rememberedEventIds.Contains(data.eventId))
            {
                return true;
            }

            if (data.visibility == KnowledgeVisibility.Public || data.visibility == KnowledgeVisibility.PersonallyObservable)
            {
                return true;
            }

            return data.visibility == KnowledgeVisibility.Private
                && !string.IsNullOrWhiteSpace(personId)
                && (string.Equals(data.primaryPersonId, personId, StringComparison.Ordinal)
                    || (data.participantPersonIds ?? Array.Empty<string>()).Contains(personId, StringComparer.Ordinal));
        }

        private HistoricalEventRecord Wrap(HistoricalEventRecordData data)
        {
            HistoricalEventDefinition definition = null;
            registry?.TryGet(data?.eventDefinitionId, out definition);
            return new HistoricalEventRecord(data, definition);
        }

        private void ClearIndexes()
        {
            eventIdsByPerson.Clear();
            eventIdsByBody.Clear();
            eventIdsByLocation.Clear();
            eventIdsByOrganization.Clear();
            eventIdsByTag.Clear();
        }

        private void RaiseChanged(HistoryOperationResult result, bool restoring)
        {
            if (!restoring && !suppressEvents)
            {
                HistoryChanged?.Invoke(this, result);
            }
        }

        private static IEnumerable<string> Distinct(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal);
        }

        private static string TransactionKey(string transactionId)
        {
            return transactionId ?? string.Empty;
        }
    }

    internal static class BodyOccupationRuntimeExtensions
    {
        public static bool IsOpenEnded(this BodyOccupationRecordData data)
        {
            return data != null && data.endedAtWorldTime < 0d;
        }
    }
}
