using UnityEngine;
using Player.Interfaces;

namespace Spells.Projectiles
{
    public class ProjectileController : MonoBehaviour
    {
        [Header("Runtime State")]
        public Transform target;
        public float speed;
        public float damage;
        public Vector3 initialForward;

        [Header("Tuning")]
        [SerializeField] private float maxLifetime = 8f;
        [SerializeField] private bool shouldDestroyOnCollision = true;

        private float life;
        private ProjectileMover mover;
        private bool hasHit;

        // Internal transient state for strategies
        public Vector3 CurrentDirection { get; set; }

        public void Init(ProjectileMover mover, Transform target, float speed, float damage, Vector3 initialForward)
        {
            this.mover = mover;
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.initialForward = initialForward.sqrMagnitude > 0.0001f ? initialForward.normalized : transform.forward;
            this.CurrentDirection = this.initialForward;
            mover?.Initialize(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            if (life > maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (mover != null)
            {
                mover.Tick(this, dt);
            }
            else
            {
                // Fallback: fly straight forward
                transform.position += CurrentDirection * speed * dt;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyDamage(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
                return;

            TryApplyDamage(collision.collider);
        }

        private void TryApplyDamage(Collider collider)
        {
            if (hasHit || collider == null)
                return;

            var damageables = collider.GetComponents<IDamageable>();
            bool hasDamageables = damageables != null && damageables.Length > 0;

            if (!hasDamageables) {
                Debug.Log("Should do nothing");
                return;
            }

            if (hasDamageables)
            {
                for (int i = 0; i < damageables.Length; i++)
                    damageables[i].ReceiveDamage(damage, transform);
            }

            hasHit = true;
            if (shouldDestroyOnCollision)
                Destroy(gameObject);
        }
    }
}
