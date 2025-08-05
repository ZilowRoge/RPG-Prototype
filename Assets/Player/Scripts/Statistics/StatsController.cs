using UnityEngine;
using Systems.SaveSystem;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, ISaveable
    {
        [SerializeField] private StatsData data;

        public float WalkSpeed => data != null ? data.walkSpeed : 0f;
        public float RunSpeed => data != null ? data.runSpeed : 0f;
        public float MaxHealth => data.maxHealth;
        public float MaxMana => data.maxMana;
        public float MaxStamina => data.maxStamina;

        public float CurrentHealth { get; private set; }
        public float CurrentMana { get; private set; }
        public float CurrentStamina { get; private set; }

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            CurrentStamina = MaxStamina;
        }

        public void OnReceiveDamage(float damage)
        {
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        }

        public bool UseMana(float amount)
        {
            if (CurrentMana >= amount) {
                CurrentMana -= amount;
                return true;
            }

            return false;
        }

        public void SetHealth(float value)
        {
            CurrentHealth = Mathf.Clamp(value, 0f, MaxHealth);
        }

        public void SetMana(float value)
        {
            CurrentMana = Mathf.Clamp(value, 0f, MaxMana);
        }

        public void SetStamina(float value)
        {
            CurrentStamina = Mathf.Clamp(value, 0f, MaxStamina);
        }

        public void OnSave()
        {
            SaveManager.Instance.SavePlayerStats(this);
        }

        public void OnLoad()
        {
            SaveManager.Instance.LoadPlayerStats(this);
        }
    }
}
