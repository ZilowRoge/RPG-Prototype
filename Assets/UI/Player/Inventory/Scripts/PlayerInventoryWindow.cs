using UI.Player;
using UnityEngine;
using InventoryData = global::Inventory.Inventory;

namespace UI.Player.Inventory
{
    public class PlayerInventoryWindow : PlayerWindowBase
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
