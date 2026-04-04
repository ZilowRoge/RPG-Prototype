using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Common.World.Interaction;
using OutlineURP;

namespace Player {
    public class Interactor : MonoBehaviour
    {
        [Header("Field Of View")]
        [SerializeField, Min(0.1f)] private float maxInteractDistance = 3f;
        [SerializeField, Range(1f, 180f)] private float fieldOfViewAngle = 100f;
        [SerializeField, Range(0.01f, 1f)] private float maxViewportRadius = 0.55f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private Camera viewCamera;

        [Header("Raycast Filtering")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask ignoredRaycastLayers = 0;

        [Header("Trigger Fallback")]
        [SerializeField] private bool enableTriggerFallback = true;

        [Header("Outline")]
        [SerializeField] private bool enableOutlineHighlight = true;
        [SerializeField] private bool autoCreateOutlineTarget = true;
        [SerializeField] private OutlineGroup autoCreatedOutlineGroup = OutlineGroup.Custom1;

        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly List<IInteractable> triggerCandidates = new();
        private readonly Dictionary<IInteractable, int> triggerCandidateCounts = new();
        private OutlineTarget outlinedInteractable;

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;
        }

        private void OnDisable()
        {
            ClearInteractableOutline();
            triggerCandidates.Clear();
            triggerCandidateCounts.Clear();
        }

        private void Update()
        {
            SyncInteractableOutline();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enableTriggerFallback)
                return;

            if (other == null || !other.isTrigger)
                return;

            if (!TryGetInteractable(other.transform, out var interactable))
                return;

            if (!UsesTriggerInteraction(interactable))
                return;

            if (triggerCandidateCounts.TryGetValue(interactable, out var currentCount))
            {
                triggerCandidateCounts[interactable] = currentCount + 1;
                return;
            }

            triggerCandidateCounts[interactable] = 1;
            if (!triggerCandidates.Contains(interactable))
                triggerCandidates.Add(interactable);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!enableTriggerFallback)
                return;

            if (other == null || !other.isTrigger)
                return;

            if (!TryGetInteractable(other.transform, out var interactable))
                return;

            if (!triggerCandidateCounts.TryGetValue(interactable, out var currentCount))
                return;

            if (currentCount <= 1)
            {
                triggerCandidateCounts.Remove(interactable);
                triggerCandidates.Remove(interactable);
                return;
            }

