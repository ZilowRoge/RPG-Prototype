using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData
{
    [Serializable]
    public class PlayerInventoryData
    {
        public int inventorySlotCount;
        public List<SerializedInventorySlot> inventorySlots = new();
        public List<SerializedEquipmentSlot> equipmentSlots = new();
    }

    [Serializable]
    public class SerializedInventorySlot
    {
        public int slotId;
        public SerializedItemInstance item;
    }

    [Serializable]
    public class SerializedEquipmentSlot
    {
        public string slot;
        public SerializedItemInstance item;
    }

    [Serializable]
    public class SerializedItemInstance
    {
        public string itemId;
        public int stackCount;
        public string instanceId;
        public List<SerializedItemModifier> modifiers = new();
    }

    [Serializable]
    public class SerializedItemModifier
    {
        public string stat;
        public string mode;
        public float value;
    }
}
