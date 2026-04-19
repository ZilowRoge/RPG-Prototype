using UnityEngine;

namespace Common.World.Interaction
{
    public interface IInteractionTooltipProvider
    {
        InteractionTooltipState GetTooltipState(GameObject player);
    }
}
