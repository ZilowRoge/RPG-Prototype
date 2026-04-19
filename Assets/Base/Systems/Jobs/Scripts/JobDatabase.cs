using System.Collections.Generic;
using UnityEngine;

namespace Systems.Jobs
{
    [CreateAssetMenu(menuName = "Jobs/Job Database")]
    public class JobDatabase : ScriptableObject
    {
        [SerializeField] private List<JobData> jobs = new();

        private Dictionary<string, JobData> index;

        private void OnEnable() => BuildIndex();

        private void BuildIndex()
        {
            if (index == null) index = new Dictionary<string, JobData>();
            else index.Clear();

            foreach (var j in jobs)
            {
                if (j == null || string.IsNullOrEmpty(j.id)) continue;
                index[j.id] = j;
            }
        }

        public JobData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (index == null || index.Count != jobs.Count) BuildIndex();
            return index.TryGetValue(id, out var jd) ? jd : null;
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildIndex();
#endif
    }
}
