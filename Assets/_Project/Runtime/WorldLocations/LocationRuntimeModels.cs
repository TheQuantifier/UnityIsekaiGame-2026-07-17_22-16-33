using System;
using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Knowledge.Access;

namespace UnityIsekaiGame.WorldLocations
{
    [Serializable]
    public sealed class LocationAssociationReferenceData
    {
        public LocationAssociationKind kind;
        public string referenceId;
        public string worldId;
        public string provenanceId;

        public LocationAssociationReferenceData Clone()
        {
            return new LocationAssociationReferenceData { kind = kind, referenceId = referenceId ?? string.Empty, worldId = worldId ?? string.Empty, provenanceId = provenanceId ?? string.Empty };
        }
    }

    [Serializable]
    public sealed class LocationNameRecordData
    {
        public string nameRecordId;
        public string locationId;
        public LocationNameCategory category;
        public string value;
        public double effectiveStartWorldTime;
        public double effectiveEndWorldTime = -1d;
        public LocationVisibility visibility;
        public string sourceEventId;
        public string sourceRecordId;
        public string provenanceId;
        public long revision = 1L;

        public LocationNameRecordData Clone()
        {
            return new LocationNameRecordData
            {
                nameRecordId = nameRecordId ?? string.Empty,
                locationId = locationId ?? string.Empty,
                category = category,
                value = value ?? string.Empty,
                effectiveStartWorldTime = effectiveStartWorldTime,
                effectiveEndWorldTime = effectiveEndWorldTime,
                visibility = visibility,
                sourceEventId = sourceEventId ?? string.Empty,
                sourceRecordId = sourceRecordId ?? string.Empty,
                provenanceId = provenanceId ?? string.Empty,
                revision = revision
            };
        }
    }

    [Serializable]
    public sealed class LocationRecordData
    {
        public string locationId;
        public string locationDefinitionId;
        public string worldId;
        public string currentOfficialNameRecordId;
        public string officialName;
        public string commonName;
        public string[] aliases = Array.Empty<string>();
        public LocationLifecycleState lifecycleState = LocationLifecycleState.Active;
        public double createdWorldTime;
        public double endedWorldTime = -1d;
        public string[] semanticTagIds = Array.Empty<string>();
        public string associatedPropertyId;
        public string associatedOrganizationId;
        public string associatedGovernmentId;
        public string[] associatedTerritoryIds = Array.Empty<string>();
        public LocationAssociationReferenceData[] associations = Array.Empty<LocationAssociationReferenceData>();
        public string prototypeSceneBindingKey;
        public LocationVisibility visibility;
        public string sourceEventId;
        public string sourceRecordId;
        public string provenanceId;
        public long revision = 1L;

        public LocationRecordData Clone()
        {
            return new LocationRecordData
            {
                locationId = locationId ?? string.Empty,
                locationDefinitionId = locationDefinitionId ?? string.Empty,
                worldId = worldId ?? string.Empty,
                currentOfficialNameRecordId = currentOfficialNameRecordId ?? string.Empty,
                officialName = officialName ?? string.Empty,
                commonName = commonName ?? string.Empty,
                aliases = CloneArray(aliases),
                lifecycleState = lifecycleState,
                createdWorldTime = createdWorldTime,
                endedWorldTime = endedWorldTime,
                semanticTagIds = CloneArray(semanticTagIds),
                associatedPropertyId = associatedPropertyId ?? string.Empty,
                associatedOrganizationId = associatedOrganizationId ?? string.Empty,
                associatedGovernmentId = associatedGovernmentId ?? string.Empty,
                associatedTerritoryIds = CloneArray(associatedTerritoryIds),
                associations = (associations ?? Array.Empty<LocationAssociationReferenceData>()).Where(value => value != null).Select(value => value.Clone()).ToArray(),
                prototypeSceneBindingKey = prototypeSceneBindingKey ?? string.Empty,
                visibility = visibility,
                sourceEventId = sourceEventId ?? string.Empty,
                sourceRecordId = sourceRecordId ?? string.Empty,
                provenanceId = provenanceId ?? string.Empty,
                revision = revision
            };
        }

