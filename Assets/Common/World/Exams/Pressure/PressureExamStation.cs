using System;
using Common.World.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Interaction entry point that anchors the player and starts the pressure exam.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Pressure Exam Station")]
    public class PressureExamStation : MonoBehaviour, IInteractable
    {
        [Serializable] private class PlayerEvent : UnityEvent<GameObject> { }

        [Header("References")]
        [SerializeField] private PressureExamController controller;
        [SerializeField] private Transform playerStandPoint;

        [Header("Behaviour")]
        [SerializeField] private bool snapPlayerToStandPoint = true;
        [SerializeField] private bool alignRotation = true;
        [SerializeField] private bool zeroPlayerVelocityOnStart = true;
        [SerializeField] private bool allowRepeatAfterCompletion = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteractionRejected;
        [SerializeField] private UnityEvent onExamStarted;
        [SerializeField] private PlayerEvent onExamStartedWithPlayer;
        [SerializeField] private UnityEvent onExamEnded;
        [SerializeField] private PlayerEvent onExamEndedWithPlayer;

        private GameObject activePlayer;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Interact(GameObject player)
        {
            if (controller == null || player == null)
                return;

            if (controller.IsRunning)
            {
                onInteractionRejected?.Invoke();
                return;
            }

            if (controller.HasCompleted && !allowRepeatAfterCompletion)
            {
                onInteractionRejected?.Invoke();
                return;
            }

            bool began = controller.TryBeginExam(player);
            if (!began)
            {
                onInteractionRejected?.Invoke();
                return;
            }

            activePlayer = player;

            if (snapPlayerToStandPoint && playerStandPoint != null)
            {
                player.transform.SetPositionAndRotation(playerStandPoint.position,
                    alignRotation ? playerStandPoint.rotation : player.transform.rotation);
            }

            if (zeroPlayerVelocityOnStart)
                ZeroPlayerVelocity(player);

            onExamStarted?.Invoke();
            onExamStartedWithPlayer?.Invoke(player);
        }

        private void HandleExamEnded()
        {
            if (activePlayer == null)
                return;

            var player = activePlayer;
            activePlayer = null;

            onExamEnded?.Invoke();
            onExamEndedWithPlayer?.Invoke(player);
        }

        private void HandleStateChanged(PressureExamController.ExamState state)
        {
            if (state == PressureExamController.ExamState.Idle)
                HandleExamEnded();
        }

        private void ZeroPlayerVelocity(GameObject player)
        {
            if (player.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (player.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                cc.enabled = true;
            }
        }

        private void Subscribe()
        {
            if (controller == null)
                return;

            controller.ExamFailed += HandleExamEnded;
            controller.ExamCompleted += HandleExamEnded;
            controller.StateChanged += HandleStateChanged;
        }

        private void Unsubscribe()
        {
            if (controller == null)
                return;

            controller.ExamFailed -= HandleExamEnded;
            controller.ExamCompleted -= HandleExamEnded;
            controller.StateChanged -= HandleStateChanged;
        }
    }
}
