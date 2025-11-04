using UnityEngine;

namespace Enemies.Config
{
    [CreateAssetMenu(menuName = "Enemies/Stats Config")]
    public class StatsConfig : ScriptableObject
    {
        [Header("Base Resources")]
        public float maxHealth = 50f;
        public float maxMana = 0f;
        public float maxStamina = 0f;

        [Header("Regeneration")]
        public float healthRegenPerSecond = 0f;
        public float manaRegenPerSecond = 0f;
        public float staminaRegenPerSecond = 0f;
    }
}
