using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class GameData {
        public PlayerStatisticsData playerData = new PlayerStatisticsData();
        public PlayerProgressData progressData = new PlayerProgressData();
        public List<SerializedDoorState> doorStates = new List<SerializedDoorState>();
    }
}
