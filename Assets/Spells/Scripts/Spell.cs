using UnityEngine;
using System;

namespace Spells {
public abstract class Spell : ScriptableObject, Interfaces.ICastable
{
    [SerializeField] protected string spellName;
    [SerializeField] protected float manaCost;
    [Header("Targeting")]
    [SerializeField] protected bool requiresTarget = true;
    [SerializeField] protected float desiredTargetRange = 25f;
    [SerializeField, Range(0f,180f)] protected float desiredFovAngle = 120f;
    [SerializeField] protected bool preferCenterRay = true;
    [Header("Casting")]
    [SerializeField] protected bool castOnActivation = false;
    [Header("Indicators")]
    [SerializeField] protected GameObject indicatorPrefab;

    public float ManaCost => manaCost;
    public bool RequiresTarget => requiresTarget;
    public float DesiredTargetRange => desiredTargetRange;
    public float DesiredFovAngle => desiredFovAngle;
    public bool PreferCenterRay => preferCenterRay;
    public bool CastOnActivation => castOnActivation;
    public GameObject IndicatorPrefab => indicatorPrefab;

    public abstract void OnActivation(CasterData caster);
    public abstract void OnCast(CasterData caster);
    public abstract void OnDeactivation(CasterData caster);
}
}
