using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Inventory
{
    [System.Serializable]
    public class Inventory
    {
        [SerializeField] private List<Slot> slots = new();

        public IReadOnlyList<Slot> Slots => slots;
        public int SlotCount => slots.Count;
        public int Capacity => slots.Count;

        public void InitializeSlots(int slotCount = -1)
        {
            var targetCount = slotCount > 0 ? slotCount : slots.Count;
            if (targetCount <= 0)
                targetCount = 1;

            slots.Clear();

            for (int i = 0; i < targetCount; i++)
            {
                slots.Add(new Slot(i));
            }
        }

        public bool TryAddItem(ItemDefinition definition, int amount)
        {
            if (definition == null || amount <= 0)
            {
                return false;
            }

            var remaining = amount;

            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.ItemInstance.Definition != definition)
                {
                    continue;
                }

                var instance = slot.ItemInstance;
                var freeSpace = definition.MaxStack - instance.StackCount;
                if (freeSpace <= 0)
                {
                    continue;
                }

                var toTransfer = Mathf.Min(remaining, freeSpace);
                instance.SetStackCount(instance.StackCount + toTransfer);
                remaining -= toTransfer;

                if (remaining <= 0)
                {
                    return true;
                }
            }

            foreach (var slot in slots)
            {
                if (!slot.IsEmpty)
                {
                    continue;
                }

                var toPlace = Mathf.Min(remaining, definition.MaxStack);
                slot.SetItem(new ItemInstance(definition, toPlace));
                remaining -= toPlace;

                if (remaining <= 0)
                {
                    return true;
                }
            }

            return remaining <= 0;
        }

        public bool TryAddItemInstance(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                return false;
            }

            var definition = instance.Definition;

            if (definition.MaxStack > 1 && instance.StackCount > 0)
            {
                foreach (var slot in slots)
                {
                    if (slot.IsEmpty || slot.ItemInstance.Definition != definition)
                    {
                        continue;
                    }

                    var slotInstance = slot.ItemInstance;
                    var freeSpace = definition.MaxStack - slotInstance.StackCount;
                    if (freeSpace <= 0)
                    {
                        continue;
                    }

                    var transfer = Mathf.Min(instance.StackCount, freeSpace);
                    slotInstance.SetStackCount(slotInstance.StackCount + transfer);
                    instance.SetStackCount(instance.StackCount - transfer);

                    if (instance.StackCount <= 0)
                    {
                        return true;
                    }
                }
            }

            foreach (var slot in slots)
            {
                if (!slot.IsEmpty)
                {
                    continue;
                }

                slot.SetItem(instance);
                return true;
            }

            return false;
        }

        public bool TryRemoveItem(ItemDefinition definition, int amount)
        {
            if (definition == null || amount <= 0)
            {
                return false;
            }

            var totalAvailable = 0;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.ItemInstance.Definition != definition)
                {
                    continue;
                }

                totalAvailable += slot.ItemInstance.StackCount;
                if (totalAvailable >= amount)
                {
                    break;
                }
            }

            if (totalAvailable < amount)
            {
                return false;
            }

            var remaining = amount;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.ItemInstance.Definition != definition)
                {
                    continue;
                }

                var instance = slot.ItemInstance;
                var toRemove = Mathf.Min(remaining, instance.StackCount);
                instance.SetStackCount(instance.StackCount - toRemove);
                remaining -= toRemove;

                if (instance.StackCount <= 0)
                {
                    slot.Clear();
                }

                if (remaining <= 0)
                {
                    return true;
                }
            }

            return remaining <= 0;
        }

        public bool TryMoveItem(int sourceIndex, int targetIndex)
        {
            if (sourceIndex == targetIndex ||
                sourceIndex < 0 || sourceIndex >= slots.Count ||
                targetIndex < 0 || targetIndex >= slots.Count)
            {
                return false;
            }

            var source = slots[sourceIndex];
            var target = slots[targetIndex];

            if (source.IsEmpty)
            {
                return false;
            }

            if (target.IsEmpty)
            {
                target.SetItem(source.ItemInstance);
                source.Clear();
                return true;
            }

            var sourceInstance = source.ItemInstance;
            var targetInstance = target.ItemInstance;

            if (sourceInstance != null && targetInstance != null &&
                sourceInstance.Definition == targetInstance.Definition)
            {
                var maxStack = sourceInstance.Definition.MaxStack;
                var freeSpace = maxStack - targetInstance.StackCount;

                if (freeSpace > 0)
                {
                    var transfer = Mathf.Min(sourceInstance.StackCount, freeSpace);
                    targetInstance.SetStackCount(targetInstance.StackCount + transfer);
                    sourceInstance.SetStackCount(sourceInstance.StackCount - transfer);

                    if (sourceInstance.StackCount <= 0)
                    {
                        source.Clear();
                    }

                    return true;
                }
            }

            var temp = target.ItemInstance;
            target.SetItem(source.ItemInstance);
            source.SetItem(temp);
            return true;
        }

        public bool TryTransferItem(int sourceIndex, Inventory targetInventory, int targetIndex = -1)
        {
            if (targetInventory == null)
                return false;

            if (ReferenceEquals(this, targetInventory))
                return TryMoveItem(sourceIndex, targetIndex);

            if (sourceIndex < 0 || sourceIndex >= slots.Count)
                return false;

            var sourceSlot = slots[sourceIndex];
            if (sourceSlot == null || sourceSlot.IsEmpty)
                return false;

            var sourceItem = sourceSlot.ItemInstance;
            var definition = sourceItem?.Definition;
            if (definition == null)
                return false;

            var targetSlots = targetInventory.slots;
            if (targetSlots == null || targetSlots.Count == 0)
                return false;

            if (targetIndex >= 0)
            {
                if (targetIndex >= targetSlots.Count)
                    return false;

                var targetSlot = targetSlots[targetIndex];
                return TryTransferToSpecificSlot(sourceSlot, targetSlot, definition);
            }

            bool changed = false;
            if (definition.MaxStack > 1)
            {
                changed |= TryMergeIntoExistingStacks(sourceSlot, targetSlots, definition.MaxStack);
                if (sourceSlot.IsEmpty)
                    return true;
            }

            if (TryPlaceIntoFirstEmptySlot(sourceSlot, targetSlots))
                return true;

            return changed;
        }

        private static bool TryTransferToSpecificSlot(Slot sourceSlot, Slot targetSlot, ItemDefinition definition)
        {
            if (sourceSlot == null || targetSlot == null)
                return false;

            var sourceItem = sourceSlot.ItemInstance;
            if (sourceItem == null || definition == null)
                return false;

            if (targetSlot.IsEmpty)
            {
                targetSlot.SetItem(sourceItem);
                sourceSlot.Clear();
                return true;
            }

            var targetItem = targetSlot.ItemInstance;
            if (targetItem != null && targetItem.Definition == definition && definition.MaxStack > 1)
            {
                var freeSpace = definition.MaxStack - targetItem.StackCount;
                if (freeSpace <= 0)
                    return false;

                var transfer = Mathf.Min(sourceItem.StackCount, freeSpace);
                if (transfer <= 0)
                    return false;

                targetItem.SetStackCount(targetItem.StackCount + transfer);
                sourceItem.SetStackCount(sourceItem.StackCount - transfer);

                if (sourceItem.StackCount <= 0)
                    sourceSlot.Clear();

                return true;
            }

            var temp = targetSlot.ItemInstance;
            targetSlot.SetItem(sourceItem);
            sourceSlot.SetItem(temp);
            return true;
        }

        private static bool TryMergeIntoExistingStacks(Slot sourceSlot, List<Slot> targetSlots, int maxStack)
        {
            if (sourceSlot == null || targetSlots == null || targetSlots.Count == 0)
                return false;

            var sourceItem = sourceSlot.ItemInstance;
            var definition = sourceItem?.Definition;
            if (sourceItem == null || definition == null)
                return false;

            bool moved = false;
            foreach (var slot in targetSlots)
            {
                if (slot == null || slot.IsEmpty)
                    continue;

                var targetItem = slot.ItemInstance;
                if (targetItem == null || targetItem.Definition != definition)
                    continue;

                var free = maxStack - targetItem.StackCount;
                if (free <= 0)
                    continue;

                var transfer = Mathf.Min(sourceItem.StackCount, free);
                if (transfer <= 0)
                    continue;

                targetItem.SetStackCount(targetItem.StackCount + transfer);
                sourceItem.SetStackCount(sourceItem.StackCount - transfer);
                moved = true;

                if (sourceItem.StackCount <= 0)
                {
                    sourceSlot.Clear();
                    break;
                }
            }

            return moved;
        }

        private static bool TryPlaceIntoFirstEmptySlot(Slot sourceSlot, List<Slot> targetSlots)
        {
            if (sourceSlot == null || targetSlots == null)
                return false;

            var item = sourceSlot.ItemInstance;
            if (item == null || item.Definition == null)
                return false;

            foreach (var slot in targetSlots)
            {
                if (slot == null || !slot.IsEmpty)
                    continue;

                slot.SetItem(item);
                sourceSlot.Clear();
                return true;
            }

            return false;
        }

    }
}
