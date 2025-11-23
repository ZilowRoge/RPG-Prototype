using UnityEngine;
using Systems.Statistics;

namespace Player.Statistics
{
    [CreateAssetMenu(fileName = "StatsData", menuName = "Player/Stats Data")]
    public class StatsData : ScriptableObject
    {
        public float walkSpeed = 3.0f;
        public float runSpeed = 6.0f;

        [Header("Combat")]
        public float baseDamageMin = 10f;
        public float baseDamageMax = 15f;
        public float baseArmor = 0f;

        public float baseHealth = 50f;
        public float baseMana = 50f;
        public float baseStamina = 50f;

        public float healthPerVit = 10f;
        public float manaPerInt = 10f;
        public float staminaPerEnd = 10f;

        [Header("Regeneration")]
        [Tooltip("Passive health regeneration in points per second.")]
        public float healthRegenPerSecond = 1f;
        [Tooltip("Passive mana regeneration in points per second.")]
        public float manaRegenPerSecond = 2.5f;
        [Tooltip("Passive stamina regeneration in points per second.")]
        public float staminaRegenPerSecond = 5f;

        [Tooltip("Stamina consumed per second while the player is sprinting.")]
        public float sprintStaminaCostPerSecond = 8f;

        [SerializeField]
        public StatsContainer container = new();

        public int statisticsPointsPerLevel = 5;
    }
}
