using UnityEngine;
using System;

namespace Spells {

[CreateAssetMenu(fileName = "MagicMissile", menuName = "Spells/Magic Missile")]
public class MagicMissile : ProjectileSpell
{
    // Casting on activation defines this as an Instant
    public override void OnActivation(CasterData caster)
    {
        OnCast(caster);
    }

    public override void OnCast(CasterData caster)
    {
        // Guided mover will home if target exists; otherwise flies straight
        LaunchProjectile(caster);
    }

    public override void OnDeactivation(CasterData caster)
    {}
}

}
