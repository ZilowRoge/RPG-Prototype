using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Quests;
using UI.Player.Common;

namespace UI.Player.Quests
{
    public class QuestInfoUI : MonoBehaviour
    {
        [Header("Basic Info")]
        [SerializeField] private TMP_Text questTitleText;
        [SerializeField] private TMP_Text questDescriptionText;

        [Header("Objectives")]
        [SerializeField] private Transform objectivesContent;
        [SerializeField] private GameObject objectiveItemPrefab;
        [SerializeField] private string defaultObjectiveLabel = "Objective";
        [SerializeField] private string emptyObjectivesLabel = "No objectives";

        [Header("Rewards")]
        [SerializeField] private Transform rewardsContent;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private string emptyRewardsLabel = "No rewards";

        private DynamicListPool<QuestObjectiveItemUI> objectivePool;
        private DynamicListPool<QuestRewardItemUI> rewardPool;
        private readonly List<ObjectiveViewModel> objectiveBuffer = new();
        private readonly List<RewardViewModel> rewardBuffer = new();

        private void Awake()
        {
            EnsurePools();
        }

        public void Clear()
        {
            if (questTitleText != null) questTitleText.text = string.Empty;
            if (questDescriptionText != null) questDescriptionText.text = string.Empty;
            objectivePool?.Clear();
            rewardPool?.Clear();
        }

        public void Display(string questTitle, string questDescription, QuestAsset asset, StageDef stage, StageProgress stageProgress)
        {
            if (questTitleText != null) questTitleText.text = questTitle ?? string.Empty;
            if (questDescriptionText != null) questDescriptionText.text = questDescription ?? string.Empty;

            PopulateObjectives(asset, stage, stageProgress);
            PopulateRewards(asset);
        }

        private void PopulateObjectives(QuestAsset asset, StageDef stage, StageProgress stageProgress)
        {
            if (objectivesContent == null || objectiveItemPrefab == null)
            {
                Debug.LogWarning("[QuestInfoUI] Objectives references not assigned.", this);
                return;
            }

            EnsurePools();
            objectiveBuffer.Clear();

            StageDef activeStage = stage ?? FindStage(asset, stageProgress?.stageId);

            if (activeStage != null && activeStage.objectives != null)
            {
                var stageObjectives = activeStage.objectives;
                for (int i = 0; i < stageObjectives.Count; i++)
                {
                    var def = stageObjectives[i];
                    if (def == null || !def.visibleInJournal)
                        continue;

                    var progress = GetProgressFor(stageProgress, def, i);
                    string label = BuildObjectiveLabel(def, progress);
                    bool completed = progress != null && progress.completed;
                    AddObjectiveEntry(label, completed);
                }
            }

            if (objectiveBuffer.Count == 0 && stageProgress != null)
            {
                var objectives = stageProgress.objectives;
                if (objectives != null)
                {
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        var progress = objectives[i];
                        if (progress == null) continue;
                        var def = FindObjectiveDefinition(asset, activeStage, progress.objectiveId);
                        string label = def != null ? BuildObjectiveLabel(def, progress) : BuildProgressOnlyLabel(progress);
                        AddObjectiveEntry(label, progress.completed);
                    }
                }
            }

            if (objectiveBuffer.Count == 0)
                AddObjectiveEntry(emptyObjectivesLabel, false);

            objectivePool.Render(objectiveBuffer, (item, data, _) =>
            {
                item.Configure(data.Label, data.Completed);
            });
        }

        private void PopulateRewards(QuestAsset asset)
        {
            if (rewardsContent == null || rewardItemPrefab == null)
            {
                Debug.LogWarning("[QuestInfoUI] Rewards references not assigned.", this);
                return;
            }

            EnsurePools();
            rewardBuffer.Clear();

            if (asset != null)
            {
                if (asset.rewardXp > 0)
                {
                    AddRewardEntry($"{asset.rewardXp} XP");
                }

                if (!string.IsNullOrEmpty(asset.rewardNote))
                {
                    AddRewardEntry(asset.rewardNote);
                }
            }

            if (rewardBuffer.Count == 0)
                AddRewardEntry(emptyRewardsLabel);

            rewardPool.Render(rewardBuffer, (item, data, _) =>
            {
                item.SetText(data.Label);
            });
        }

