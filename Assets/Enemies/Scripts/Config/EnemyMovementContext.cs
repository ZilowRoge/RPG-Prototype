using Systems.Debugging;
using UnityEngine;

namespace Enemies.Config
{
    public readonly struct EnemyMovementContext
    {
        public readonly Transform Owner;
        public readonly ComponentLogger Logger;
        public readonly UnityEngine.AI.NavMeshAgent Agent;

        public EnemyMovementContext(
            Transform owner,
            UnityEngine.AI.NavMeshAgent agent,
            ComponentLogger logger)
        {
            Owner = owner;
            Agent = agent;
            Logger = logger;
        }
    }
}
