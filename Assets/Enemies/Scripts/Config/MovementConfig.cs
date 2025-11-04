using System;
using UnityEngine;

namespace Enemies.Config
{
    [CreateAssetMenu(menuName = "Enemies/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [SerializeField] private IdleSettings idle = new();
        [SerializeField] private ChaseSettings chase = new();
        [SerializeField] private ChargeSettings charge = new();

        public IdleSettings Idle => idle;
        public ChaseSettings Chase => chase;
        public ChargeSettings Charge => charge;

        [Serializable]
        public struct IdleSettings
        {
            [Tooltip("Rotation speed in degrees per second while idling.")]
            public float rotationSpeed;
            [Tooltip("Seconds between picking a new idle destination.")]
            public float moveInterval;
            [Tooltip("Radius around the anchor point for idle wandering.")]
            public float moveRadius;
            [Tooltip("Movement speed during idle wandering.")]
            public float moveSpeed;
        }

        [Serializable]
        public struct ChaseSettings
        {
            [Tooltip("Agent speed while chasing.")]
            public float speed;
            [Tooltip("Agent acceleration while chasing.")]
            public float acceleration;
            [Tooltip("Preferred minimum distance to the target.")]
            public float preferredMinDistance;
            [Tooltip("Preferred maximum distance to the target.")]
            public float preferredMaxDistance;
        }

        [Serializable]
        public struct ChargeSettings
        {
            [Tooltip("Dash speed during charge attacks.")]
            public float dashSpeed;
            [Tooltip("Duration of the charge dash.")]
            public float dashDuration;
            [Tooltip("Radius used for collision checks during charge.")]
            public float collisionRadius;
            [Tooltip("Layers considered for stopping the charge dash.")]
            public LayerMask collisionMask;
        }
    }
}
