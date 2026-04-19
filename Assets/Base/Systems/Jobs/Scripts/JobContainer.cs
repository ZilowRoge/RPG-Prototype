using System;
using System.Collections.Generic;

namespace Systems.Jobs
{
    [System.Serializable]
    public class JobContainer
    {
        private readonly Dictionary<string, JobInstance> jobs = new();

        public void AddJob(JobData data, Action<JobInstance> onAdvanced)
        {
            if (!jobs.ContainsKey(data.id))
            {
                jobs[data.id] = new JobInstance(data, onAdvanced);
            }
        }

        public bool HasJob(string jobId) => jobs.ContainsKey(jobId);

        public JobInstance GetJob(string jobId) =>
            jobs.TryGetValue(jobId, out var job) ? job : null;

        public IEnumerable<JobInstance> GetAllJobs() => jobs.Values;

        public int AddExperience(string jobId, int amount)
        {
            if (jobs.TryGetValue(jobId, out var job))
                return job.AddExperience(amount);

            return amount > 0 ? amount : 0;
        }

        public void Clear() => jobs.Clear();
    }
}
