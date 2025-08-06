using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class PlayerStatisticsData {
        public float currentHealth;
        public float currentMana;
        public float currentStamina;
        public int level;
        public int experience;
        public int pendingStatPoints;

        public List<SerializedStatEntry> stats;
        public List<SerializedJobEntry> jobs;
    }

    [Serializable]
    public class SerializedStatEntry {
        public string key;
        public int value;
    }
    
    [Serializable]
    public class SerializedJobEntry
    {
        public string jobId;
        public int level;
        public int experience;
    }
}