using UnityEngine;
using Player.Interfaces;
using Systems.Statistics;
using Player.Perks;
using Systems.Perks;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, IStatsReadOnly
    {
        [SerializeField] private StatsData statistics;
        [SerializeField] private PlayerPerkRuntime perkRuntime;

        private float currentHealth;
        private float currentMana;
        private float currentStamina;
        public StatsData Statistics => statistics;

        public float maxHealth
        {
            get
            {
                return statistics.baseHealth + GetTotalStat(EStatistics.VIT) * statistics.healthPerVit;
            }
        }

        public float maxMana
        {
            get
            {
                float baseValue = statistics.baseMana + GetTotalStat(EStatistics.INT) * statistics.manaPerInt;
                baseValue += perkRuntime != null ? perkRuntime.GetFlatBonus(EEffectType.AddMaxMana) : 0f;
                return baseValue;
            }
        }

        public float maxStamina
        {
            get
            {
                return statistics.baseStamina + GetTotalStat(EStatistics.END) * statistics.staminaPerEnd;
            }
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
            if (perkRuntime == null)
            {
                perkRuntime = GetComponent<PlayerPerkRuntime>() ?? GetComponentInParent<PlayerPerkRuntime>();
            }

            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }

        private void OnEnable()
        {
            if (perkRuntime != null)
                perkRuntime.UpdateResources += OnPerkResourcesUpdated;
        }

        private void OnDisable()
        {
            if (perkRuntime != null)
                perkRuntime.UpdateResources -= OnPerkResourcesUpdated;
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

        private void OnPerkResourcesUpdated()
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            currentMana = Mathf.Min(currentMana, maxMana);
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f || statistics == null)
                return;

            RegenerateResource(ref currentHealth, maxHealth, statistics.healthRegenPerSecond, delta);
            RegenerateResource(ref currentMana, maxMana, GetManaRegenRate(), delta);
            RegenerateResource(ref currentStamina, maxStamina, statistics.staminaRegenPerSecond, delta);
        }

        private static void RegenerateResource(ref float current, float max, float ratePerSecond, float deltaTime)
        {
            if (ratePerSecond <= 0f || current >= max)
                return;

            current = Mathf.Min(max, current + ratePerSecond * deltaTime);
        }

        private int GetTotalStat(EStatistics stat)
        {
            int baseValue = statistics != null ? statistics.container.Get(stat) : 0;
            int bonus = perkRuntime != null ? perkRuntime.GetStatBonus(stat) : 0;
            return baseValue + bonus;
        }

        private float GetManaRegenRate()
        {
            float baseRate = statistics.manaRegenPerSecond;
            float percentBonus = perkRuntime != null ? perkRuntime.GetPercentBonus(EEffectType.ManaRegenPercent) : 0f;
            return baseRate * (1f + percentBonus);
        }
    }
}
