using UnityEngine;

namespace Common.World.Interaction
{
    public static class InteractionTooltipResolver
    {
        public static InteractionTooltip Resolve(Component owner, InteractionTooltip current)
        {
            if (current != null)
                return current;

            if (owner == null)
                return null;

            return owner.GetComponent<InteractionTooltip>()
                ?? owner.GetComponentInParent<InteractionTooltip>()
                ?? owner.GetComponentInChildren<InteractionTooltip>(true);
        }
    }
}
