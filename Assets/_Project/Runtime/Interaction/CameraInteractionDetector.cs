using UnityEngine;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.Interaction
{
    public sealed class CameraInteractionDetector : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameObject interactor;
        [SerializeField] private Transform rayOrigin;
        [SerializeField, Min(0.1f)] private float maxDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private IInteractable currentInteractable;
        private RaycastHit currentHit;

        public IInteractable CurrentInteractable => currentInteractable;
        public RaycastHit CurrentHit => currentHit;
        public bool HasTarget => currentInteractable != null;

        private void Reset()
        {
            rayOrigin = transform;
        }

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = gameObject;
            }

            if (rayOrigin == null)
            {
                rayOrigin = transform;
            }
        }

        private void Update()
        {
            RefreshTarget();

            if (input == null || !input.ConsumeInteract() || currentInteractable == null)
            {
                return;
            }

            InteractionContext context = CreateContext();
            if (currentInteractable.CanInteract(context))
            {
                currentInteractable.Interact(context);
            }
        }

        private void RefreshTarget()
        {
            currentInteractable = null;
            currentHit = default;

            if (rayOrigin == null)
            {
                return;
            }

            if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, maxDistance, interactionMask, triggerInteraction))
            {
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            InteractionContext context = new InteractionContext(interactor, rayOrigin, hit);
            if (!interactable.CanInteract(context))
            {
                return;
            }

            currentHit = hit;
            currentInteractable = interactable;
        }

        private InteractionContext CreateContext()
        {
            return new InteractionContext(interactor, rayOrigin, currentHit);
        }
    }
}