        private static string[] CloneArray(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray();
        }
    }

    [Serializable]
    public sealed class LocationTransactionRecordData
    {
        public string transactionId;
        public string operation;
        public string locationId;
        public long revision;

        public LocationTransactionRecordData Clone()
        {
            return new LocationTransactionRecordData { transactionId = transactionId ?? string.Empty, operation = operation ?? string.Empty, locationId = locationId ?? string.Empty, revision = revision };
        }
    }

    [Serializable]
    public sealed class LocationRuntimeSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string worldId;
        public long revision;
        public List<LocationRecordData> records = new List<LocationRecordData>();
        public List<LocationNameRecordData> names = new List<LocationNameRecordData>();
        public List<LocationTransactionRecordData> transactions = new List<LocationTransactionRecordData>();

        public LocationRuntimeSaveData Clone()
        {
            return new LocationRuntimeSaveData
            {
                schemaVersion = schemaVersion,
                worldId = worldId ?? string.Empty,
                revision = revision,
                records = (records ?? new List<LocationRecordData>()).Where(value => value != null).Select(value => value.Clone()).ToList(),
                names = (names ?? new List<LocationNameRecordData>()).Where(value => value != null).Select(value => value.Clone()).ToList(),
                transactions = (transactions ?? new List<LocationTransactionRecordData>()).Where(value => value != null).Select(value => value.Clone()).ToList()
            };
        }
    }

    public sealed class LocationCreateRequest
    {
        public string transactionId;
        public string locationId;
        public string locationDefinitionId;
        public string officialName;
        public string commonName;
        public IEnumerable<string> aliases;
        public LocationLifecycleState initialLifecycleState = LocationLifecycleState.Active;
        public double createdWorldTime;
        public IEnumerable<string> semanticTagIds;
        public string associatedPropertyId;
        public string associatedOrganizationId;
        public string associatedGovernmentId;
        public IEnumerable<string> associatedTerritoryIds;
        public IEnumerable<LocationAssociationReferenceData> associations;
        public string prototypeSceneBindingKey;
        public LocationVisibility visibility = LocationVisibility.Public;
        public string sourceEventId;
        public string sourceRecordId;
        public string provenanceId;
        public long expectedRevision = -1L;
        public bool preview;
    }

    public sealed class LocationRenameRequest
    {
        public string transactionId;
        public string locationId;
        public string newName;
        public LocationNameCategory category = LocationNameCategory.Official;
        public double effectiveWorldTime;
        public LocationVisibility visibility = LocationVisibility.Public;
        public string sourceEventId;
        public string sourceRecordId;
        public string provenanceId;
        public long expectedRevision = -1L;
        public bool preview;
    }

    public sealed class LocationLifecycleTransitionRequest
    {
        public string transactionId;
        public string locationId;
        public LocationLifecycleState targetState;
        public double worldTime;
        public string sourceEventId;
        public string sourceRecordId;
        public string provenanceId;
        public long expectedRevision = -1L;
        public bool preview;
    }

    public sealed class LocationReferenceData
    {
        public string locationId;
        public string locationDefinitionId;
        public string worldId;
    }

    public sealed class LocationSnapshot
    {
        private readonly LocationRecordData data;

        public LocationSnapshot(LocationRecordData record)
        {
            data = record?.Clone() ?? new LocationRecordData();
        }

        public string LocationId => data.locationId ?? string.Empty;
        public string LocationDefinitionId => data.locationDefinitionId ?? string.Empty;
        public string WorldId => data.worldId ?? string.Empty;
        public string OfficialName => data.officialName ?? string.Empty;
        public string CommonName => data.commonName ?? string.Empty;
        public LocationLifecycleState LifecycleState => data.lifecycleState;
        public LocationVisibility Visibility => data.visibility;
        public string PrototypeSceneBindingKey => data.prototypeSceneBindingKey ?? string.Empty;
        public string AssociatedPropertyId => data.associatedPropertyId ?? string.Empty;
        public string AssociatedOrganizationId => data.associatedOrganizationId ?? string.Empty;
        public string AssociatedGovernmentId => data.associatedGovernmentId ?? string.Empty;
        public IReadOnlyList<string> AssociatedTerritoryIds => (data.associatedTerritoryIds ?? Array.Empty<string>()).ToArray();
        public IReadOnlyList<string> Aliases => (data.aliases ?? Array.Empty<string>()).ToArray();
        public IReadOnlyList<string> SemanticTagIds => (data.semanticTagIds ?? Array.Empty<string>()).ToArray();
        public IReadOnlyList<LocationAssociationReferenceData> Associations => (data.associations ?? Array.Empty<LocationAssociationReferenceData>()).Where(value => value != null).Select(value => value.Clone()).ToArray();
        public long Revision => data.revision;
        public LocationRecordData ToSaveData() => data.Clone();
        public LocationReferenceData ToReference() => new LocationReferenceData { locationId = LocationId, locationDefinitionId = LocationDefinitionId, worldId = WorldId };
        public InformationSubjectReferenceData ToInformationSubject() => new InformationSubjectReferenceData { subjectType = InformationSubjectType.Location, subjectId = LocationId, controllingEntityId = AssociatedOrganizationId, tags = SemanticTagIds.ToArray() };
    }

    public sealed class LocationProjection
    {
        public LocationProjection(LocationSnapshot snapshot, bool redacted)
        {
            Snapshot = snapshot;
            Redacted = redacted;
        }

        public LocationSnapshot Snapshot { get; }
        public bool Redacted { get; }
    }

    public sealed class LocationOperationResult
    {
        private LocationOperationResult(LocationOperationStatus status, string message, LocationSnapshot snapshot, bool duplicate, bool preview, long before, long after)
        {
            Status = status;
            Message = message ?? string.Empty;
            Snapshot = snapshot;
            Duplicate = duplicate;
            Preview = preview;
            RevisionBefore = before;
            RevisionAfter = after;
        }

        public LocationOperationStatus Status { get; }
        public string Message { get; }
        public LocationSnapshot Snapshot { get; }
        public bool Succeeded => Status == LocationOperationStatus.Succeeded || Status == LocationOperationStatus.Preview || Status == LocationOperationStatus.Duplicate;
        public bool Duplicate { get; }
        public bool Preview { get; }
        public long RevisionBefore { get; }
        public long RevisionAfter { get; }

        public static LocationOperationResult Success(LocationSnapshot snapshot, string message, long before, long after, bool duplicate = false, bool preview = false)
        {
            return new LocationOperationResult(preview ? LocationOperationStatus.Preview : duplicate ? LocationOperationStatus.Duplicate : LocationOperationStatus.Succeeded, message, snapshot, duplicate, preview, before, after);
        }

        public static LocationOperationResult Failure(LocationOperationStatus status, string message, long before)
        {
            return new LocationOperationResult(status, message, null, false, false, before, before);
        }
    }

    public sealed class LocationReferenceResolutionResult
    {
        private LocationReferenceResolutionResult(LocationReferenceResolutionStatus status, LocationSnapshot snapshot, string message)
        {
            Status = status;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public LocationReferenceResolutionStatus Status { get; }
        public LocationSnapshot Snapshot { get; }
        public string Message { get; }
        public bool Succeeded => Status == LocationReferenceResolutionStatus.Resolved;

        public static LocationReferenceResolutionResult Resolved(LocationSnapshot snapshot) => new LocationReferenceResolutionResult(LocationReferenceResolutionStatus.Resolved, snapshot, "Location resolved.");
        public static LocationReferenceResolutionResult Failure(LocationReferenceResolutionStatus status, string message) => new LocationReferenceResolutionResult(status, null, message);
    }

    public sealed class LocationValidationReport
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool Succeeded => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }

        public string Summary => Succeeded ? "Location validation passed." : string.Join(" | ", errors);
    }
}