        private void AddObjectiveEntry(string text, bool completed)
        {
            if (string.IsNullOrEmpty(text))
                text = defaultObjectiveLabel;

            objectiveBuffer.Add(new ObjectiveViewModel(text, completed));
        }

        private void AddRewardEntry(string text)
        {
            if (string.IsNullOrEmpty(text))
                text = emptyRewardsLabel;

            rewardBuffer.Add(new RewardViewModel(text));
        }

        private static ObjectiveProgress GetProgressFor(StageProgress stageProgress, ObjectiveDef def, int index)
        {
            if (stageProgress == null) return null;

            var objectives = stageProgress.objectives;
            if (objectives == null) return null;

            if (index >= 0 && index < objectives.Count)
            {
                var candidate = objectives[index];
                if (candidate != null && (string.IsNullOrEmpty(def.id) || candidate.objectiveId == def.id))
                    return candidate;
            }

            if (string.IsNullOrEmpty(def.id))
                return null;

            for (int i = 0; i < objectives.Count; i++)
            {
                var candidate = objectives[i];
                if (candidate != null && candidate.objectiveId == def.id)
                    return candidate;
            }

            return null;
        }

        private static StageDef FindStage(QuestAsset asset, string stageId)
        {
            if (asset == null || string.IsNullOrEmpty(stageId))
                return null;

            var stages = asset.stages;
            if (stages == null)
                return null;

            for (int i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage != null && stage.id == stageId)
                    return stage;
            }

            return null;
        }

        private static ObjectiveDef FindObjectiveDefinition(QuestAsset asset, StageDef stage, string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
                return null;

            if (stage != null && stage.objectives != null)
            {
                for (int i = 0; i < stage.objectives.Count; i++)
                {
                    var def = stage.objectives[i];
                    if (def != null && def.id == objectiveId)
                        return def;
                }
            }

            if (asset != null && asset.stages != null)
            {
                for (int i = 0; i < asset.stages.Count; i++)
                {
                    var candidateStage = asset.stages[i];
                    if (candidateStage == null || candidateStage.objectives == null) continue;
                    for (int j = 0; j < candidateStage.objectives.Count; j++)
                    {
                        var def = candidateStage.objectives[j];
                        if (def != null && def.id == objectiveId)
                            return def;
                    }
                }
            }

            return null;
        }

        private static string BuildObjectiveLabel(ObjectiveDef def, ObjectiveProgress progress)
        {
            string display = !string.IsNullOrEmpty(def.displayName) ? def.displayName :
                             !string.IsNullOrEmpty(def.extraId) ? def.extraId :
                             !string.IsNullOrEmpty(def.targetId) ? def.targetId :
                             def.id;

            int current = progress != null ? Mathf.Max(0, progress.currentCount) : 0;
            if (def.requiredCount > 1)
                display += $" ({Mathf.Clamp(current, 0, def.requiredCount)}/{def.requiredCount})";

            return string.IsNullOrEmpty(display) ? "Objective" : display;
        }

        private string BuildProgressOnlyLabel(ObjectiveProgress progress)
        {
            if (progress == null)
                return defaultObjectiveLabel;

            if (!string.IsNullOrEmpty(progress.objectiveId))
                return progress.objectiveId;

            return defaultObjectiveLabel;
        }

        private void EnsurePools()
        {
            if (objectivePool == null)
                objectivePool = new DynamicListPool<QuestObjectiveItemUI>(objectiveItemPrefab, objectivesContent);
            if (rewardPool == null)
                rewardPool = new DynamicListPool<QuestRewardItemUI>(rewardItemPrefab, rewardsContent);
        }

        private readonly struct ObjectiveViewModel
        {
            public ObjectiveViewModel(string label, bool completed)
            {
                Label = label;
                Completed = completed;
            }

            public string Label { get; }
            public bool Completed { get; }
        }

        private readonly struct RewardViewModel
        {
            public RewardViewModel(string label)
            {
                Label = label;
            }

            public string Label { get; }
        }
    }
}
