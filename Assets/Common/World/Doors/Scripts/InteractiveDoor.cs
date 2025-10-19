using System;
using Common.World.Interaction;
using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Interactive Door")]
    public class InteractiveDoor : MonoBehaviour, IInteractable
    {
        [Header("Door Components")]
        [SerializeField] private DoorController door;
        [Header("Open Condition (Strategy)")]
        private IDoorOpenCondition openCondition;

        public void Interact(GameObject player)
        {
            if (door == null) door = GetComponent<DoorController>();
            if (openCondition == null)
            {
                openCondition = GetComponent<IDoorOpenCondition>();
            }

            if (door != null && door.AlreadyOpened)
                return;

            if (openCondition == null)
            {
                Debug.LogWarning("[InteractiveDoor] Missing open condition. Opening by interaction.", this);
                UnlockAndOpen();
                return;
            }

            if (openCondition.CanOpen(player))
                UnlockAndOpen();
        }

        private void UnlockAndOpen()
        {
            if (door == null) door = GetComponent<DoorController>();
            if (door == null)
            {
                Debug.LogWarning("[InteractiveDoor] Missing DoorController component.", this);
                return;
            }
            door.TryOpen();
        }
    }
}
