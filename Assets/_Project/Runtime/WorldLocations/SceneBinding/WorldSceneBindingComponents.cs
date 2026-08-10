using System;
using UnityEngine;
using UnityIsekaiGame.Interaction;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
    public abstract class WorldSceneBindingComponent : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string worldId = PersistenceService.LocalWorldId;
        [SerializeField] private string sceneKey = "scene.prototype";
        [SerializeField] private string logicalId;
        [SerializeField] private string bindingKey;
        [SerializeField] private string displayName;
        [SerializeField] private WorldSceneBindingRole role = WorldSceneBindingRole.Primary;
        [SerializeField] private bool required;
        [SerializeField] private bool autoRegister = true;
        [SerializeField] private Transform anchor;

        private WorldSceneBindingStatus status = WorldSceneBindingStatus.Unregistered;
        private string diagnostics = string.Empty;
        private WorldSceneBindingRuntime runtime;

        public abstract WorldSceneBindingCategory Category { get; }
        public string InstanceId => instanceId ?? string.Empty;
        public string WorldId => worldId ?? string.Empty;
        public string SceneKey => sceneKey ?? string.Empty;
        public string LogicalId => logicalId ?? string.Empty;
        public string BindingKey => bindingKey ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public WorldSceneBindingRole Role => role;
        public bool Required => required;
        public bool AutoRegister => autoRegister;
        public Transform BindingTransform => anchor != null ? anchor : transform;
        public WorldSceneBindingStatus Status => status;
        public string Diagnostics => diagnostics ?? string.Empty;
        protected WorldSceneBindingRuntime Runtime => runtime ?? WorldSceneBindingRuntime.Default;

        protected virtual void Reset()
        {
            RefreshGeneratedInstanceId();
            if (string.IsNullOrWhiteSpace(bindingKey))
            {
                bindingKey = $"{gameObject.scene.name}.{gameObject.name}".Replace(' ', '-').ToLowerInvariant();
            }
        }

        protected virtual void OnEnable()
        {
            if (autoRegister)
            {
                Register(WorldSceneBindingRuntime.Default);
            }
        }

        protected virtual void OnDisable()
        {
            if (autoRegister)
            {
                Runtime.Unregister(this);
            }
        }

        public void ConfigureBinding(string logicalRecordId, string sceneBindingKey, string scene, string world, WorldSceneBindingRole bindingRole = WorldSceneBindingRole.Primary, bool requiredBinding = false)
        {
            logicalId = N(logicalRecordId);
            bindingKey = N(sceneBindingKey);
            sceneKey = string.IsNullOrWhiteSpace(scene) ? sceneKey : scene.Trim();
            worldId = string.IsNullOrWhiteSpace(world) ? worldId : world.Trim();
            role = bindingRole;
            required = requiredBinding;
        }

        public WorldSceneBindingSnapshot Register(WorldSceneBindingRuntime targetRuntime)
        {
            runtime = targetRuntime ?? WorldSceneBindingRuntime.Default;
            RefreshGeneratedInstanceId();
            return runtime.Register(this);
        }

        internal void AttachRuntime(WorldSceneBindingRuntime targetRuntime)
        {
            runtime = targetRuntime ?? WorldSceneBindingRuntime.Default;
        }

        public void RefreshGeneratedInstanceId()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instanceId = $"{GetType().Name}.{Guid.NewGuid():N}";
            }
        }

        public void ApplyBindingResolution(WorldSceneBindingStatus nextStatus, string message)
        {
            status = nextStatus;
            diagnostics = message ?? string.Empty;
            OnBindingResolutionChanged(nextStatus, diagnostics);
        }

        public virtual void SyncFromAuthoritative(WorldSceneBindingRuntime bindingRuntime, bool initialSync)
        {
        }

        protected virtual void OnBindingResolutionChanged(WorldSceneBindingStatus nextStatus, string message)
        {
        }

        public WorldSceneBindingSnapshot CreateSnapshot()
        {
            return new WorldSceneBindingSnapshot(
                instanceId,
                worldId,
                sceneKey,
                gameObject.scene.name,
                Category,
                role,
                logicalId,
                bindingKey,
                DisplayName,
                status,
                required,
                diagnostics);
        }

        protected static string N(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class LocationSceneBinding : WorldSceneBindingComponent
    {
        [SerializeField] private string locationDefinitionId;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Location;
        public string LocationDefinitionId => locationDefinitionId ?? string.Empty;

        public void ConfigureLocation(string locationId, string sceneBindingKey, string scene, string world, string expectedDefinitionId = "", WorldSceneBindingRole bindingRole = WorldSceneBindingRole.Primary, bool requiredBinding = false)
        {
            ConfigureBinding(locationId, sceneBindingKey, scene, world, bindingRole, requiredBinding);
            locationDefinitionId = N(expectedDefinitionId);
        }
    }

    public sealed class SpawnAnchorSceneBinding : WorldSceneBindingComponent
    {
        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.SpawnAnchor;
    }

    public sealed class InteractionPointSceneBinding : WorldSceneBindingComponent, IInteractable
    {
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private bool requirePhysicalRange = true;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.InteractionPoint;
        public string InteractionPrompt => string.IsNullOrWhiteSpace(DisplayName) ? "Interact" : $"Interact: {DisplayName}";
        public InteractionPointSnapshot LastPoint { get; private set; }

        public bool CanInteract(in InteractionContext context)
        {
            if (Status != WorldSceneBindingStatus.Bound)
            {
                return false;
            }

            if (requirePhysicalRange && context.Origin != null)
            {
                float distance = Vector3.Distance(context.Origin.position, BindingTransform.position);
                if (distance > Mathf.Max(0.01f, interactionRange))
                {
                    return false;
                }
            }

            return Runtime.TryGetInteractionPoint(LogicalId, out InteractionPointSnapshot point) && point.IsActive;
        }

        public void Interact(in InteractionContext context)
        {
            if (!Runtime.TryGetInteractionPoint(LogicalId, out InteractionPointSnapshot point))
            {
                ApplyBindingResolution(WorldSceneBindingStatus.WaitingForLogicalRecord, "Interaction request blocked because the authoritative point is missing.");
                return;
            }

            LastPoint = point;
            Debug.Log($"Scene interaction routed to logical interaction point '{point.InteractionPointId}'.");
        }
    }

    public sealed class ConnectionSceneBinding : WorldSceneBindingComponent, IInteractable
    {
        [SerializeField] private Collider stateControlledCollider;
        [SerializeField] private bool colliderBlocksWhenClosed = true;
        [SerializeField] private bool interactTogglesOpenClosed;
        [SerializeField] private string sourceLocationId;
        [SerializeField] private string destinationLocationId;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Connection;
        public string InteractionPrompt => LastConnection != null ? $"{(LastConnection.OpenState == LocationConnectionOpenState.Open ? "Close" : "Open")} {DisplayName}" : $"Use {DisplayName}";
        public LocationConnectionSnapshot LastConnection { get; private set; }

        public void ConfigureConnection(string connectionId, string sceneBindingKey, string sourceLocation, string destinationLocation, string scene, string world, Collider controlledCollider = null, bool requiredBinding = false)
        {
            ConfigureBinding(connectionId, sceneBindingKey, scene, world, WorldSceneBindingRole.Primary, requiredBinding);
            sourceLocationId = N(sourceLocation);
            destinationLocationId = N(destinationLocation);
            stateControlledCollider = controlledCollider;
        }

        public override void SyncFromAuthoritative(WorldSceneBindingRuntime bindingRuntime, bool initialSync)
        {
            if (!bindingRuntime.TryGetConnection(LogicalId, out LocationConnectionSnapshot connection))
            {
                return;
            }

            LastConnection = connection;
            Collider controlled = stateControlledCollider != null ? stateControlledCollider : GetComponent<Collider>();
            if (controlled != null && colliderBlocksWhenClosed)
            {
                bool closed = connection.OpenState == LocationConnectionOpenState.Closed || connection.BlockageState != LocationConnectionBlockageState.Clear;
                controlled.enabled = closed;
            }
        }

        public bool CanInteract(in InteractionContext context)
        {
            return Status == WorldSceneBindingStatus.Bound && Runtime.TryGetConnection(LogicalId, out _);
        }

        public void Interact(in InteractionContext context)
        {
            if (!interactTogglesOpenClosed || !Runtime.TryGetConnection(LogicalId, out LocationConnectionSnapshot connection))
            {
                return;
            }

            LocationConnectionOpenState next = connection.OpenState == LocationConnectionOpenState.Open ? LocationConnectionOpenState.Closed : LocationConnectionOpenState.Open;
            LocationConnectionOperationResult result = Runtime.RequestConnectionOpenState($"scene-binding.toggle.{LogicalId}.{next}", LogicalId, next, null, null, 0d, false);
            if (result.Succeeded)
            {
                SyncFromAuthoritative(Runtime, false);
            }
        }

        public SceneBindingTransitionResult RequestTraversal(EntityLocationReferenceData actor, LocationConnectionAccessContextData accessContext = null, double worldTime = 0d, bool preview = false)
        {
            return Runtime.RequestTransition(new SceneBindingTransitionRequest
            {
                transactionId = $"scene-binding.traverse.{LogicalId}.{Guid.NewGuid():N}",
                actor = actor,
                connectionId = LogicalId,
                fromLocationId = sourceLocationId,
                toLocationId = destinationLocationId,
                accessContext = accessContext,
                worldTime = worldTime,
                preview = preview
            });
        }
    }

    public sealed class WorldEntitySceneBinding : WorldSceneBindingComponent
    {
        [SerializeField] private LocationOccupantEntityType entityType = LocationOccupantEntityType.Person;
        [SerializeField] private string entityId;
        [SerializeField] private bool snapToGroundAfterMaterialization = true;
        [SerializeField] private float groundProbeHeight = 25f;
        [SerializeField] private float groundProbeDistance = 80f;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Entity;
        public bool SnapToGroundAfterMaterialization => snapToGroundAfterMaterialization;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public EntityLocationReferenceData EntityReference => new EntityLocationReferenceData { entityType = entityType, entityId = N(entityId), worldId = WorldId };

        public void ConfigureEntity(LocationOccupantEntityType type, string id, string sceneBindingKey, string scene, string world, bool snapToGround = true)
        {
            entityType = type;
            entityId = N(id);
            ConfigureBinding(EntityLocationReferenceKey.Build(type, entityId, string.IsNullOrWhiteSpace(world) ? PersistenceService.LocalWorldId : world), sceneBindingKey, scene, world, WorldSceneBindingRole.Primary, false);
            snapToGroundAfterMaterialization = snapToGround;
        }

        public override void SyncFromAuthoritative(WorldSceneBindingRuntime bindingRuntime, bool initialSync)
        {
            SceneBindingMaterializationResult result = bindingRuntime.MaterializeEntity(this);
            if (!result.Succeeded)
            {
                ApplyBindingResolution(WorldSceneBindingStatus.Degraded, result.Message);
            }
        }
    }

    public sealed class RouteSegmentSceneBinding : WorldSceneBindingComponent
    {
        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.RouteSegment;
    }

    public sealed class JourneySceneBinding : WorldSceneBindingComponent
    {
        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Journey;
    }

    public sealed class CheckpointSceneBinding : WorldSceneBindingComponent
    {
        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Checkpoint;
    }

    public sealed class WorldSceneBindingBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldSceneBindingBootstrapMode bootstrapMode = WorldSceneBindingBootstrapMode.ProductionBindOnly;
        [SerializeField] private bool registerChildrenOnEnable = true;
        [SerializeField] private bool syncAfterRegister = true;

        public WorldSceneBindingBootstrapMode BootstrapMode => bootstrapMode;
        public WorldSceneBindingValidationReport LastReport { get; private set; }

        private void OnEnable()
        {
            if (registerChildrenOnEnable)
            {
                RegisterChildren(WorldSceneBindingRuntime.Default);
            }
        }

        public WorldSceneBindingValidationReport RegisterChildren(WorldSceneBindingRuntime runtime)
        {
            WorldSceneBindingRuntime target = runtime ?? WorldSceneBindingRuntime.Default;
            foreach (WorldSceneBindingComponent binding in GetComponentsInChildren<WorldSceneBindingComponent>(true))
            {
                binding.Register(target);
            }

            LastReport = syncAfterRegister ? target.SyncAllFromAuthoritative(true) : target.Validate();
            return LastReport;
        }
    }
}
