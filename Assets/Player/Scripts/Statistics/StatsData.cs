using UnityEngine;
using Systems.Statistics;

namespace Player.Statistics
{
    [CreateAssetMenu(fileName = "StatsData", menuName = "Player/Stats Data")]
    public class StatsData : ScriptableObject
    {
        public float walkSpeed = 3.0f;
        public float runSpeed = 6.0f;

        public float baseHealth = 50f;
        public float baseMana = 50f;
        public float baseStamina = 50f;

        public float healthPerVit = 10f;
        public float manaPerInt = 10f;
        public float staminaPerEnd = 10f;

        [SerializeField]
        public StatsContainer container = new();

        public int statisticsPointsPerLevel = 5;
    }
}
