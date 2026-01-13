using UnityEngine;
using Player.Events;
using Player.Progress;
using Player.Statistics;
using Player.Targeting;
using Inventory;

namespace UI.Player.Common
{
    public sealed class PlayerUIReferenceBinder : MonoBehaviour
    {
        [SerializeField] private bool includeInactive = true;

        private GameObject currentPlayer;

        public void BindPlayer(GameObject player)
        {
            if (player == currentPlayer && player != null)
                return;

            currentPlayer = player;
            var refs = player != null
                ? new PlayerUIReferences(player, includeInactive)
                : PlayerUIReferences.Empty;

            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                if (behaviour is IPlayerReferenceReceiver receiver)
                    receiver.BindPlayerReferences(refs);
            }
        }

    }

    public readonly struct PlayerUIReferences
    {
        public static PlayerUIReferences Empty => default;

        public PlayerUIReferences(GameObject player, bool includeInactive)
        {
            Player = player;
            Stats = player != null ? player.GetComponentInChildren<StatsController>(includeInactive) : null;
            Progress = player != null ? player.GetComponentInChildren<ProgressController>(includeInactive) : null;
            EventHub = player != null ? player.GetComponentInChildren<PlayerEventHub>(includeInactive) : null;
            Inventory = player != null ? player.GetComponentInChildren<InventoryController>(includeInactive) : null;
            Equipment = player != null ? player.GetComponentInChildren<EquipmentController>(includeInactive) : null;
            TargetSelector = player != null ? player.GetComponentInChildren<TargetSelector>(includeInactive) : null;
        }

        public GameObject Player { get; }
        public StatsController Stats { get; }
        public ProgressController Progress { get; }
        public PlayerEventHub EventHub { get; }
        public InventoryController Inventory { get; }
        public EquipmentController Equipment { get; }
        public TargetSelector TargetSelector { get; }
    }

    public interface IPlayerReferenceReceiver
    {
        void BindPlayerReferences(PlayerUIReferences refs);
    }
}
