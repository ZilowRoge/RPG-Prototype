using UnityEngine;

namespace Player.Statistics
{
    [CreateAssetMenu(fileName = "StatsData", menuName = "Player/Statistics/Data")]
    public class StatsData : ScriptableObject
    {
        public float maxHealth = 100f;
        public float maxMana = 100f;
        public float maxStamina = 100f;

        public float walkSpeed = 3.0f;
        public float runSpeed = 6.0f;
    }
}