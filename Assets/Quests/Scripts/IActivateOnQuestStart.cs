namespace Quests
{
    /// <summary>
    /// Implement on behaviours that should react when a specific quest starts.
    /// </summary>
    public interface IActivateOnQuestStart
    {
        void ActivateOnQuestStart(string questId);
    }
}
