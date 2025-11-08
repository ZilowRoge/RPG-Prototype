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
    }
}
