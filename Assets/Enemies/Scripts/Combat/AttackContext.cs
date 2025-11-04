using UnityEngine;

namespace Enemies.Combat
{
    /// <summary>
    /// Runtime context passed to an attack behaviour so it has all necessary data without
    /// accessing scene objects directly.
    /// </summary>
    public readonly struct AttackContext
    {
        public readonly Transform Source;
        public readonly Transform Target;
        public readonly AttackDefinition Attack;
        public readonly float DeltaTime;

        public AttackContext(
            Transform source,
            Transform target,
            AttackDefinition attack,
            float deltaTime)
        {
            Source = source;
            Target = target;
            Attack = attack;
            DeltaTime = deltaTime;
        }
    }
}
