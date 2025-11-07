namespace Player.FightSystem.Magic {
public interface ISymbolConsumer
{
    void OnSymbolRecognized(string symbolId);
    void OnSymbolSequenceCommitted();
}
}
