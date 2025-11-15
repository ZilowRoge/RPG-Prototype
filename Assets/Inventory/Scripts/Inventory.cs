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

        public bool TryUseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return false;
            }

            var slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                return false;
            }

            var definition = slot.ItemInstance.Definition;
            var itemName = definition != null ? definition.Name : slot.ItemInstance.InstanceId;
            Debug.Log($"Used item '{itemName}' from slot {slotIndex}.");
            return true;
        }
    }
}
