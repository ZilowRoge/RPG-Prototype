// Filename: GameEvents.cs
// Namespace: Game.Quests
using System;
using UnityEngine;

namespace Quests
{
    public static class GameEvents
    {
        public static event Action<string> onJobAcquired;
        public static event Action<string> onSymbolLearned;
        public static event Action<string> onNpcTalked;
        public static event Action onRegisteredForExam;

        public static void EmitJobAcquired(string jobId) => onJobAcquired?.Invoke(jobId);
        public static void EmitSymbolLearned(string symbolId) => onSymbolLearned?.Invoke(symbolId);
        public static void EmitNpcTalked(string npcId) => onNpcTalked?.Invoke(npcId);
        public static void EmitRegisteredForExam() => onRegisteredForExam?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            onJobAcquired = null;
            onSymbolLearned = null;
            onNpcTalked = null;
            onRegisteredForExam = null;
        }
    }
}
