using UnityEngine;
using Systems.SaveSystem;
using Systems.Statistics;
using Systems.Jobs;

namespace Player.Statistics
{
    public class ProgressController : MonoBehaviour, ISaveable
    {
        [SerializeField] private StatsData statistics;
        [SerializeField] private JobDatabase jobDatabase;

        public JobContainer Jobs { get; private set; } = new();
        public StatsData Statistics => statistics;
        public StatsContainer StatisticsContainer => statistics.container;
        public float WalkSpeed => statistics.walkSpeed;
        public float RunSpeed => statistics.runSpeed;
        public float MaxHealth => statistics.baseHealth + StatisticsContainer.Get(EStatistics.VIT) * statistics.healthPerVit;
        public float MaxMana => statistics.baseMana + StatisticsContainer.Get(EStatistics.INT) * statistics.manaPerInt;
        public float MaxStamina => statistics.baseStamina + StatisticsContainer.Get(EStatistics.END) * statistics.staminaPerEnd;

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
            Jobs.AddJob(jobDatabase.GetById("wizard"), OnAnyJobAdvanced);
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
            if (statistics != null && StatisticsContainer != null)
                StatisticsContainer.AddPendingPoint(statistics.statisticsPointsPerLevel);

            CurrentHealth  = MaxHealth;
            CurrentMana    = MaxMana;
            CurrentStamina = MaxStamina;
        }

        public void AllocateStatPoint(EStatistics stat) {
            StatisticsContainer.AllocatePoint(stat);
        }

        public void AddPendingPoint(int amount = 1) {
            StatisticsContainer.AddPendingPoint(amount);
        }

        public void OnSave(Systems.SaveSystem.SaveData.GameData data)
        {
            if (data == null) return;
            data.playerData ??= new Systems.SaveSystem.SaveData.PlayerStatisticsData();

            data.playerData.SetBasics(CurrentHealth, CurrentMana, CurrentStamina, CurrentLevel);
            data.playerData.SetStats(StatisticsContainer.GetAll(), StatisticsContainer.GetPendingPoints());
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
            StatisticsContainer.SetStats(pairs, pendingPoints);

            data.playerData.ApplyJobsTo(Jobs, jobDatabase.GetById, OnAnyJobAdvanced);
        }
    }
}
