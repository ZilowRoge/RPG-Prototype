using System;
using Items;
using UnityEngine;

namespace Inventory
{
    [Serializable]
    public class EquipmentSlotEntry
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private ItemInstance itemInstance;

        public EquipmentSlotEntry()
        {
        }

        public EquipmentSlotEntry(EquipmentSlot slot)
        {
            this.slot = slot;
        }

        public EquipmentSlot Slot => slot;
        public ItemInstance ItemInstance => itemInstance;
        public bool IsEmpty => itemInstance == null || itemInstance.IsEmpty;

        public void SetItem(ItemInstance instance)
        {
            itemInstance = instance;
        }

        public void Clear()
        {
            itemInstance = null;
        }
    }
}
