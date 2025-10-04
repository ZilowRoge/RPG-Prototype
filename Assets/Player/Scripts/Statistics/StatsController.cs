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

        public void RefillOnLevelUp()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }
    }
}
