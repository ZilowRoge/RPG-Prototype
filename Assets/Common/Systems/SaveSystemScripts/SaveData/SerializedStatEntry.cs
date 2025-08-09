using System;
using System.Collections.Generic;
using Systems.Jobs;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class SerializedStatEntry {
        public string key;
        public int value;

        public SerializedStatEntry(string key, int value) {
            this.key = key;
            this.value = value;
        }
    }
}