            triggerCandidateCounts[interactable] = currentCount - 1;
        }

        public void OnInteract(InputValue v)
        {
            if (!v.isPressed) return;

            if (!TryResolveInteractable(out var interactable))
                return;

            var tooltip = FindTooltip(interactable);
            interactable.Interact(gameObject);
            tooltip?.HideAfterInteraction();
        }

        private bool TryResolveInteractable(out IInteractable interactable)
        {
            if (enableTriggerFallback && TryResolveFromTriggerCandidates(out interactable))
                return true;

            if (TryResolveFromFieldOfView(out interactable))
                return true;

            interactable = null;
            return false;
        }

        private bool TryResolveFromFieldOfView(out IInteractable interactable)
        {
            interactable = null;

            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
                return false;

            var origin = cam.transform.position;
            var forward = cam.transform.forward;
            float halfFov = fieldOfViewAngle * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(
                origin,
                maxInteractDistance,
                overlapBuffer,
                interactableMask,
                QueryTriggerInteraction.Collide);

            if (count <= 0)
                return false;

            float bestCenterDistance = float.MaxValue;
            float bestWorldDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hitCollider = overlapBuffer[i];
                if (hitCollider == null)
                    continue;

                if (!TryGetInteractable(hitCollider.transform, out var candidate))
                    continue;

                if (candidate is not Component candidateComponent || candidateComponent == null)
                    continue;

                if (UsesTriggerInteraction(candidate))
                    continue;

                var targetPoint = hitCollider.bounds.center;
                var toTarget = targetPoint - origin;
                float worldDistance = toTarget.magnitude;
                if (worldDistance <= 0.001f || worldDistance > maxInteractDistance)
                    continue;

                float angle = Vector3.Angle(forward, toTarget);
                if (angle > halfFov)
                    continue;

                var viewport = cam.WorldToViewportPoint(targetPoint);
                if (viewport.z <= 0f)
                    continue;

                float dx = viewport.x - 0.5f;
                float dy = viewport.y - 0.5f;
                float centerDistance = Mathf.Sqrt(dx * dx + dy * dy);
                if (centerDistance > maxViewportRadius)
                    continue;

                if (requireLineOfSight && !HasLineOfSight(origin, targetPoint, candidate))
                    continue;

                if (!IsBetterCandidate(centerDistance, worldDistance, bestCenterDistance, bestWorldDistance))
                    continue;

                bestCenterDistance = centerDistance;
                bestWorldDistance = worldDistance;
                interactable = candidate;
            }

            return interactable != null;
        }

        private bool TryResolveFromTriggerCandidates(out IInteractable interactable)
        {
            interactable = null;

            if (triggerCandidates.Count == 0)
                return false;

            for (int i = triggerCandidates.Count - 1; i >= 0; i--)
            {
                var candidate = triggerCandidates[i];
                if (candidate is not Component component || component == null)
                {
                    if (!ReferenceEquals(candidate, null))
                        triggerCandidateCounts.Remove(candidate);
                    triggerCandidates.RemoveAt(i);
                    continue;
                }

                if (!triggerCandidateCounts.ContainsKey(candidate))
                {
                    triggerCandidates.RemoveAt(i);
                    continue;
                }

                interactable = candidate;
                return true;
            }

            return false;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 targetPoint, IInteractable candidate)
        {
            var direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
                return true;

            direction /= distance;
            int raycastMask = ~ignoredRaycastLayers.value;

            if (!Physics.Raycast(origin, direction, out var hit, distance, raycastMask, QueryTriggerInteraction.Collide))
                return true;

            if (!TryGetInteractable(hit.collider.transform, out var firstHitInteractable))
                return false;

            return ReferenceEquals(firstHitInteractable, candidate);
        }

        private static bool IsBetterCandidate(float centerDistance, float worldDistance, float bestCenterDistance, float bestWorldDistance)
        {
            if (centerDistance < bestCenterDistance - 0.0001f)
                return true;

            if (Mathf.Abs(centerDistance - bestCenterDistance) <= 0.0001f && worldDistance < bestWorldDistance)
                return true;

            return false;
        }

        private static bool TryGetInteractable(Transform from, out IInteractable interactable)
        {
            interactable = null;
            if (from == null)
                return false;

            interactable = from.GetComponent<IInteractable>()
                ?? from.GetComponentInParent<IInteractable>()
                ?? from.GetComponentInChildren<IInteractable>(true);

            return interactable != null;
        }

        private static bool UsesTriggerInteraction(IInteractable interactable)
        {
            if (interactable is not Component component || component == null)
                return false;

            var colliders = component.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                    return true;
            }

            return false;
        }

        private void SyncInteractableOutline()
        {
            if (!enableOutlineHighlight)
            {
                ClearInteractableOutline();
                return;
            }

            if (!TryResolveInteractable(out var interactable))
            {
                ClearInteractableOutline();
                return;
            }

            var newOutlineTarget = ResolveOutlineTarget(interactable);
            if (outlinedInteractable == newOutlineTarget)
            {
                if (newOutlineTarget != null)
                    OutlineController.SetHovered(newOutlineTarget, true);
                return;
            }

            if (outlinedInteractable != null)
                OutlineController.SetHovered(outlinedInteractable, false);

            outlinedInteractable = newOutlineTarget;
            if (outlinedInteractable != null)
                OutlineController.SetHovered(outlinedInteractable, true);
        }

        private OutlineTarget ResolveOutlineTarget(IInteractable interactable)
        {
            if (interactable is not Component component || component == null)
                return null;

            var existingOutline = component.GetComponent<OutlineTarget>()
                ?? component.GetComponentInParent<OutlineTarget>()
                ?? component.GetComponentInChildren<OutlineTarget>(true);

            if (existingOutline != null)
                return existingOutline;

            if (!autoCreateOutlineTarget)
                return null;

            var createdOutline = component.GetComponent<OutlineTarget>();
            if (createdOutline != null)
                return createdOutline;

            createdOutline = component.gameObject.AddComponent<OutlineTarget>();
            createdOutline.SetGroup(autoCreatedOutlineGroup);
            return createdOutline;
        }

        private void ClearInteractableOutline()
        {
            if (outlinedInteractable == null)
                return;

            OutlineController.SetHovered(outlinedInteractable, false);
            outlinedInteractable = null;
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
