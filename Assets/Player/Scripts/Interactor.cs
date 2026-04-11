using System.Collections.Generic;
using Common.World.Interaction;
using Player.Interfaces;
using Player.Targeting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class Interactor : MonoBehaviour
    {
        private enum InteractionSelectionSource
        {
            None,
            Target,
            Trigger
        }

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float maxInteractDistance = 3f;
        [SerializeField] private Camera viewCamera;

        [Header("Target Selection")]
        [SerializeField] private TargetSelector targetSelector;

        [Header("Raycast Filtering")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask ignoredRaycastLayers = 0;

        [Header("Trigger Fallback")]
        [SerializeField] private bool enableTriggerFallback = true;

        [Header("Debug")]
        [SerializeField] private bool debugInteractionSelection;

        private readonly List<IInteractable> triggerCandidates = new();
        private readonly Dictionary<IInteractable, int> triggerCandidateCounts = new();
        private IInteractable presentedInteractable;
        private InteractionTooltip activeTooltip;
        private InteractionSelectionSource currentSelectionSource;

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;

            if (targetSelector == null)
            {
                targetSelector = GetComponent<TargetSelector>()
                    ?? GetComponentInParent<TargetSelector>()
                    ?? GetComponentInChildren<TargetSelector>(true);
            }
        }

        private void OnDisable()
        {
            ClearTooltipPresentation();
            currentSelectionSource = InteractionSelectionSource.None;
            triggerCandidates.Clear();
            triggerCandidateCounts.Clear();
        }

        private void Update()
        {
            SyncInteractionPresentation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enableTriggerFallback || other == null || !other.isTrigger)
                return;

            if (!TryGetInteractable(other.transform, out var interactable))
                return;

            if (!SupportsMode(interactable, InteractionMode.Trigger))
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
            if (!enableTriggerFallback || other == null || !other.isTrigger)
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

        public void OnInteract(InputValue value)
        {
            if (!value.isPressed)
                return;

            if (!TryResolveInteractable(out var interactable))
                return;

            if (IsInteractionBlocked(interactable))
                return;

            interactable.Interact(gameObject);
            activeTooltip?.HideAfterInteraction();
        }

        private bool TryResolveInteractable(out IInteractable interactable)
        {
            currentSelectionSource = InteractionSelectionSource.None;

            if (TryResolveFromTargetSelection(out interactable))
            {
                currentSelectionSource = InteractionSelectionSource.Target;
                return true;
            }

            if (enableTriggerFallback && TryResolveFromTriggerCandidates(out interactable))
            {
                currentSelectionSource = InteractionSelectionSource.Trigger;
                return true;
            }

            interactable = null;
            return false;
        }

        private bool TryResolveFromTargetSelection(out IInteractable interactable)
        {
            interactable = null;

            var target = targetSelector != null ? targetSelector.CurrentInteractionTarget : null;
            if (target == null)
                return false;

            if (!TryGetInteractable(target, out interactable))
                return false;

            if (!SupportsMode(interactable, InteractionMode.Target))
                return false;

            if (interactable is not Component component || component == null)
                return false;

            var interactionOrigin = GetInteractionOrigin();
            var targetPoint = target.position;
            if (!IsWithinInteractionDistance(interactionOrigin, targetPoint))
                return false;

            var lineOfSightOrigin = GetLineOfSightOrigin();
            if (requireLineOfSight && !HasLineOfSight(lineOfSightOrigin, targetPoint, interactable))
                return false;

            return true;
        }

        private bool TryResolveFromTriggerCandidates(out IInteractable interactable)
        {
            interactable = null;

            if (triggerCandidates.Count == 0)
                return false;

            var interactionOrigin = GetInteractionOrigin();
            var facingForward = GetInteractionForward();

            float bestCenterDistance = float.MaxValue;
            float bestWorldDistance = float.MaxValue;

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

                if (!SupportsMode(candidate, InteractionMode.Trigger))
                    continue;

                var targetPoint = GetInteractionReferencePoint(component);
                var toTarget = targetPoint - interactionOrigin;
                var flatDirection = FlattenDirection(toTarget);
                float worldDistance = GetInteractionDistance(interactionOrigin, targetPoint);
                if (worldDistance <= 0.001f || worldDistance > maxInteractDistance)
                    continue;

                if (flatDirection == Vector3.zero)
                    continue;

                float facingDot = Vector3.Dot(facingForward, flatDirection);
                if (facingDot <= 0f)
                    continue;

                float centerDistance = Vector3.Angle(facingForward, flatDirection);
                if (!IsBetterCandidate(centerDistance, worldDistance, bestCenterDistance, bestWorldDistance))
                    continue;

                bestCenterDistance = centerDistance;
                bestWorldDistance = worldDistance;
                interactable = candidate;
            }

            return interactable != null;
        }

        private void SyncInteractionPresentation()
        {
            if (!TryResolvePresentationInteractable(out var interactable))
            {
                ClearTooltipPresentation();
                currentSelectionSource = InteractionSelectionSource.None;
                return;
            }

            SyncTooltipPresentation(interactable);
        }

        private bool TryResolvePresentationInteractable(out IInteractable interactable)
        {
            interactable = null;
            currentSelectionSource = InteractionSelectionSource.None;

            var target = targetSelector != null ? targetSelector.CurrentInteractionTarget : null;
            if (target != null &&
                TryGetInteractable(target, out interactable) &&
                SupportsMode(interactable, InteractionMode.Target))
            {
                currentSelectionSource = InteractionSelectionSource.Target;
                return true;
            }

            if (enableTriggerFallback && TryResolveFromTriggerCandidates(out interactable))
            {
                currentSelectionSource = InteractionSelectionSource.Trigger;
                return true;
            }

            return false;
        }

        private void SyncTooltipPresentation(IInteractable interactable)
        {
            if (presentedInteractable != null && !ReferenceEquals(presentedInteractable, interactable))
            {
                LogTooltipDebug($"Switching tooltip from '{GetInteractableName(presentedInteractable)}' to '{GetInteractableName(interactable)}'.");
                ClearTooltipPresentation();
            }

            var tooltip = interactable.Tooltip;
            if (tooltip == null)
            {
                presentedInteractable = interactable;
                activeTooltip = null;
                LogTooltipDebug($"No tooltip assigned/resolved for '{GetInteractableName(interactable)}'.");
                return;
            }

            if (!ReferenceEquals(activeTooltip, tooltip))
            {
                ClearTooltipPresentation();
                activeTooltip = tooltip;
                activeTooltip.ShowFor(gameObject);
                LogTooltipDebug($"Tooltip show requested for '{GetInteractableName(interactable)}' from {currentSelectionSource} selection.");
            }
            else
            {
                if (currentSelectionSource == InteractionSelectionSource.Target)
                    activeTooltip.ShowFor(gameObject);

                activeTooltip.RefreshFor(gameObject);
                LogTooltipDebug($"Tooltip refresh requested for '{GetInteractableName(interactable)}' from {currentSelectionSource} selection.");
            }

            presentedInteractable = interactable;
        }

        private void ClearTooltipPresentation()
        {
            if (activeTooltip != null)
            {
                LogTooltipDebug($"Hiding tooltip for '{GetInteractableName(presentedInteractable)}'.");
                activeTooltip.HideFor(gameObject);
            }

            activeTooltip = null;
            presentedInteractable = null;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 targetPoint, IInteractable candidate)
        {
            var direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
                return true;

            direction /= distance;
            int raycastMask = ~ignoredRaycastLayers.value;
            var hits = Physics.RaycastAll(origin, direction, distance, raycastMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return true;

            System.Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null)
                    continue;

                var hitInteractor = hit.collider.GetComponentInParent<Interactor>();
                if (hitInteractor == this)
                    continue;

                if (!TryGetInteractable(hit.collider.transform, out var firstHitInteractable))
                    return false;

                return ReferenceEquals(firstHitInteractable, candidate);
            }

            return true;
        }

        private static Vector3 GetTargetPoint(Component candidateComponent, Collider sourceCollider, Vector3 origin)
        {
            if (sourceCollider != null)
            {
                var closestPoint = sourceCollider.ClosestPoint(origin);
                if ((closestPoint - origin).sqrMagnitude > 0.0001f)
                    return closestPoint;

                return sourceCollider.bounds.center;
            }

            var colliders = candidateComponent.GetComponentsInChildren<Collider>(true);
            var bestPoint = candidateComponent.transform.position;
            var bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null)
                    continue;

                var point = collider.ClosestPoint(origin);
                var distanceSqr = (point - origin).sqrMagnitude;
                if (distanceSqr <= 0.0001f)
                {
                    point = collider.bounds.center;
                    distanceSqr = (point - origin).sqrMagnitude;
                }

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                bestPoint = point;
            }

            return bestPoint;
        }

        private static Vector3 GetInteractionReferencePoint(Component interactableComponent)
        {
            if (interactableComponent == null)
                return Vector3.zero;

            var targetable = interactableComponent.GetComponent<ITargetable>()
                ?? interactableComponent.GetComponentInChildren<ITargetable>(true)
                ?? interactableComponent.GetComponentInParent<ITargetable>();

            if (targetable?.TargetTransform != null)
                return targetable.TargetTransform.position;

            return interactableComponent.transform.position;
        }


        private Vector3 GetInteractionOrigin()
        {
            return transform.position;
        }

        private Vector3 GetInteractionForward()
        {
            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }

        private Vector3 GetLineOfSightOrigin()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            return cam != null ? cam.transform.position : transform.position;
        }

        private bool IsWithinInteractionDistance(Vector3 origin, Vector3 targetPoint)
        {
            return GetInteractionDistance(origin, targetPoint) <= maxInteractDistance;
        }

        private float GetInteractionDistance(Vector3 origin, Vector3 targetPoint)
        {
            var delta = targetPoint - origin;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude <= 0.0001f ? Vector3.zero : direction.normalized;
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

        private bool IsInteractionBlocked(IInteractable interactable)
        {
            if (interactable is not Component component)
                return false;

            var provider = component.GetComponent<IInteractionTooltipProvider>()
                ?? component.GetComponentInParent<IInteractionTooltipProvider>()
                ?? component.GetComponentInChildren<IInteractionTooltipProvider>(true);

            if (provider == null)
                return false;

            return provider.GetTooltipState(gameObject).IsBlocked;
        }

        private static bool SupportsMode(IInteractable interactable, InteractionMode mode)
        {
            return interactable != null && (interactable.SupportedModes & mode) != 0;
        }

        private void LogTooltipDebug(string message)
        {
            if (!debugInteractionSelection)
                return;

            Debug.Log($"[Interactor][Tooltip] {message}", this);
        }

        private static string GetInteractableName(IInteractable interactable)
        {
            if (interactable is Component component && component != null)
                return component.name;

            return interactable?.GetType().Name ?? "null";
        }
    }
}
