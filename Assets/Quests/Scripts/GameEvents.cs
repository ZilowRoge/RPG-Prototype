// Filename: GameEvents.cs
// Namespace: Quests
using System;
using UnityEngine;

namespace Quests
{
    public static class GameEvents
    {
        public static event Action<string> onQuestStarted;
        public static event Action<string, string, string, int, int> onQuestObjectiveProgressed;
        public static event Action<string, string, string> onQuestObjectiveCompleted;
        public static event Action<string, string> onQuestStageCompleted;
        public static event Action<string> onQuestCompleted;

        public static void EmitQuestStarted(string questId) => onQuestStarted?.Invoke(questId);
        public static void EmitQuestObjectiveProgressed(string questId, string stageId, string objectiveId, int current, int required) =>
            onQuestObjectiveProgressed?.Invoke(questId, stageId, objectiveId, current, required);
        public static void EmitQuestObjectiveCompleted(string questId, string stageId, string objectiveId) =>
            onQuestObjectiveCompleted?.Invoke(questId, stageId, objectiveId);
        public static void EmitQuestStageCompleted(string questId, string stageId) =>
            onQuestStageCompleted?.Invoke(questId, stageId);
        public static void EmitQuestCompleted(string questId) => onQuestCompleted?.Invoke(questId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            onQuestStarted = null;
            onQuestObjectiveProgressed = null;
            onQuestObjectiveCompleted = null;
            onQuestStageCompleted = null;
            onQuestCompleted = null;
        }
    }
}
