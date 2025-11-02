using UnityEngine;
using Player.Interfaces;
using Systems.Statistics;
using Player.Perks;
using Systems.Perks;
using Player.Events;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, IStatsReadOnly
    {
        [SerializeField] private StatsData statistics;
        [SerializeField] private PlayerPerkRuntime perkRuntime;
        [SerializeField] private PlayerEventHub playerEvents;
        private bool loggedMissingEventHub;

        private float currentHealth;
        private float currentMana;
        private float currentStamina;

        public StatsData Statistics => statistics;

        public float maxHealth =>
            statistics.baseHealth + GetTotalStat(EStatistics.VIT) * statistics.healthPerVit;

        public float maxMana
        {
            get
            {
                float baseValue = statistics.baseMana + GetTotalStat(EStatistics.INT) * statistics.manaPerInt;
                baseValue += perkRuntime != null ? perkRuntime.GetFlatBonus(EEffectType.AddMaxMana) : 0f;
                return baseValue;
            }
        }

        public float maxStamina =>
            statistics.baseStamina + GetTotalStat(EStatistics.END) * statistics.staminaPerEnd;

        public float walkSpeed => statistics.walkSpeed;
        public float runSpeed => statistics.runSpeed;

        public float CurrentHealth => currentHealth;
        public float CurrentMana => currentMana;
        public float CurrentStamina => currentStamina;

        private void Awake()
        {
            if (perkRuntime == null)
                perkRuntime = GetComponent<PlayerPerkRuntime>() ?? GetComponentInParent<PlayerPerkRuntime>();

            EnsureEventHub();

            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }

        private void OnEnable()
        {
            EnsureEventHub();

            if (playerEvents != null)
                playerEvents.PerkResourcesUpdated += OnPerkResourcesUpdated;
        }

        private void OnDisable()
        {
            if (playerEvents != null)
            {
                playerEvents.PerkResourcesUpdated -= OnPerkResourcesUpdated;
            }
        }

        public bool UseMana(float amount)
        {
            if (amount <= 0f)
                return true;

            if (currentMana >= amount)
            {
                currentMana -= amount;
                return true;
            }

            return false;
        }

        public void ReceiveDamage(float amount, Component source = null)
        {
            if (amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
        }

        public bool TryConsumeStamina(float amount)
        {
            if (amount <= 0f)
                return true;

            if (currentStamina >= amount)
            {
                currentStamina -= amount;
                if (currentStamina < 0f)
                    currentStamina = 0f;
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

        private void EnsureEventHub()
        {
            if (playerEvents != null)
            {
                loggedMissingEventHub = false;
                return;
            }

            playerEvents = GetComponent<PlayerEventHub>() ?? GetComponentInParent<PlayerEventHub>() ?? FindFirstObjectByType<PlayerEventHub>();

            if (playerEvents == null && !loggedMissingEventHub)
            {
                Debug.LogWarning("[StatsController] PlayerEventHub is not assigned. Perk resource updates will not be received.");
                loggedMissingEventHub = true;
            }
        }
    }
}
