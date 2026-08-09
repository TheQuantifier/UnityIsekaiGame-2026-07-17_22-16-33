using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.WorldLocations
{
    public sealed class LocationRuntime : IDisposable
    {
        private readonly Dictionary<string, LocationRecordData> recordsById = new Dictionary<string, LocationRecordData>(StringComparer.Ordinal);
        private readonly Dictionary<string, LocationNameRecordData> namesById = new Dictionary<string, LocationNameRecordData>(StringComparer.Ordinal);
        private readonly Dictionary<string, LocationTransactionRecordData> transactionsById = new Dictionary<string, LocationTransactionRecordData>(StringComparer.Ordinal);
        private DefinitionRegistry registry;
        private string worldId = PersistenceService.LocalWorldId;
        private HashSet<string> knownPropertyIds = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownOrganizationIds = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownGovernmentIds = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> knownTerritoryIds = new HashSet<string>(StringComparer.Ordinal);
        private bool disposed;

        public long Revision { get; private set; }
        public bool IsDirty { get; private set; }
        public bool IsReady => registry != null && !string.IsNullOrWhiteSpace(worldId) && !disposed;
        public bool IsDisposed => disposed;
        public int Count => recordsById.Count;
        public string WorldId => worldId;
        public IReadOnlyList<LocationSnapshot> Snapshots => recordsById.Values.OrderBy(record => record.locationId, StringComparer.Ordinal).Select(BuildSnapshot).ToArray();

        public void Configure(
            DefinitionRegistry definitionRegistry,
            string world,
            IEnumerable<string> knownProperties = null,
            IEnumerable<string> knownOrganizations = null,
            IEnumerable<string> knownGovernments = null,
            IEnumerable<string> knownTerritories = null)
        {
            registry = definitionRegistry ?? registry;
            worldId = string.IsNullOrWhiteSpace(world) ? worldId : world.Trim();
            knownPropertyIds = new HashSet<string>(Clean(knownProperties), StringComparer.Ordinal);
            knownOrganizationIds = new HashSet<string>(Clean(knownOrganizations), StringComparer.Ordinal);
            knownGovernmentIds = new HashSet<string>(Clean(knownGovernments), StringComparer.Ordinal);
            knownTerritoryIds = new HashSet<string>(Clean(knownTerritories), StringComparer.Ordinal);
            disposed = false;
        }

        public LocationOperationResult CreateLocation(LocationCreateRequest request)
        {
            request ??= new LocationCreateRequest();
            long before = Revision;
            if (disposed) return Fail(LocationOperationStatus.Disposed, "Location runtime is disposed.", before);
            string locationId = Normalize(request.locationId);
            string tx = Normalize(request.transactionId);
            if (TryDuplicate(tx, locationId, "create", before, out LocationOperationResult duplicate))
            {
                return duplicate;
            }

            if (request.expectedRevision >= 0L && request.expectedRevision != Revision)
            {
                return Fail(LocationOperationStatus.RevisionConflict, $"Expected location runtime revision {request.expectedRevision}, but current revision is {Revision}.", before);
            }

            if (string.IsNullOrWhiteSpace(locationId) || string.IsNullOrWhiteSpace(request.locationDefinitionId))
            {
                return Fail(LocationOperationStatus.InvalidRequest, "Location ID and definition ID are required.", before);
            }

            if (recordsById.TryGetValue(locationId, out LocationRecordData existing))
            {
                if (string.Equals(existing.locationDefinitionId, Normalize(request.locationDefinitionId), StringComparison.Ordinal)
                    && string.Equals(existing.officialName, NormalizeName(request.officialName), StringComparison.Ordinal))
                {
                    return LocationOperationResult.Success(BuildSnapshot(existing), "Location already exists.", before, before, duplicate: true);
                }

                return Fail(LocationOperationStatus.DuplicateLocationId, $"Location '{locationId}' already exists with different data.", before);
            }

            if (!TryGetDefinition(request.locationDefinitionId, out LocationDefinition definition, before, out LocationOperationResult failure))
            {
                return failure;
            }

            LocationLifecycleState lifecycle = request.initialLifecycleState == LocationLifecycleState.Unknown ? LocationLifecycleState.Active : request.initialLifecycleState;
            if (!CanCreateLifecycle(lifecycle))
            {
                return Fail(LocationOperationStatus.InvalidLifecycleTransition, "Locations must be created proposed, active, inactive, closed, or historical.", before);
            }

            string officialName = NormalizeName(request.officialName);
            if (string.IsNullOrWhiteSpace(officialName))
            {
                return Fail(LocationOperationStatus.InvalidName, "Location official name is required.", before);
            }

            if (!definition.SupportsVisibility(request.visibility))
            {
                return Fail(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not support visibility '{request.visibility}'.", before);
            }

            string[] tags = Clean(request.semanticTagIds);
            foreach (string tag in tags)
            {
                if (!definition.AllowsSemanticTag(tag))
                {
                    return Fail(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not allow semantic tag '{tag}'.", before);
                }
            }

            if (!ValidateAssociations(definition, request.associatedPropertyId, request.associatedOrganizationId, request.associatedGovernmentId, request.associatedTerritoryIds, request.associations, before, out failure))
            {
                return failure;
            }

            if (request.preview)
            {
                return LocationOperationResult.Success(BuildSnapshot(CreateRecord(request, definition, locationId, officialName, tags)), "Location create preview.", before, before, preview: true);
            }

            LocationRecordData record = CreateRecord(request, definition, locationId, officialName, tags);
            recordsById.Add(record.locationId, record);
            namesById.Add(record.currentOfficialNameRecordId, new LocationNameRecordData
            {
                nameRecordId = record.currentOfficialNameRecordId,
                locationId = record.locationId,
                category = LocationNameCategory.Official,
                value = record.officialName,
                effectiveStartWorldTime = request.createdWorldTime,
                visibility = request.visibility,
                sourceEventId = Normalize(request.sourceEventId),
                sourceRecordId = Normalize(request.sourceRecordId),
                provenanceId = Normalize(request.provenanceId),
                revision = 1L
            });

            RegisterTransaction(tx, "create", record.locationId);
            Revision++;
            IsDirty = true;
            return LocationOperationResult.Success(BuildSnapshot(record), "Location created.", before, Revision);
        }

        public LocationOperationResult RenameLocation(LocationRenameRequest request)
        {
            request ??= new LocationRenameRequest();
            long before = Revision;
            if (disposed) return Fail(LocationOperationStatus.Disposed, "Location runtime is disposed.", before);
            string locationId = Normalize(request.locationId);
            string tx = Normalize(request.transactionId);
            if (TryDuplicate(tx, locationId, "rename", before, out LocationOperationResult duplicate))
            {
                return duplicate;
            }

            if (request.expectedRevision >= 0L && request.expectedRevision != Revision)
            {
                return Fail(LocationOperationStatus.RevisionConflict, $"Expected location runtime revision {request.expectedRevision}, but current revision is {Revision}.", before);
            }

            if (!recordsById.TryGetValue(locationId, out LocationRecordData record))
            {
                return Fail(LocationOperationStatus.MissingLocation, $"Location '{locationId}' does not exist.", before);
            }

            string newName = NormalizeName(request.newName);
            if (string.IsNullOrWhiteSpace(newName))
            {
                return Fail(LocationOperationStatus.InvalidName, "Location name is required.", before);
            }

            if (request.preview)
            {
                LocationRecordData preview = record.Clone();
                ApplyName(preview, request.category, newName);
                return LocationOperationResult.Success(BuildSnapshot(preview), "Location rename preview.", before, before, preview: true);
            }

            LocationNameRecordData previousOfficial = string.IsNullOrWhiteSpace(record.currentOfficialNameRecordId) || !namesById.TryGetValue(record.currentOfficialNameRecordId, out LocationNameRecordData previous) ? null : previous;
            if (request.category == LocationNameCategory.Official && previousOfficial != null)
            {
                previousOfficial.effectiveEndWorldTime = request.effectiveWorldTime;
                previousOfficial.revision++;
            }

            string nameId = BuildNameId(locationId, request.category.ToString().ToLowerInvariant(), namesById.Count + 1);
            namesById[nameId] = new LocationNameRecordData
            {
                nameRecordId = nameId,
                locationId = locationId,
                category = request.category,
                value = newName,
                effectiveStartWorldTime = request.effectiveWorldTime,
                visibility = request.visibility,
                sourceEventId = Normalize(request.sourceEventId),
                sourceRecordId = Normalize(request.sourceRecordId),
                provenanceId = Normalize(request.provenanceId),
                revision = 1L
            };

            ApplyName(record, request.category, newName);
            if (request.category == LocationNameCategory.Official)
            {
                record.currentOfficialNameRecordId = nameId;
            }

            record.revision++;
            RegisterTransaction(tx, "rename", locationId);
            Revision++;
            IsDirty = true;
            return LocationOperationResult.Success(BuildSnapshot(record), "Location renamed.", before, Revision);
        }

        public LocationOperationResult TransitionLifecycle(LocationLifecycleTransitionRequest request)
        {
            request ??= new LocationLifecycleTransitionRequest();
            long before = Revision;
            if (disposed) return Fail(LocationOperationStatus.Disposed, "Location runtime is disposed.", before);
            string locationId = Normalize(request.locationId);
            string tx = Normalize(request.transactionId);
            if (TryDuplicate(tx, locationId, "lifecycle", before, out LocationOperationResult duplicate))
            {
                return duplicate;
            }

            if (request.expectedRevision >= 0L && request.expectedRevision != Revision)
            {
                return Fail(LocationOperationStatus.RevisionConflict, $"Expected location runtime revision {request.expectedRevision}, but current revision is {Revision}.", before);
            }

            if (!recordsById.TryGetValue(locationId, out LocationRecordData record))
            {
                return Fail(LocationOperationStatus.MissingLocation, $"Location '{locationId}' does not exist.", before);
            }

            if (!CanTransition(record.lifecycleState, request.targetState))
            {
                return Fail(LocationOperationStatus.InvalidLifecycleTransition, $"Cannot transition location from '{record.lifecycleState}' to '{request.targetState}'.", before);
            }

            if (record.lifecycleState == request.targetState)
            {
                return LocationOperationResult.Success(BuildSnapshot(record), "Location lifecycle already matches requested state.", before, before, duplicate: true);
            }

            if (request.preview)
            {
                LocationRecordData preview = record.Clone();
                ApplyLifecycle(preview, request.targetState, request.worldTime);
                return LocationOperationResult.Success(BuildSnapshot(preview), "Location lifecycle preview.", before, before, preview: true);
            }

            ApplyLifecycle(record, request.targetState, request.worldTime);
            record.sourceEventId = string.IsNullOrWhiteSpace(request.sourceEventId) ? record.sourceEventId : Normalize(request.sourceEventId);
            record.sourceRecordId = string.IsNullOrWhiteSpace(request.sourceRecordId) ? record.sourceRecordId : Normalize(request.sourceRecordId);
            record.provenanceId = string.IsNullOrWhiteSpace(request.provenanceId) ? record.provenanceId : Normalize(request.provenanceId);
            record.revision++;
            RegisterTransaction(tx, "lifecycle", locationId);
            Revision++;
            IsDirty = true;
            return LocationOperationResult.Success(BuildSnapshot(record), "Location lifecycle updated.", before, Revision);
        }

        public bool TryGetSnapshot(string locationId, out LocationSnapshot snapshot)
        {
            if (recordsById.TryGetValue(Normalize(locationId), out LocationRecordData record))
            {
                snapshot = BuildSnapshot(record);
                return true;
            }

            snapshot = null;
            return false;
        }

        public LocationReferenceResolutionResult ResolveReference(LocationReferenceData reference)
        {
            if (reference == null || string.IsNullOrWhiteSpace(reference.locationId))
            {
                return LocationReferenceResolutionResult.Failure(LocationReferenceResolutionStatus.InvalidRequest, "Location reference is empty.");
            }

            if (!string.IsNullOrWhiteSpace(reference.worldId) && !string.Equals(Normalize(reference.worldId), worldId, StringComparison.Ordinal))
            {
                return LocationReferenceResolutionResult.Failure(LocationReferenceResolutionStatus.WrongWorld, $"Location reference targets world '{reference.worldId}', not '{worldId}'.");
            }

            if (!recordsById.TryGetValue(Normalize(reference.locationId), out LocationRecordData record))
            {
                return LocationReferenceResolutionResult.Failure(LocationReferenceResolutionStatus.MissingLocation, $"Location '{reference.locationId}' does not exist.");
            }

            if (record.lifecycleState == LocationLifecycleState.Destroyed || record.lifecycleState == LocationLifecycleState.Removed)
            {
                return LocationReferenceResolutionResult.Failure(LocationReferenceResolutionStatus.Destroyed, $"Location '{reference.locationId}' is no longer active.");
            }

            return LocationReferenceResolutionResult.Resolved(BuildSnapshot(record));
        }

        public IReadOnlyList<LocationSnapshot> QueryByCategory(LocationCategory category)
        {
            return recordsById.Values
                .Where(record => TryGetDefinition(record.locationDefinitionId, out LocationDefinition definition, 0L, out _) && definition.Category == category)
                .OrderBy(record => record.locationId, StringComparer.Ordinal)
                .Select(BuildSnapshot)
                .ToArray();
        }

        public IReadOnlyList<LocationSnapshot> QueryByDefinition(string definitionId)
        {
            string id = Normalize(definitionId);
            return recordsById.Values.Where(record => string.Equals(record.locationDefinitionId, id, StringComparison.Ordinal)).OrderBy(record => record.locationId, StringComparer.Ordinal).Select(BuildSnapshot).ToArray();
        }

        public IReadOnlyList<LocationSnapshot> QueryByTag(string tagId)
        {
            string id = Normalize(tagId);
            return recordsById.Values.Where(record => (record.semanticTagIds ?? Array.Empty<string>()).Contains(id, StringComparer.Ordinal)).OrderBy(record => record.locationId, StringComparer.Ordinal).Select(BuildSnapshot).ToArray();
        }

        public IReadOnlyList<LocationSnapshot> QueryByLifecycle(LocationLifecycleState state)
        {
            return recordsById.Values.Where(record => record.lifecycleState == state).OrderBy(record => record.locationId, StringComparer.Ordinal).Select(BuildSnapshot).ToArray();
        }

        public LocationRuntimeSaveData CreateSaveData()
        {
            return new LocationRuntimeSaveData
            {
                schemaVersion = LocationRuntimeSaveData.CurrentSchemaVersion,
                worldId = worldId,
                revision = Revision,
                records = recordsById.Values.OrderBy(record => record.locationId, StringComparer.Ordinal).Select(record => record.Clone()).ToList(),
                names = namesById.Values.OrderBy(record => record.nameRecordId, StringComparer.Ordinal).Select(record => record.Clone()).ToList(),
                transactions = transactionsById.Values.OrderBy(record => record.transactionId, StringComparer.Ordinal).Select(record => record.Clone()).ToList()
            };
        }

        public LocationOperationResult RestoreFromSaveData(LocationRuntimeSaveData saveData, DefinitionRegistry definitionRegistry, string expectedWorldId = PersistenceService.LocalWorldId, bool restoring = true)
        {
            long before = Revision;
            if (!ValidateSaveData(saveData, definitionRegistry ?? registry, expectedWorldId, knownPropertyIds, knownOrganizationIds, knownGovernmentIds, knownTerritoryIds, out string failure))
            {
                return Fail(LocationOperationStatus.PersistenceInvalid, failure, before);
            }

            LocationRuntimeSaveData rollback = CreateSaveData();
            try
            {
                RestoreInternal(saveData ?? new LocationRuntimeSaveData());
                registry = definitionRegistry ?? registry;
                worldId = string.IsNullOrWhiteSpace(expectedWorldId) ? PersistenceService.LocalWorldId : expectedWorldId.Trim();
                IsDirty = !restoring;
                return LocationOperationResult.Success(null, "Locations restored.", before, Revision);
            }
            catch (Exception exception)
            {
                RestoreInternal(rollback);
                return Fail(LocationOperationStatus.RestoreFailed, $"Location restore failed: {exception.Message}", before);
            }
        }

        public LocationValidationReport ValidateRuntime()
        {
            ValidateSaveData(CreateSaveData(), registry, worldId, knownPropertyIds, knownOrganizationIds, knownGovernmentIds, knownTerritoryIds, out _, out LocationValidationReport report);
            return report;
        }

        public static bool ValidateSaveData(
            LocationRuntimeSaveData saveData,
            DefinitionRegistry registry,
            string expectedWorldId,
            IEnumerable<string> knownProperties,
            IEnumerable<string> knownOrganizations,
            IEnumerable<string> knownGovernments,
            IEnumerable<string> knownTerritories,
            out string failure)
        {
            return ValidateSaveData(saveData, registry, expectedWorldId, knownProperties, knownOrganizations, knownGovernments, knownTerritories, out failure, out _);
        }

        public static bool ValidateSaveData(
            LocationRuntimeSaveData saveData,
            DefinitionRegistry registry,
            string expectedWorldId,
            IEnumerable<string> knownProperties,
            IEnumerable<string> knownOrganizations,
            IEnumerable<string> knownGovernments,
            IEnumerable<string> knownTerritories,
            out string failure,
            out LocationValidationReport report)
        {
            report = new LocationValidationReport();
            saveData ??= new LocationRuntimeSaveData();
            string world = string.IsNullOrWhiteSpace(expectedWorldId) ? PersistenceService.LocalWorldId : expectedWorldId.Trim();
            if (saveData.schemaVersion != LocationRuntimeSaveData.CurrentSchemaVersion) report.AddError($"Unsupported location save schema {saveData.schemaVersion}.");
            if (!string.IsNullOrWhiteSpace(saveData.worldId) && !string.Equals(saveData.worldId.Trim(), world, StringComparison.Ordinal)) report.AddError($"Location save world '{saveData.worldId}' does not match expected world '{world}'.");

            HashSet<string> recordIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> nameIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> properties = new HashSet<string>(Clean(knownProperties), StringComparer.Ordinal);
            HashSet<string> organizations = new HashSet<string>(Clean(knownOrganizations), StringComparer.Ordinal);
            HashSet<string> governments = new HashSet<string>(Clean(knownGovernments), StringComparer.Ordinal);
            HashSet<string> territories = new HashSet<string>(Clean(knownTerritories), StringComparer.Ordinal);

            foreach (LocationRecordData record in saveData.records ?? new List<LocationRecordData>())
            {
                if (record == null)
                {
                    report.AddError("Location save contains a null record.");
                    continue;
                }

                string locationId = Normalize(record.locationId);
                if (string.IsNullOrWhiteSpace(locationId)) report.AddError("Location record is missing a stable location ID.");
                else if (!recordIds.Add(locationId)) report.AddError($"Duplicate location ID '{locationId}'.");
                if (string.IsNullOrWhiteSpace(record.locationDefinitionId)) report.AddError($"Location '{locationId}' is missing a definition ID.");
                else if (registry == null || !registry.TryGet(Normalize(record.locationDefinitionId), out LocationDefinition definition))
                {
                    report.AddError($"Location '{locationId}' references missing Location Definition '{record.locationDefinitionId}'.");
                    definition = null;
                }
                else
                {
                    if (!definition.SupportsVisibility(record.visibility)) report.AddError($"Location '{locationId}' visibility '{record.visibility}' is not supported by definition '{definition.Id}'.");
                    foreach (string tag in Clean(record.semanticTagIds))
                    {
                        if (!definition.AllowsSemanticTag(tag)) report.AddError($"Location '{locationId}' tag '{tag}' is not supported by definition '{definition.Id}'.");
                    }

                    if (!string.IsNullOrWhiteSpace(record.associatedPropertyId) && !definition.SupportsPropertyAssociation) report.AddError($"Location '{locationId}' cannot reference a property.");
                    if (!string.IsNullOrWhiteSpace(record.associatedOrganizationId) && !definition.SupportsOrganizationAssociation) report.AddError($"Location '{locationId}' cannot reference an organization.");
                    if (!string.IsNullOrWhiteSpace(record.associatedGovernmentId) && !definition.SupportsGovernmentAssociation) report.AddError($"Location '{locationId}' cannot reference a government.");
                    if ((record.associatedTerritoryIds ?? Array.Empty<string>()).Length > 0 && !definition.SupportsTerritoryAssociation) report.AddError($"Location '{locationId}' cannot reference territories.");
                }

                if (!string.IsNullOrWhiteSpace(record.worldId) && !string.Equals(record.worldId.Trim(), world, StringComparison.Ordinal)) report.AddError($"Location '{locationId}' belongs to world '{record.worldId}', not '{world}'.");
                if (string.IsNullOrWhiteSpace(record.officialName)) report.AddError($"Location '{locationId}' has no official name.");
                if (!Enum.IsDefined(typeof(LocationLifecycleState), record.lifecycleState) || record.lifecycleState == LocationLifecycleState.Unknown) report.AddError($"Location '{locationId}' has invalid lifecycle '{record.lifecycleState}'.");
                if (properties.Count > 0 && !string.IsNullOrWhiteSpace(record.associatedPropertyId) && !properties.Contains(record.associatedPropertyId)) report.AddError($"Location '{locationId}' references unknown property '{record.associatedPropertyId}'.");
                if (organizations.Count > 0 && !string.IsNullOrWhiteSpace(record.associatedOrganizationId) && !organizations.Contains(record.associatedOrganizationId)) report.AddError($"Location '{locationId}' references unknown organization '{record.associatedOrganizationId}'.");
                if (governments.Count > 0 && !string.IsNullOrWhiteSpace(record.associatedGovernmentId) && !governments.Contains(record.associatedGovernmentId)) report.AddError($"Location '{locationId}' references unknown government '{record.associatedGovernmentId}'.");
                foreach (string territory in Clean(record.associatedTerritoryIds))
                {
                    if (territories.Count > 0 && !territories.Contains(territory)) report.AddError($"Location '{locationId}' references unknown territory '{territory}'.");
                }
            }

            foreach (LocationNameRecordData name in saveData.names ?? new List<LocationNameRecordData>())
            {
                if (name == null)
                {
                    report.AddError("Location save contains a null name record.");
                    continue;
                }

                string nameId = Normalize(name.nameRecordId);
                if (string.IsNullOrWhiteSpace(nameId)) report.AddError("Location name record is missing a stable ID.");
                else if (!nameIds.Add(nameId)) report.AddError($"Duplicate location name record ID '{nameId}'.");
                if (!recordIds.Contains(Normalize(name.locationId))) report.AddError($"Location name '{nameId}' references missing location '{name.locationId}'.");
                if (string.IsNullOrWhiteSpace(name.value)) report.AddError($"Location name '{nameId}' has no value.");
            }

            foreach (LocationRecordData record in saveData.records ?? new List<LocationRecordData>())
            {
                if (record != null && !string.IsNullOrWhiteSpace(record.currentOfficialNameRecordId) && !nameIds.Contains(Normalize(record.currentOfficialNameRecordId)))
                {
                    report.AddError($"Location '{record.locationId}' references missing current official name '{record.currentOfficialNameRecordId}'.");
                }
            }

            foreach (LocationTransactionRecordData tx in saveData.transactions ?? new List<LocationTransactionRecordData>())
            {
                if (tx == null) continue;
                if (string.IsNullOrWhiteSpace(tx.transactionId)) report.AddError("Location transaction has no ID.");
                if (!string.IsNullOrWhiteSpace(tx.locationId) && !recordIds.Contains(Normalize(tx.locationId))) report.AddError($"Location transaction '{tx.transactionId}' references missing location '{tx.locationId}'.");
            }

            failure = report.Summary;
            return report.Succeeded;
        }

        public void Reset()
        {
            recordsById.Clear();
            namesById.Clear();
            transactionsById.Clear();
            Revision = 0L;
            IsDirty = false;
            disposed = false;
        }

        public void Dispose()
        {
            Reset();
            disposed = true;
        }

        private LocationRecordData CreateRecord(LocationCreateRequest request, LocationDefinition definition, string locationId, string officialName, string[] tags)
        {
            return new LocationRecordData
            {
                locationId = locationId,
                locationDefinitionId = Normalize(definition.Id),
                worldId = worldId,
                currentOfficialNameRecordId = BuildNameId(locationId, "official", 1),
                officialName = officialName,
                commonName = NormalizeName(request.commonName),
                aliases = Clean(request.aliases),
                lifecycleState = request.initialLifecycleState == LocationLifecycleState.Unknown ? LocationLifecycleState.Active : request.initialLifecycleState,
                createdWorldTime = request.createdWorldTime,
                semanticTagIds = tags,
                associatedPropertyId = Normalize(request.associatedPropertyId),
                associatedOrganizationId = Normalize(request.associatedOrganizationId),
                associatedGovernmentId = Normalize(request.associatedGovernmentId),
                associatedTerritoryIds = Clean(request.associatedTerritoryIds),
                associations = (request.associations ?? Array.Empty<LocationAssociationReferenceData>()).Where(value => value != null).Select(value => value.Clone()).OrderBy(value => value.kind.ToString(), StringComparer.Ordinal).ThenBy(value => value.referenceId, StringComparer.Ordinal).ToArray(),
                prototypeSceneBindingKey = definition.AllowsSceneBinding ? Normalize(request.prototypeSceneBindingKey) : string.Empty,
                visibility = request.visibility,
                sourceEventId = Normalize(request.sourceEventId),
                sourceRecordId = Normalize(request.sourceRecordId),
                provenanceId = Normalize(request.provenanceId),
                revision = 1L
            };
        }

        private void RestoreInternal(LocationRuntimeSaveData saveData)
        {
            recordsById.Clear();
            namesById.Clear();
            transactionsById.Clear();
            foreach (LocationRecordData record in saveData.records ?? new List<LocationRecordData>()) recordsById[Normalize(record.locationId)] = record.Clone();
            foreach (LocationNameRecordData name in saveData.names ?? new List<LocationNameRecordData>()) namesById[Normalize(name.nameRecordId)] = name.Clone();
            foreach (LocationTransactionRecordData tx in saveData.transactions ?? new List<LocationTransactionRecordData>()) transactionsById[Normalize(tx.transactionId)] = tx.Clone();
            Revision = Math.Max(0L, saveData.revision);
        }

        private bool ValidateAssociations(LocationDefinition definition, string propertyId, string organizationId, string governmentId, IEnumerable<string> territoryIds, IEnumerable<LocationAssociationReferenceData> associations, long before, out LocationOperationResult failure)
        {
            failure = null;
            if (!string.IsNullOrWhiteSpace(propertyId) && !definition.SupportsPropertyAssociation) return SetFailure(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not support property association.", before, out failure);
            if (!string.IsNullOrWhiteSpace(organizationId) && !definition.SupportsOrganizationAssociation) return SetFailure(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not support organization association.", before, out failure);
            if (!string.IsNullOrWhiteSpace(governmentId) && !definition.SupportsGovernmentAssociation) return SetFailure(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not support government association.", before, out failure);
            if ((territoryIds ?? Array.Empty<string>()).Any(value => !string.IsNullOrWhiteSpace(value)) && !definition.SupportsTerritoryAssociation) return SetFailure(LocationOperationStatus.UnsupportedByDefinition, $"Location definition '{definition.Id}' does not support territory association.", before, out failure);
            if (!ValidateKnown(propertyId, knownPropertyIds, "property", before, out failure)) return false;
            if (!ValidateKnown(organizationId, knownOrganizationIds, "organization", before, out failure)) return false;
            if (!ValidateKnown(governmentId, knownGovernmentIds, "government", before, out failure)) return false;
            foreach (string territoryId in Clean(territoryIds))
            {
                if (!ValidateKnown(territoryId, knownTerritoryIds, "territory", before, out failure)) return false;
            }

            foreach (LocationAssociationReferenceData association in associations ?? Array.Empty<LocationAssociationReferenceData>())
            {
                if (association == null) continue;
                if (association.kind == LocationAssociationKind.Unknown || string.IsNullOrWhiteSpace(association.referenceId))
                {
                    return SetFailure(LocationOperationStatus.InvalidReference, "Location association references require a kind and reference ID.", before, out failure);
                }
            }

            return true;
        }

        private bool ValidateKnown(string value, ISet<string> known, string label, long before, out LocationOperationResult failure)
        {
            failure = null;
            if (!string.IsNullOrWhiteSpace(value) && known != null && known.Count > 0 && !known.Contains(value.Trim()))
            {
                return SetFailure(LocationOperationStatus.InvalidReference, $"Location references unknown {label} '{value}'.", before, out failure);
            }

            return true;
        }

        private bool SetFailure(LocationOperationStatus status, string message, long before, out LocationOperationResult failure)
        {
            failure = Fail(status, message, before);
            return false;
        }

        private bool TryGetDefinition(string id, out LocationDefinition definition, long before, out LocationOperationResult failure)
        {
            definition = null;
            failure = null;
            if (registry == null || !registry.TryGet(Normalize(id), out definition))
            {
                failure = Fail(LocationOperationStatus.MissingDefinition, $"Location definition '{id}' is not registered.", before);
                return false;
            }

            return true;
        }

        private bool TryDuplicate(string tx, string locationId, string operation, long before, out LocationOperationResult result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(tx) || !transactionsById.TryGetValue(tx, out LocationTransactionRecordData found))
            {
                return false;
            }

            if (string.Equals(found.operation, operation, StringComparison.Ordinal) && string.Equals(found.locationId, locationId, StringComparison.Ordinal))
            {
                recordsById.TryGetValue(locationId, out LocationRecordData record);
                result = LocationOperationResult.Success(record == null ? null : BuildSnapshot(record), "Duplicate location transaction ignored.", before, before, duplicate: true);
                return true;
            }

            result = Fail(LocationOperationStatus.InvalidRequest, $"Transaction '{tx}' was already used for a different location operation.", before);
            return true;
        }

        private void RegisterTransaction(string tx, string operation, string locationId)
        {
            if (string.IsNullOrWhiteSpace(tx)) return;
            transactionsById[tx] = new LocationTransactionRecordData { transactionId = tx, operation = operation, locationId = locationId, revision = Revision + 1L };
        }

        private static void ApplyName(LocationRecordData record, LocationNameCategory category, string name)
        {
            if (category == LocationNameCategory.Official) record.officialName = name;
            else if (category == LocationNameCategory.Common) record.commonName = name;
            else if (category == LocationNameCategory.Alias)
            {
                record.aliases = Clean((record.aliases ?? Array.Empty<string>()).Concat(new[] { name }));
            }
        }

        private static void ApplyLifecycle(LocationRecordData record, LocationLifecycleState state, double worldTime)
        {
            record.lifecycleState = state;
            if (state == LocationLifecycleState.Destroyed || state == LocationLifecycleState.Historical || state == LocationLifecycleState.Removed)
            {
                record.endedWorldTime = worldTime;
            }
        }

        private static bool CanCreateLifecycle(LocationLifecycleState state)
        {
            return state == LocationLifecycleState.Proposed || state == LocationLifecycleState.Active || state == LocationLifecycleState.Inactive || state == LocationLifecycleState.Closed || state == LocationLifecycleState.Historical;
        }

        private static bool CanTransition(LocationLifecycleState from, LocationLifecycleState to)
        {
            if (!Enum.IsDefined(typeof(LocationLifecycleState), to) || to == LocationLifecycleState.Unknown) return false;
            if (from == LocationLifecycleState.Removed || from == LocationLifecycleState.Destroyed) return from == to;
            if (from == LocationLifecycleState.Historical && to == LocationLifecycleState.Active) return false;
            return true;
        }

        private static LocationSnapshot BuildSnapshot(LocationRecordData record) => new LocationSnapshot(record);
        private static LocationOperationResult Fail(LocationOperationStatus status, string message, long before) => LocationOperationResult.Failure(status, message, before);
        private static string BuildNameId(string locationId, string category, int sequence) => $"{locationId}.name.{category}.{Math.Max(1, sequence):0000}";
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string NormalizeName(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string[] Clean(IEnumerable<string> values) => (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }
}
