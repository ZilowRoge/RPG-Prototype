using System;
using UnityEngine;
using Player.Interfaces;
using Quests;
using System.Collections.Generic;
using Systems.Jobs;
using Systems.SaveSystem.SaveData;
using Player.Events;

namespace Player.Progress
{
    public class ProgressController : MonoBehaviour, IProgressReadOnly
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private JobDatabase jobDatabase;
        [SerializeField] private SymbolProgress symbolProgress;
        [SerializeField] private int startingAvailableExperience = 20000;
        [SerializeField] private PlayerEventHub playerEvents;

        private readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
        private readonly JobContainer jobs = new JobContainer();
        private int availableExperience;

        public int AvailableExperience => availableExperience;
        public PlayerEventHub EventHub
        {
            get
            {
                if (playerEvents == null)
                    CacheEventHub();
                return playerEvents;
            }
        }

        public QuestManager QuestManager
        {
            get
            {
                if (questManager == null)
                    CacheQuestManager();
                return questManager;
            }
        }
        
        public bool HasJob(string jobId)
        {
            return jobs.HasJob(jobId);
        }

        public void AddJob(string jobId)
        {
            var asset = jobDatabase != null ? jobDatabase.GetById(jobId) : null;
            if (asset == null) return;
            jobs.AddJob(asset, OnAnyJobAdvanced);
            var jobInstance = jobs.GetJob(jobId);
            NotifyJobExperienceChanged(jobInstance);
            EvaluateQuests();
        }

        public JobInstance GetJob(string jobId) => jobs.GetJob(jobId);

        public IEnumerable<JobInstance> GetAllJobs() => jobs.GetAllJobs();

        public void ApplyJobsFromSnapshot(PlayerStatisticsData data, bool notify = true)
        {
            if (data == null)
                return;

            if (jobDatabase == null)
            {
                Debug.LogWarning("[ProgressController] JobDatabase is not assigned. Unable to restore jobs from save.");
                return;
            }

            data.ApplyJobsTo(jobs, jobDatabase.GetById, OnAnyJobAdvanced);

            if (!notify)
                return;

            foreach (var job in jobs.GetAllJobs())
            {
                NotifyJobExperienceChanged(job);
            }
        }

        public void OverrideAvailableExperience(int amount, bool notify = true)
        {
            availableExperience = Mathf.Max(0, amount);
            if (notify)
                NotifyAvailableExperienceChanged();
        }

        public void GrantExperience(int amount)
        {
            if (amount <= 0) return;

            availableExperience += amount;
            NotifyAvailableExperienceChanged();
        }

        public int AllocateExperienceToJob(string jobId, int amount)
        {
            if (string.IsNullOrEmpty(jobId) || amount <= 0) return 0;
            var job = jobs.GetJob(jobId);
            if (job == null) return 0;

            int spendable = Mathf.Min(amount, availableExperience);
            if (spendable <= 0) return 0;

            availableExperience -= spendable;
            int overflow = job.AddExperience(spendable);
            int consumed = spendable - overflow;

            if (overflow > 0)
                availableExperience += overflow;

            if (consumed > 0)
            {
                NotifyAvailableExperienceChanged();
                NotifyJobExperienceChanged(job);
            }

            return consumed;
        }

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

        public List<int> ExportKnownSymbols()
        {
            return symbolProgress != null
                ? new List<int>(symbolProgress.GetLearnedSymbols())
                : new List<int>();
        }

        public void OverwriteKnownSymbols(IEnumerable<int> symbols)
        {
            if (symbolProgress == null)
                return;

            symbolProgress.OverwriteLearnedSymbols(symbols);
            EvaluateQuests();
        }

        public IEnumerable<KeyValuePair<string, bool>> ExportFlags()
        {
            foreach (var entry in flags)
                yield return entry;
        }

        public void OverwriteFlags(IEnumerable<KeyValuePair<string, bool>> entries, bool notify = true)
        {
            flags.Clear();
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                    continue;

                flags[entry.Key] = entry.Value;
                if (notify)
                    NotifyFlagChanged(entry.Key, entry.Value);
            }

            if (notify)
                EvaluateQuests();
        }

        public bool GetFlag(string key)
        {
            return flags.TryGetValue(key, out var v) && v;
        }

        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            flags[key] = value;
            NotifyFlagChanged(key, value);
            EvaluateQuests();
        }

        public void ReportElimination(string targetId, string extraId = null, int count = 1)
        {
            if (questManager == null)
                return;
            questManager.ReportElimination(this, targetId, extraId, count);
        }

        public void StartQuest(string questId)
        {
            if (questManager == null || string.IsNullOrEmpty(questId)) return;
            if (questManager.StartQuest(questId)) {
                EvaluateQuests();
                Quests.GameEvents.EmitQuestStarted(questId);
            }
        }

        public void EvaluateQuests()
        {
            if (questManager != null) questManager.EvaluateAll(this);
        }

        private void Awake()
        {
            CacheEventHub();
            CacheQuestManager();
            availableExperience = Mathf.Max(0, startingAvailableExperience);
        }

        private void OnValidate()
        {
            CacheEventHub();
            CacheQuestManager();
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
            NotifyJobExperienceChanged(job);
            EvaluateQuests();
        }

        private void NotifyAvailableExperienceChanged()
        {
            if (playerEvents == null)
            {
                WarnMissingEventHub();
                return;
            }
            playerEvents.NotifyAvailableExperienceChanged(availableExperience);
        }

        private void NotifyJobExperienceChanged(JobInstance job)
        {
            if (playerEvents == null)
            {
                WarnMissingEventHub();
                return;
            }
            playerEvents.NotifyJobExperienceChanged(job);
        }

        private void NotifyFlagChanged(string key, bool value)
        {
            if (playerEvents == null)
            {
                WarnMissingEventHub();
                return;
            }
            playerEvents.NotifyFlagChanged(key, value);
        }

        private void CacheEventHub()
        {
            if (playerEvents == null)
                playerEvents = GetComponent<PlayerEventHub>() ?? GetComponentInParent<PlayerEventHub>() ?? FindAnyObjectByType<PlayerEventHub>();
        }

        private void CacheQuestManager()
        {
            if (questManager != null)
                return;

            var gameManagerObject = GameObject.FindGameObjectWithTag("GameManager");
            if (gameManagerObject != null)
                questManager = gameManagerObject.GetComponent<QuestManager>();

            if (questManager == null)
                Debug.LogWarning("[ProgressController] QuestManager was not found on the GameManager tagged object.");
        }

        private void WarnMissingEventHub()
        {
            Debug.LogWarning("[ProgressController] PlayerEventHub is not assigned. Progress notifications will not be broadcast.");
        }
    }
}

