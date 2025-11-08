using UnityEngine;

namespace Spells.Projectiles
{
    [CreateAssetMenu(fileName = "GuidedMover", menuName = "Spells/Projectile Movers/Guided")]
    public class GuidedMover : ProjectileMover
    {
        [SerializeField] private float turnRateDegPerSec = 360f;
        [SerializeField, Range(-1f, 1f)]
        private float breakOrbitDotThreshold = 0.05f;

        public override void Initialize(ProjectileController controller)
        {
            // No state; degrade gracefully if no target
        }

        public override void Tick(ProjectileController controller, float dt)
        {
            var tr = controller.transform;
            Vector3 currentDir = controller.CurrentDirection;
            Vector3 desiredDir = currentDir;

            if (controller.target != null)
            {
                Vector3 toTarget = controller.target.position - tr.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    desiredDir = toTarget.normalized;
                    float closingDot = Vector3.Dot(currentDir, desiredDir);

                    if (closingDot <= breakOrbitDotThreshold)
                    {
                        // If we're no longer closing in, snap directly to the target
                        currentDir = desiredDir;
                    }
                    else
                    {
                        float maxRadians = Mathf.Deg2Rad * turnRateDegPerSec * dt;
                        currentDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f);
                    }
                }
            }

            controller.CurrentDirection = currentDir.normalized;
            tr.position += controller.CurrentDirection * controller.speed * dt;
        }
    }
}
