using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Knowledge;

namespace UnityIsekaiGame.Knowledge.History
{
    public sealed class PersonMemoryRuntime
    {
        private readonly Dictionary<string, HistoryMemoryRecordData> memoriesById = new Dictionary<string, HistoryMemoryRecordData>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> memoryIdsByEvent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly HashSet<string> processedTransactions = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownPersonIds = new HashSet<string>(StringComparer.Ordinal);
        private AuthoritativeHistoryRuntime historyRuntime;
        private DefinitionRegistry registry;
        private bool suppressEvents;

        public event Action<PersonMemoryRuntime, HistoryOperationResult> MemoryChanged;

        public string PersonId { get; private set; } = string.Empty;
        public long MemoryRevision { get; private set; }

        public void Configure(string personId, DefinitionRegistry definitionRegistry, AuthoritativeHistoryRuntime authoritativeHistory, IEnumerable<string> knownPersons = null)
        {
            PersonId = personId ?? string.Empty;
            registry = definitionRegistry ?? registry;
            historyRuntime = authoritativeHistory ?? historyRuntime;
            knownPersonIds = new HashSet<string>((knownPersons ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
        }

        public HistoryOperationResult PreviewFormMemory(FormMemoryRequest request, PersonKnowledgeRuntime knowledge = null)
        {
            return FormMemory(request, knowledge, preview: true, restoring: false);
        }

        public HistoryOperationResult FormMemory(FormMemoryRequest request, PersonKnowledgeRuntime knowledge = null, bool preview = false, bool restoring = false)
        {
            long priorRevision = MemoryRevision;
            if (!ValidateFormMemory(request, out string failure, out HistoryResultCode code))
            {
                return HistoryOperationResult.Failure(code, failure, request?.TransactionId, preview, MemoryRevision);
            }

            if (!preview && processedTransactions.Contains(request.TransactionId ?? string.Empty))
            {
                return HistoryOperationResult.Success("Memory transaction already processed.", request.TransactionId, null, TryWrap(request.MemoryId), null, priorRevision, MemoryRevision, duplicate: true);
            }

            HistoryMemoryRecordData data = new HistoryMemoryRecordData
            {
                memoryId = request.MemoryId,
                ownerPersonId = request.OwnerPersonId,
                historicalEventId = request.HistoricalEventId,
                beliefId = request.BeliefId,
                evidenceIds = request.EvidenceIds == null ? Array.Empty<string>() : request.EvidenceIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray(),
                source = request.Source,
                formedAtWorldTime = request.FormedAtWorldTime,
                rememberedOccurredAtWorldTime = request.RememberedOccurredAtWorldTime,
                lastRecalledWorldTime = request.FormedAtWorldTime,
                confidence = HistoryMath.ClampMetric(request.Confidence),
                clarity = HistoryMath.ClampMetric(request.Clarity),
                salience = HistoryMath.ClampMetric(request.Salience),
                firstHand = request.FirstHand,
                state = MemoryState.Accessible,
                visibility = request.Visibility,
                identityAtTimeId = request.IdentityAtTimeId,
                bodyAtTimeId = request.BodyAtTimeId,
                tags = request.Tags == null ? Array.Empty<string>() : request.Tags.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray(),
                debugDescription = request.DebugDescription
            };

            KnowledgeOperationResult knowledgeResult = null;
            if (request.CreateKnowledgeEvidence && knowledge != null && historyRuntime != null && historyRuntime.TryGetEvent(request.HistoricalEventId, out HistoricalEventRecord historicalEvent))
            {
                knowledgeResult = CreateKnowledgeForMemory(request, knowledge, historicalEvent, preview);
                if (!knowledgeResult.Succeeded)
                {
                    return HistoryOperationResult.Failure(HistoryResultCode.KnowledgeRejected, knowledgeResult.Message, request.TransactionId, preview, MemoryRevision);
                }
            }

            HistoryMemoryRecord snapshot = new HistoryMemoryRecord(data);
            if (preview)
            {
                return HistoryOperationResult.Success("Memory preview succeeded.", request.TransactionId, null, snapshot, knowledgeResult, priorRevision, MemoryRevision, preview: true);
            }

            memoriesById[data.memoryId] = data;
            AddIndex(data.historicalEventId, data.memoryId);
            processedTransactions.Add(request.TransactionId ?? string.Empty);
            MemoryRevision++;
            HistoryOperationResult result = HistoryOperationResult.Success("Memory formed.", request.TransactionId, null, new HistoryMemoryRecord(data), knowledgeResult, priorRevision, MemoryRevision);
            RaiseChanged(result, restoring);
            return result;
        }

        public HistoryOperationResult RecallMemory(string memoryId, string transactionId, double recalledAtWorldTime, int clarityReinforcement = 50, bool preview = false, bool restoring = false)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.InvalidRequest, "Memory recall requires a transaction ID.", transactionId, preview, MemoryRevision);
            }

