using Items;
using UnityEngine;

namespace Inventory
{
    [System.Serializable]
    public class Slot
    {
        [SerializeField] private int slotId;
        [SerializeField] private ItemInstance itemInstance;

        public Slot(int slotId, ItemInstance itemInstance = null)
        {
            this.slotId = slotId;
            this.itemInstance = itemInstance ?? new ItemInstance(null, 0);
        }

        public int SlotId => slotId;
        public ItemInstance ItemInstance => itemInstance;

        public bool IsEmpty => itemInstance == null || itemInstance.IsEmpty;

        public void SetItem(ItemInstance newItem)
        {
            itemInstance = newItem ?? new ItemInstance(null, 0);
        }

        public void Clear()
        {
            SetItem(null);
        }
    }
}
