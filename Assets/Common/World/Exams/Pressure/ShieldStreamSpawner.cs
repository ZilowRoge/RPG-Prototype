using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Responsible for spawning and pooling exam dummies.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Shield Stream Spawner")]
    public class ShieldStreamSpawner : MonoBehaviour
    {
        [SerializeField] private ExamDummy dummyPrefab;
        [SerializeField] private List<Transform> spawnPoints = new();
        [SerializeField, Min(0)] private int prewarmCount = 6;
        [SerializeField] private bool expandPool = true;
        [SerializeField, Min(0.1f)] private float travelDistance = 10f;

        private readonly Queue<ExamDummy> pool = new();
        private readonly HashSet<ExamDummy> active = new();

        private void Awake()
        {
            if (dummyPrefab == null)
            {
                Debug.LogError($"{nameof(ShieldStreamSpawner)} on {name} has no dummy prefab assigned.", this);
                enabled = false;
                return;
            }

            Prewarm(prewarmCount);
        }

        /// <summary>
        /// Spawns a stationary dummy that exists for the specified lifetime.
        /// </summary>
        public ExamDummy SpawnStationary(float lifetime,
                                         Action<ExamDummy> onHit,
                                         Action<ExamDummy> onMiss,
                                         Action<ExamDummy> onReleased = null)
        {
            var origin = ResolveSpawnPosition();
            return SpawnInternal(origin, origin, ExamDummy.MotionMode.StationaryTimed, 0f, lifetime, onHit, onMiss, onReleased);
        }

        /// <summary>
        /// Spawns a dummy that advances towards a provided world-space target.
        /// </summary>
        public ExamDummy SpawnAdvancing(float speed,
                                        Vector3 targetPosition,
                                        Action<ExamDummy> onHit,
                                        Action<ExamDummy> onMiss,
                                        Action<ExamDummy> onReleased = null)
        {
            var origin = ResolveSpawnPosition();
            if ((targetPosition - origin).sqrMagnitude <= 0.0001f)
                targetPosition = ResolveTargetPosition(origin);

            return SpawnInternal(origin, targetPosition, ExamDummy.MotionMode.Advancing, speed, 0f, onHit, onMiss, onReleased);
        }

        /// <summary>
        /// Aborts all active dummies immediately without registering misses.
        /// </summary>
        public void AbortAll()
        {
            if (active.Count == 0)
                return;

            var snapshot = new List<ExamDummy>(active);
            foreach (var dummy in snapshot)
                dummy?.Abort();
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var dummy = CreateInstance();
                pool.Enqueue(dummy);
            }
        }

        private ExamDummy SpawnInternal(Vector3 origin,
                                        Vector3 target,
                                        ExamDummy.MotionMode mode,
                                        float speed,
                                        float lifetime,
                                        Action<ExamDummy> onHit,
                                        Action<ExamDummy> onMiss,
                                        Action<ExamDummy> onReleased)
        {
            var dummy = Acquire();
            if (dummy == null)
                return null;

            active.Add(dummy);
            dummy.Launch(origin, target, mode, speed, lifetime, onHit, onMiss, releasedDummy =>
            {
                onReleased?.Invoke(releasedDummy);
                Release(releasedDummy);
            });

            return dummy;
        }

        private ExamDummy Acquire()
        {
            if (pool.Count > 0)
                return pool.Dequeue();

            if (!expandPool)
                return null;

            return CreateInstance();
        }

        private ExamDummy CreateInstance()
        {
            var dummy = Instantiate(dummyPrefab, transform);
            dummy.gameObject.name = "Exam Target";
            dummy.gameObject.SetActive(false);
            return dummy;
        }

        private void Release(ExamDummy dummy)
        {
            if (dummy == null)
                return;

            if (active.Remove(dummy))
            {
                dummy.ResetForPool();
                dummy.gameObject.SetActive(false);
                pool.Enqueue(dummy);
            }
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (spawnPoints != null)
            {
                // Remove null references lazily.
                for (int i = spawnPoints.Count - 1; i >= 0; i--)
                {
                    if (spawnPoints[i] != null)
                        continue;
                    spawnPoints.RemoveAt(i);
                }

                if (spawnPoints.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, spawnPoints.Count);
                    var chosen = spawnPoints[index];
                    if (chosen != null)
                        return chosen.position;
                }
            }

            return transform.position;
        }

        private Vector3 ResolveTargetPosition(Vector3 origin)
        {
            float distance = Mathf.Max(0.1f, travelDistance);
            return origin + Vector3.right * distance;
        }
    }
}
