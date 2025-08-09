using UnityEngine;
using Systems.SaveSystem;
using Systems.Statistics;
using Systems.Jobs;

namespace Player.Statistics
{
    public class StatsController : MonoBehaviour, ISaveable
    {
        public JobData jobData;
        [SerializeField] private StatsData data;
        [SerializeField] private JobDatabase jobDatabase;

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

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            CurrentStamina = MaxStamina;
            SaveManager.Instance?.Register(this);
            Jobs.AddJob(jobData, OnAnyJobAdvanced);
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

        private void OnAnyJobAdvanced(JobInstance job)
        {
            CurrentLevel++;
            if (data != null && data.stats != null)
                data.stats.AddPendingPoint(Data.statisticsPointsPerLevel);

            CurrentHealth  = MaxHealth;
            CurrentMana    = MaxMana;
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
            return data.stats.Get(stat);
        }

        public StatsContainer GetCurrentStats() => data.stats;
        public int GetPendingPoints() => data.stats.GetPendingPoints();

        public void OnSave(Systems.SaveSystem.SaveData.GameData data)
        {
            if (data == null) return;
            data.playerData ??= new Systems.SaveSystem.SaveData.PlayerStatisticsData();

            data.playerData.SetBasics(CurrentHealth, CurrentMana, CurrentStamina, CurrentLevel);
            data.playerData.SetStats(Data.stats.GetAll(), Data.stats.GetPendingPoints());
            data.playerData.SetJobs(Jobs.GetAllJobs());
        }

        public void OnLoad(Systems.SaveSystem.SaveData.GameData data)
        {
            if (data?.playerData == null) return;

            data.playerData.GetBasics(out var h, out var m, out var s, out var lvl);
            CurrentLevel   = Mathf.Max(1, lvl);
            CurrentHealth  = h;
            CurrentMana    = m;
            CurrentStamina = s;

            data.playerData.GetStatsPairs(out var pairs, out var pendingPoints);
            this.data.stats.SetStats(pairs, pendingPoints);

            data.playerData.ApplyJobsTo(Jobs, jobDatabase.GetById, OnAnyJobAdvanced);
        }
    }
}
