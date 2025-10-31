using Common.World.Interaction;
using UnityEngine;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Interaction entry point that anchors the player and starts the pressure exam.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Pressure Exam Station")]
    public class PressureExamStation : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private PressureExamController controller;
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
                Debug.LogWarning($"{nameof(PressureExamStation)} on {name} expects a trigger collider.", this);
                stationTrigger.isTrigger = true;
            }
        }

        public void Interact(GameObject player)
        {
            if (controller == null || player == null)
                return;

            if (controller.IsRunning)
            {
                return;
            }

            if (controller.HasCompleted && !allowRepeatAfterCompletion)
            {
                return;
            }

            bool began = controller.TryBeginExam(player);
            if (!began)
            {
                return;
            }

            currentParticipant = player;
        }

        private void OnTriggerExit(Collider other)
        {
            if (controller == null || !controller.IsRunning)
                return;

            if (other == null || currentParticipant == null)
                return;

            if (!ReferenceEquals(other.gameObject, currentParticipant) &&
                !other.transform.IsChildOf(currentParticipant.transform))
            {
                return;
            }

            controller.AbortExam();
            currentParticipant = null;
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
