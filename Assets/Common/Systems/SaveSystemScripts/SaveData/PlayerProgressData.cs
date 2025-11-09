using System;
using System.Collections.Generic;
using Quests;
using UnityEngine;

namespace Systems.SaveSystem.SaveData
{
    [Serializable]
    public class PlayerProgressData
    {
        public int availableExperience;
        public List<SerializedFlagEntry> flags = new();
        public List<int> learnedSymbols = new();
        public List<SerializedQuestProgress> quests = new();
        public bool hasPlayerTransform;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
    }

    [Serializable]
    public class SerializedFlagEntry
    {
        public string key;
        public bool value;

        public SerializedFlagEntry() { }

        public SerializedFlagEntry(string key, bool value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    public class SerializedQuestProgress
    {
        public string questId;
        public int stageIndex;
        public QuestState state;
        public List<SerializedStageProgress> stages = new();
    }

    [Serializable]
    public class SerializedStageProgress
    {
        public string stageId;
        public bool completed;
        public List<SerializedObjectiveProgress> objectives = new();
    }

    [Serializable]
    public class SerializedObjectiveProgress
    {
        public string objectiveId;
        public int currentCount;
        public bool completed;
    }
}
