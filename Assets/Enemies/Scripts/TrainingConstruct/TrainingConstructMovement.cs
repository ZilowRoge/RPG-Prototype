using Enemies.Abstraction;
using Enemies.Config;
using Enemies.Interfaces;
using Systems.Debugging;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.TrainingConstruct
{
    [System.Serializable]
    public class TrainingConstructMovement : EnemyMovementBase
    {
        private float idleTimer;
        private Vector3 idleAnchor;
        private float baseSpeed;
        private float baseAcceleration;
        private EnemyMovementState lastState = EnemyMovementState.None;

        [Header("Idle")]
        [SerializeField] private float idleRotationSpeed = 20f;
        [SerializeField] private float idleMoveInterval = 5f;
        [SerializeField] private float idleMoveRadius = 4f;
        [SerializeField] private float idleMoveSpeed = 0f;

        [Header("Chase")]
        [SerializeField] private float chaseSpeed = 0f;
        [SerializeField] private float chaseAcceleration = 0f;
        [SerializeField] private float preferredMinDistance = 3f;
        [SerializeField] private float preferredMaxDistance = 5f;

        public override void Initialize(in EnemyMovementContext context)
        {
            base.Initialize(in context);
            idleAnchor = context.Owner != null ? context.Owner.position : Vector3.zero;
            if (context.Agent != null)
            {
                baseSpeed = context.Agent.speed;
                baseAcceleration = context.Agent.acceleration;
            }
        }

        public override float Move(EnemyMovementState state, Transform target, float deltaTime)
        {
            if (state != lastState)
            {
                switch (state)
                {
                    case EnemyMovementState.Idle:
                        EnterIdle();
                        break;
                    case EnemyMovementState.Chase:
                        EnterChase();
                        break;
                }
                lastState = state;
            }

            switch (state)
            {
                case EnemyMovementState.Idle:
                    return TickIdle(target, deltaTime);
                case EnemyMovementState.Chase:
                    return TickChase(target, deltaTime);
                default:
                    Stop();
                    return Mathf.Infinity;
            }
        }

        private void EnterIdle()
        {
            idleTimer = 0f;
            var agent = context.Agent;
            if (agent != null)
            {
                float moveSpeed = idleMoveSpeed > 0f ? idleMoveSpeed : baseSpeed * 0.5f;
                agent.speed = moveSpeed;
                agent.acceleration = chaseAcceleration > 0f ? chaseAcceleration : baseAcceleration;
            }
            SetAgentStopped(false);
        }

        private void EnterChase()
        {
            var agent = context.Agent;
            if (agent != null)
            {
                float resolvedChaseSpeed = chaseSpeed > 0f ? chaseSpeed : baseSpeed;
                float resolvedChaseAcceleration = chaseAcceleration > 0f ? chaseAcceleration : baseAcceleration;
                agent.speed = resolvedChaseSpeed;
                agent.acceleration = resolvedChaseAcceleration;
            }
            SetAgentStopped(false);
        }

        private float TickIdle(Transform target, float deltaTime)
        {
            var owner = context.Owner;
            var agent = context.Agent;
            if (owner == null || agent == null)
                return Mathf.Infinity;

            float rotationSpeed = idleRotationSpeed != 0f ? idleRotationSpeed : 20f;
            float moveInterval = idleMoveInterval > 0f ? idleMoveInterval : 5f;

            owner.Rotate(Vector3.up, rotationSpeed * deltaTime, Space.World);

            idleTimer += deltaTime;
            if (idleTimer >= moveInterval)
            {
                idleTimer = 0f;
                TryMoveToRandomIdlePoint();
            }

            if (target == null)
                return Mathf.Infinity;

            return Vector3.Distance(owner.position, target.position);
        }

        private float TickChase(Transform target, float deltaTime)
        {
            var owner = context.Owner;
            var agent = context.Agent;
            if (owner == null || agent == null)
                return Mathf.Infinity;

            if (target == null)
            {
                SetAgentStopped(true);
                return Mathf.Infinity;
            }

            float minDistance = preferredMinDistance > 0f ? preferredMinDistance : 3f;
            float maxDistance = preferredMaxDistance > 0f ? preferredMaxDistance : 5f;

            float distance = Vector3.Distance(owner.position, target.position);
            context.Logger?.Log(ComponentLogger.LogFlag.Events, "Distance to player: {0:F2}", distance);
            Debug.DrawLine(owner.position, target.position, Color.green);

            if (distance > maxDistance)
            {
                SetDestinationSafe(target.position, "Chase towards player");
            }
            else if (distance < minDistance)
            {
                Vector3 retreatDir = (owner.position - target.position).normalized;
                Vector3 retreatPoint = owner.position + retreatDir * 2f;
                if (NavMesh.SamplePosition(retreatPoint, out var hit, 2f, agent.areaMask))
                    SetDestinationSafe(hit.position, "Retreat from player");
            }
            else
            {
                agent.velocity = Vector3.zero;
                SetAgentStopped(true);
            }

            return distance;
        }

        private void Stop()
        {
            var agent = context.Agent;
            if (agent != null)
                agent.velocity = Vector3.zero;
            SetAgentStopped(true);
        }

        private void TryMoveToRandomIdlePoint()
        {
            var agent = context.Agent;
            if (agent == null)
                return;

            float radius = idleMoveRadius > 0f ? idleMoveRadius : 4f;
            Vector3 randomPoint = idleAnchor + Random.insideUnitSphere * radius;
            randomPoint.y = idleAnchor.y;

            if (NavMesh.SamplePosition(randomPoint, out var hit, radius, agent.areaMask))
            {
                SetDestinationSafe(hit.position, "Idle wander");
            }
        }
    }
}
