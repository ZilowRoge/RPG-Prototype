using Common.World.Interaction;
using UnityEngine;

namespace Common.World.Exams.Combat
{
    /// <summary>
    /// Interaction entry point for starting the combat exam.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Combat/Combat Exam Station")]
    public class CombatExamStation : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private CombatExamController controller;
        [SerializeField] private Collider stationTrigger;

        [Header("Behaviour")]
        [SerializeField] private bool allowRepeatAfterCompletion = true;

        private GameObject currentParticipant;

        private void Awake()
        {
            if (stationTrigger == null)
                stationTrigger = GetComponent<Collider>();

            if (stationTrigger != null && !stationTrigger.isTrigger)
            {
                Debug.LogWarning($"{nameof(CombatExamStation)} on {name} expects a trigger collider.", this);
                stationTrigger.isTrigger = true;
            }
        }

        public void Interact(GameObject player)
        {
            if (controller == null || player == null)
                return;

            if (controller.IsRunning)
                return;

            if (controller.HasCompleted && !allowRepeatAfterCompletion)
                return;

            if (controller.TryBeginExam(player))
                currentParticipant = player;
        }

        private void OnTriggerExit(Collider other)
        {
            if (controller == null || other == null || currentParticipant == null)
                return;

            if (ReferenceEquals(other.gameObject, currentParticipant) ||
                other.transform.IsChildOf(currentParticipant.transform))
            {
                currentParticipant = null;
            }
        }

        private void LateUpdate()
        {
            if (controller == null)
                return;

            if (!controller.IsRunning)
                currentParticipant = null;
        }
    }
}
