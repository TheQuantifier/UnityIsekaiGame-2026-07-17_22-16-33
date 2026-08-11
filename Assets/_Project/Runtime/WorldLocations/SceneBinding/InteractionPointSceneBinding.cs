using UnityEngine;
using UnityIsekaiGame.Interaction;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
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
}
