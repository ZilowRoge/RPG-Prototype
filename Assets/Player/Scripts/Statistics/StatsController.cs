using UnityEngine;
using Systems.SaveSystem;
using Systems.Statistics;
using Systems.Jobs;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, ISaveable
    {
        [SerializeField]
        private StatsData data;

        public JobContainer Jobs { get; private set; } = new();
        public StatsData Data => data;
        public float WalkSpeed => data.walkSpeed;
        public float RunSpeed => data.runSpeed;
        public float MaxHealth => data.baseHealth + GetStat(EStatistics.VIT) * data.healthPerVit;
        public float MaxMana => data.baseMana + GetStat(EStatistics.INT) * data.manaPerInt;
        public float MaxStamina => data.baseStamina + GetStat(EStatistics.END) * data.staminaPerEnd;

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
            if (CurrentExperience >= GetExperienceToNextLevel()) {
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

        private int GetStat(EStatistics stat)
        {
            return data.stats.Get(stat); // Zakładam, że masz metodę `Get(EStatistics)` w `StatsContainer`
        }
        
        public int GetExperienceForLevel(int level)
        {
            return Mathf.RoundToInt(data.baseExperienceToLevelUp * Mathf.Pow(data.experienceGrowthRate, level - 1));
        }
        
        public int GetExperienceToNextLevel()
        {
            return GetExperienceForLevel(CurrentLevel - 1);
        }

        public StatsContainer GetCurrentStats() => data.stats;
        public int GetPendingPoints() => data.stats.GetPendingPoints();

        public void SetHealth(float value) => CurrentHealth = value;
        public void SetMana(float value) => CurrentMana = value;
        public void SetStamina(float value) => CurrentStamina = value;
        public void SetExperience(int exp) => CurrentExperience = exp;
        public void SetLevel(int level) => CurrentLevel = level;

        // ISaveable
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
