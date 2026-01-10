using System.Collections.Generic;
using Items;
using Player.Save;
using UnityEngine;
using System.Linq;

namespace Inventory
{
    /// <summary>
    /// MonoBehaviour wrapper that exposes an <see cref="Inventory"/> instance so it can be attached to players, NPCs, or containers.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryController : MonoBehaviour, IInventoryOwner
    {
        [Header("References")]
        [SerializeField] private Inventory inventory = new();
        [SerializeField] private EquipmentController equipmentController;
        [Header("Debug")]
        [SerializeField] private List<ItemDefinition> debugItems = new();
        [SerializeField] private bool initializeOnAwake = true;
        [Tooltip("Optional override. Use a positive value to force a specific slot count when initializing.")]
        [SerializeField] private int initialSlotCount = -1;

        private IItemUseContext itemUseContext;

        public Inventory Inventory
        {
            get => inventory;
            set => inventory = value;
        }

        public bool TryAddItem(ItemDefinition definition, int amount = 1) => inventory != null && inventory.TryAddItem(definition, amount);

        public bool TryAddItemInstance(ItemInstance instance) => inventory != null && inventory.TryAddItemInstance(instance);

        public bool TryRemoveItem(ItemDefinition definition, int amount) => inventory != null && inventory.TryRemoveItem(definition, amount);

        public bool TryMoveItem(int sourceIndex, int targetIndex) => inventory != null && inventory.TryMoveItem(sourceIndex, targetIndex);

        public bool TryTransferItemTo(InventoryController target, int sourceIndex, int targetIndex = -1)
        {
            if (inventory == null || target == null || target.Inventory == null)
                return false;

            return inventory.TryTransferItem(sourceIndex, target.Inventory, targetIndex);
        }

        public bool TryUseItem(int slotIndex)
        {
            return TryUseItem(slotIndex, null);
        }

        public bool TryUseEquippedConsumable(EquipmentSlot slot)
        {
            if (equipmentController == null)
            {
                Debug.LogWarning("[InventoryController] Missing equipment controller reference.", this);
                return false;
            }

            var context = GetOrResolveItemUseContext();
            if (context == null)
            {
                Debug.LogWarning("[InventoryController] Missing IItemUseContext reference.", this);
                return false;
            }

            if (!EquipmentController.IsConsumableSlot(slot))
            {
                Debug.LogWarning($"[InventoryController] Slot {slot} is not a consumable slot.");
                return false;
            }

            return context.TryUseEquippedConsumable(equipmentController, slot);
        }

        public bool TryUseItem(int slotIndex, EquipmentSlot? preferredEquipmentSlot)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[InventoryController] No inventory assigned.");
                return false;
            }

            var context = GetOrResolveItemUseContext();
            if (context == null)
            {
                Debug.LogWarning("[InventoryController] Missing IItemUseContext reference.", this);
                return false;
            }

            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            {
                Debug.LogWarning($"[InventoryController] Slot index {slotIndex} out of range.");
                return false;
            }

            Debug.Log($"[InventoryController] Requesting use of slot {slotIndex}.");
            var request = new ItemUseRequest
            {
                InventoryData = inventory,
                SlotIndex = slotIndex,
                EquipmentController = equipmentController,
                PreferredEquipmentSlot = preferredEquipmentSlot
            };

            var success = context.TryUseItem(request);
            if (!success)
            {
                Debug.LogWarning($"[InventoryController] Item use request for slot {slotIndex} failed.");
            }

            return success;
        }

        public bool TryUnequipItem(EquipmentSlot slot)
        {
            return TryUnequipItem(slot, -1);
        }

        public bool TryUnequipItem(EquipmentSlot slot, int targetInventoryIndex)
        {
            if (inventory == null || equipmentController == null)
            {
                Debug.LogWarning("[InventoryController] Missing inventory or equipment controller reference.", this);
                return false;
            }

            if (!equipmentController.TryUnequip(slot, out var removedItem) || removedItem == null)
            {
                Debug.LogWarning($"[InventoryController] Failed to unequip slot {slot}.", this);
                return false;
            }

            if (targetInventoryIndex >= 0 && targetInventoryIndex < inventory.SlotCount)
            {
                var targetSlot = inventory.Slots[targetInventoryIndex];
                if (targetSlot.IsEmpty)
                {
                    targetSlot.SetItem(removedItem);
                    return true;
                }

                Debug.LogWarning("[InventoryController] Target slot occupied, cannot unequip.");
            }
            else if (inventory.TryAddItemInstance(removedItem))
            {
                return true;
            }

            Debug.LogWarning("[InventoryController] Inventory full, re-equipping item.");
            equipmentController.TryEquipItem(slot, removedItem, out _);
            return false;
        }

        private void Awake()
        {
            CacheDependencies();

            if (initializeOnAwake)
            {
                Initialize();
                SpawnDebugItems();
            }
        }

        private void Reset()
        {
            equipmentController = GetComponent<EquipmentController>();
        }

        public void Initialize(int slotCount = -1)
        {
            var count = slotCount > 0 ? slotCount : initialSlotCount;
            inventory.InitializeSlots(count);
        }

        private void SpawnDebugItems()
        {
            if (SaveState.IsRestoring)
                return;

            if (inventory == null || debugItems == null || debugItems.Count == 0)
                return;

            // Skip spawning if inventory already has items (e.g., loaded from save).
            if (inventory.Slots != null)
            {
                foreach (var slot in inventory.Slots)
                {
                    if (slot != null && !slot.IsEmpty)
                        return;
                }
            }

            foreach (var item in debugItems)
            {
                if (item == null)
                    continue;

                TryAddItem(item);
            }
        }

        private void CacheDependencies()
        {
            if (equipmentController == null)
                equipmentController = GetComponent<EquipmentController>();

            itemUseContext = ResolveItemUseContext();
        }

        private IItemUseContext ResolveItemUseContext()
        {
            var context = GetComponent<IItemUseContext>();
            if (context == null)
                Debug.LogWarning("InventoryController could not find IItemUseContext on the same GameObject.", this);
            return context;
        }

        private IItemUseContext GetOrResolveItemUseContext()
        {
            if (itemUseContext == null)
                itemUseContext = ResolveItemUseContext();

            return itemUseContext;
        }
    }
}
