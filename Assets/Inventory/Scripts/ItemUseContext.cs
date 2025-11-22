using Items;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Basic implementation that handles equipping gear and consuming consumables.
    /// </summary>
    public class ItemUseContext : MonoBehaviour, IItemUseContext
    {
        private const string LogPrefix = "[ItemUseContext]";

        public bool TryUseItem(ItemUseRequest request)
        {
            if (request.InventoryData == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing inventory reference in request.");
                return false;
            }

            var inventory = request.InventoryData;
            var slotIndex = request.SlotIndex;

            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            {
                Debug.LogWarning($"{LogPrefix} Slot index {slotIndex} is out of range.");
                return false;
            }

            var slot = inventory.Slots[slotIndex];
            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"{LogPrefix} Slot {slotIndex} is empty.");
                return false;
            }

            var instance = slot.ItemInstance;
            var definition = instance?.Definition;
            if (definition == null)
            {
                Debug.LogWarning($"{LogPrefix} Item instance in slot {slotIndex} has no definition.");
                return false;
            }

            Debug.Log($"{LogPrefix} Trying to use '{definition.Name}' of type {definition.Type} from slot {slotIndex}.");
            switch (definition.Type)
            {
                case ItemType.Consumable:
                    return HandleConsumableUse(slot, definition.Name, slotIndex);
                case ItemType.Equipment:
                    return HandleEquipmentUse(request, slot);
                default:
                    Debug.LogWarning($"{LogPrefix} Item type {definition.Type} not supported.");
                    return false;
            }
        }

        public void HandleItemEquipped(EquipmentSlot slot, ItemInstance item)
        {
            // Placeholder for stat/buff integration.
        }

        public void HandleItemUnequipped(EquipmentSlot slot, ItemInstance item)
        {
            // Placeholder for stat/buff removal.
        }

        private static bool HandleConsumableUse(Slot slot, string itemName, int slotIndex)
        {
            var instance = slot.ItemInstance;
            if (instance == null)
                return false;

            instance.SetStackCount(instance.StackCount - 1);
            if (instance.StackCount <= 0)
            {
                slot.Clear();
            }

            Debug.Log($"{LogPrefix} Consumed '{itemName}' from slot {slotIndex}. Remaining stack: {instance.StackCount}.");
            return true;
        }

        private static bool HandleEquipmentUse(ItemUseRequest request, Slot slot)
        {
            if (request.EquipmentController == null)
            {
                Debug.LogWarning($"{LogPrefix} No EquipmentController available, cannot equip item.");
                return false;
            }

            var instance = slot.ItemInstance;
            var definition = instance?.Definition;
            if (definition == null)
                return false;

            var targetSlot = request.PreferredEquipmentSlot ?? ResolveEquipmentSlot(definition);
            if (!targetSlot.HasValue)
            {
                Debug.LogWarning($"{LogPrefix} Could not resolve target equipment slot for '{definition.Name}'.");
                return false;
            }

            Debug.Log($"{LogPrefix} Equipping '{definition.Name}' into slot {targetSlot.Value}.");
            if (!request.EquipmentController.TryEquipItem(targetSlot.Value, instance, out var replaced))
            {
                Debug.LogWarning($"{LogPrefix} Failed to equip '{definition.Name}' into slot {targetSlot.Value}.");
                return false;
            }

            if (replaced != null && !replaced.IsEmpty)
            {
                slot.SetItem(replaced);
                Debug.Log($"{LogPrefix} Replaced existing item; returned to inventory slot.");
            }
            else
            {
                slot.Clear();
                Debug.Log($"{LogPrefix} Slot cleared after equipping '{definition.Name}'.");
            }

            return true;
        }

        private static EquipmentSlot? ResolveEquipmentSlot(ItemDefinition definition)
        {
            var armor = definition.GetStatBlock<ArmorItemData>();
            if (armor != null)
                return armor.Slot;

            var weapon = definition.GetStatBlock<WeaponItemData>();
            if (weapon != null)
                return weapon.Slot;

            return null;
        }
    }
}
