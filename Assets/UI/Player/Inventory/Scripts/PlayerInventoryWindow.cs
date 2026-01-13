using UI.Player;
using UnityEngine;
using InventoryData = global::Inventory.Inventory;
using UI.Player.Common;

namespace UI.Player.Inventory
{
    public class PlayerInventoryWindow : PlayerWindowBase, IPlayerReferenceReceiver
    {
        [SerializeField] private InventoryPanelUI inventoryPanel;
        [SerializeField] private EquipmentPanelUI equipmentPanel;
        private bool dependenciesInitialized;

        private void EnsureDependencies()
        {
            if (dependenciesInitialized)
                return;

            if (inventoryPanel == null)
                inventoryPanel = GetComponentInChildren<InventoryPanelUI>(true);
            if (equipmentPanel == null)
                equipmentPanel = GetComponentInChildren<EquipmentPanelUI>(true);

            if (inventoryPanel != null)
            {
                inventoryPanel.SetRefreshCallback(HandlePanelsRefreshRequest);
            }

            if (equipmentPanel != null)
            {
                equipmentPanel.SetRefreshCallback(HandlePanelsRefreshRequest);
            }

            dependenciesInitialized = true;
        }

        protected override void Awake()
        {
            EnsureDependencies();
            base.Awake();
        }

        protected override void OnShow()
        {
            EnsureDependencies();
            base.OnShow();
            HandlePanelsRefreshRequest();
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            EnsureDependencies();

            var inventory = refs.Inventory;
            var equipment = refs.Equipment;

            if (inventoryPanel != null)
                inventoryPanel.SetInventoryController(inventory);

            if (equipmentPanel != null)
                equipmentPanel.SetControllers(equipment, inventory, inventoryPanel);

            if (IsVisible)
                HandlePanelsRefreshRequest();
        }

        private void HandlePanelsRefreshRequest()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.Refresh();
            }

            if (equipmentPanel != null)
            {
                equipmentPanel.Refresh();
            }
        }
    }
}
