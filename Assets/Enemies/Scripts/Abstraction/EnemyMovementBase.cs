using Enemies.Config;
using Enemies.Interfaces;
using Systems.Debugging;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Abstraction
{
    [System.Serializable]
    public abstract class EnemyMovementBase : IEnemyMovement
    {
        protected EnemyMovementContext context;

        public virtual void Initialize(in EnemyMovementContext context)
        {
            this.context = context;
        }

        public abstract float Move(EnemyMovementState state, Transform target, float deltaTime);

        public bool EnsureAgentOnNavMesh(string contextTag)
        {
            var agent = context.Agent;
            if (agent == null || !agent.enabled)
                return false;

            if (agent.isOnNavMesh)
                return true;

            Vector3 position = context.Owner != null ? context.Owner.position : agent.transform.position;
            if (NavMesh.SamplePosition(position, out var hit, 1.5f, agent.areaMask))
            {
                agent.Warp(hit.position);
                context.Logger?.Log(ComponentLogger.LogFlag.Events,
                    "{0}: warped agent onto NavMesh.",
                    contextTag);
                return true;
            }

            context.Logger?.LogWarning(ComponentLogger.LogFlag.Events,
                "{0}: failed to find NavMesh.",
                contextTag);
            return false;
        }

        protected void SetDestinationSafe(Vector3 destination, string debugContext)
        {
            if (!EnsureAgentOnNavMesh(debugContext))
                return;

            var agent = context.Agent;
            if (agent == null || !agent.enabled)
                return;

            if (!agent.SetDestination(destination))
            {
                context.Logger?.LogWarning(ComponentLogger.LogFlag.Events,
                    "{0}: SetDestination failed.",
                    debugContext);
            }
        }

        protected void SetAgentStopped(bool stop)
        {
            var agent = context.Agent;
            if (agent == null || !agent.enabled)
                return;

            if (!agent.isOnNavMesh)
            {
                context.Logger?.LogWarning(ComponentLogger.LogFlag.Events,
                    "Tried to set isStopped={0} while agent off NavMesh.",
                    stop);
                return;
            }

            agent.isStopped = stop;
        }
    }
}
