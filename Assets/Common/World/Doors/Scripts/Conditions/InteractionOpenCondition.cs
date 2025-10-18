using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Doors/Conditions/Interaction Only")]
    public class InteractionOpenCondition : MonoBehaviour, IDoorOpenCondition
    {
        [SerializeField] private string id = string.Empty; // optional identifier
        public string Id => id;
        public bool CanOpen(GameObject player) => true;
    }
}
