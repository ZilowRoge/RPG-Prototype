namespace Inventory
{
    public interface IInventoryReferenceReceiver
    {
        void BindInventoryReferences(InventoryController inventory, EquipmentController equipment);
    }
}
