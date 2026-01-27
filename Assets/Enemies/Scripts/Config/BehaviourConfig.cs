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
        [SerializeField] private MovementConfig movement;
        [SerializeField] private DetectionSettings detection = new();
        [FormerlySerializedAs("reboot")]
        [SerializeField] private VulnerableSettings vulnerable = new();
        [SerializeField] private List<AttackRule> attacks = new();

        public MovementConfig Movement => movement;
        public DetectionSettings Detection => detection;
        public VulnerableSettings Vulnerable => vulnerable;
        public IReadOnlyList<AttackRule> Attacks => attacks;

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
    }
}
