using System.Collections;
using UnityEngine;
using Player.Interfaces;
using Systems.Statistics;
using Player.Perks;
using Systems.Perks;
using Player.Events;
using Inventory;
using Items;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, IStatsReadOnly, IDamageable, IKnockbackable, IHealthProvider
    {
        [SerializeField] private StatsData statistics;
        [SerializeField] private PlayerPerkRuntime perkRuntime;
        [SerializeField] private PlayerEventHub playerEvents;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private StatsRuntime runtime = new();
        private IDamageShield activeDamageShield;
        [Header("Knockback")]
        [SerializeField, Tooltip("Duration in seconds over which knockback displacement is applied.")]
        private float knockbackDuration = 0.15f;
        [SerializeField, Tooltip("Optional override for the CharacterController used to apply knockback.")]
        private CharacterController characterController;
        private bool loggedMissingEventHub;
        private bool isDead;
        private readonly System.Collections.Generic.Dictionary<ItemStatType, StatAccumulator> equipmentModifiers = new();
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
        public float DamageMin => statistics != null ? ApplyEquipmentStat(ItemStatType.Damage, statistics.baseDamageMin) : 0f;
        public float DamageMax => statistics != null ? ApplyEquipmentStat(ItemStatType.Damage, statistics.baseDamageMax) : 0f;
        public float Armor => statistics != null ? ApplyEquipmentStat(ItemStatType.Armor, statistics.baseArmor) : 0f;

        public float CurrentHealth => runtime.CurrentHealth;
        public float CurrentMana => runtime.CurrentMana;
        public float CurrentStamina => runtime.CurrentStamina;

        private Coroutine knockbackRoutine;

        private void Awake()
        {
            if (perkRuntime == null)
                perkRuntime = GetComponent<PlayerPerkRuntime>() ?? GetComponentInParent<PlayerPerkRuntime>();

            if (equipmentController == null)
                equipmentController = GetComponent<EquipmentController>() ?? GetComponentInParent<EquipmentController>();

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

        public void RegisterDamageShield(IDamageShield shield)
        {
            if (shield == null)
                return;

            if (isDead)
            {
                shield.Deactivate();
                return;
            }

            if (activeDamageShield != null && activeDamageShield != shield)
                activeDamageShield.Deactivate();

            activeDamageShield = shield;
        }

        public bool IsShieldActive()
        {
            return activeDamageShield != null;
        }

        public void UnregisterDamageShield(IDamageShield shield)
        {
            if (shield == null || activeDamageShield != shield)
                return;

            activeDamageShield = null;
        }

        public void ReceiveDamage(float amount, Transform source = null)
        {
            if (isDead)
                return;

            if (amount <= 0f)
                return;

            if (activeDamageShield != null)
                amount = activeDamageShield.AbsorbDamage(amount);

            if (amount <= 0f)
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
            ClearDamageShield();
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

            RebuildEquipmentBonuses();

            runtime.Regenerate(
                delta,
                maxHealth,
                GetHealthRegenRate(),
                maxMana,
                GetManaRegenRate(),
                maxStamina,
                GetStaminaRegenRate());
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
            var equipmentAdjusted = ApplyEquipmentStat(ItemStatType.ManaRegen, baseRate);
            return equipmentAdjusted * (1f + percentBonus);
        }

        private float GetHealthRegenRate()
        {
            float baseRate = statistics.healthRegenPerSecond;
            return ApplyEquipmentStat(ItemStatType.HealthRegen, baseRate);
        }

        private float GetStaminaRegenRate()
        {
            float baseRate = statistics.staminaRegenPerSecond;
            return ApplyEquipmentStat(ItemStatType.StaminaRegen, baseRate);
        }

        private float ApplyEquipmentStat(ItemStatType statType, float baseValue)
        {
            if (!equipmentModifiers.TryGetValue(statType, out var accumulator) || accumulator == null)
                return baseValue;

            return accumulator.Apply(baseValue);
        }

        private void RebuildEquipmentBonuses()
        {
            equipmentModifiers.Clear();

            var slots = equipmentController != null ? equipmentController.Slots : null;
            if (slots == null)
                return;

            foreach (var entry in slots)
            {
                if (entry == null || entry.IsEmpty)
                    continue;

                var item = entry.ItemInstance;
                var mods = item?.Modifiers;
                if (mods == null)
                    continue;

                foreach (var mod in mods)
                {
                    if (!equipmentModifiers.TryGetValue(mod.Stat, out var accumulator) || accumulator == null)
                    {
                        accumulator = new StatAccumulator();
                        equipmentModifiers[mod.Stat] = accumulator;
                    }

                    accumulator.Add(mod);
                }
            }
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
            ClearDamageShield();

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

        private sealed class StatAccumulator
        {
            public float AddValue;
            public float Multiply = 1f;
            public float? Override;

            public void Add(ItemStatModifier modifier)
            {
                switch (modifier.Mode)
                {
                    case ModifierMode.Add:
                        AddValue += modifier.Value;
                        break;
                    case ModifierMode.Multiply:
                        Multiply *= 1f + modifier.Value;
                        break;
                    case ModifierMode.Override:
                        Override = modifier.Value;
                        break;
                }
            }

            public float Apply(float baseValue)
            {
                var value = Override ?? baseValue;
                value = value * Multiply + AddValue;
                return value;
            }
        }

        private void ClearDamageShield()
        {
            if (activeDamageShield == null)
                return;

            var shield = activeDamageShield;
            activeDamageShield = null;
            shield.Deactivate();
        }
    }
}
