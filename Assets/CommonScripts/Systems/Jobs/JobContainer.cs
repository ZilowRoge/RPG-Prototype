using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Jobs {
[System.Serializable]
public class JobContainer
{
    private Dictionary<string, JobInstance> jobs = new();

    public void AddJob(JobData data)
    {
        if (!jobs.ContainsKey(data.id))
        {
            jobs[data.id] = new JobInstance
            {
                data = data,
                currentLevel = 1,
                experience = 0
            };
        }
    }

    public bool HasJob(string jobId) => jobs.ContainsKey(jobId);

    public JobInstance GetJob(string jobId) =>
        jobs.TryGetValue(jobId, out var job) ? job : null;

    public IEnumerable<JobInstance> GetAllJobs() => jobs.Values;

    public void AddExperience(string jobId, int amount)
    {
        if (!jobs.TryGetValue(jobId, out var job)) return;

        job.experience += amount;

        while (job.currentLevel < job.data.maxLevel &&
               job.experience >= GetExpForLevel(job))
        {
            job.currentLevel++;
            // Możesz dodać: trigger odblokowania spellów/perków
        }
    }

    public void Clear()
    {
        jobs.Clear();
    }

    private int GetExpForLevel(JobInstance job)
    {
        var data = job.data;
        return Mathf.RoundToInt(data.baseExpToLevel * Mathf.Pow(data.expGrowthRate, job.currentLevel - 1));
    }
}
}