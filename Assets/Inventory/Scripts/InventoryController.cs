using Items;
using UnityEngine;
using System.Collections.Generic;

namespace Inventory
{
    /// <summary>
    /// MonoBehaviour wrapper that exposes an <see cref="Inventory"/> instance so it can be attached to players, NPCs, or containers.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryController : MonoBehaviour, IInventoryOwner
    {
        [SerializeField] private Inventory inventory = new();
        [Header("Debug")]
        [SerializeField] private List<ItemDefinition> debugItems = new();
        [SerializeField] private bool initializeOnAwake = true;
        [Tooltip("Optional override. Use a positive value to force a specific slot count when initializing.")]
        [SerializeField] private int initialSlotCount = -1;

        public Inventory Inventory
        {
            get => inventory;
            set => inventory = value;
        }

        public bool TryAddItem(ItemDefinition definition, int amount = 1) => inventory != null && inventory.TryAddItem(definition, amount);

        public bool TryRemoveItem(ItemDefinition definition, int amount) => inventory != null && inventory.TryRemoveItem(definition, amount);

        public bool TryMoveItem(int sourceIndex, int targetIndex) => inventory != null && inventory.TryMoveItem(sourceIndex, targetIndex);

        private void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
                SpawnDebugItems();
            }
        }

        public void Initialize(int slotCount = -1)
        {
            var count = slotCount > 0 ? slotCount : initialSlotCount;
            inventory.InitializeSlots(count);
        }

        private void SpawnDebugItems()
        {
            if (inventory == null || debugItems == null || debugItems.Count == 0)
                return;

            foreach (var item in debugItems)
            {
                if (item == null)
                    continue;

                TryAddItem(item);
            }
        }
    }
}
