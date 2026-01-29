using Enemies.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Enemies.Config
{
    [CreateAssetMenu(menuName = "Enemies/Behaviour Config")]
    public class BehaviourConfig : ScriptableObject
    {
        [SerializeField] private DetectionSettings detection = new();
        [FormerlySerializedAs("reboot")]
        [SerializeField] private VulnerableSettings vulnerable = new();
        [SerializeField] private List<AttackRule> attacks = new();
        [SerializeField] private List<ChargeUpForAbilityEntry> chargeUpForAbilityEntries = new();

        public DetectionSettings Detection => detection;
        public VulnerableSettings Vulnerable => vulnerable;
        public IReadOnlyList<AttackRule> Attacks => attacks;
        public IReadOnlyList<ChargeUpForAbilityEntry> ChargeUpForAbilityEntries => chargeUpForAbilityEntries;

        [Serializable]
        public struct DetectionSettings
        {
            [Tooltip("Distance at which the enemy starts chasing the player.")]
            public float detectionRange;
            [Tooltip("Multiplier applied to detection range for leashing back to idle.")]
            public float leashRangeMultiplier;
        }

        [Serializable]
        public struct VulnerableSettings
        {
            [Tooltip("Whether enemy becomes vulnerable after certain attacks.")]
            public bool enabled;
            [Tooltip("Duration of vulnerable state.")]
            public float duration;
        }

        [Serializable]
        public struct ChargeUpForAbilityEntry
        {
            [Tooltip("Identifier matched against charge-up VFX id in the brain.")]
            public string vfxId;
            public GameObject prefab;
            [Tooltip("Local offset from the owner when attaching, otherwise world offset.")]
            public Vector3 offset;
            public bool attachToOwner;
            [Tooltip("How long the charge VFX should stay alive.")]
            public float chargingTime;
        }
    }
}
