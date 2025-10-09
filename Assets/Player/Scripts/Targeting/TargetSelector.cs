using System.Collections.Generic;
using UnityEngine;
using Player.Statistics;
using Player.Interfaces;

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
        [SerializeField] private bool debugVerbose = false;
        [SerializeField] private Color debugRayColor = new Color(0f, 1f, 1f, 0.7f);
        [SerializeField] private Color debugHitColor = new Color(1f, 0.92f, 0.016f, 0.9f);
        [SerializeField] private float debugHitMarkerSize = 0.2f;

        [Header("References")] 
        [SerializeField] private Camera viewCamera;

        private float nextUpdateTime;
        private readonly Collider[] overlapBuffer = new Collider[64];

        public Transform CurrentTarget { get; private set; }

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (selfRoot == null)
                selfRoot = transform.root;
            Debug.Log($"[TargetSelector] Awake. Camera={(viewCamera != null ? viewCamera.name : "null")}, Self={(selfRoot != null ? selfRoot.name : "null")}");
        }

        private void OnEnable()
        {
            nextUpdateTime = 0f;
        }

        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + Mathf.Max(0.01f, updateInterval);

            var prev = CurrentTarget;
            Transform target = null;
            if (preferCenterRay && viewCamera != null)
            {
                if (TryPickByRay(viewCamera, out target, maxDistance, rayRadius))
                {
                    if (prev != target)
                        Debug.Log($"[TargetSelector] Target set (Ray): {(target != null ? target.name : "null")} ");
                    CurrentTarget = target;
                    return;
                }
            }

            if (TryPickByFov(out target, maxDistance, fovAngle))
            {
                if (prev != target)
                    Debug.Log($"[TargetSelector] Target set (FOV): {(target != null ? target.name : "null")} ");
                CurrentTarget = target;
                return;
            }

            if (prev != null)
                Debug.Log("[TargetSelector] Target cleared");
            CurrentTarget = null;
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
                    Debug.Log($"[TargetSelector] Ray hit {hit.collider.name} but not a valid target");
                }
            }
            return false;
        }

        private bool TryPickByFov(out Transform target, float range, float fov)
        {
            target = null;
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, overlapBuffer, targetMask, QueryTriggerInteraction.Ignore);
            if (debugVerbose) Debug.Log($"[TargetSelector] Overlap count={count}, range={range}");
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
                    if (vp.z <= 0f) { if (debugVerbose) Debug.Log($"[TargetSelector] Reject {col.name}: behind camera"); continue; }
                    float dx = vp.x - 0.5f, dy = vp.y - 0.5f;
                    float screenDist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (screenDist > maxViewportRadius) { if (debugVerbose) Debug.Log($"[TargetSelector] Reject {col.name}: too far from center ({screenDist:F2} > {maxViewportRadius})"); continue; }

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
                    if (debugVerbose)
                        Debug.Log($"[TargetSelector] Cand {candidate.name}: screen={screenDist:F2}, dist={dist:F1}, dot={dot:F2}, occ={(occluded?1:0)}, score={score:F2}");
                }
                else
                {
                    var dot = Vector3.Dot(fwd, dir);
                    var angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
                    if (angle > fov * 0.5f) { if (debugVerbose) Debug.Log($"[TargetSelector] Reject {col.name}: out of FOV ({angle:F1} deg)"); continue; }
                    if (Physics.Raycast(origin, dir, out var hit, dist, occluderMask, QueryTriggerInteraction.Ignore))
                    {
                        if (IsSelf(hit.collider.transform))
                        {
                            var stepOrigin = hit.point + dir * 0.01f;
                            if (Physics.Raycast(stepOrigin, dir, out var hit2, dist - hit.distance - 0.01f, occluderMask, QueryTriggerInteraction.Ignore))
                                if (GetTargetTransform(hit2.collider) != candidate) { if (debugVerbose) Debug.Log($"[TargetSelector] Reject {col.name}: occluded"); continue; }
                        }
                        else if (GetTargetTransform(hit.collider) != candidate) { if (debugVerbose) Debug.Log($"[TargetSelector] Reject {col.name}: occluded"); continue; }
                    }
                    score = dot * 2f - (dist / range);
                    if (debugVerbose)
                        Debug.Log($"[TargetSelector] Cand {candidate.name}: dist={dist:F1}, dot={dot:F2}, score={score:F2}");
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
                            if (debugDraw || debugVerbose) Debug.Log("[TargetSelector] Hysteresis kept current target");
                            return true;
                        }
                    }
                }

                target = best;
                Debug.Log($"[TargetSelector] FOV best: {best.name}");
                return true;
            }
            return false;
        }

        private Transform GetTargetTransform(Collider col)
        {
            if (col == null) return null;
            var t = col.transform;
            if (IsSelf(t)) return null;
            // Prefer explicit targetable marker if present
            var targetable = t.GetComponentInParent<ITargetable>();
            if (targetable != null)
            {
                var tr = targetable.TargetTransform;
                if (tr != null && !(selfRoot != null && tr.IsChildOf(selfRoot)))
                    return tr;
            }

            // Fallback: allow StatsController as legacy target source
            var sc = t.GetComponentInParent<StatsController>();
            if (sc != null && !(selfRoot != null && sc.transform.IsChildOf(selfRoot)))
                return sc.transform;
            return null;
        }
        
        public void ClearTarget()
        {
            CurrentTarget = null;
            Debug.Log("[TargetSelector] Target cleared by request");
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

    }
}
