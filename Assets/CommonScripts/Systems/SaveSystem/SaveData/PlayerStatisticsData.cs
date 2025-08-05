using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class PlayerStatisticsData {
        public float currentHealth;
        public float currentMana;
        public float currentStamina;
        public List<SerializedStatEntry> stats;
    }

    [Serializable]
    public class SerializedStatEntry {
        public string key;
        public int value;
    }
}