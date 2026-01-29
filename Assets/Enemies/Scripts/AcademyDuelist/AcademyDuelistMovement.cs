using Enemies.Abstraction;
using Enemies.Config;
using Enemies.Interfaces;
using UnityEngine;

namespace Enemies.AcademyDuelist
{
    [System.Serializable]
    public class AcademyDuelistMovement : EnemyMovementBase
    {
        [Header("Strafe")]
        [SerializeField] private float strafeAngularSpeed = 60f;
        [SerializeField] private Vector2 strafeMoveDurationRange = new Vector2(2f, 4f);
        [SerializeField] private Vector2 strafePauseDurationRange = new Vector2(0.5f, 1.25f);
        [SerializeField, Range(0f, 1f)] private float strafeDistanceBias = 0.5f;
        [SerializeField] private bool startStrafeRight = true;

        private bool strafePaused;
        private float strafeTimer;
        private float strafeDirection;
        private Vector3 strafeHeading;
        private EnemyMovementState lastState = EnemyMovementState.None;

        [Header("Movement")]
        [SerializeField] private float idleMoveSpeed = 0f;
        [SerializeField] private float preferredMinDistance = 4f;
        [SerializeField] private float preferredMaxDistance = 6f;

        public override void Initialize(in EnemyMovementContext context)
        {
            base.Initialize(in context);
            strafeDirection = startStrafeRight ? 1f : -1f;
        }

        public override float Move(EnemyMovementState state, Transform target, float deltaTime)
        {
            if (state != lastState)
            {
                if (state == EnemyMovementState.Idle || state == EnemyMovementState.Chase)
                    EnterIdle();
                lastState = state;
            }

            switch (state)
            {
                case EnemyMovementState.Idle:
                case EnemyMovementState.Chase:
                    return TickIdle(target, deltaTime);
                default:
                    Stop();
                    return Mathf.Infinity;
            }
        }

        private void EnterIdle()
        {
            strafeHeading = Vector3.zero;
            BeginStrafeMove();
            SetAgentStopped(false);
        }

        private float TickIdle(Transform target, float deltaTime)
        {
            if (context.Agent == null)
                return Mathf.Infinity;

            if (target == null)
            {
                SetAgentStopped(true);
                return Mathf.Infinity;
            }

            UpdateStrafe(target, deltaTime);

            return Vector3.Distance(context.Owner.position, target.position);
        }

        private void Stop()
        {
            SetAgentStopped(true);
        }

        private void UpdateStrafe(Transform target, float deltaTime)
        {
            var agent = context.Agent;
            if (agent == null)
                return;

            float moveSpeed = idleMoveSpeed > 0f ? idleMoveSpeed : agent.speed;
            agent.speed = moveSpeed;

            strafeTimer -= deltaTime;
            if (strafeTimer <= 0f)
            {
                if (strafePaused)
                    BeginStrafeMove();
                else
                    BeginStrafePause();
            }

            if (strafePaused)
            {
                SetAgentStopped(true);
                return;
            }

            SetAgentStopped(false);

            Vector3 fromPlayer = context.Owner.position - target.position;
            fromPlayer.y = 0f;

            if (strafeHeading.sqrMagnitude < 0.001f)
                strafeHeading = fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : context.Owner.forward;

            float angular = strafeAngularSpeed != 0f ? strafeAngularSpeed : 60f;
            Quaternion rotation = Quaternion.AngleAxis(angular * strafeDirection * deltaTime, Vector3.up);
            strafeHeading = rotation * strafeHeading.normalized;

            float targetDistance = GetPreferredDistance();
            Vector3 desiredPosition = target.position + strafeHeading.normalized * targetDistance;

            SetDestinationSafe(desiredPosition, "Strafe");
        }

        private void BeginStrafeMove()
        {
            strafePaused = false;
            strafeTimer = GetRandomRange(strafeMoveDurationRange, 2f);
            if (Random.value > 0.5f)
                strafeDirection *= -1f;
        }

        private void BeginStrafePause()
        {
            strafePaused = true;
            strafeTimer = GetRandomRange(strafePauseDurationRange, 0.5f);
        }

        private float GetPreferredDistance()
        {
            float min = preferredMinDistance > 0f ? preferredMinDistance : 4f;
            float max = preferredMaxDistance > 0f ? preferredMaxDistance : min + 2f;
            if (max < min + 0.1f)
                max = min + 0.5f;

            float t = Mathf.Clamp01(strafeDistanceBias);
            return Mathf.Lerp(min, max, t);
        }

        private float GetRandomRange(Vector2 range, float fallback)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            if (max <= 0f)
                return fallback;

            min = Mathf.Max(0f, min);
            if (min > max)
                min = max;

            return Random.Range(min, max);
        }

    }
}
