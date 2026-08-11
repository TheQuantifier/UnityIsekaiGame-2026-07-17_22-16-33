using System;
using UnityEngine;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
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
}
