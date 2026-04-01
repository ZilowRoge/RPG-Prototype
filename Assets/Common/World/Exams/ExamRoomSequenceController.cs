using Common.World.Exams.Pressure;
using Player.Save;
using UnityEngine;

namespace Common.World.Exams
{
    /// <summary>
    /// Coordinates the entrance exam room.
    /// Keeps the pressure station available.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Exam Room Sequence Controller")]
    public class ExamRoomSequenceController : MonoBehaviour
    {
        [Header("Exam Controllers")]
        [SerializeField] private PressureExamController pressureExam;

        [Header("Station Roots")]
        [SerializeField] private GameObject pressureExamStationRoot;

        private void Awake()
        {
            if (pressureExam == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no pressure exam reference assigned.", this);

            if (pressureExamStationRoot == null)
                Debug.LogWarning($"{nameof(ExamRoomSequenceController)} on {name} has no pressure station root assigned.", this);
        }

        private void OnEnable()
        {
            if (pressureExam != null)
                pressureExam.StateChanged += OnPressureExamStateChanged;

            SaveState.PlayerLoadedFromSave += OnSaveLoaded;
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

            SaveState.PlayerLoadedFromSave -= OnSaveLoaded;
        }

        private void OnPressureExamStateChanged(PressureExamController.ExamState state)
        {
            switch (state)
            {
                case PressureExamController.ExamState.Idle:
                case PressureExamController.ExamState.Failed:
                    if (pressureExam != null && !pressureExam.HasCompleted)
                        ShowPressureStation();
                    break;
            }
        }

        private void UpdateActiveStations()
        {
            ShowPressureStation();
        }

        private void ShowPressureStation()
        {
            SetActive(pressureExamStationRoot, true);
        }

        private void OnSaveLoaded()
        {
            ResetExam(pressureExam);
            UpdateActiveStations();
        }

        private static void ResetExam(IExamResettable exam)
        {
            exam?.ResetExamToIdle();
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
