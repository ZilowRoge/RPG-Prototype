using System;
using System.Collections;
using System.Collections.Generic;
using Player.Statistics;
using UI.Player.Exams;
using UnityEngine;
using UnityEngine.Events;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Coordinates the pressure exam: two shield stages, scoring and signalling results.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Pressure Exam Controller")]
    public class PressureExamController : MonoBehaviour, IExamResettable
    {
        public enum ExamState
        {
            Idle,
            Preparing,
            Running,
            Failed,
            Completed
        }

        [Header("References")]
        [SerializeField] private PressureExamConfig config;
        [SerializeField] private ShieldStreamSpawner shieldSpawner;
        [SerializeField] private PressureExamUI examUI;

        [Header("Events")]
        [SerializeField] private UnityEvent onExamPassed;

        [Header("Behaviour")]
        [SerializeField] private bool allowRepeatAfterSuccess = true;

        public event Action<ExamState> StateChanged;

        public ExamState State { get; private set; } = ExamState.Idle;
        public bool IsRunning => State == ExamState.Preparing || State == ExamState.Running;
        public bool HasCompleted { get; private set; }
        public GameObject CurrentParticipant { get; private set; }

        private readonly HashSet<ExamDummy> liveDummies = new();
        private Coroutine examRoutine;
        private Coroutine postRoutine;

        private int currentHits;
        private int currentMisses;
        private int currentStageHits;
        private int currentStageMisses;
        private int currentStageShieldCount;
        private int currentStageRequiredHits;

        /// <summary>
        /// Attempts to start the pressure exam for the provided player object.
        /// </summary>
        public bool TryBeginExam(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            if (shieldSpawner == null || config == null)
            {
                Debug.LogWarning($"{nameof(PressureExamController)} lacks required references.", this);
                return false;
            }

            if (IsRunning)
                return false;

            if (HasCompleted && !allowRepeatAfterSuccess)
                return false;

            StopExamRoutine();
            StopPostRoutine();
            shieldSpawner.AbortAll();
            liveDummies.Clear();
            ResetCounters(notifyUi: false);

            CurrentParticipant = playerObject;
            RefillParticipantMana();

            SetState(ExamState.Preparing);
            examUI?.HandleExamPreparing(this);

            examRoutine = StartCoroutine(RunExamRoutine());
            return true;
        }

        /// <summary>
        /// Immediately aborts the active exam, if any.
        /// </summary>
        public void AbortExam()
        {
            if (!IsRunning)
                return;

            StopExamRoutine();
            StopPostRoutine();
            shieldSpawner?.AbortAll();
            liveDummies.Clear();

            SetState(ExamState.Idle);
            CurrentParticipant = null;
            ResetCounters();
            examUI?.HandleExamAborted(this);
        }

        private IEnumerator RunExamRoutine()
        {
            float introDelay = config.IntroDelay;
            if (introDelay > 0f)
                yield return new WaitForSeconds(introDelay);

            if (State != ExamState.Preparing)
                yield break;

            SetState(ExamState.Running);
            examUI?.HandleExamStarted(this);

            var stages = config.Stages;
            if (stages == null || stages.Count == 0)
            {
                HandleSuccess();
                yield break;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                if (State != ExamState.Running)
                    yield break;

                BeginStage(i, stages[i], stages.Count);

                yield return RunStage(stages[i]);

                if (State != ExamState.Running)
                    yield break;

                float delay = stages[i].DelayAfterStage;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            if (State == ExamState.Running)
                HandleSuccess();
        }

        private void BeginStage(int stageIndex, PressureExamConfig.StageDefinition stage, int totalStages)
        {
            currentStageHits = 0;
            currentStageMisses = 0;
            currentStageShieldCount = stage != null ? stage.ShieldCount : 0;
            currentStageRequiredHits = stage != null ? stage.RequiredHits : 0;

            examUI?.HandleWaveAdvanced(stageIndex, totalStages);
            examUI?.HandleMissCountChanged(0, GetCurrentStageMaxMisses());
        }

        private IEnumerator RunStage(PressureExamConfig.StageDefinition stage)
        {
            if (stage == null)
                yield break;

            int spawnCount = stage.ShieldCount;
            float interval = stage.SpawnInterval;

            for (int i = 0; i < spawnCount; i++)
            {
                if (State != ExamState.Running)
                    yield break;

                var spawnedDummy = SpawnDummy(stage);
                bool isLastSpawn = i == spawnCount - 1;

                if (stage.Mode == PressureExamConfig.StageMode.StationaryTimed && spawnedDummy != null)
                {
                    while (State == ExamState.Running && liveDummies.Contains(spawnedDummy))
                        yield return null;

                    if (!isLastSpawn && interval > 0f)
                    {
                        float timer = 0f;
                        while (timer < interval && State == ExamState.Running)
                        {
                            timer += Time.deltaTime;
                            yield return null;
                        }
                    }
                }

                if (stage.Mode == PressureExamConfig.StageMode.StationaryTimed)
                {
                    yield return null;
                }
                else if (interval <= 0f || isLastSpawn)
                {
                    yield return null;
                }
                else
                {
                    float timer = 0f;
                    while (timer < interval && State == ExamState.Running)
                    {
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }
            }

            while (liveDummies.Count > 0 && State == ExamState.Running)
                yield return null;

            if (State != ExamState.Running)
                yield break;

            if (currentStageHits < currentStageRequiredHits)
                HandleFailure();
        }

        private ExamDummy SpawnDummy(PressureExamConfig.StageDefinition stage)
        {
            ExamDummy dummy;
            if (stage.Mode == PressureExamConfig.StageMode.StationaryTimed)
            {
                dummy = shieldSpawner.SpawnStationary(stage.StationaryLifetime, OnDummyHit, OnDummyMiss, OnDummyReleased);
            }
            else
            {
                dummy = shieldSpawner.SpawnAdvancing(stage.ShieldSpeed, ResolveAdvancingTarget(), OnDummyHit, OnDummyMiss, OnDummyReleased);
            }

            if (dummy != null)
            {
                liveDummies.Add(dummy);
                return dummy;
            }

            // Treat a failed spawn as a miss to prevent stalled stage progression.
            OnDummyMiss(null);
            return null;
        }

        private void OnDummyHit(ExamDummy dummy)
        {
            currentHits++;
            currentStageHits++;
            examUI?.HandleHitCountChanged(currentHits);
        }

        private void OnDummyMiss(ExamDummy dummy)
        {
            currentMisses++;
            currentStageMisses++;
            examUI?.HandleMissCountChanged(currentStageMisses, GetCurrentStageMaxMisses());

            if (!CanStillPassCurrentStage())
                HandleFailure();
        }

        private void OnDummyReleased(ExamDummy dummy)
        {
            liveDummies.Remove(dummy);
        }

        private bool CanStillPassCurrentStage()
        {
            if (currentStageShieldCount <= 0)
                return false;

            int remainingPotentialHits = currentStageShieldCount - (currentStageHits + currentStageMisses);
            return currentStageHits + Mathf.Max(0, remainingPotentialHits) >= currentStageRequiredHits;
        }

        private int GetCurrentStageMaxMisses()
        {
            return Mathf.Max(0, currentStageShieldCount - currentStageRequiredHits);
        }

        private Vector3 ResolveAdvancingTarget()
        {
            if (CurrentParticipant != null)
            {
                var participantTransform = CurrentParticipant.transform;
                if (participantTransform != null)
                    return participantTransform.position;
            }

            return shieldSpawner != null
                ? shieldSpawner.transform.position + Vector3.right * 3f
                : transform.position + Vector3.right * 3f;
        }

        private void HandleFailure()
        {
            if (State != ExamState.Running)
                return;

            StopExamRoutine();
            StopPostRoutine();
            shieldSpawner?.AbortAll();
            liveDummies.Clear();

            SetState(ExamState.Failed);
            CurrentParticipant = null;
            examUI?.HandleExamFailed(this, currentStageMisses, GetCurrentStageMaxMisses());

            float restartDelay = config != null ? config.RestartDelay : 0f;
            postRoutine = StartCoroutine(TransitionToIdle(restartDelay));
        }

        private void HandleSuccess()
        {
            if (State != ExamState.Running)
                return;

            StopExamRoutine();
            StopPostRoutine();
            shieldSpawner?.AbortAll();
            liveDummies.Clear();

            HasCompleted = true;
            SetState(ExamState.Completed);
            CurrentParticipant = null;
            int maxMisses = CalculateTotalAllowedMisses();
            examUI?.HandleExamCompleted(this, currentHits, currentMisses, maxMisses);
            onExamPassed?.Invoke();

            if (allowRepeatAfterSuccess)
            {
                float hold = config != null ? config.CompletionHoldDuration : 0f;
                postRoutine = StartCoroutine(TransitionToIdle(hold));
            }
        }

        private IEnumerator TransitionToIdle(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SetState(ExamState.Idle);
            CurrentParticipant = null;
            ResetCounters();
            postRoutine = null;
        }

        private void ResetCounters(bool notifyUi = true)
        {
            currentHits = 0;
            currentMisses = 0;
            currentStageHits = 0;
            currentStageMisses = 0;
            currentStageShieldCount = 0;
            currentStageRequiredHits = 0;

            if (!notifyUi)
                return;

            int maxMisses = CalculateTotalAllowedMisses();
            int totalWaves = config != null && config.Stages != null ? config.Stages.Count : 0;

            examUI?.HandleHitCountChanged(currentHits);
            examUI?.HandleMissCountChanged(currentMisses, maxMisses);
            examUI?.HandleWaveAdvanced(0, totalWaves);
        }

        private int CalculateTotalAllowedMisses()
        {
            if (config == null || config.Stages == null)
                return 0;

            int totalAllowed = 0;
            for (int i = 0; i < config.Stages.Count; i++)
            {
                var stage = config.Stages[i];
                if (stage == null)
                    continue;

                totalAllowed += Mathf.Max(0, stage.ShieldCount - stage.RequiredHits);
            }

            return totalAllowed;
        }

        private void RefillParticipantMana()
        {
            var stats = FindStatsController(CurrentParticipant);
            stats?.RefillMana();
        }

        private static StatsController FindStatsController(GameObject participant)
        {
            if (participant == null)
                return null;

            return participant.GetComponent<StatsController>() ??
                   participant.GetComponentInChildren<StatsController>() ??
                   participant.GetComponentInParent<StatsController>();
        }

        private void SetState(ExamState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(State);
        }

        private void StopExamRoutine()
        {
            if (examRoutine != null)
            {
                StopCoroutine(examRoutine);
                examRoutine = null;
            }
        }

        private void StopPostRoutine()
        {
            if (postRoutine != null)
            {
                StopCoroutine(postRoutine);
                postRoutine = null;
            }
        }

        public void ResetExamToIdle()
        {
            bool wasRunning = State == ExamState.Preparing || State == ExamState.Running;

            StopExamRoutine();
            StopPostRoutine();
            shieldSpawner?.AbortAll();
            liveDummies.Clear();
            CurrentParticipant = null;

            HasCompleted = false;
            SetState(ExamState.Idle);
            ResetCounters();

            if (wasRunning)
            {
                examUI?.HandleExamAborted(this);
            }
        }
    }
}
