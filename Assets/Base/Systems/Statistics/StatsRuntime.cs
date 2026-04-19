using UnityEngine;

namespace Systems.Statistics
{
    /// <summary>
    /// Shared runtime state for character resources such as health, mana, and stamina.
    /// </summary>
    [System.Serializable]
    public class StatsRuntime
    {
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentMana;
        [SerializeField] private float currentStamina;

        public float CurrentHealth => currentHealth;
        public float CurrentMana => currentMana;
        public float CurrentStamina => currentStamina;

        public void Initialize(float maxHealth, float maxMana, float maxStamina)
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            currentStamina = maxStamina;
        }

        public void Refill(float maxHealth, float maxMana, float maxStamina)
        {
            Initialize(maxHealth, maxMana, maxStamina);
        }

        public void OverrideResources(
            float health,
            float mana,
            float stamina,
            float maxHealth,
            float maxMana,
            float maxStamina)
        {
            currentHealth = Clamp(health, maxHealth);
            currentMana = Clamp(mana, maxMana);
            currentStamina = Clamp(stamina, maxStamina);
        }

        public void ClampToMax(float maxHealth, float maxMana, float maxStamina)
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            currentMana = Mathf.Min(currentMana, maxMana);
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        public void RestoreManaToMax(float maxMana)
        {
            currentMana = Mathf.Max(0f, maxMana);
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

        public void ReceiveDamage(float amount)
        {
            if (amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
        }

        public float RestoreHealth(float amount, float maxHealth)
        {
            if (amount <= 0f)
                return 0f;

            float previous = currentHealth;
            currentHealth = Clamp(currentHealth + amount, maxHealth);
            return currentHealth - previous;
        }

        public float RestoreMana(float amount, float maxMana)
        {
            if (amount <= 0f)
                return 0f;

            float previous = currentMana;
            currentMana = Clamp(currentMana + amount, maxMana);
            return currentMana - previous;
        }

        public float RestoreStamina(float amount, float maxStamina)
        {
            if (amount <= 0f)
                return 0f;

            float previous = currentStamina;
            currentStamina = Clamp(currentStamina + amount, maxStamina);
            return currentStamina - previous;
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

        public void Regenerate(
            float deltaTime,
            float maxHealth,
            float healthRegenPerSecond,
            float maxMana,
            float manaRegenPerSecond,
            float maxStamina,
            float staminaRegenPerSecond)
        {
            if (deltaTime <= 0f)
                return;

            RegenerateResource(ref currentHealth, maxHealth, healthRegenPerSecond, deltaTime);
            RegenerateResource(ref currentMana, maxMana, manaRegenPerSecond, deltaTime);
            RegenerateResource(ref currentStamina, maxStamina, staminaRegenPerSecond, deltaTime);
        }

        private static void RegenerateResource(ref float current, float max, float ratePerSecond, float deltaTime)
        {
            if (ratePerSecond <= 0f || current >= max)
                return;

            current = Mathf.Min(max, current + ratePerSecond * deltaTime);
        }

        private static float Clamp(float value, float max)
        {
            if (max <= 0f)
                return Mathf.Max(0f, value);
            return Mathf.Clamp(value, 0f, max);
        }
    }
}
