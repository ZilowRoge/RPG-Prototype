using UnityEngine;

namespace Spells.Projectiles
{
    public abstract class ProjectileMover : ScriptableObject
    {
        // Stateless strategy: all state lives in controller
        public abstract void Initialize(ProjectileController controller);
        public abstract void Tick(ProjectileController controller, float dt);
    }
}

