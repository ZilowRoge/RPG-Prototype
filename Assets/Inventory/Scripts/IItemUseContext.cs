using Items;

namespace Inventory
{
    /// <summary>
    /// Provides the environment required to consume or equip items.
    /// </summary>
    public interface IItemUseContext
    {
        bool TryUseItem(ItemUseRequest request);

        bool TryUseEquippedConsumable(EquipmentController equipmentController, EquipmentSlot slot);

        void HandleItemEquipped(EquipmentSlot slot, ItemInstance item);

        void HandleItemUnequipped(EquipmentSlot slot, ItemInstance item);
    }

    public struct ItemUseRequest
    {
        public Inventory InventoryData;
        public int SlotIndex;
        public EquipmentController EquipmentController;
        public EquipmentSlot? PreferredEquipmentSlot;
    }
}
