using UnityEngine;
using Common.UI;
using Player.Events;
using Player.Interfaces;
using Player.Progress;
using Player.Statistics;
using Player.Targeting;
using Inventory;

namespace UI.Player.Common
{
    public sealed class PlayerUIReferenceBinder : MonoBehaviour, IPlayerUiBinder
    {
        [SerializeField] private bool includeInactive = true;

        private GameObject currentPlayer;

        public void BindPlayer(GameObject player)
        {
            if (player == currentPlayer && player != null)
                return;

            currentPlayer = player;
            var stats = player != null ? player.GetComponentInChildren<StatsController>(includeInactive) : null;
            var progress = player != null ? player.GetComponentInChildren<ProgressController>(includeInactive) : null;
            var eventHub = player != null ? player.GetComponentInChildren<PlayerEventHub>(includeInactive) : null;
            var inventory = player != null ? player.GetComponentInChildren<InventoryController>(includeInactive) : null;
            var equipment = player != null ? player.GetComponentInChildren<EquipmentController>(includeInactive) : null;
            var targetSelector = player != null ? player.GetComponentInChildren<TargetSelector>(includeInactive) : null;

            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                if (behaviour is IDialogueProgressReceiver dialogueReceiver)
                    dialogueReceiver.BindDialogueProgress(progress as IDialogueProgressContext);

                if (behaviour is IInventoryReferenceReceiver inventoryReceiver)
                    inventoryReceiver.BindInventoryReferences(inventory, equipment);

                if (behaviour is IStatsControllerReceiver statsReceiver)
                    statsReceiver.BindStatsController(stats);

                if (behaviour is IProgressControllerReceiver progressReceiver)
                    progressReceiver.BindProgressController(progress);

                if (behaviour is IPlayerEventHubReceiver eventHubReceiver)
                    eventHubReceiver.BindPlayerEventHub(eventHub);

                if (behaviour is ITargetSelectorReceiver targetReceiver)
                    targetReceiver.BindTargetSelector(targetSelector);
            }
        }

    }

    public interface IStatsControllerReceiver
    {
        void BindStatsController(StatsController statsController);
    }

    public interface IProgressControllerReceiver
    {
        void BindProgressController(ProgressController progressController);
    }

    public interface IPlayerEventHubReceiver
    {
        void BindPlayerEventHub(PlayerEventHub eventHub);
    }

    public interface ITargetSelectorReceiver
    {
        void BindTargetSelector(TargetSelector targetSelector);
    }
}
