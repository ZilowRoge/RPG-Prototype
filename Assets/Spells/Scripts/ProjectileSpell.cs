using UnityEngine;
using Spells.Projectiles;

namespace Spells
{
    public abstract class ProjectileSpell : TargetedSpell
    {
        [Header("Projectile")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 10f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected ProjectileMover mover;

        protected void LaunchProjectile(CasterData caster)
        {
            if (projectilePrefab == null || caster.castOrigin == null)
            {
                Debug.LogWarning("[ProjectileSpell] Missing prefab or castOrigin!");
                return;
            }

            var proj = Object.Instantiate(projectilePrefab, caster.castOrigin.position, Quaternion.LookRotation(caster.castOrigin.forward, Vector3.up));
            var ctrl = proj.GetComponent<ProjectileController>();

            float spellPowerMultiplier = caster != null ? caster.SpellPowerMultiplier : 1f;
            float finalDamage = damage * Mathf.Max(0f, spellPowerMultiplier);
            ctrl.Init(mover, caster.target, projectileSpeed, finalDamage, caster.castOrigin.forward);
        }
    }
}
