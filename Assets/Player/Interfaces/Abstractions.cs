namespace Player.Interfaces
{
    public interface IProgressReadOnly
    {
        bool HasJob(string jobId);
        bool KnowsSymbol(int symbolId);
        int KnownSymbolCount { get; }
        bool GetFlag(string key);
    }

    public interface IStatsReadOnly
    {
        float maxHealth { get; }
        float maxMana { get; }
        float maxStamina { get; }
        float walkSpeed { get; }
        float runSpeed { get; }
    }
}
