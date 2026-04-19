using Inventory;

namespace Crafting
{
    public interface ICraftingWindow
    {
        void OpenForStation(CraftingStation station, InventoryController playerInventory, string recipeId = null);
    }
}
