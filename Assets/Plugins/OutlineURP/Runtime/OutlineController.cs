using UnityEngine;

namespace OutlineURP
{
    public static class OutlineController
    {
        private static bool hasOcclusionOverride;
        private static OutlineOcclusionMode occlusionOverride;

        public static bool HasOcclusionOverride => hasOcclusionOverride;
        public static OutlineOcclusionMode? CurrentOcclusionOverride => hasOcclusionOverride ? occlusionOverride : null;

        public static void SetOcclusionMode(OutlineOcclusionMode mode)
        {
            hasOcclusionOverride = true;
            occlusionOverride = mode;
        }

        public static void ClearOcclusionModeOverride()
        {
            hasOcclusionOverride = false;
        }

        public static OutlineOcclusionMode ResolveOcclusionMode(OutlineOcclusionMode fallback)
        {
            return hasOcclusionOverride ? occlusionOverride : fallback;
        }

        public static void SetHovered(OutlineTarget target, bool hovered)
        {
            if (target == null)
            {
                return;
            }

            target.SetHovered(hovered);
        }

        public static void SetSelected(OutlineTarget target, bool selected)
        {
            if (target == null)
            {
                return;
            }

            target.SetSelected(selected);
        }

        public static void SetHovered(GameObject root, bool hovered)
        {
            SetState(root, hovered, null);
        }

        public static void SetSelected(GameObject root, bool selected)
        {
            SetState(root, null, selected);
        }

        public static void SetExclusiveSelected(OutlineTarget target)
        {
            OutlineRegistry.SetExclusiveSelected(target);
        }

        public static void ClearAllStates()
        {
            OutlineRegistry.ClearAllStates();
        }

        private static void SetState(GameObject root, bool? hovered, bool? selected)
        {
            if (root == null)
            {
                return;
            }

            var targets = root.GetComponentsInChildren<OutlineTarget>(true);
            for (var i = 0; i < targets.Length; i++)
            {
                if (hovered.HasValue)
                {
                    targets[i].SetHovered(hovered.Value);
                }

                if (selected.HasValue)
                {
                    targets[i].SetSelected(selected.Value);
                }
            }
        }
    }
}
