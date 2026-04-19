namespace Common.Symbols
{
    public interface ISymbolConsumer
    {
        void OnSymbolRecognized(string symbolId);
        void OnSymbolSequenceCommitted();
    }

    public interface ICancelableSymbolFlow
    {
        void CancelSymbolFlow();
    }

    public interface ISymbolInputRouter
    {
        ISymbolConsumer ActiveConsumer { get; }
        ISymbolConsumer DefaultCombatConsumer { get; }
        ISymbolConsumer SetActiveConsumer(ISymbolConsumer consumer);
        void ResetToDefaultConsumer();
    }
}
