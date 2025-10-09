using UnityEngine;
using Spells.Projectiles;

namespace Spells
{
    public abstract class ProjectileSpell : Spell
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
            if (ctrl == null)
            {
                ctrl = proj.AddComponent<ProjectileController>();
                // If an old projectile script exists, disable it to avoid conflicts
                var legacy = proj.GetComponent<MagicMissileProjectile>();
                if (legacy != null) legacy.enabled = false;
            }
            ctrl.Init(mover, caster.target, projectileSpeed, damage, caster.castOrigin.forward);
        }
    }
}

