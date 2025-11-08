using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies.Controllers;
using UnityEngine;
using UnityEngine.Events;
using PlayerStatsController = Player.Statistics.StatsController;
namespace Common.World.Exams.Combat
{
    /// <summary>
    /// Coordinates combat exams that spawn enemy waves after the player activates a switch.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Combat/Combat Exam Controller")]
    public class CombatExamController : MonoBehaviour
    {
        public enum ExamState
        {
            Idle,
            Countdown,
            Running,
            Failed,
            Completed
        }

        [Header("References")]
        [SerializeField] private CombatExamConfig config;

        [Header("Events")]
        [SerializeField] private UnityEvent onExamPassed;

        [Header("Behaviour")]
        [SerializeField] private bool allowRepeatAfterSuccess = true;

        public event Action<ExamState> StateChanged;
        public event Action<int, int> EnemyProgressChanged;

        public ExamState State { get; private set; } = ExamState.Idle;
        public bool IsRunning =>
            State == ExamState.Countdown ||
            State == ExamState.Running;

        public bool HasCompleted { get; private set; }
        public GameObject CurrentParticipant { get; private set; }

        private readonly List<StatsController> trackedEnemies = new();
        private readonly List<GameObject> spawnedEnemies = new();
        [Header("Spawn Points")]
        [SerializeField] private List<Transform> spawnPoints = new();

        private Coroutine countdownRoutine;
        private Coroutine idleTransitionRoutine;
        private int totalEnemiesToDefeat;

        public bool TryBeginExam(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            if (config == null)
            {
                Debug.LogWarning($"{nameof(CombatExamController)} on {name} lacks a config reference.", this);
                return false;
            }

            if (IsRunning)
                return false;

            if (HasCompleted && !allowRepeatAfterSuccess)
                return false;

            StopCountdownRoutine();
            StopIdleTransitionRoutine();
            ResetExamState(destroyEnemies: true);

            CurrentParticipant = playerObject;
            HasCompleted = false;
            RefillParticipantMana();

            float countdown = config.ActivationCountdown;
            if (countdown > 0f)
            {
                SetState(ExamState.Countdown);

                countdownRoutine = StartCoroutine(ActivationCountdownRoutine(countdown));
            }
            else
            {
                BeginCombat();
            }

            return true;
        }

        public void AbortExam()
        {
            if (!IsRunning)
                return;

            StopCountdownRoutine();
            StopIdleTransitionRoutine();
            ResetExamState(destroyEnemies: true);

            SetState(ExamState.Idle);
            CurrentParticipant = null;
        }

        private IEnumerator ActivationCountdownRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            countdownRoutine = null;

            if (State != ExamState.Countdown)
                yield break;

            BeginCombat();
        }

        private void BeginCombat()
        {
            SetState(ExamState.Running);

            SpawnEnemies();
            if (trackedEnemies.Count == 0)
            {
                HandleSuccess();
            }
            else
            {
                NotifyEnemyProgress();
            }
        }

        private void SpawnEnemies()
        {
            if (config == null)
                return;

            var spawns = config.EnemySpawns;
            if (spawns == null || spawns.Count == 0)
            {
                Debug.LogWarning($"{nameof(CombatExamController)} on {name} has no enemy spawns configured.", this);
                return;
            }

            Transform[] availableSpawnPoints = ResolveSpawnPoints();
            if (availableSpawnPoints.Length == 0)
            {
                Debug.LogWarning($"{nameof(CombatExamController)} on {name} has no spawn points assigned.", this);
                return;
            }

            var usedPoints = new HashSet<Transform>();

            foreach (var spawn in spawns)
            {
                if (spawn == null)
                    continue;

                var prefab = spawn.EnemyPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"{nameof(CombatExamController)} spawn entry missing prefab on {name}.", this);
                    continue;
                }

                Transform anchor = SelectSpawnPoint(availableSpawnPoints, usedPoints);
                Vector3 position = ComputeSpawnPosition(anchor, spawn.PositionOffset);
                Quaternion rotation = ComputeSpawnRotation(anchor, spawn.Rotation);

                var instance = Instantiate(prefab, position, rotation);
                if (instance == null)
                    continue;

                instance.name = $"{prefab.name} (CombatExam)";
                spawnedEnemies.Add(instance);

