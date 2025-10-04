using UnityEngine;
using System;

namespace Spells {

[CreateAssetMenu(fileName = "MagicMissile", menuName = "Spells/Magic Missile")]
public class MagicMissile : Spell
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float damage = 10f;

    // Casting on activation defines this as an Instant
    // TODO make an abstract class Instant
    public override void OnActivation(CasterData caster)
    {
        OnCast(caster);
    }

    public override void OnCast(CasterData caster)
    {
        if (projectilePrefab == null || caster.castOrigin == null || caster.target == null)
        {
            Debug.LogWarning("MagicMissile: Missing prefab, castOrigin, or target!");
            return;
        }

        var projectile = Instantiate(projectilePrefab, caster.castOrigin.position, Quaternion.identity);

        var projComp = projectile.GetComponent<MagicMissileProjectile>();
        if (projComp != null)
        {
            projComp.Init(caster.target, projectileSpeed, damage);
        }
    }

    public override void OnDeactivation(CasterData caster)
    {}
}

}