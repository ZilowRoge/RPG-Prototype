using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData
{
    [Serializable]
    public class ContainerInventoryData
    {
        public string containerId;
        public int slotCount;
        public List<SerializedInventorySlot> slots = new();
    }
}
