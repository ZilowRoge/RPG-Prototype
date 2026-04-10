using UnityEngine;

namespace Common.World.Interaction
{
    public interface IInteractable
    {
        InteractionMode SupportedModes { get; }
        InteractionTooltip Tooltip { get; }
        void Interact(GameObject player);
    }
}

