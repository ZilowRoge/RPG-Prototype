using System;
using System.Collections.Generic;
using UnityEngine;
using Player.Progress;
using Player.Statistics;
using Quests;
using Systems.SaveSystem;
using Systems.SaveSystem.SaveData;

namespace Player.Save
{
    [DisallowMultipleComponent]
    public class SaveState : MonoBehaviour, ISaveable
    {
        public static event Action PlayerLoadedFromSave;
        public static bool IsRestoring { get; private set; }

        [Header("References")]
        [SerializeField] private ProgressController progressController;
        [SerializeField] private StatsController statsController;
        [SerializeField] private QuestManager questManager;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SaveManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.Unregister(this);
        }

        public void OnSave(GameData data)
        {
            if (data == null)
                return;

            data.playerData ??= new PlayerStatisticsData();
            data.progressData ??= new PlayerProgressData();

            WriteStats(data.playerData);
            WriteProgress(data.progressData);
        }

        public void OnLoad(GameData data)
        {
            if (data == null)
                return;

            IsRestoring = true;
            ReadStats(data.playerData);
            ReadProgress(data.progressData);
            IsRestoring = false;
            PlayerLoadedFromSave?.Invoke();
        }

        private void CacheReferences()
        {
            if (progressController == null)
                progressController = GetComponentInParent<ProgressController>() ?? FindFirstObjectByType<ProgressController>();

            if (statsController == null)
                statsController = GetComponentInParent<StatsController>() ?? FindFirstObjectByType<StatsController>();

            if (questManager == null)
                questManager = progressController?.QuestManager ?? FindFirstObjectByType<QuestManager>();
        }

        private void WriteStats(PlayerStatisticsData snapshot)
        {
            if (snapshot == null)
                return;

            if (statsController != null)
            {
                snapshot.SetBasics(
                    statsController.CurrentHealth,
                    statsController.CurrentMana,
                    statsController.CurrentStamina,
                    ResolvePlayerLevel());

                var statsData = statsController.Statistics;
                if (statsData != null && statsData.container != null)
                {
                    snapshot.SetStats(
                        statsData.container.GetAll(),
                        statsData.container.GetPendingPoints());
                }
            }

            if (progressController != null)
            {
                snapshot.SetJobs(progressController.GetAllJobs());
            }
        }

        private void ReadStats(PlayerStatisticsData snapshot)
        {
            if (snapshot == null)
                return;

            if (progressController != null)
                progressController.ApplyJobsFromSnapshot(snapshot);

            if (statsController == null)
                return;

            var statsData = statsController.Statistics;
            if (statsData != null && statsData.container != null)
            {
                snapshot.GetStatsPairs(out var stats, out var pendingPoints);
                statsData.container.SetStats(stats, pendingPoints);
            }

            snapshot.GetBasics(out var health, out var mana, out var stamina, out _);
            statsController.OverrideResources(health, mana, stamina);
        }

        private void WriteProgress(PlayerProgressData snapshot)
        {
            if (snapshot == null)
                return;

            snapshot.flags ??= new List<SerializedFlagEntry>();
            snapshot.learnedSymbols ??= new List<int>();
            snapshot.quests ??= new List<SerializedQuestProgress>();

            snapshot.hasPlayerTransform = true;
            snapshot.playerPosition = transform.position;
            snapshot.playerRotation = transform.rotation;

            if (progressController == null)
                return;

            snapshot.availableExperience = progressController.AvailableExperience;

            snapshot.flags.Clear();
            foreach (var flag in progressController.ExportFlags())
            {
                snapshot.flags.Add(new SerializedFlagEntry(flag.Key, flag.Value));
            }

            snapshot.learnedSymbols.Clear();
            var symbols = progressController.ExportKnownSymbols();
            if (symbols != null && symbols.Count > 0)
                snapshot.learnedSymbols.AddRange(symbols);

            snapshot.quests.Clear();
            if (questManager != null)
            {
                foreach (var quest in questManager.ActiveQuests)
                {
                    var serialized = SerializeQuest(quest);
                    if (serialized != null)
                        snapshot.quests.Add(serialized);
                }
            }
        }

