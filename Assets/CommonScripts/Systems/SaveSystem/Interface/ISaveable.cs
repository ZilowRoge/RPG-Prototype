namespace Systems.SaveSystem
{
    public interface ISaveable
    {
        void OnSave();
        void OnLoad();
    }
}