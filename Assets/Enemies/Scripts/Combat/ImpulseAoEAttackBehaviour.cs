using Player.Interfaces;
using UnityEngine;

namespace Enemies.Combat
{
    [CreateAssetMenu(menuName = "Combat/Behaviours/Impulse AoE")]
    public class ImpulseAoEAttackBehaviour : AttackBehaviour
    {
        [SerializeField] private float radius = 4f;
        [SerializeField] private LayerMask targetsMask = ~0;
        [SerializeField] private bool includeTriggers = false;
        [SerializeField, Tooltip("Multiplier applied to base attack damage when dealing AoE damage.")]
        private float damageMultiplier = 1f;
        [SerializeField, Tooltip("Optional VFX prefab spawned at source position.")]
        private GameObject impactVfx;
        [SerializeField, Tooltip("Optional VFX prefab spawned while the attack is charging.")]
        private GameObject chargingVfx;
        [SerializeField, Tooltip("Lifetime of spawned VFX in seconds.")]
        private float vfxLifetime = 2f;

        public float Radius => radius;
        public GameObject ChargingVfx => chargingVfx;
        public GameObject ImpactVfx => impactVfx;

        public override void Execute(in AttackContext context)
        {
            if (context.Source == null)
                return;

            Vector3 center = context.Source.position;
            QueryTriggerInteraction triggerInteraction = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            Collider[] hits = Physics.OverlapSphere(center, radius, targetsMask, triggerInteraction);

            if (impactVfx != null)
            {
                var spawned = Object.Instantiate(impactVfx, center, Quaternion.identity);
                if (vfxLifetime > 0f)
                    Object.Destroy(spawned, vfxLifetime);
            }

            if (hits == null || hits.Length == 0)
                return;

            float finalDamage = context.Attack.Damage * Mathf.Max(0f, damageMultiplier);

            foreach (var collider in hits)
            {
                if (collider == null)
                    continue;

                TryDealDamage(collider, finalDamage, context.Source);
            }
        }

        private static void TryDealDamage(Collider collider, float damage, Transform source)
        {
            var damageables = collider.GetComponents<IDamageable>();
            for (int i = 0; i < damageables.Length; i++)
            {
                damageables[i].ReceiveDamage(damage, source);
            }
        }

        public void DrawGizmos(Vector3 position)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, radius);
        }
    }
}
