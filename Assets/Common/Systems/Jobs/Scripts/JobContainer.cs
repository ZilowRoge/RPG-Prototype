using System.Collections.Generic;

namespace Systems.Jobs
{
    [System.Serializable]
    public class JobContainer
    {
        private readonly Dictionary<string, JobInstance> jobs = new();

        public void AddJob(JobData data)
        {
            if (!jobs.ContainsKey(data.id))
            {
                jobs[data.id] = new JobInstance(data);
            }
        }

        public bool HasJob(string jobId) => jobs.ContainsKey(jobId);

        public JobInstance GetJob(string jobId) =>
            jobs.TryGetValue(jobId, out var job) ? job : null;

        public IEnumerable<JobInstance> GetAllJobs() => jobs.Values;

        public void AddExperience(string jobId, int amount)
        {
            if (jobs.TryGetValue(jobId, out var job))
            {
                job.AddExperience(amount); // delegacja do JobInstance
            }
        }

        public void Clear() => jobs.Clear();
    }
}
