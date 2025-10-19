using System;
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
        [SerializeField] private SymbolProgress symbolProgress;

        private readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
        private readonly JobContainer jobs = new JobContainer();

        public event System.Action<string, bool> FlagChanged;

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

        public bool KnowsSymbol(string symbolKey)
        {
            if (string.IsNullOrWhiteSpace(symbolKey))
                return false;

            var parsedId = ParseSymbolId(symbolKey);
            if (parsedId < 0)
                return false;

            return KnowsSymbol(parsedId);
        }

        public bool KnowsSymbol(int symbolId)
        {
            return symbolProgress != null && symbolProgress.IsSymbolLearned(symbolId);
        }

        public bool IsQuestActive(string questId)
        {
            if (string.IsNullOrEmpty(questId) || questManager == null)
                return false;

            return questManager.IsQuestActive(questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId) || questManager == null)
                return false;

            return questManager.IsQuestCompleted(questId);
        }

        public bool IsQuestStarted(string questId)
        {
            if (string.IsNullOrEmpty(questId) || questManager == null)
                return false;

            return questManager.IsQuestActive(questId) || questManager.IsQuestCompleted(questId);
        }
        public void LearnSymbol(string symbolKey)
        {
            var parsedId = ParseSymbolId(symbolKey);
            if (parsedId < 0)
            {
                Debug.LogWarning($"[ProgressController] Invalid symbol identifier '{symbolKey}'.");
                return;
            }

            LearnSymbol(parsedId);
        }

        public void LearnSymbol(int symbolId)
        {
            if (symbolProgress == null) return;
            if (symbolProgress.MarkSymbolLearned(symbolId))
                EvaluateQuests();
        }

        public int KnownSymbolCount => symbolProgress != null ? symbolProgress.LearnedSymbolCount : 0;

        public bool GetFlag(string key)
        {
            return flags.TryGetValue(key, out var v) && v;
        }

        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            flags[key] = value;
            FlagChanged?.Invoke(key, value);
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
            if (questManager != null) questManager.EvaluateAll(this);
        }

        private static int ParseSymbolId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return -1;

            var trimmed = raw.Trim();
            if (trimmed.StartsWith("sym_", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(4);

            return int.TryParse(trimmed, out var id) ? id : -1;
        }

        private void OnAnyJobAdvanced(Systems.Jobs.JobInstance job)
        {
            EvaluateQuests();
        }
    }
}




