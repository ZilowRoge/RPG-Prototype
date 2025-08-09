namespace Systems.SaveSystem
{
    public interface ISaveable
    {
        void OnSave(SaveData.GameData data);
        void OnLoad(SaveData.GameData data);
    }
}