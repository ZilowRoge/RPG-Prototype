using UnityEngine;
using Player.Interfaces;
using Systems.Statistics;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, IStatsReadOnly
    {
        [SerializeField] private StatsData statistics;

        private float currentHealth;
        private float currentMana;
        private float currentStamina;
        public StatsData Statistics => statistics;

        public float maxHealth
        {
            get { return statistics.baseHealth + statistics.container.Get(EStatistics.VIT) * statistics.healthPerVit; }
        }

        public float maxMana
        {
            get { return statistics.baseMana + statistics.container.Get(EStatistics.INT) * statistics.manaPerInt; }
        }

        public float maxStamina
        {
            get { return statistics.baseStamina + statistics.container.Get(EStatistics.END) * statistics.staminaPerEnd; }
        }

        public float walkSpeed
        {
            get { return statistics.walkSpeed; }
        }

        public float runSpeed
        {
            get { return statistics.runSpeed; }
        }

        public float CurrentHealth
        {
            get { return currentHealth; }
        }

        public float CurrentMana
        {
            get { return currentMana; }
        }

        public float CurrentStamina
        {
            get { return currentStamina; }
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }

        public bool UseMana(float amount)
        {
            if (currentMana >= amount)
            {
                currentMana -= amount;
                return true;
            }
            return false;
        }

        public void ReceiveDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth < 0f) currentHealth = 0f;
        }

        public bool TryConsumeStamina(float amount)
        {
            if (amount <= 0f)
                return true;

            if (currentStamina >= amount)
            {
                currentStamina -= amount;
                if (currentStamina < 0f) currentStamina = 0f;
                return true;
            }

            return false;
        }

        public void RefillOnLevelUp()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f || statistics == null)
                return;

            RegenerateResource(ref currentHealth, maxHealth, statistics.healthRegenPerSecond, delta);
            RegenerateResource(ref currentMana, maxMana, statistics.manaRegenPerSecond, delta);
            RegenerateResource(ref currentStamina, maxStamina, statistics.staminaRegenPerSecond, delta);
        }

        private static void RegenerateResource(ref float current, float max, float ratePerSecond, float deltaTime)
        {
            if (ratePerSecond <= 0f || current >= max)
                return;

            current = Mathf.Min(max, current + ratePerSecond * deltaTime);
        }
    }
}
