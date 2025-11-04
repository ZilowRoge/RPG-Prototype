using UnityEngine;
using Player.Statistics;
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
        [SerializeField] private float hitRadius = 0.25f;

        private float life;
        private ProjectileMover mover;

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

            if (target != null)
            {
                var tPos = target.position;
                if ((transform.position - tPos).sqrMagnitude <= hitRadius * hitRadius)
                {
                    var damageables = target.GetComponents<IDamageable>();
                    for (int i = 0; i < damageables.Length; i++)
                        damageables[i].ReceiveDamage(damage, transform);
                    Destroy(gameObject);
                }
            }
        }
    }
}