        private void ReadProgress(PlayerProgressData snapshot)
        {
            if (snapshot == null)
                return;

            ApplySavedTransform(snapshot);

            if (progressController != null)
            {
                progressController.OverrideAvailableExperience(snapshot.availableExperience);

                var flagEntries = snapshot.flags ?? new List<SerializedFlagEntry>();
                var restoredFlags = new List<KeyValuePair<string, bool>>(flagEntries.Count);
                foreach (var flag in flagEntries)
                {
                    if (flag == null || string.IsNullOrEmpty(flag.key))
                        continue;
                    restoredFlags.Add(new KeyValuePair<string, bool>(flag.key, flag.value));
                }
                progressController.OverwriteFlags(restoredFlags);

                var learnedSymbols = snapshot.learnedSymbols ?? new List<int>();
                progressController.OverwriteKnownSymbols(learnedSymbols);
            }

            if (questManager != null)
            {
                var questEntries = snapshot.quests ?? new List<SerializedQuestProgress>();
                var restoredQuests = new List<QuestProgress>(questEntries.Count);
                foreach (var serialized in questEntries)
                {
                    var quest = DeserializeQuest(serialized);
                    if (quest != null)
                        restoredQuests.Add(quest);
                }

                questManager.OverwriteActiveQuests(restoredQuests);

                if (progressController != null)
                    progressController.EvaluateQuests();
            }

            PlayerLoadedFromSave?.Invoke();
        }

        private void ApplySavedTransform(PlayerProgressData snapshot)
        {
            if (snapshot == null || !snapshot.hasPlayerTransform)
                return;

            var target = transform;
            if (target == null)
                return;

            CharacterController controller = null;
            bool controllerWasEnabled = false;

            if (TryGetComponent(out controller))
            {
                controllerWasEnabled = controller.enabled;
                controller.enabled = false;
            }

            target.SetPositionAndRotation(snapshot.playerPosition, snapshot.playerRotation);

            var body = target.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (controller != null)
                controller.enabled = controllerWasEnabled;
        }

        private int ResolvePlayerLevel()
        {
            if (progressController == null)
                return 1;

            int aggregatedLevel = 0;
            bool hasJob = false;
            foreach (var job in progressController.GetAllJobs())
            {
                if (job == null)
                    continue;
                hasJob = true;
                aggregatedLevel += Mathf.Max(1, job.CurrentLevel);
            }

            if (hasJob && aggregatedLevel > 0)
                return aggregatedLevel;

            return 1;
        }

        private static SerializedQuestProgress SerializeQuest(QuestProgress source)
        {
            if (source == null || string.IsNullOrEmpty(source.questId))
                return null;

            var serialized = new SerializedQuestProgress
            {
                questId = source.questId,
                stageIndex = source.stageIndex,
                state = source.state
            };

            if (source.stages == null)
                return serialized;

            foreach (var stage in source.stages)
            {
                if (stage == null)
                    continue;

                var stageData = new SerializedStageProgress
                {
                    stageId = stage.stageId,
                    completed = stage.completed
                };

                if (stage.objectives != null)
                {
                    foreach (var objective in stage.objectives)
                    {
                        if (objective == null)
                            continue;

                        stageData.objectives.Add(new SerializedObjectiveProgress
                        {
                            objectiveId = objective.objectiveId,
                            currentCount = objective.currentCount,
                            completed = objective.completed
                        });
                    }
                }

                serialized.stages.Add(stageData);
            }

            return serialized;
        }

        private static QuestProgress DeserializeQuest(SerializedQuestProgress source)
        {
            if (source == null || string.IsNullOrEmpty(source.questId))
                return null;

            var quest = new QuestProgress
            {
                questId = source.questId,
                stageIndex = source.stageIndex,
                state = source.state,
                stages = new List<StageProgress>()
            };

            if (source.stages == null)
                return quest;

            foreach (var stageData in source.stages)
            {
                if (stageData == null)
                    continue;

                var stage = new StageProgress
                {
                    stageId = stageData.stageId,
                    completed = stageData.completed,
                    objectives = new List<ObjectiveProgress>()
                };

                if (stageData.objectives != null)
                {
                    foreach (var objectiveData in stageData.objectives)
                    {
                        if (objectiveData == null)
                            continue;

                        stage.objectives.Add(new ObjectiveProgress
                        {
                            objectiveId = objectiveData.objectiveId,
                            currentCount = objectiveData.currentCount,
                            completed = objectiveData.completed
                        });
                    }
                }

                quest.stages.Add(stage);
            }

            return quest;
        }
    }
}
