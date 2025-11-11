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
                return false;

            unlocked = true;
            if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
                doorAnimator.SetTrigger(openTriggerName);
            else
            {
                return false;
            }

            return true;
        }

        public void Open() => TryOpen();

        public bool TryClose()
        {
            if (doorAnimator == null || string.IsNullOrEmpty(closeTriggerName))
                return false;
            unlocked = false;
            doorAnimator.SetTrigger(closeTriggerName);
            return true;
        }

        public void Close() => TryClose();

        public void RestoreState(bool open)
        {
            if (unlocked == open)
                return;

            unlocked = open;

            if (doorAnimator == null)
                return;

            var trigger = open ? openTriggerName : closeTriggerName;
            if (string.IsNullOrEmpty(trigger))
                return;

            doorAnimator.SetTrigger(trigger);
        }
    }
}
