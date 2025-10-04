using UnityEngine;
using System;

namespace Spells {
public abstract class Spell : ScriptableObject, Interfaces.ICastable
{
    [SerializeField] protected string spellName;
    [SerializeField] protected float manaCost;

    public float ManaCost => manaCost;

    public abstract void OnActivation(CasterData caster);
    public abstract void OnCast(CasterData caster);
    public abstract void OnDeactivation(CasterData caster);
}
}