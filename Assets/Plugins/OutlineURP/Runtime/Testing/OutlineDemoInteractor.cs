using UnityEngine;

namespace OutlineURP.Testing
{
    public sealed class OutlineDemoInteractor : MonoBehaviour
    {
        [SerializeField]
        private Camera rayCamera;

        [SerializeField]
        private LayerMask raycastLayers = ~0;

        [SerializeField]
        private float maxDistance = 250f;

        [SerializeField]
        private bool hoverEnemies;

        [SerializeField]
        private bool hoverPickups = true;

        [SerializeField]
        private bool selectEnemies = true;

        [SerializeField]
        private bool selectPickups;

        [SerializeField]
        private KeyCode toggleXRayKey = KeyCode.X;

        [SerializeField]
        private KeyCode cycleEnemyTargetKey = KeyCode.Tab;

        [SerializeField]
        private KeyCode clearSelectionKey = KeyCode.Escape;

        [SerializeField]
        private KeyCode clearAllStatesKey = KeyCode.C;

        [SerializeField]
        private bool autoSelectFirstEnemyOnStart = true;

        private bool xRayEnabled;
        private bool autoSelectionAttempted;
        private OutlineTarget hoveredTarget;
        private OutlineTarget selectedTarget;

        public OutlineTarget HoveredTarget => hoveredTarget;
        public OutlineTarget SelectedTarget => selectedTarget;

        private void Awake()
        {
            if (rayCamera == null)
            {
                rayCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (rayCamera == null)
            {
                rayCamera = Camera.main;
                if (rayCamera == null)
                {
                    return;
                }
            }

            TryAutoSelectFirstEnemy();

            if (Input.GetKeyDown(toggleXRayKey))
            {
                xRayEnabled = !xRayEnabled;
                OutlineController.SetOcclusionMode(xRayEnabled ? OutlineOcclusionMode.XRay : OutlineOcclusionMode.RespectDepth);
            }

            if (Input.GetKeyDown(clearAllStatesKey))
            {
                ClearAll();
                return;
            }

            if (Input.GetKeyDown(clearSelectionKey))
            {
                SetSelectedTarget(null);
            }

            if (Input.GetKeyDown(cycleEnemyTargetKey))
            {
                CycleEnemySelection();
            }

            UpdateHover();

            if (Input.GetMouseButtonDown(0))
            {
                var hitTarget = RaycastTarget();
                if (CanSelect(hitTarget))
                {
                    SetSelectedTarget(hitTarget);
                }
                else
                {
                    SetSelectedTarget(null);
                }
            }
        }

        private void UpdateHover()
        {
            var hitTarget = RaycastTarget();
            if (!CanHover(hitTarget))
            {
                hitTarget = null;
            }

            if (hitTarget == hoveredTarget)
            {
                return;
            }

            if (hoveredTarget != null && hoveredTarget != selectedTarget)
            {
                hoveredTarget.SetHovered(false);
            }

            hoveredTarget = hitTarget;
            if (hoveredTarget != null && hoveredTarget != selectedTarget)
            {
                hoveredTarget.SetHovered(true);
            }
        }

        private OutlineTarget RaycastTarget()
        {
            var ray = rayCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, maxDistance, raycastLayers, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            return hit.collider != null ? hit.collider.GetComponentInParent<OutlineTarget>() : null;
        }

        private bool CanHover(OutlineTarget target)
        {
            if (target == null)
            {
                return false;
            }

            return target.Group switch
            {
                OutlineGroup.Enemy => hoverEnemies,
                OutlineGroup.Pickup => hoverPickups,
                _ => true
            };
        }

        private bool CanSelect(OutlineTarget target)
        {
            if (target == null)
            {
                return false;
            }

            return target.Group switch
            {
                OutlineGroup.Enemy => selectEnemies,
                OutlineGroup.Pickup => selectPickups,
                _ => true
            };
        }

        private void SetSelectedTarget(OutlineTarget target)
        {
            if (selectedTarget == target)
            {
                return;
            }

            if (selectedTarget != null)
            {
                selectedTarget.SetSelected(false);
            }

            selectedTarget = target;
            if (selectedTarget != null)
            {
                OutlineController.SetExclusiveSelected(selectedTarget);
                selectedTarget.SetHovered(false);
            }
        }

        private void ClearAll()
        {
            hoveredTarget = null;
            selectedTarget = null;
            OutlineController.ClearAllStates();
        }

        private void TryAutoSelectFirstEnemy()
        {
            if (!autoSelectFirstEnemyOnStart || autoSelectionAttempted || selectedTarget != null)
            {
                return;
            }

            var allTargets = FindObjectsByType<OutlineTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allTargets == null || allTargets.Length == 0)
            {
                return;
            }

            for (var i = 0; i < allTargets.Length; i++)
            {
                var candidate = allTargets[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.Group != OutlineGroup.Enemy || !CanSelect(candidate))
                {
                    continue;
                }

                SetSelectedTarget(candidate);
                autoSelectionAttempted = true;
                return;
            }
        }

        private void CycleEnemySelection()
        {
            var allTargets = FindObjectsByType<OutlineTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allTargets == null || allTargets.Length == 0)
            {
                SetSelectedTarget(null);
                return;
            }

            var currentIndex = -1;
            for (var i = 0; i < allTargets.Length; i++)
            {
                if (allTargets[i] == selectedTarget)
                {
                    currentIndex = i;
                    break;
                }
            }

            for (var step = 1; step <= allTargets.Length; step++)
            {
                var index = (currentIndex + step) % allTargets.Length;
                var candidate = allTargets[index];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.Group != OutlineGroup.Enemy)
                {
                    continue;
                }

                SetSelectedTarget(candidate);
                return;
            }

            SetSelectedTarget(null);
        }
    }
}
