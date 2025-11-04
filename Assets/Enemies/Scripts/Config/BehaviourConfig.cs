using Enemies.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Config
{
    [CreateAssetMenu(menuName = "Enemies/Behaviour Config")]
    public class BehaviourConfig : ScriptableObject
    {
        [SerializeField] private MovementConfig movement;
        [SerializeField] private DetectionSettings detection = new();
        [SerializeField] private RebootSettings reboot = new();
        [SerializeField] private List<AttackRule> attacks = new();

        public MovementConfig Movement => movement;
        public DetectionSettings Detection => detection;
        public RebootSettings Reboot => reboot;
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
        public struct RebootSettings
        {
            [Tooltip("Whether enemy performs a reboot state after certain attacks.")]
            public bool enabled;
            [Tooltip("Duration of reboot state.")]
            public float duration;
        }
    }
}
