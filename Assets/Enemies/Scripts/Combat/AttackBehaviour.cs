using UnityEngine;

namespace Enemies.Combat
{
    public abstract class AttackBehaviour : ScriptableObject
    {
        /// <summary>
        /// Executes the attack logic (damage, projectiles, effects).
        /// </summary>
        public abstract void Execute(in AttackContext context);
    }
}
