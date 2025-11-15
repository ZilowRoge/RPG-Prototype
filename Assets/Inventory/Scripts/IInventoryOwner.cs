using Items;

namespace Inventory
{
    /// <summary>
    /// Contracts access to an inventory data container.
    /// </summary>
    public interface IInventoryOwner
    {
        Inventory Inventory { get; set; }
        bool TryAddItem(ItemDefinition definition, int amount);
        bool TryRemoveItem(ItemDefinition definition, int amount);
        bool TryMoveItem(int sourceIndex, int targetIndex);
    }
}
