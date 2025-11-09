using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Door Controller")]
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openTriggerName = "Open";
        [SerializeField] private string closeTriggerName = "Close";
        [SerializeField] private bool oneTimeUnlock = true;

        private bool unlocked;

        public bool IsOpen => unlocked;

        public bool AlreadyOpened => unlocked && oneTimeUnlock;

        public bool TryOpen()
        {
            if (AlreadyOpened)
            {
                Debug.Log($"[DoorController] {name} TryOpen ignored – already opened and oneTimeUnlock=true.", this);
                return false;
            }

            unlocked = true;
            if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
            {
                doorAnimator.SetTrigger(openTriggerName);
                Debug.Log($"[DoorController] {name} trigger '{openTriggerName}' sent (TryOpen).", this);
            }
            else
            {
                Debug.LogWarning($"[DoorController] {name} cannot trigger open (Animator missing or trigger empty).", this);
            }

            Debug.Log($"[DoorController] {name} opened.", this);
            return true;
        }

        public void Open() => TryOpen();

        public bool TryClose()
        {
            if (doorAnimator == null || string.IsNullOrEmpty(closeTriggerName))
            {
                Debug.LogWarning($"[DoorController] {name} cannot close – animator or trigger missing.", this);
                return false;
            }
            unlocked = false;
            doorAnimator.SetTrigger(closeTriggerName);
            Debug.Log($"[DoorController] {name} trigger '{closeTriggerName}' sent (TryClose).", this);
            Debug.Log($"[DoorController] {name} closed.", this);
            return true;
        }

        public void Close() => TryClose();

        public void RestoreState(bool open)
        {
            if (unlocked == open)
            {
                Debug.Log($"[DoorController] {name} already {(open ? "open" : "closed")} – skip trigger.", this);
                return;
            }

            unlocked = open;

            if (doorAnimator == null)
            {
                Debug.LogWarning($"[DoorController] {name} restore failed – animator missing.", this);
                return;
            }

            var trigger = open ? openTriggerName : closeTriggerName;
            if (string.IsNullOrEmpty(trigger))
            {
                Debug.LogWarning($"[DoorController] {name} restore failed – {(open ? "open" : "close")} trigger empty.", this);
                return;
            }

            doorAnimator.SetTrigger(trigger);
            Debug.Log($"[DoorController] {name} trigger '{trigger}' sent (RestoreState target={open}).", this);
            Debug.Log($"[DoorController] {name} restore state -> {(open ? "open" : "closed")}.", this);
        }
    }
}
