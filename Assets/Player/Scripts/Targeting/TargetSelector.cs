using UnityEngine;
using Player.Interfaces;
using Systems.Debugging;
using Common.World.Interaction;

namespace Player.Targeting
{
    [DisallowMultipleComponent]
    public class TargetSelector : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] private float maxDistance = 25f;
        [SerializeField, Range(0f, 180f)] private float fovAngle = 120f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private bool preferCenterRay = true;
        [SerializeField] private float rayRadius = 0.2f;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private Transform selfRoot;
        [SerializeField] private LayerMask occluderMask = ~0;

        [Header("Screen Center")]
        [SerializeField] private bool preferScreenCenter = true;
        [SerializeField, Range(0f,1f)] private float maxViewportRadius = 0.6f;
        [SerializeField] private float screenWeight = 2f;
        [SerializeField] private float distWeight = 1f;
        [SerializeField] private float dotWeight = 2f;
        [SerializeField] private float occlusionPenalty = 1f;
        [SerializeField] private float hysteresisThreshold = 0.15f;

        [Header("Debug")] 
        [SerializeField] private bool debugDraw = true;
        [SerializeField] private Color debugRayColor = new Color(0f, 1f, 1f, 0.7f);
        [SerializeField] private Color debugHitColor = new Color(1f, 0.92f, 0.016f, 0.9f);
        [SerializeField] private float debugHitMarkerSize = 0.2f;
        
        [Header("References")] 
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ComponentLogger logger = new ComponentLogger();

        private float nextUpdateTime;
        private readonly Collider[] overlapBuffer = new Collider[64];

        public Transform CurrentTarget { get; private set; }
        public Transform CurrentCombatTarget => IsCombatTarget(CurrentTarget) ? CurrentTarget : null;
        public Transform CurrentInteractionTarget => IsInteractionTarget(CurrentTarget) ? CurrentTarget : null;

        private void Awake()
        {
            InitializeLogger();
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (selfRoot == null)
                selfRoot = transform.root;
        }

        private void OnEnable()
        {
            InitializeLogger();
            nextUpdateTime = 0f;
        }
        
        private void OnDisable()
        {
            SetCurrentTarget(null);
        }

        private void OnValidate()
        {
            InitializeLogger();
        }

        private void Update()
        {
            if (CurrentTarget != null && !IsTargetValid(CurrentTarget))
                SetCurrentTarget(null);

            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + Mathf.Max(0.01f, updateInterval);

            Transform target = null;
            if (preferCenterRay && viewCamera != null)
            {
                if (TryPickByRay(viewCamera, out target, maxDistance, rayRadius))
                {
                    SetCurrentTarget(target);
                    return;
                }
            }

            if (TryPickByFov(out target, maxDistance, fovAngle))
            {
                SetCurrentTarget(target);
                return;
            }

            SetCurrentTarget(null);
        }

        private bool TryPickByRay(Camera cam, out Transform target, float range, float radius)
        {
            target = null;
            var origin = cam.transform.position;
            var dir = cam.transform.forward;
            if (debugDraw)
            {
                Debug.DrawRay(origin, dir * range, debugRayColor);
            }
            if (Physics.SphereCast(origin, radius, dir, out var hit, range, targetMask, QueryTriggerInteraction.Ignore))
            {
                var t = GetTargetTransform(hit.collider);
                if (t != null)
                {
                    if (debugDraw)
                    {
                        Debug.DrawLine(origin, hit.point, debugHitColor);
                        var s = debugHitMarkerSize;
                        Debug.DrawLine(hit.point - Vector3.right * s, hit.point + Vector3.right * s, debugHitColor);
                        Debug.DrawLine(hit.point - Vector3.up * s, hit.point + Vector3.up * s, debugHitColor);
                        Debug.DrawLine(hit.point - Vector3.forward * s, hit.point + Vector3.forward * s, debugHitColor);
                    }
                    target = t;
                    return true;
                }
                else
                {
                }
            }
            return false;
        }

        private bool TryPickByFov(out Transform target, float range, float fov)
        {
            target = null;
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, overlapBuffer, targetMask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            Transform best = null;
            float bestScore = float.NegativeInfinity;
            var fwd = (viewCamera != null ? viewCamera.transform.forward : transform.forward);
            var origin = (viewCamera != null ? viewCamera.transform.position : transform.position);

            for (int i = 0; i < count; i++)
            {
                var col = overlapBuffer[i];
                if (col == null) continue;
                var candidate = GetTargetTransform(col);
                if (candidate == null) continue;
                var center = col.bounds.center;
                var to = (center - origin);
                var dist = to.magnitude;
                if (dist <= 0.001f) continue;
                var dir = to / dist;

                float score;
                if (preferScreenCenter && viewCamera != null)
                {
                    var vp = viewCamera.WorldToViewportPoint(center);
                    if (vp.z <= 0f) continue;
                    float dx = vp.x - 0.5f, dy = vp.y - 0.5f;
                    float screenDist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (screenDist > maxViewportRadius) continue;

                    var dot = Mathf.Max(0f, Vector3.Dot(fwd, dir));
                    bool occluded = false;
                    if (Physics.Raycast(origin, dir, out var hit, dist, occluderMask, QueryTriggerInteraction.Ignore))
                    {
                        if (IsSelf(hit.collider.transform))
                        {
                            // Step through self and recheck occlusion
                            var stepOrigin = hit.point + dir * 0.01f;
                            if (Physics.Raycast(stepOrigin, dir, out var hit2, dist - hit.distance - 0.01f, occluderMask, QueryTriggerInteraction.Ignore))
                                occluded = GetTargetTransform(hit2.collider) != candidate;
                        }
                        else occluded = GetTargetTransform(hit.collider) != candidate;
                    }

                    score = -screenWeight * screenDist - distWeight * (dist / range) + dotWeight * dot - (occluded ? occlusionPenalty : 0f);
                }
                else
                {
                    var dot = Vector3.Dot(fwd, dir);
                    var angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
                    if (angle > fov * 0.5f) continue;
                    if (Physics.Raycast(origin, dir, out var hit, dist, occluderMask, QueryTriggerInteraction.Ignore))
                    {
                        if (IsSelf(hit.collider.transform))
                        {
                            var stepOrigin = hit.point + dir * 0.01f;
                            if (Physics.Raycast(stepOrigin, dir, out var hit2, dist - hit.distance - 0.01f, occluderMask, QueryTriggerInteraction.Ignore))
                                if (GetTargetTransform(hit2.collider) != candidate) continue;
                        }
                        else if (GetTargetTransform(hit.collider) != candidate) continue;
                    }
                    score = dot * 2f - (dist / range);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best != null)
            {
                if (preferScreenCenter && CurrentTarget != null)
                {
                    var cur = CurrentTarget;
                    var curPos = cur.position;
                    var toCur = curPos - origin;
                    var dCur = toCur.magnitude;
                    if (dCur > 0.001f)
                    {
                        var dirCur = toCur / dCur;
                        float curScore;
                        var vpCur = viewCamera != null ? viewCamera.WorldToViewportPoint(curPos) : new Vector3(0.5f,0.5f,1f);
                        if (viewCamera != null && vpCur.z > 0f)
                        {
                            float dx = vpCur.x - 0.5f, dy = vpCur.y - 0.5f;
                            float screenDist = Mathf.Sqrt(dx * dx + dy * dy);
                            var dot = Mathf.Max(0f, Vector3.Dot(fwd, dirCur));
                            bool occluded = false;
                            if (Physics.Raycast(origin, dirCur, out var hitCur, dCur, targetMask, QueryTriggerInteraction.Ignore))
                                occluded = GetTargetTransform(hitCur.collider) != cur;
                            curScore = -screenWeight * screenDist - distWeight * (dCur / range) + dotWeight * dot - (occluded ? occlusionPenalty : 0f);
                        }
                        else
                        {
                            var dot = Vector3.Dot(fwd, dirCur);
                            curScore = dot * 2f - (dCur / range);
                        }
                        if (curScore >= bestScore - hysteresisThreshold)
                        {
                            target = cur;
                            return true;
                        }
                    }
                }

                target = best;
                return true;
            }
            return false;
        }

        private Transform GetTargetTransform(Collider col)
        {
            if (col == null) return null;
            var t = col.transform;
            if (IsSelf(t)) return null;

            if (TryGetCombatTargetTransform(t, out var combatTarget))
                return combatTarget;

            if (TryGetInteractionTargetTransform(t, out var interactionTarget))
                return interactionTarget;

            return null;
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null)
                return false;

            if (!target.gameObject.activeInHierarchy)
                return false;

            var healthProvider = target.GetComponentInParent<IHealthProvider>();
            if (healthProvider != null && healthProvider.CurrentHealth <= 0f)
                return false;

            var deathState = target.GetComponentInParent<IDeathState>();
            if (deathState != null && deathState.IsDead)
                return false;

            return !IsSelf(target) && (IsCombatTarget(target) || IsInteractionTarget(target));
        }
        
        public void ClearTarget()
        {
            SetCurrentTarget(null);
        }
        public Transform FindBestTarget(float range, float fov, bool preferCenter)
        {
            if (viewCamera != null && preferCenter)
            {
                if (TryPickByRay(viewCamera, out var t, range, rayRadius))
                    return t;
            }
            if (TryPickByFov(out var t2, range, fov))
                return t2;
            return null;
        }

        private bool IsSelf(Transform t)
        {
            return selfRoot != null && t != null && t.IsChildOf(selfRoot);
        }

        private bool TryGetCombatTargetTransform(Transform source, out Transform target)
        {
            target = null;

            var damageable = source.GetComponentInParent<IDamageable>();
            if (damageable is not Component damageComponent)
                return false;

            if (IsSelf(damageComponent.transform))
                return false;

            if (damageable is IHealthProvider healthProvider && healthProvider.CurrentHealth <= 0f)
                return false;

            if (damageable is IDeathState deathState && deathState.IsDead)
                return false;

            target = ResolveTargetTransform(damageComponent.transform);
            return target != null && !(selfRoot != null && target.IsChildOf(selfRoot));
        }

        private bool TryGetInteractionTargetTransform(Transform source, out Transform target)
        {
            target = null;

            var interactable = source.GetComponent<IInteractable>()
                ?? source.GetComponentInParent<IInteractable>()
                ?? source.GetComponentInChildren<IInteractable>(true);

            if (interactable is not Component interactableComponent || interactableComponent == null)
                return false;

            if ((interactable.SupportedModes & InteractionMode.Target) == 0)
                return false;

            if (IsSelf(interactableComponent.transform))
                return false;

            target = ResolveTargetTransform(interactableComponent.transform);
            return target != null &&
                   !(selfRoot != null && target.IsChildOf(selfRoot)) &&
                   IsWithinInteractionFacing(target.position);
        }

        private static Transform ResolveTargetTransform(Transform root)
        {
            if (root == null)
                return null;

            var targetable = root.GetComponent<ITargetable>() ?? root.GetComponentInChildren<ITargetable>();
            return targetable != null ? targetable.TargetTransform : root;
        }

        private bool IsCombatTarget(Transform target)
        {
            return target != null && target.GetComponentInParent<IDamageable>() != null;
        }

        private bool IsInteractionTarget(Transform target)
        {
            if (target == null)
                return false;

            var interactable = target.GetComponent<IInteractable>()
                ?? target.GetComponentInParent<IInteractable>()
                ?? target.GetComponentInChildren<IInteractable>(true);

            return interactable != null &&
                   (interactable.SupportedModes & InteractionMode.Target) != 0 &&
                   IsWithinInteractionFacing(target.position);
        }

        private bool IsWithinInteractionFacing(Vector3 targetPosition)
        {
            var facingRoot = selfRoot != null ? selfRoot : transform;
            if (facingRoot == null)
                return true;

            var origin = facingRoot.position;
            var forward = facingRoot.forward;
            var toTarget = targetPosition - origin;

            toTarget.y = 0f;
            forward.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
                return true;

            if (forward.sqrMagnitude <= 0.0001f)
                forward = transform.forward;

            forward.Normalize();
            toTarget.Normalize();

            float halfFov = Mathf.Clamp(fovAngle * 0.5f, 0f, 180f);
            float minDot = Mathf.Cos(halfFov * Mathf.Deg2Rad);
            return Vector3.Dot(forward, toTarget) >= minDot;
        }

        private void InitializeLogger()
        {
            if (logger == null)
                logger = new ComponentLogger();
            logger.BindContext(this);
        }
        
        private void SetCurrentTarget(Transform target)
        {
            CurrentTarget = target;
        }

    }
}
