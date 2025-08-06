using UnityEngine;
using Systems.SaveSystem;
using Systems.Statistics;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, ISaveable
    {
        [SerializeField]
        private StatsData data;

        public StatsData Data => data;
        public float WalkSpeed => data.walkSpeed;
        public float RunSpeed => data.runSpeed;
        public float MaxHealth => data.maxHealth;
        public float MaxMana => data.maxMana;
        public float MaxStamina => data.maxStamina;

        public float CurrentHealth { get; private set; }
        public float CurrentMana { get; private set; }
        public float CurrentStamina { get; private set; }

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExperience { get; private set; } = 0;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            CurrentStamina = MaxStamina;
            SaveManager.Instance?.Register(this);
        }

        public void OnReceiveDamage(float damage) => CurrentHealth -= damage;

        public bool UseMana(float amount)
        {
            if (CurrentMana >= amount) {
                CurrentMana -= amount;
                return true;
            }
            return false;
        }

        public void GainExperience(int amount)
        {
            CurrentExperience += amount;
            while (CurrentExperience >= GetExperienceToNextLevel()) {
                CurrentExperience -= GetExperienceToNextLevel();
                LevelUp();
            }
        }

        private void LevelUp()
        {
            CurrentLevel++;
            // np. pełne leczenie lub inne efekty
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            CurrentStamina = MaxStamina;
        }

        public void AllocateStatPoint(EStatistics stat) {
            data.stats.AllocatePoint(stat);
        }

        public void AddPendingPoint(int amount = 1) {
            data.stats.AddPendingPoint(amount);
        }

        // ISaveable
        public void OnSave()
        {
            SaveManager.Instance.SavePlayerStats(this);
        }

        public void OnLoad()
        {
            SaveManager.Instance.LoadPlayerStats(this);
        }
        
        public int GetExperienceToNextLevel()
        {
            return Mathf.FloorToInt(data.baseExperienceToLevelUp * Mathf.Pow(data.experienceGrowthRate, CurrentLevel - 1));
        }
        public StatsContainer GetCurrentStats() => data.stats;
        public int GetPendingPoints() => data.stats.GetPendingPoints();

        public void SetHealth(float value) => CurrentHealth = value;
        public void SetMana(float value) => CurrentMana = value;
        public void SetStamina(float value) => CurrentStamina = value;
        public void SetExperience(int exp) => CurrentExperience = exp;
        public void SetLevel(int level) => CurrentLevel = level;
    }
}
