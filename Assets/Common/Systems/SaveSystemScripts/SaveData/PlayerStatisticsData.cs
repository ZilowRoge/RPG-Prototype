using System;
using System.Collections.Generic;
using Systems.Jobs;

namespace Systems.SaveSystem.SaveData {
 [Serializable]
    public class PlayerStatisticsData
    {
        public float currentHealth;
        public float currentMana;
        public float currentStamina;
        public int level;

        public int pendingStatPoints;
        public List<SerializedStatEntry> stats;
        public List<SerializedJobEntry> jobs;

        public void SetBasics(float health, float mana, float stamina, int lvl)
        {
            currentHealth  = health;
            currentMana    = mana;
            currentStamina = stamina;
            level          = lvl < 1 ? 1 : lvl;
        }

        public void SetStats(List<(Statistics.EStatistics key, int value)> allStats, int pendingPoints)
        {
            var serialized = new List<SerializedStatEntry>(allStats.Count);
            foreach (var (stat, value) in allStats)
            {
                serialized.Add(new SerializedStatEntry(stat.ToString(), value));
            }
            stats = serialized;
            pendingStatPoints = pendingPoints < 0 ? 0 : pendingPoints;
        }

        public void SetJobs(IEnumerable<Systems.Jobs.JobInstance> jobs)
        {
            var serialized = new List<SerializedJobEntry>();
            foreach (var job in jobs)
            {
                serialized.Add(new SerializedJobEntry(
                    job.Data.id,
                    job.CurrentLevel,
                    job.Experience,
                    job.PerkPoints,
                    job.GetUnlockedPerkIds()
                ));
            }
            this.jobs = serialized;
        }

        public void GetBasics(out float health, out float mana, out float stamina, out int lvl)
        {
            health  = currentHealth;
            mana    = currentMana;
            stamina = currentStamina;
            lvl     = level < 1 ? 1 : level;
        }

        public void GetStatsPairs(
            out List<(Statistics.EStatistics stat, int value)> pairs,
            out int pendingPoints)
        {
            pairs = new List<(Statistics.EStatistics, int)>(stats?.Count ?? 0);
            if (stats != null)
            {
                foreach (var e in stats)
                {
                    if (System.Enum.TryParse(e.key, out Statistics.EStatistics parsed))
                        pairs.Add((parsed, e.value));
                }
            }
            pendingPoints = pendingStatPoints < 0 ? 0 : pendingStatPoints;
        }

        public void ApplyJobsTo(
            JobContainer container,
            Func<string, JobData> resolveJobData,
            Action<JobInstance> onAdvanced)
        {
            if (container == null) return;
            container.Clear();

            if (jobs == null || resolveJobData == null) return;

            foreach (var s in jobs)
            {
                var jd = resolveJobData(s.jobId);
                if (jd == null) continue;

                container.AddJob(jd, onAdvanced);
                var ji = container.GetJob(s.jobId);
                if (ji == null) continue;

                ji.SetLevel(s.level);
                ji.SetExperience(s.experience);
                ji.SetPerkPoints(s.perkPoints);
                ji.SetUnlockedPerks(s.perkNames);
            }
        }
    }
}