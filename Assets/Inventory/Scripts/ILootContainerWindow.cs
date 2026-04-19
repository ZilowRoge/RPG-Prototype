namespace Inventory
{
    public interface ILootContainerWindow
    {
        void Open(InventoryController playerInventory, InventoryController containerInventory, string containerName = null);
    }
}
