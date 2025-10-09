using UnityEngine;

namespace Spells.Projectiles
{
    [CreateAssetMenu(fileName = "GuidedMover", menuName = "Spells/Projectile Movers/Guided")]
    public class GuidedMover : ProjectileMover
    {
        [SerializeField] private float turnRateDegPerSec = 360f;

        public override void Initialize(ProjectileController controller)
        {
            // No state; degrade gracefully if no target
        }

        public override void Tick(ProjectileController controller, float dt)
        {
            var tr = controller.transform;
            Vector3 desiredDir = controller.CurrentDirection;
            if (controller.target != null)
            {
                Vector3 toTarget = controller.target.position - tr.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    desiredDir = toTarget.normalized;
                }
            }

            // Rotate current direction towards desired with limited turn rate
            float maxRadians = Mathf.Deg2Rad * turnRateDegPerSec * dt;
            controller.CurrentDirection = Vector3.RotateTowards(controller.CurrentDirection, desiredDir, maxRadians, 0f).normalized;
            tr.position += controller.CurrentDirection * controller.speed * dt;
        }
    }
}

