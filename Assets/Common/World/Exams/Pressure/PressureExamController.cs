using System;
using System.Collections;
using System.Collections.Generic;
using Common.Progress;
using Player.Statistics;
using UI.Player.Exams;
using UnityEngine;
using UnityEngine.Events;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Coordinates the pressure exam: spawning dummies, tracking misses and signalling results.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Pressure Exam Controller")]
    public class PressureExamController : MonoBehaviour
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

        private int currentWaveIndex = -1;
        private int currentHits;
        private int currentMisses;

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

            var waves = config.Waves;
            if (waves == null || waves.Count == 0)
            {
                HandleSuccess();
                yield break;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                if (State != ExamState.Running)
                    yield break;

                currentWaveIndex = i;
                examUI?.HandleWaveAdvanced(i, waves.Count);

                yield return RunWave(waves[i]);

                if (State != ExamState.Running)
                    yield break;

                float delay = waves[i].DelayAfterWave;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            if (State == ExamState.Running)
                HandleSuccess();
        }

        private IEnumerator RunWave(PressureExamConfig.WaveDefinition wave)
        {
            if (wave == null)
                yield break;

            int spawnCount = wave.DummyCount;
            float interval = wave.SpawnInterval;

            for (int i = 0; i < spawnCount; i++)
            {
                if (State != ExamState.Running)
                    yield break;

                SpawnDummy(wave.DummySpeed);

                if (interval <= 0f || i == spawnCount - 1)
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
        }

        private void SpawnDummy(float speed)
        {
            var dummy = shieldSpawner.Spawn(speed, OnDummyHit, OnDummyMiss, OnDummyReleased);
            if (dummy != null)
                liveDummies.Add(dummy);
        }

        private void OnDummyHit(ExamDummy dummy)
        {
            currentHits++;
            examUI?.HandleHitCountChanged(currentHits);
        }

        private void OnDummyMiss(ExamDummy dummy)
        {
            currentMisses++;
            examUI?.HandleMissCountChanged(currentMisses, config.MaxMisses);

            if (config.MaxMisses > 0 && currentMisses >= config.MaxMisses)
                HandleFailure();
        }

        private void OnDummyReleased(ExamDummy dummy)
        {
            liveDummies.Remove(dummy);
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
            examUI?.HandleExamFailed(this, currentMisses, config.MaxMisses);

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
            int maxMisses = config != null ? config.MaxMisses : 0;
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
            currentWaveIndex = -1;
            currentHits = 0;
            currentMisses = 0;

            if (!notifyUi)
                return;

            int maxMisses = config != null ? config.MaxMisses : 0;
            int totalWaves = config != null && config.Waves != null ? config.Waves.Count : 0;

            examUI?.HandleHitCountChanged(currentHits);
            examUI?.HandleMissCountChanged(currentMisses, maxMisses);
            examUI?.HandleWaveAdvanced(0, totalWaves);
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
    }
}
