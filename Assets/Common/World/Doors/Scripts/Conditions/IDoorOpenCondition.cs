using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    public interface IDoorOpenCondition
    {
        string Id { get; }
        bool CanOpen(GameObject player);
    }
}
