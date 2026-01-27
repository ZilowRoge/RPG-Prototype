using UnityEngine;
using System;

namespace Spells {
public abstract class Spell : ScriptableObject, Interfaces.ICastable
{
    [SerializeField] protected string spellName;
    [SerializeField] protected float manaCost;
    [Header("Casting")]
    [SerializeField] protected bool castOnActivation = false;
    [Header("Indicators")]
    [SerializeField] protected GameObject indicatorPrefab;

    public float ManaCost => manaCost;
    public virtual bool RequiresTarget => false;
    public virtual float DesiredTargetRange => 0f;
    public virtual float DesiredFovAngle => 0f;
    public virtual bool PreferCenterRay => true;
    public bool CastOnActivation => castOnActivation;
    public GameObject IndicatorPrefab => indicatorPrefab;

    public abstract void OnActivation(CasterData caster);
    public abstract void OnCast(CasterData caster);
    public abstract void OnDeactivation(CasterData caster);
}
}
