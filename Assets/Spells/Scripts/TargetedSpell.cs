using UnityEngine;

namespace Spells
{
    public abstract class TargetedSpell : Spell
    {
        [Header("Targeting")]
        [SerializeField] protected bool requiresTarget = true;
        [SerializeField] protected float desiredTargetRange = 25f;
        [SerializeField, Range(0f, 180f)] protected float desiredFovAngle = 120f;
        [SerializeField] protected bool preferCenterRay = true;

        public override bool RequiresTarget => requiresTarget;
        public override float DesiredTargetRange => desiredTargetRange;
        public override float DesiredFovAngle => desiredFovAngle;
        public override bool PreferCenterRay => preferCenterRay;
    }
}
