// Filename: GameEvents.cs
// Namespace: Quests
using System;
using UnityEngine;

namespace Quests
{
    public static class GameEvents
    {
        public static event Action<string> onQuestStarted;
        public static event Action<string, string, string> onQuestObjectiveCompleted;
        public static event Action<string, string> onQuestStageCompleted;

        public static void EmitQuestStarted(string questId) => onQuestStarted?.Invoke(questId);
        public static void EmitQuestObjectiveCompleted(string questId, string stageId, string objectiveId) =>
            onQuestObjectiveCompleted?.Invoke(questId, stageId, objectiveId);
        public static void EmitQuestStageCompleted(string questId, string stageId) =>
            onQuestStageCompleted?.Invoke(questId, stageId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            onQuestStarted = null;
            onQuestObjectiveCompleted = null;
            onQuestStageCompleted = null;
        }
    }
}
