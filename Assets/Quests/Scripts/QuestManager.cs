using System;
using System.Collections.Generic;
using UnityEngine;
using Player.Interfaces;
using Inventory;
using Items;

namespace Quests
{
    public enum QuestState { Inactive, Active, Completed, Failed }

    [Serializable]
    public class ObjectiveProgress
    {
        public string objectiveId;
        public int currentCount;
        public bool completed;
    }

    [Serializable]
    public class StageProgress
    {
        public string stageId;
        public List<ObjectiveProgress> objectives = new List<ObjectiveProgress>();
        public bool completed;
    }

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public int stageIndex;
        public QuestState state;
        public List<StageProgress> stages = new List<StageProgress>();
    }

    public class QuestManager : MonoBehaviour
    {
        [SerializeField] QuestDatabase database;
        [SerializeField] InventoryController playerInventory;
        [SerializeField] List<QuestProgress> activeQuests = new();

        public QuestDatabase Database => database;

        public IReadOnlyList<QuestProgress> ActiveQuests => activeQuests;

        private void Awake()
        {
            CacheDependencies();
        }

        private void OnValidate()
        {
            CacheDependencies();
        }

        public void OverwriteActiveQuests(IEnumerable<QuestProgress> restored)
        {
            activeQuests.Clear();
            if (restored == null)
                return;

            foreach (var qp in restored)
            {
                var clone = CloneProgress(qp);
                if (clone != null)
                    activeQuests.Add(clone);
            }
        }

        public bool StartQuest(string questId)
        {
            var asset = FindAsset(questId);
            if (asset == null) return false;
            if (activeQuests.Exists(q => q.questId == questId && (q.state == QuestState.Active || q.state == QuestState.Completed))) return false;
            var qp = CreateProgress(asset);
            activeQuests.Add(qp);
            return true;
        }

        public bool IsQuestActive(string questId) => activeQuests.Exists(q => q.questId == questId && q.state == QuestState.Active);
        public bool IsQuestCompleted(string questId) => activeQuests.Exists(q => q.questId == questId && q.state == QuestState.Completed);
        public QuestProgress GetProgress(string questId) => activeQuests.Find(q => q.questId == questId);

        public void EvaluateAll(IQuestProgressContext progress)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                var qp = activeQuests[i];
                if (qp.state != QuestState.Active) continue;

                var asset = FindAsset(qp.questId);
                if (asset == null) continue;
                if (qp.stageIndex < 0 || qp.stageIndex >= asset.stages.Count) continue;

                var stageDef = asset.stages[qp.stageIndex];
                var stageProg = qp.stages[qp.stageIndex];

                bool anyChanged = false;
                for (int j = 0; j < stageDef.objectives.Count; j++)
                {
                    var def = stageDef.objectives[j];
                    var op = stageProg.objectives[j];
                    if (op.completed) continue;

                    if (IsObjectiveSatisfied(def, op, progress))
                    {
                        op.currentCount = Mathf.Max(op.currentCount, def.requiredCount);
                        op.completed = true;
                        GameEvents.EmitQuestObjectiveCompleted(qp.questId, stageDef.id, def.id);
                        anyChanged = true;
                    }
                }

                if (anyChanged) TryAdvanceStage(asset, qp, progress);
            }
        }

        QuestProgress CreateProgress(QuestAsset asset)
        {
            var qp = new QuestProgress { questId = asset.questId, stageIndex = 0, state = QuestState.Active, stages = new List<StageProgress>() };
            for (int i = 0; i < asset.stages.Count; i++)
            {
                var sd = asset.stages[i];
                var sp = new StageProgress { stageId = sd.id, objectives = new List<ObjectiveProgress>(), completed = false };
                for (int j = 0; j < sd.objectives.Count; j++)
                    sp.objectives.Add(new ObjectiveProgress { objectiveId = sd.objectives[j].id, currentCount = 0, completed = false });
                qp.stages.Add(sp);
            }
            return qp;
        }

        bool IsObjectiveSatisfied(ObjectiveDef def, ObjectiveProgress op, IQuestProgressContext progress)
        {
            switch (def.type)
            {
                case ObjectiveType.AcquireJob:
                    return string.IsNullOrEmpty(def.targetId) || progress.HasJob(def.targetId);
                case ObjectiveType.LearnSymbol:
                    if (string.IsNullOrEmpty(def.targetId) || def.targetId == "any") return progress.KnownSymbolCount > 0;
                        var symbolId = ParseSymbolId(def.targetId);
                        return symbolId >= 0 && progress.KnowsSymbol(symbolId);
                case ObjectiveType.FlagTrue:
                    return progress.GetFlag(def.targetId);
                case ObjectiveType.Elimination:
                    if (op == null)
                        return false;
                    int required = Mathf.Max(1, def.requiredCount);
                    return op.currentCount >= required;
                default:
                    return false;
            }
        }

        public void ReportElimination(IQuestProgressContext progress, string targetId, string extraId = null, int count = 1)
        {
            if (count <= 0 || activeQuests.Count == 0)
                return;

            bool anyProgressMade = false;

            for (int i = 0; i < activeQuests.Count; i++)
            {
                var qp = activeQuests[i];
                if (qp.state != QuestState.Active)
                    continue;

                var asset = FindAsset(qp.questId);
                if (asset == null)
                    continue;
                if (qp.stageIndex < 0 || qp.stageIndex >= asset.stages.Count)
                    continue;

                var stageDef = asset.stages[qp.stageIndex];
                if (stageDef.objectives == null || stageDef.objectives.Count == 0)
                    continue;

                var stageProg = qp.stages.Count > qp.stageIndex ? qp.stages[qp.stageIndex] : null;
                if (stageProg == null || stageProg.objectives == null || stageProg.objectives.Count == 0)
                    continue;

                bool stageUpdated = false;

                int objectiveCount = Mathf.Min(stageDef.objectives.Count, stageProg.objectives.Count);
                for (int j = 0; j < objectiveCount; j++)
                {
                    var def = stageDef.objectives[j];
                    if (def == null || def.type != ObjectiveType.Elimination)
                        continue;

                    var op = stageProg.objectives[j];
                    if (op == null || op.completed)
                        continue;

                    if (!MatchesCriterion(def.targetId, targetId))
                        continue;
                    if (!MatchesCriterion(def.extraId, extraId))
                        continue;

                    int required = Mathf.Max(1, def.requiredCount);
                    int previous = op.currentCount;
                    op.currentCount = Mathf.Clamp(previous + count, 0, required);

                    if (op.currentCount == previous)
                        continue;

                    anyProgressMade = true;
                    GameEvents.EmitQuestObjectiveProgressed(qp.questId, stageDef.id, def.id, op.currentCount, required);

                    if (op.currentCount >= required)
                    {
                        op.completed = true;
                        GameEvents.EmitQuestObjectiveCompleted(qp.questId, stageDef.id, def.id);
                        stageUpdated = true;
                    }
                }

                if (stageUpdated)
                {
                    TryAdvanceStage(asset, qp, progress);
                }
            }

            if (anyProgressMade && progress != null)
                EvaluateAll(progress);
        }

        void TryAdvanceStage(QuestAsset asset, QuestProgress qp, IQuestProgressContext progress)
        {
            var sp = qp.stages[qp.stageIndex];
            bool allDone = true;
            for (int i = 0; i < sp.objectives.Count; i++)
                if (!sp.objectives[i].completed) { allDone = false; break; }

            if (!allDone) return;

            sp.completed = true;
            GameEvents.EmitQuestStageCompleted(qp.questId, sp.stageId);
            qp.stageIndex++;
            if (qp.stageIndex >= asset.stages.Count)
            {
                qp.state = QuestState.Completed;
                GameEvents.EmitQuestCompleted(qp.questId);
                GrantRewards(asset, progress);
            }
        }

        QuestAsset FindAsset(string questId) => database != null ? database.Get(questId) : null;

        int ParseSymbolId(string symbolKey)
        {
            if (string.IsNullOrEmpty(symbolKey))
                return -1;

            if (symbolKey.StartsWith("sym_"))
                symbolKey = symbolKey.Substring(4);

            return int.TryParse(symbolKey, out var id) ? id : -1;
        }

        static bool MatchesCriterion(string expected, string candidate)
        {
            if (string.IsNullOrEmpty(expected) || expected == "any")
                return true;

            if (string.IsNullOrEmpty(candidate))
                return false;

            return string.Equals(expected, candidate, StringComparison.OrdinalIgnoreCase);
        }

        private static QuestProgress CloneProgress(QuestProgress source)
        {
            if (source == null || string.IsNullOrEmpty(source.questId))
                return null;

            var clone = new QuestProgress
            {
                questId = source.questId,
                stageIndex = source.stageIndex,
                state = source.state,
                stages = new List<StageProgress>()
            };

            if (source.stages == null)
                return clone;

            foreach (var stage in source.stages)
            {
                if (stage == null)
                    continue;

                var stageClone = new StageProgress
                {
                    stageId = stage.stageId,
                    completed = stage.completed,
                    objectives = new List<ObjectiveProgress>()
                };

                if (stage.objectives != null)
                {
                    foreach (var objective in stage.objectives)
                    {
                        if (objective == null)
                            continue;

                        stageClone.objectives.Add(new ObjectiveProgress
                        {
                            objectiveId = objective.objectiveId,
                            currentCount = objective.currentCount,
                            completed = objective.completed
                        });
                    }
                }

                clone.stages.Add(stageClone);
            }

            return clone;
        }

        void GrantRewards(QuestAsset asset, IQuestProgressContext progress)
        {
            if (asset == null)
                return;

            if (asset.rewardXp > 0 && progress != null)
                progress.GrantExperience(asset.rewardXp);

            if (asset.itemRewards == null || asset.itemRewards.Count == 0)
                return;

            var inventory = ResolveInventory(progress);
            if (inventory == null)
            {
                Debug.LogWarning("[QuestManager] Cannot grant item rewards because no InventoryController is assigned.", this);
                return;
            }

            for (int i = 0; i < asset.itemRewards.Count; i++)
            {
                var source = asset.itemRewards[i];
                if (source == null || source.Definition == null)
                    continue;

                int amount = Mathf.Max(1, source.StackCount);
                var copy = new ItemInstance(source.Definition, amount, null, source.Modifiers);

                bool added = inventory.TryAddItemInstance(copy);
                if (!added)
                    Debug.LogWarning($"[QuestManager] Failed to add quest reward item '{source.Definition.Name}' x{amount} to inventory.", this);
            }
        }

        InventoryController ResolveInventory(IQuestProgressContext progress)
        {
            if (playerInventory != null)
                return playerInventory;

            if (progress is Component progressComponent)
            {
                var ownedInventory = progressComponent.GetComponent<InventoryController>() ?? progressComponent.GetComponentInParent<InventoryController>();
                if (ownedInventory != null)
                {
                    playerInventory = ownedInventory;
                    return playerInventory;
                }
            }

            return null;
        }

        void CacheDependencies()
        {
            if (playerInventory == null)
                playerInventory = GetComponent<InventoryController>() ?? GetComponentInParent<InventoryController>();
        }
    }
}
