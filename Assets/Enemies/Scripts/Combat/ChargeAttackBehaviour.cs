using Player.Interfaces;
using UnityEngine;

namespace Enemies.Combat
{
    [CreateAssetMenu(menuName = "Combat/Behaviours/Charge Attack")]
    public class ChargeAttackBehaviour : AttackBehaviour
    {
        [SerializeField, Tooltip("Multiplier applied to the base attack damage when the charge connects.")]
        private float damageMultiplier = 1.5f;
        [SerializeField, Tooltip("Optional knockback force applied to the target's rigidbody.")]
        private float knockbackForce = 8f;
        [SerializeField, Tooltip("Optional VFX prefab spawned when the charge begins.")]
        private GameObject chargeVfxPrefab;
        [SerializeField, Tooltip("Offset along the charge direction (local Z) where the VFX should appear.")]
        private float chargeVfxOffset = 2f;

        public GameObject ChargeVfxPrefab => chargeVfxPrefab;
        public float ChargeVfxOffset => Mathf.Max(0f, chargeVfxOffset);

        public override void Execute(in AttackContext context)
        {
            if (context.Target == null)
                return;

            float damage = context.Attack.Damage * Mathf.Max(0f, damageMultiplier);

            var damageables = context.Target.GetComponents<IDamageable>();
            for (int i = 0; i < damageables.Length; i++)
            {
                damageables[i].ReceiveDamage(damage, context.Source);
            }

            ApplyKnockback(context.Target, context.Source, knockbackForce);
        }

        private static void ApplyKnockback(Transform target, Transform source, float force)
        {
            if (force <= 0f || target == null)
                return;

            Vector3 direction = source != null
                ? (target.position - source.position).normalized
                : target.forward;

            direction.y = 0f;

            var knockbackables = target.GetComponents<IKnockbackable>();
            for (int i = 0; i < knockbackables.Length; i++)
            {
                knockbackables[i].ApplyKnockback(direction, force);
            }

            if (knockbackables.Length > 0)
                return;

            var body = target.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.AddForce(direction.normalized * force, ForceMode.Impulse);
                return;
            }

            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.Move(direction.normalized * force);
            }
        }
    }
}
