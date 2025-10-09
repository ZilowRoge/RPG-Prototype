using UnityEngine;

namespace Spells.Projectiles
{
    [CreateAssetMenu(fileName = "StraightMover", menuName = "Spells/Projectile Movers/Straight")]
    public class StraightMover : ProjectileMover
    {
        public override void Initialize(ProjectileController controller)
        {
            // Direction already set to initialForward
        }

        public override void Tick(ProjectileController controller, float dt)
        {
            controller.transform.position += controller.CurrentDirection * controller.speed * dt;
        }
    }
}

