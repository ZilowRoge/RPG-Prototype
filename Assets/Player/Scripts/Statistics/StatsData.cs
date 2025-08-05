using UnityEngine;
using Systems.Statistics;

namespace Player.Statistics
{
    [CreateAssetMenu(fileName = "StatsData", menuName = "Player/Stats Data")]
    public class StatsData : ScriptableObject
    {
        public float walkSpeed = 3.0f;
        public float runSpeed = 6.0f;
        public float maxHealth = 100f;
        public float maxMana = 50f;
        public float maxStamina = 75f;

        [SerializeField]
        public StatsContainer stats = new();

        public int baseExperienceToLevelUp = 100;
        public float experienceGrowthRate = 1.25f; // np. kolejne poziomy potrzebują więcej expa
    }
}