            if (!memoriesById.TryGetValue(memoryId ?? string.Empty, out HistoryMemoryRecordData memory))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.MissingMemory, $"Memory '{memoryId}' is missing.", transactionId, preview, MemoryRevision);
            }

            long priorRevision = MemoryRevision;
            if (preview)
            {
                return HistoryOperationResult.Success("Memory recall preview succeeded.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision, preview: true);
            }

            if (processedTransactions.Contains(transactionId))
            {
                return HistoryOperationResult.Success("Memory recall transaction already processed.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision, duplicate: true);
            }

            memory.lastRecalledWorldTime = Math.Max(memory.lastRecalledWorldTime, recalledAtWorldTime);
            memory.clarity = HistoryMath.ClampMetric(memory.clarity + Math.Max(0, clarityReinforcement));
            if (memory.state == MemoryState.Inaccessible || memory.state == MemoryState.Uncertain)
            {
                memory.state = MemoryState.Accessible;
            }

            processedTransactions.Add(transactionId);
            MemoryRevision++;
            HistoryOperationResult result = HistoryOperationResult.Success("Memory recalled.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision);
            RaiseChanged(result, restoring);
            return result;
        }

        public HistoryOperationResult ForgetMemory(string memoryId, string transactionId, int clarityReduction = 1000, bool preview = false, bool restoring = false)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.InvalidRequest, "Memory forgetting requires a transaction ID.", transactionId, preview, MemoryRevision);
            }

            if (!memoriesById.TryGetValue(memoryId ?? string.Empty, out HistoryMemoryRecordData memory))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.MissingMemory, $"Memory '{memoryId}' is missing.", transactionId, preview, MemoryRevision);
            }

            long priorRevision = MemoryRevision;
            if (preview)
            {
                return HistoryOperationResult.Success("Memory forgetting preview succeeded.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision, preview: true);
            }

            if (processedTransactions.Contains(transactionId))
            {
                return HistoryOperationResult.Success("Memory forgetting transaction already processed.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision, duplicate: true);
            }

            memory.clarity = HistoryMath.ClampMetric(memory.clarity - Math.Max(0, clarityReduction));
            memory.state = memory.clarity == 0 ? MemoryState.Forgotten : MemoryState.Inaccessible;
            processedTransactions.Add(transactionId);
            MemoryRevision++;
            HistoryOperationResult result = HistoryOperationResult.Success("Memory access reduced.", transactionId, null, new HistoryMemoryRecord(memory), null, priorRevision, MemoryRevision);
            RaiseChanged(result, restoring);
            return result;
        }

        public HistoryOperationResult CorrectMemory(string existingMemoryId, FormMemoryRequest correctedMemoryRequest, bool preview = false, bool restoring = false)
        {
            if (!memoriesById.TryGetValue(existingMemoryId ?? string.Empty, out HistoryMemoryRecordData existing))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.MissingMemory, $"Memory '{existingMemoryId}' is missing.", correctedMemoryRequest?.TransactionId, preview, MemoryRevision);
            }

            HistoryOperationResult formed = FormMemory(correctedMemoryRequest, null, preview, restoring);
            if (!formed.Succeeded || preview)
            {
                return formed;
            }

            existing.state = MemoryState.Corrected;
            existing.correctedByMemoryId = correctedMemoryRequest.MemoryId;
            memoriesById[correctedMemoryRequest.MemoryId].correctionOfMemoryId = existingMemoryId;
            MemoryRevision++;
            return formed;
        }

        public bool TryGetMemory(string memoryId, out HistoryMemoryRecord memory)
        {
            if (memoriesById.TryGetValue(memoryId ?? string.Empty, out HistoryMemoryRecordData data))
            {
                memory = new HistoryMemoryRecord(data);
                return true;
            }

            memory = null;
            return false;
        }

        public IReadOnlyList<HistoryMemoryRecord> QueryByEvent(string eventId)
        {
            if (!memoryIdsByEvent.TryGetValue(eventId ?? string.Empty, out List<string> ids))
            {
                return Array.Empty<HistoryMemoryRecord>();
            }

            return ids.Where(id => memoriesById.ContainsKey(id)).Select(id => new HistoryMemoryRecord(memoriesById[id])).OrderBy(record => record.MemoryId, StringComparer.Ordinal).ToArray();
        }

        public PersonMemorySnapshot CreateSnapshot()
        {
            return new PersonMemorySnapshot(PersonId, MemoryRevision, memoriesById.Values.Select(data => new HistoryMemoryRecord(data)).ToArray());
        }

        public PersonMemorySaveData CreateSaveData()
        {
            return new PersonMemorySaveData
            {
                schemaVersion = PersonMemorySaveData.CurrentSchemaVersion,
                personId = PersonId,
                memoryRevision = MemoryRevision,
                memories = memoriesById.Values.OrderBy(data => data.memoryId, StringComparer.Ordinal).Select(data => data.Clone()).ToArray(),
                processedTransactions = processedTransactions.OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }

        public HistoryOperationResult RestoreFromSaveData(PersonMemorySaveData saveData, DefinitionRegistry definitionRegistry, AuthoritativeHistoryRuntime authoritativeHistory, IEnumerable<string> knownPersons, bool restoring = true)
        {
            if (!ValidateSaveData(saveData, authoritativeHistory, knownPersons, out string failureReason))
            {
                return HistoryOperationResult.Failure(HistoryResultCode.RestoreFailed, failureReason, revision: MemoryRevision);
            }

            PersonMemorySaveData rollback = CreateSaveData();
            try
            {
                suppressEvents = restoring;
                Configure(saveData.personId, definitionRegistry, authoritativeHistory, knownPersons);
                memoriesById.Clear();
                memoryIdsByEvent.Clear();
                processedTransactions.Clear();

                foreach (HistoryMemoryRecordData memory in saveData.memories ?? Array.Empty<HistoryMemoryRecordData>())
                {
                    HistoryMemoryRecordData clone = memory.Clone();
                    memoriesById[clone.memoryId] = clone;
                    AddIndex(clone.historicalEventId, clone.memoryId);
                }

                foreach (string transaction in saveData.processedTransactions ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(transaction))
                    {
                        processedTransactions.Add(transaction);
                    }
                }

                MemoryRevision = Math.Max(0L, saveData.memoryRevision);
                return HistoryOperationResult.Success("Person memories restored.", string.Empty, null, null, null, MemoryRevision, MemoryRevision);
            }
            catch (Exception exception)
            {
                RestoreFromSaveData(rollback, registry, historyRuntime, knownPersonIds, restoring: true);
                return HistoryOperationResult.Failure(HistoryResultCode.RestoreFailed, exception.Message, revision: MemoryRevision);
            }
            finally
            {
                suppressEvents = false;
            }
        }

        public static bool ValidateSaveData(PersonMemorySaveData saveData, AuthoritativeHistoryRuntime authoritativeHistory, IEnumerable<string> knownPersons, out string failureReason)
        {
            failureReason = string.Empty;
            if (saveData == null)
            {
                failureReason = "Person memory save data is missing.";
                return false;
            }

            if (saveData.schemaVersion != PersonMemorySaveData.CurrentSchemaVersion)
            {
                failureReason = $"Unsupported Person Memory schema version {saveData.schemaVersion}.";
                return false;
            }

            HashSet<string> known = new HashSet<string>((knownPersons ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(saveData.personId) || known.Count > 0 && !known.Contains(saveData.personId))
            {
                failureReason = $"Person memory save references unknown Person '{saveData.personId}'.";
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (HistoryMemoryRecordData memory in saveData.memories ?? Array.Empty<HistoryMemoryRecordData>())
            {
                if (!ValidateMemoryData(memory, saveData.personId, authoritativeHistory, out failureReason) || !ids.Add(memory.memoryId ?? string.Empty))
                {
                    failureReason = string.IsNullOrWhiteSpace(failureReason) ? $"Missing or duplicate memory ID '{memory?.memoryId}'." : failureReason;
                    return false;
                }
            }

            return true;
        }

        private bool ValidateFormMemory(FormMemoryRequest request, out string failure, out HistoryResultCode code)
        {
            failure = string.Empty;
            code = HistoryResultCode.InvalidRequest;
            if (request == null || string.IsNullOrWhiteSpace(request.TransactionId) || string.IsNullOrWhiteSpace(request.MemoryId))
            {
                failure = "Memory formation requires transaction and memory IDs.";
                return false;
            }

            if (!string.Equals(request.OwnerPersonId, PersonId, StringComparison.Ordinal))
            {
                code = HistoryResultCode.MissingPerson;
                failure = $"Memory request targets Person '{request.OwnerPersonId}', but runtime owns '{PersonId}'.";
                return false;
            }

            if (knownPersonIds.Count > 0 && !knownPersonIds.Contains(request.OwnerPersonId))
            {
                code = HistoryResultCode.MissingPerson;
                failure = $"Memory request references unknown Person '{request.OwnerPersonId}'.";
                return false;
            }

            if (memoriesById.ContainsKey(request.MemoryId))
            {
                failure = $"Memory '{request.MemoryId}' already exists.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.HistoricalEventId) && historyRuntime != null && !historyRuntime.TryGetEvent(request.HistoricalEventId, out _))
            {
                code = HistoryResultCode.MissingEvent;
                failure = $"Memory references missing historical event '{request.HistoricalEventId}'.";
                return false;
            }

            if (request.FormedAtWorldTime < request.RememberedOccurredAtWorldTime)
            {
                code = HistoryResultCode.InvalidTimeRange;
                failure = "Memory cannot form before the remembered event occurs.";
                return false;
            }

            return true;
        }

        private static bool ValidateMemoryData(HistoryMemoryRecordData memory, string expectedPersonId, AuthoritativeHistoryRuntime authoritativeHistory, out string failure)
        {
            failure = string.Empty;
            if (memory == null || string.IsNullOrWhiteSpace(memory.memoryId))
            {
                failure = "Memory data is missing an ID.";
                return false;
            }

            if (!string.Equals(memory.ownerPersonId, expectedPersonId, StringComparison.Ordinal))
            {
                failure = $"Memory '{memory.memoryId}' is owned by '{memory.ownerPersonId}' instead of '{expectedPersonId}'.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(memory.historicalEventId) && authoritativeHistory != null && !authoritativeHistory.TryGetEvent(memory.historicalEventId, out _))
            {
                failure = $"Memory '{memory.memoryId}' references missing historical event '{memory.historicalEventId}'.";
                return false;
            }

            if (memory.formedAtWorldTime < memory.rememberedOccurredAtWorldTime)
            {
                failure = $"Memory '{memory.memoryId}' has invalid time ordering.";
                return false;
            }

            return true;
        }

        private KnowledgeOperationResult CreateKnowledgeForMemory(FormMemoryRequest request, PersonKnowledgeRuntime knowledge, HistoricalEventRecord historicalEvent, bool preview)
        {
            KnowledgeObservationRequest observation = new KnowledgeObservationRequest
            {
                PersonId = request.OwnerPersonId,
                TransactionId = $"{request.TransactionId}.knowledge",
                Proposition = new KnowledgePropositionData
                {
                    factDefinitionId = BuiltInKnowledgeFacts.EventOccurred,
                    subjectType = KnowledgeSubjectType.Event,
                    subjectId = request.HistoricalEventId,
                    valueType = KnowledgeValueType.Boolean,
                    booleanValue = true,
                    bodyContextId = request.BodyAtTimeId,
                    sourceContextId = request.MemoryId,
                    sourceRevision = historicalEvent.Sequence
                },
                AcquisitionSource = AcquisitionSourceFor(request.Source),
                Provenance = ProvenanceFor(request.Source),
                Direction = KnowledgeEvidenceDirection.Supports,
                Strength = request.Confidence,
                Credibility = request.Clarity,
                GameTimeSeconds = request.FormedAtWorldTime,
                SourceId = request.HistoricalEventId,
                Visibility = request.Visibility,
                RelatedEventId = request.HistoricalEventId,
                EvidenceId = $"evidence.memory.{request.MemoryId}",
                PrivateAccessAuthorized = request.Visibility >= KnowledgeVisibility.Private,
                Tags = (request.Tags ?? Array.Empty<string>()).Concat(new[] { "history-memory", request.Source.ToString() }).ToArray()
            };
            return preview ? knowledge.PreviewObservation(observation) : knowledge.RecordObservation(observation);
        }

        private static KnowledgeAcquisitionSource AcquisitionSourceFor(HistoryMemorySource source)
        {
            return source switch
            {
                HistoryMemorySource.WitnessTestimony => KnowledgeAcquisitionSource.Testimony,
                HistoryMemorySource.WrittenRecord => KnowledgeAcquisitionSource.WrittenSource,
                HistoryMemorySource.Investigation => KnowledgeAcquisitionSource.SkillOrEducation,
                HistoryMemorySource.Examination => KnowledgeAcquisitionSource.Examination,
                HistoryMemorySource.Diagnosis => KnowledgeAcquisitionSource.Examination,
                HistoryMemorySource.Inference => KnowledgeAcquisitionSource.SkillOrEducation,
                HistoryMemorySource.KnowledgeSharing => KnowledgeAcquisitionSource.Testimony,
                HistoryMemorySource.PreviousBody => KnowledgeAcquisitionSource.PersonalExperience,
                HistoryMemorySource.ScriptedSetup => KnowledgeAcquisitionSource.ScriptedRevelation,
                HistoryMemorySource.DevelopmentFixture => KnowledgeAcquisitionSource.DevelopmentFixture,
                _ => KnowledgeAcquisitionSource.DirectObservation
            };
        }

        private static KnowledgeProvenance ProvenanceFor(HistoryMemorySource source)
        {
            return source switch
            {
                HistoryMemorySource.WitnessTestimony => KnowledgeProvenance.Testimony,
                HistoryMemorySource.WrittenRecord => KnowledgeProvenance.Document,
                HistoryMemorySource.Investigation => KnowledgeProvenance.Inference,
                HistoryMemorySource.Examination => KnowledgeProvenance.Examination,
                HistoryMemorySource.Diagnosis => KnowledgeProvenance.Examination,
                HistoryMemorySource.Inference => KnowledgeProvenance.Inference,
                HistoryMemorySource.KnowledgeSharing => KnowledgeProvenance.Testimony,
                HistoryMemorySource.PreviousBody => KnowledgeProvenance.Memory,
                HistoryMemorySource.ScriptedSetup => KnowledgeProvenance.ScriptedDiscovery,
                HistoryMemorySource.DevelopmentFixture => KnowledgeProvenance.DevelopmentFixture,
                _ => KnowledgeProvenance.DirectObservation
            };
        }

        private void AddIndex(string eventId, string memoryId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(memoryId))
            {
                return;
            }

            if (!memoryIdsByEvent.TryGetValue(eventId, out List<string> ids))
            {
                ids = new List<string>();
                memoryIdsByEvent[eventId] = ids;
            }

            if (!ids.Contains(memoryId, StringComparer.Ordinal))
            {
                ids.Add(memoryId);
            }
        }

        private HistoryMemoryRecord TryWrap(string memoryId)
        {
            return memoriesById.TryGetValue(memoryId ?? string.Empty, out HistoryMemoryRecordData data) ? new HistoryMemoryRecord(data) : null;
        }

        private void RaiseChanged(HistoryOperationResult result, bool restoring)
        {
            if (!restoring && !suppressEvents)
            {
                MemoryChanged?.Invoke(this, result);
            }
        }
    }
}
