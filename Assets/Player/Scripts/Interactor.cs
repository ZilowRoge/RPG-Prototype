using UnityEngine;
using UnityEngine.InputSystem;
using Common.World.Interaction;

namespace Player {
    public class Interactor : MonoBehaviour
    {
        // [SerializeField] private float maxDistance = 2.0f;
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
            current?.Interact(gameObject);
        }
    }
}
