using System;
using System.Collections.Generic;
using UnityEngine;
using Player.Progress;

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
        [SerializeField] List<QuestProgress> activeQuests = new();

        public QuestDatabase Database => database;

        public IReadOnlyList<QuestProgress> ActiveQuests => activeQuests;

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

        public void EvaluateAll(ProgressController progress)
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

                    if (IsObjectiveSatisfied(def, progress))
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

        bool IsObjectiveSatisfied(ObjectiveDef def, ProgressController progress)
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
                default:
                    return false;
            }
        }

        void TryAdvanceStage(QuestAsset asset, QuestProgress qp, ProgressController progress)
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
                if (asset.rewardXp > 0 && progress != null)
                    progress.GrantExperience(asset.rewardXp);
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
    }
}
