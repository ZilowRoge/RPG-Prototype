using Enemies.Combat;
using System;
using UnityEngine;

namespace Enemies.Config
{
    [Serializable]
    public class AttackRule
    {
        [SerializeField] private AttackType type = AttackType.Impulse;
        [SerializeField] private AttackDefinition attack;
        [SerializeField] private float minDistance = 0f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField, Tooltip("Optional preparation delay before executing the attack.")]
        private float chargeUpDuration = 0f;
        [SerializeField, Tooltip("Optional modifier applied to the attack cooldown when triggered.")]
        private float cooldownModifier = 1f;
        [SerializeField, Tooltip("Optional delay after the attack before resuming chase.")]
        private float recoveryDuration = 0f;

        public AttackType Type => type;
        public AttackDefinition Attack => attack;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float ChargeUpDuration => chargeUpDuration;
        public float CooldownModifier => cooldownModifier;
        public float RecoveryDuration => recoveryDuration;

        public bool IsDistanceSatisfied(float distance)
        {
            return distance >= minDistance && distance <= maxDistance;
        }
    }

    public enum AttackType
    {
        Impulse,
        Charge
    }
}
