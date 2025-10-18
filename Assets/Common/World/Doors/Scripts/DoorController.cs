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

        public bool AlreadyOpened => unlocked && oneTimeUnlock;

        public bool TryOpen()
        {
            if (AlreadyOpened)
                return false;

            unlocked = true;
            if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
            {
                doorAnimator.SetTrigger(openTriggerName);
            }

            return true;
        }

        public bool TryClose()
        {
            if (doorAnimator == null || string.IsNullOrEmpty(closeTriggerName))
                return false;

            doorAnimator.SetTrigger(closeTriggerName);
            return true;
        }
    }
}
