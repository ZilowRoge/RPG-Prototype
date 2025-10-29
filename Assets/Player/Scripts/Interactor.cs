using UnityEngine;
using UnityEngine.InputSystem;
using Common.World.Interaction;

namespace Player {
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private LayerMask interactableMask = ~0;

        private IInteractable current;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IInteractable>(out var i))
                current = i;
        }

        private void OnTriggerExit(Collider other)
        {
            if (current != null && other.GetComponent<IInteractable>() == current)
                current = null;
        }

        public void OnInteract(InputValue v)
        {
            if (!v.isPressed) return;
            Debug.Log("Key [E] pressed");
            var interactable = current;
            var tooltip = FindTooltip(interactable);
            interactable?.Interact(gameObject);
            tooltip?.HideAfterInteraction();
        }

        private static InteractionTooltip FindTooltip(IInteractable interactable)
        {
            if (interactable is not Component component) return null;
            return component.GetComponent<InteractionTooltip>()
                ?? component.GetComponentInParent<InteractionTooltip>()
                ?? component.GetComponentInChildren<InteractionTooltip>(true);
        }
    }
}
