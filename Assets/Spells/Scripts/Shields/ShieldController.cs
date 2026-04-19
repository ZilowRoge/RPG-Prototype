using UnityEngine;
using Player.Interfaces;

namespace Spells.Shields
{
    public class ShieldController : MonoBehaviour, IDamageShield
    {
        [Header("State")]
        [SerializeField] private bool followTarget = true;

        private ISpellCasterStats stats;
        private Transform follow;
        private float remainingShield;
        private float remainingDuration;
        private bool initialized;
        private bool pendingDestroy;

        public static bool TryInit(GameObject instance, ISpellCasterStats stats, float shieldAmount, float duration, Transform followTarget)
        {
            if (instance == null)
            {
                Debug.LogWarning("[ShieldController] Missing shield instance.");
                return false;
            }

            if (!instance.TryGetComponent<ShieldController>(out var controller) || controller == null)
            {
                Debug.LogWarning("[ShieldController] Shield prefab lacks ShieldController component.");
                Destroy(instance);
                return false;
            }

            controller.Init(stats, shieldAmount, duration, followTarget);
            return true;
        }

        public void Init(ISpellCasterStats stats, float shieldAmount, float duration, Transform followTarget)
        {
            if (stats == null)
            {
                Debug.LogWarning("[ShieldController] Missing spell caster stats.", this);
                Destroy(gameObject);
                return;
            }

            float amount = Mathf.Max(0f, shieldAmount);
            if (amount <= 0f)
            {
                Debug.LogWarning("[ShieldController] Shield amount must be greater than zero.", this);
                Destroy(gameObject);
                return;
            }

            float finalDuration = Mathf.Max(0f, duration);
            if (finalDuration <= 0f)
            {
                Debug.LogWarning("[ShieldController] Shield duration must be greater than zero.", this);
                Destroy(gameObject);
                return;
            }

            this.stats = stats;
            follow = followTarget;
            remainingShield = amount;
            remainingDuration = finalDuration;
            initialized = true;
            stats.RegisterDamageShield(this);
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (stats == null)
            {
                Destroy(gameObject);
                return;
            }

            if (pendingDestroy)
            {
                if (stats != null)
                    stats.UnregisterDamageShield(this);
                Destroy(gameObject);
                return;
            }

            if (remainingDuration > 0f)
            {
                remainingDuration -= Time.deltaTime;
                if (remainingDuration <= 0f)
                {
                    pendingDestroy = true;
                    if (stats != null)
                        stats.UnregisterDamageShield(this);
                    Destroy(gameObject);
                    return;
                }
            }

            if (followTarget && follow != null)
            {
                transform.position = follow.position;
                transform.rotation = follow.rotation;
            }

            if (remainingShield <= 0f)
            {
                if (stats != null)
                    stats.UnregisterDamageShield(this);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.UnregisterDamageShield(this);
        }

        public float AbsorbDamage(float amount)
        {
            if (!initialized || amount <= 0f || remainingShield <= 0f)
                return amount;

            float absorbed = Mathf.Min(remainingShield, amount);
            remainingShield -= absorbed;
            amount -= absorbed;

            // Shield breaks on first hit, regardless of remaining capacity.
            remainingShield = 0f;
            pendingDestroy = true;

            return amount;
        }

        public void Deactivate()
        {
            if (!initialized)
                return;

            if (stats != null)
                stats.UnregisterDamageShield(this);
            Destroy(gameObject);
        }
    }
}