                var stats = instance.GetComponent<StatsController>();
                if (stats != null)
                {
                    stats.Died += OnTrackedEnemyDied;
                    if (!stats.IsDead)
                        trackedEnemies.Add(stats);
                }
                else
                {
                    Debug.LogWarning($"{nameof(CombatExamController)} spawned '{instance.name}' without a {nameof(StatsController)} component.", instance);
                }
            }

            totalEnemiesToDefeat = trackedEnemies.Count;
        }

        private Transform[] ResolveSpawnPoints()
        {
            if (spawnPoints != null)
            {
                spawnPoints.RemoveAll(point => point == null);
                if (spawnPoints.Count > 0)
                    return spawnPoints.ToArray();
            }

            return new[] { transform };
        }

        private Transform SelectSpawnPoint(Transform[] allPoints, HashSet<Transform> usedPoints)
        {
            if (allPoints == null || allPoints.Length == 0)
                return transform;

            // Try to use each spawn point once before reusing any.
            var candidates = new List<Transform>();
            for (int i = 0; i < allPoints.Length; i++)
            {
                var point = allPoints[i];
                if (point == null)
                    continue;

                if (!usedPoints.Contains(point))
                    candidates.Add(point);
            }

            Transform chosen;
            if (candidates.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                chosen = candidates[index];
            }
            else
            {
                int index = UnityEngine.Random.Range(0, allPoints.Length);
                chosen = allPoints[index] != null ? allPoints[index] : transform;
            }

            usedPoints.Add(chosen);
            return chosen;
        }

        private Vector3 ComputeSpawnPosition(Transform anchor, Vector3 localOffset)
        {
            if (anchor == null)
                anchor = transform;

            return anchor.TransformPoint(localOffset);
        }

        private Quaternion ComputeSpawnRotation(Transform anchor, Quaternion localRotation)
        {
            if (anchor == null)
                anchor = transform;

            return anchor.rotation * localRotation;
        }

        private void OnTrackedEnemyDied(StatsController stats, Transform killer)
        {
            if (stats != null)
            {
                stats.Died -= OnTrackedEnemyDied;
                trackedEnemies.Remove(stats);
            }

            if (stats != null)
            {
                var enemyObject = stats.gameObject;
                for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
                {
                    if (spawnedEnemies[i] == null || spawnedEnemies[i] == enemyObject)
                        spawnedEnemies.RemoveAt(i);
                }
            }
            else
            {
                spawnedEnemies.RemoveAll(instance => instance == null);
            }

            if (State == ExamState.Running)
            {
                NotifyEnemyProgress();

                if (trackedEnemies.Count == 0)
                    HandleSuccess();
            }
        }

        private void HandleSuccess()
        {
            if (State != ExamState.Running && State != ExamState.Countdown)
                return;

            StopCountdownRoutine();
            StopIdleTransitionRoutine();

            HasCompleted = true;
            SetState(ExamState.Completed);

            CurrentParticipant = null;

            ResetExamState(destroyEnemies: false);

            onExamPassed?.Invoke();

            if (allowRepeatAfterSuccess)
            {
                float restartDelay = config != null ? config.RestartDelay : 0f;
                idleTransitionRoutine = StartCoroutine(TransitionToIdle(restartDelay));
            }
        }

        public void RegisterFailure()
        {
            HandleFailure();
        }

        private void HandleFailure()
        {
            if (State != ExamState.Countdown &&
                State != ExamState.Running)
            {
                return;
            }

            StopCountdownRoutine();
            StopIdleTransitionRoutine();

            SetState(ExamState.Failed);
            CurrentParticipant = null;
            HasCompleted = false;

            ResetExamState(destroyEnemies: true);
            idleTransitionRoutine = StartCoroutine(TransitionToIdle(config != null ? config.RestartDelay : 0f));
        }

        private IEnumerator TransitionToIdle(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            ResetExamState(destroyEnemies: true);
            SetState(ExamState.Idle);
            idleTransitionRoutine = null;
        }

        private void ResetExamState(bool destroyEnemies)
        {
            for (int i = 0; i < trackedEnemies.Count; i++)
            {
                var stats = trackedEnemies[i];
                if (stats != null)
                    stats.Died -= OnTrackedEnemyDied;
            }

            trackedEnemies.Clear();
            totalEnemiesToDefeat = 0;

            if (destroyEnemies)
            {
                foreach (var instance in spawnedEnemies)
                {
                    if (instance != null)
                        Destroy(instance);
                }
            }

            spawnedEnemies.Clear();
        }

        private void RefillParticipantMana()
        {
            var stats = FindStatsController(CurrentParticipant);
            stats?.RefillMana();
        }

        private static PlayerStatsController FindStatsController(GameObject participant)
        {
            if (participant == null)
                return null;

            return participant.GetComponent<PlayerStatsController>() ??
                   participant.GetComponentInChildren<PlayerStatsController>() ??
                   participant.GetComponentInParent<PlayerStatsController>();
        }

        private void StopCountdownRoutine()
        {
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }
        }

        private void StopIdleTransitionRoutine()
        {
            if (idleTransitionRoutine != null)
            {
                StopCoroutine(idleTransitionRoutine);
                idleTransitionRoutine = null;
            }
        }

        private void NotifyEnemyProgress()
        {
            int defeated = totalEnemiesToDefeat - trackedEnemies.Count;
            EnemyProgressChanged?.Invoke(
                Mathf.Clamp(defeated, 0, totalEnemiesToDefeat),
                totalEnemiesToDefeat);
        }

        private void SetState(ExamState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(State);
        }
    }
}
