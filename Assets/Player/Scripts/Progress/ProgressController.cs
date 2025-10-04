using UnityEngine;
using Player.Interfaces;
using Quests;
using System.Collections.Generic;
using Systems.Jobs;

namespace Player.Progress
{
    public class ProgressController : MonoBehaviour, IProgressReadOnly
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private JobDatabase jobDatabase;

        private readonly HashSet<int> symbols = new HashSet<int>();
        private readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
        private readonly JobContainer jobs = new JobContainer();

        public bool HasJob(string jobId)
        {
            return jobs.HasJob(jobId);
        }

        public void AddJob(string jobId)
        {
            Debug.Log("Add Job");
            var asset = jobDatabase != null ? jobDatabase.GetById(jobId) : null;
            if (asset == null) return;
            Debug.Log("Evaluate");
            jobs.AddJob(asset, OnAnyJobAdvanced);
            EvaluateQuests();
        }

        public IEnumerable<JobInstance> GetAllJobs() => jobs.GetAllJobs();

        public bool KnowsSymbol(int symbolId)
        {
            return symbols.Contains(symbolId);
        }

        public void LearnSymbol(int symbolId)
        {
            if (symbols.Add(symbolId)) EvaluateQuests();
        }

        public int KnownSymbolCount
        {
            get { return symbols.Count; }
        }

        public bool GetFlag(string key)
        {
            return flags.TryGetValue(key, out var v) && v;
        }

        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            flags[key] = value;
            EvaluateQuests();
        }

        public void StartQuest(string questId)
        {
            Debug.Log($"Starting quest {questId}");
            if (questManager == null || string.IsNullOrEmpty(questId)) return;
            if (questManager.StartQuest(questId)) {
                Debug.Log($"Evaluate quests");
                EvaluateQuests();
            }
        }

        public void EvaluateQuests()
        {
            // if (questManager != null) questManager.EvaluateAll(this);
        }

        private void OnAnyJobAdvanced(Systems.Jobs.JobInstance job)
        {
            EvaluateQuests();
        }
    }
}
