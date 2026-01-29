using Enemies.Config;
using UnityEngine;

namespace Enemies.Interfaces
{
    public interface IEnemyMovement
    {
        void Initialize(in EnemyMovementContext context);
        float Move(EnemyMovementState state, Transform target, float deltaTime);
    }

    public enum EnemyMovementState
    {
        None,
        Idle,
        Chase
    }

}
