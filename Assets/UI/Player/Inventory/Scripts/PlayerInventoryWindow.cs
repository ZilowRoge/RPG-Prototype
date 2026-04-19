using UI.Player;
using UnityEngine;
using Inventory;
using InventoryData = global::Inventory.Inventory;

namespace UI.Player.Inventory
{
    public class PlayerInventoryWindow : PlayerWindowBase, IInventoryReferenceReceiver
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

        public void BindInventoryReferences(global::Inventory.InventoryController inventory, global::Inventory.EquipmentController equipment)
        {
            EnsureDependencies();

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
