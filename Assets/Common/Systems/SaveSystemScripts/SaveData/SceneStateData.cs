using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData
{
    [Serializable]
    public class SceneStateData
    {
        public string sceneId;
        public List<SerializedDoorState> doorStates = new List<SerializedDoorState>();
        public List<ContainerInventoryData> containerInventories = new List<ContainerInventoryData>();
    }
}
