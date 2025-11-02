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
        [SerializeField] private StatsRuntime runtime = new();
        private bool loggedMissingEventHub;

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

        public float CurrentHealth => runtime.CurrentHealth;
        public float CurrentMana => runtime.CurrentMana;
        public float CurrentStamina => runtime.CurrentStamina;

        private void Awake()
        {
            if (perkRuntime == null)
                perkRuntime = GetComponent<PlayerPerkRuntime>() ?? GetComponentInParent<PlayerPerkRuntime>();

            EnsureEventHub();

            runtime.Initialize(maxHealth, maxMana, maxStamina);
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
            return runtime.UseMana(amount);
        }

        public void ReceiveDamage(float amount, Component source = null)
        {
            runtime.ReceiveDamage(amount);
        }

        public bool TryConsumeStamina(float amount)
        {
            return runtime.TryConsumeStamina(amount);
        }

        public void RefillOnLevelUp()
        {
            runtime.Refill(maxHealth, maxMana, maxStamina);
        }

        private void OnPerkResourcesUpdated()
        {
            runtime.ClampToMax(maxHealth, maxMana, maxStamina);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f || statistics == null)
                return;

            runtime.Regenerate(
                delta,
                maxHealth,
                statistics.healthRegenPerSecond,
                maxMana,
                GetManaRegenRate(),
                maxStamina,
                statistics.staminaRegenPerSecond);
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
