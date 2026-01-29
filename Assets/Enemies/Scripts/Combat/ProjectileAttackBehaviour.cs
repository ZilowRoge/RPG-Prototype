using Spells.Projectiles;
using UnityEngine;

namespace Enemies.Combat
{
    [CreateAssetMenu(menuName = "Combat/Behaviours/Projectile Attack")]
    public class ProjectileAttackBehaviour : AttackBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private ProjectileMover mover;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField, Tooltip("Local offset from the source where the projectile spawns.")]
        private Vector3 spawnOffset = new Vector3(0f, 1f, 0.5f);
        [SerializeField, Tooltip("When true, aim toward the target if available.")]
        private bool aimAtTarget = true;

        public override void Execute(in AttackContext context)
        {
            if (context.Source == null)
                return;

            if (projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectileAttackBehaviour] Projectile prefab is not assigned.");
                return;
            }

            Vector3 spawnPosition = context.Source.position + context.Source.TransformDirection(spawnOffset);
            Vector3 forward = context.Source.forward;

            if (aimAtTarget && context.Target != null)
            {
                Vector3 direction = context.Target.position - spawnPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    forward = direction.normalized;
            }

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            var instance = Object.Instantiate(projectilePrefab, spawnPosition, rotation);
            var controller = instance.GetComponent<ProjectileController>();
            if (controller == null)
            {
                Debug.LogWarning("[ProjectileAttackBehaviour] Projectile prefab missing ProjectileController.");
                Object.Destroy(instance);
                return;
            }

            controller.Init(mover, context.Target, projectileSpeed, context.Attack.Damage, forward);
        }
    }
}
