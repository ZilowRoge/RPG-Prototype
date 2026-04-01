using System;
using UnityEngine;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// A training dummy that travels towards the player and reports hits or misses.
    /// </summary>
    [AddComponentMenu("Game/World/Exams/Pressure/Exam Dummy")]
    public class ExamDummy : MonoBehaviour
    {
        public enum MotionMode
        {
            StationaryTimed = 0,
            Advancing = 1
        }

        private enum Resolution
        {
            Hit,
            Miss,
            Aborted
        }

        [SerializeField] private LayerMask spellLayers = ~0;
        [SerializeField, Min(0f)] private float defaultSpeed = 4f;
        private Vector3 targetPosition;
        private Vector3 direction;
        private float speed;
        private MotionMode motionMode;
        private float lifetimeRemaining;

        private Action<ExamDummy> onHit;
        private Action<ExamDummy> onMiss;
        private Action<ExamDummy> onReleased;

        private bool active;
        private bool resolved;

        /// <summary>
        /// Launches the dummy and starts listening for collisions.
        /// </summary>
        public void Launch(Vector3 origin,
                           Vector3 target,
                           MotionMode mode,
                           float overrideSpeed,
                           float stationaryLifetime,
                           Action<ExamDummy> hitCallback,
                           Action<ExamDummy> missCallback,
                           Action<ExamDummy> releaseCallback)
        {
            transform.position = origin;
            targetPosition = target;
            direction = (target - origin).normalized;
            if (direction.sqrMagnitude <= float.Epsilon)
                direction = transform.forward == Vector3.zero ? Vector3.forward : transform.forward;

            transform.forward = direction;

            motionMode = mode;
            speed = overrideSpeed > 0f ? overrideSpeed : Mathf.Max(0.1f, defaultSpeed);
            lifetimeRemaining = Mathf.Max(0f, stationaryLifetime);
            onHit = hitCallback;
            onMiss = missCallback;
            onReleased = releaseCallback;
            resolved = false;
            active = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!active)
                return;

            if (motionMode == MotionMode.StationaryTimed)
            {
                lifetimeRemaining -= Time.deltaTime;
                if (lifetimeRemaining <= 0f)
                    Resolve(Resolution.Miss);
                return;
            }

            float step = speed * Time.deltaTime;
            transform.position += direction * step;

            // If we have travelled past the target plane, count it as a miss.
            float remaining = Vector3.Dot(direction, targetPosition - transform.position);
            if (remaining <= 0f)
                Resolve(Resolution.Miss);
        }

        private void OnTriggerEnter(Collider other)
        {
            EvaluateCollision(other.gameObject.layer);
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision.gameObject.layer);
        }

        private void EvaluateCollision(int layer)
        {
            if (!active)
                return;

            if ((spellLayers.value & (1 << layer)) == 0)
                return;

            Resolve(Resolution.Hit);
        }

        /// <summary>
        /// Terminates the dummy without affecting hit/miss counters.
        /// </summary>
        public void Abort()
        {
            Resolve(Resolution.Aborted);
        }

        /// <summary>
        /// Clears callbacks so the dummy can return to the pool.
        /// </summary>
        public void ResetForPool()
        {
            active = false;
            resolved = false;
            onHit = null;
            onMiss = null;
            onReleased = null;
            targetPosition = Vector3.zero;
            direction = Vector3.forward;
            speed = defaultSpeed;
            motionMode = MotionMode.Advancing;
            lifetimeRemaining = 0f;
        }

        private void Resolve(Resolution resolution)
        {
            if (!active || resolved)
                return;

            resolved = true;
            active = false;

            switch (resolution)
            {
                case Resolution.Hit:
                    onHit?.Invoke(this);
                    break;
                case Resolution.Miss:
                    onMiss?.Invoke(this);
                    break;
            }

            onReleased?.Invoke(this);
        }
    }
}
