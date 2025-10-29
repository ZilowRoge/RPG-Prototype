using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Responsible for spawning and pooling exam dummies that fly towards the player.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Shield Stream Spawner")]
    public class ShieldStreamSpawner : MonoBehaviour
    {
        [SerializeField] private ExamDummy dummyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform targetPoint;
        [SerializeField, Min(0)] private int prewarmCount = 6;
        [SerializeField] private bool expandPool = true;

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
        /// Spawns and launches a dummy towards the configured target.
        /// </summary>
        public ExamDummy Spawn(float speed,
                               Action<ExamDummy> onHit,
                               Action<ExamDummy> onMiss,
                               Action<ExamDummy> onReleased = null)
        {
            var dummy = Acquire();
            if (dummy == null)
                return null;

            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            var target = targetPoint != null ? targetPoint.position : origin + transform.forward * 5f;

            active.Add(dummy);
            dummy.Launch(origin, target, speed, onHit, onMiss, releasedDummy =>
            {
                onReleased?.Invoke(releasedDummy);
                Release(releasedDummy);
            });

            return dummy;
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
    }
}
