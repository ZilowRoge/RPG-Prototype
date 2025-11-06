using Common.World.Exams.Combat;
using Common.World.Exams.Pressure;
using UnityEngine;

namespace Common.World.Exams
{
    /// <summary>
    /// Coordinates a room that hosts both the pressure and combat exams.
    /// Ensures the pressure station is available first and swaps to the combat station after completion.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Exam Room Sequence Controller")]
    public class ExamRoomSequenceController : MonoBehaviour
    {
        [Header("Exam Controllers")]
        [SerializeField] private PressureExamController pressureExam;
        [SerializeField] private CombatExamController combatExam;

        [Header("Station Roots")]
        [SerializeField] private GameObject pressureExamStationRoot;
        [SerializeField] private GameObject combatExamStationRoot;

        private void Awake()
        {
            if (pressureExam == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no pressure exam reference assigned.", this);

            if (combatExam == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no combat exam reference assigned.", this);

            if (pressureExamStationRoot == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no pressure station root assigned.", this);

            if (combatExamStationRoot == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no combat station root assigned.", this);
        }

        private void OnEnable()
        {
            if (pressureExam != null)
                pressureExam.StateChanged += OnPressureExamStateChanged;

            UpdateActiveStations();
        }

        private void Start()
        {
            UpdateActiveStations();
        }

        private void OnDisable()
        {
            if (pressureExam != null)
                pressureExam.StateChanged -= OnPressureExamStateChanged;
        }

        private void OnPressureExamStateChanged(PressureExamController.ExamState state)
        {
            switch (state)
            {
                case PressureExamController.ExamState.Completed:
                    ShowCombatStation();
                    break;

                case PressureExamController.ExamState.Idle:
                case PressureExamController.ExamState.Failed:
                    if (pressureExam != null && !pressureExam.HasCompleted)
                        ShowPressureStation();
                    break;
            }
        }

        private void UpdateActiveStations()
        {
            if (pressureExam != null && pressureExam.HasCompleted)
                ShowCombatStation();
            else
                ShowPressureStation();
        }

        private void ShowPressureStation()
        {
            SetActive(pressureExamStationRoot, true);
            SetActive(combatExamStationRoot, false);
        }

        private void ShowCombatStation()
        {
            SetActive(pressureExamStationRoot, false);
            SetActive(combatExamStationRoot, true);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
