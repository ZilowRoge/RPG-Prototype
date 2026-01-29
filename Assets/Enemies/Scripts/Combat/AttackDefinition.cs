using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Combat
{
    [CreateAssetMenu(menuName = "Combat/AttackDefinition")]
    public class AttackDefinition : ScriptableObject
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 2f;
        [SerializeField, Tooltip("Base cooldown in seconds between consecutive uses of this attack.")]
        private float baseCooldown = 1.5f;
        [SerializeField] private AttackBehaviour behaviour;
        [SerializeField, Tooltip("Optional tags usable for AI decision making (e.g., Melee, Ranged, AoE).")]
        private List<string> tags = new();

        public float Damage => damage;
        public float Range => range;
        public float BaseCooldown => baseCooldown;
        public IReadOnlyList<string> Tags => tags;
        public AttackBehaviour Behaviour => behaviour;

        /// <summary>
        /// Returns the cooldown to apply after executing the attack.
        /// </summary>
        public float GetCooldown(float cooldownModifier = 1f)
        {
            float computedCooldown = baseCooldown;

            computedCooldown = Mathf.Max(0f, computedCooldown);
            return computedCooldown * Mathf.Max(0f, cooldownModifier);
        }

        /// <summary>
        /// Executes the attached behaviour if available.
        /// </summary>
        public void Execute(in AttackContext context)
        {
            if (behaviour == null)
            {
                Debug.LogWarning($"[AttackDefinition] Missing behaviour on {name}");
                return;
            }

            behaviour.Execute(in context);
        }
    }
}
