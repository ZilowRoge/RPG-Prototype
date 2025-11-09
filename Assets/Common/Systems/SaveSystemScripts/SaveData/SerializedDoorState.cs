using System;

namespace Systems.SaveSystem.SaveData
{
    [Serializable]
    public class SerializedDoorState
    {
        public string doorId;
        public bool isOpen;
    }
}
