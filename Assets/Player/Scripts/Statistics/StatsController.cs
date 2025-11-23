using System.Collections;
using UnityEngine;
using Player.Interfaces;
using Systems.Statistics;
using Player.Perks;
using Systems.Perks;
using Player.Events;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, IStatsReadOnly, IDamageable, IKnockbackable, IHealthProvider
    {
        [SerializeField] private StatsData statistics;
        [SerializeField] private PlayerPerkRuntime perkRuntime;
        [SerializeField] private PlayerEventHub playerEvents;
        [SerializeField] private StatsRuntime runtime = new();
        [Header("Knockback")]
        [SerializeField, Tooltip("Duration in seconds over which knockback displacement is applied.")]
        private float knockbackDuration = 0.15f;
        [SerializeField, Tooltip("Optional override for the CharacterController used to apply knockback.")]
        private CharacterController characterController;
        private bool loggedMissingEventHub;
        private bool isDead;
        public bool IsDead => isDead;

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

        private Coroutine knockbackRoutine;

        private void Awake()
        {
            if (perkRuntime == null)
                perkRuntime = GetComponent<PlayerPerkRuntime>() ?? GetComponentInParent<PlayerPerkRuntime>();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            EnsureEventHub();

            runtime.Initialize(maxHealth, maxMana, maxStamina);
            isDead = false;
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
            if (isDead)
                return false;

            return runtime.UseMana(amount);
        }

        public void ReceiveDamage(float amount, Transform source = null)
        {
            if (isDead)
                return;

            runtime.ReceiveDamage(amount);

            if (runtime.CurrentHealth <= 0f)
                HandleDeath();
        }

        public float RestoreHealth(float amount)
        {
            if (isDead)
                return 0f;

            return runtime.RestoreHealth(amount, maxHealth);
        }

        public float RestoreMana(float amount)
        {
            if (isDead)
                return 0f;

            return runtime.RestoreMana(amount, maxMana);
        }

        public float RestoreStamina(float amount)
        {
            if (isDead)
                return 0f;

            return runtime.RestoreStamina(amount, maxStamina);
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (isDead)
                return;

            if (force <= 0f)
                return;

            direction.y = 0f;
            direction = direction == Vector3.zero ? transform.forward : direction.normalized;

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.AddForce(direction * force, ForceMode.Impulse);
                return;
            }

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (characterController != null)
            {
                if (knockbackRoutine != null)
                    StopCoroutine(knockbackRoutine);

                knockbackRoutine = StartCoroutine(ApplyControllerKnockback(direction, force));
            }
        }

        public bool TryConsumeStamina(float amount)
        {
            if (isDead)
                return false;

            return runtime.TryConsumeStamina(amount);
        }

        public void RefillOnLevelUp()
        {
            runtime.Refill(maxHealth, maxMana, maxStamina);
        }

        public void RefillMana()
        {
            runtime.RestoreManaToMax(maxMana);
        }

        public void OverrideResources(float health, float mana, float stamina)
        {
            runtime.OverrideResources(health, mana, stamina, maxHealth, maxMana, maxStamina);
            if (runtime.CurrentHealth > 0f)
                isDead = false;
        }

        private void OnPerkResourcesUpdated()
        {
            if (isDead)
                return;

            runtime.ClampToMax(maxHealth, maxMana, maxStamina);
        }

        private void Update()
        {
            if (isDead)
                return;

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
            float percentBonus = perkRuntime != null ? perkRuntime.GetPercentBonus(EEffectType.ManaRegeneration) : 0f;
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
                WarnMissingEventHub();
        }

        private IEnumerator ApplyControllerKnockback(Vector3 direction, float force)
        {
            if (characterController == null)
            {
                knockbackRoutine = null;
                yield break;
            }

            float duration = Mathf.Max(0.01f, knockbackDuration);
            float elapsed = 0f;
            Vector3 displacementPerSecond = direction * (force / duration);

            while (elapsed < duration)
            {
                float delta = Time.deltaTime;
                characterController.Move(displacementPerSecond * delta);
                elapsed += delta;
                yield return null;
            }

            knockbackRoutine = null;
        }

        private void HandleDeath()
        {
            if (isDead)
                return;

            isDead = true;

            if (playerEvents != null)
            {
                playerEvents.NotifyPlayerDied();
            }
            else
            {
                WarnMissingEventHub();
            }
        }

        private void WarnMissingEventHub()
        {
            if (loggedMissingEventHub)
                return;

            Debug.LogWarning("[StatsController] PlayerEventHub is not assigned. Perk resource updates and death notifications will not be broadcast.");
            loggedMissingEventHub = true;
        }
    }
}
