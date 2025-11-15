using System.Collections.Generic;
using UnityEngine;
using Inventory;
using InventoryData = global::Inventory.Inventory;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Responsible for presenting the player's inventory contents.
    /// </summary>
    public class InventoryPanelUI : MonoBehaviour
    {
        [Header("Inventory Source")]
        [SerializeField] private InventoryController inventoryController;

        [Header("UI")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private List<InventorySlotUI> slots = new();

        public IReadOnlyList<InventorySlotUI> SlotViews => slots;

        private void Awake()
        {
            EnsureSlotsRoot();
        }

        public void Refresh()
        {
            EnsureSlotsRoot();
            var inventory = inventoryController != null ? inventoryController.Inventory : null;

            if (inventory == null || slotPrefab == null || slotsRoot == null)
            {
                ClearSlots();
                return;
            }

            EnsureSlotCount(inventory.SlotCount);
            var slotData = inventory.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                var slotView = slots[i];
                if (slotView == null)
                    continue;

                slotView.Configure(i);

                var data = slotData[i];
                var icon = (!data.IsEmpty) ? data.ItemInstance.Definition?.Icon : null;
                slotView.SetIcon(icon);
                var stackCount = data.IsEmpty ? 0 : data.ItemInstance.StackCount;
                slotView.SetStackCount(stackCount);
            }
        }

        private void EnsureSlotsRoot()
        {
            if (slotsRoot == null)
                slotsRoot = transform;
        }

        private void EnsureSlotCount(int targetCount)
        {
            if (slotPrefab == null || slotsRoot == null)
                return;

            while (slots.Count < targetCount)
            {
                var slotObject = Instantiate(slotPrefab, slotsRoot);
                var newSlot = slotObject.GetComponent<InventorySlotUI>();
                if (newSlot == null)
                {
                    if (Application.isPlaying)
                        Destroy(slotObject);
                    else
                        DestroyImmediate(slotObject);
                    Debug.LogError("Slot prefab does not contain InventorySlotUI component.", this);
                    break;
                }
                slots.Add(newSlot);
            }

            while (slots.Count > targetCount)
            {
                var lastIndex = slots.Count - 1;
                var slot = slots[lastIndex];
                if (slot != null)
                {
                    if (Application.isPlaying)
                        Destroy(slot.gameObject);
                    else
                        DestroyImmediate(slot.gameObject);
                }
                slots.RemoveAt(lastIndex);
            }
        }

        private void ClearSlots()
        {
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (slot == null) continue;

                if (Application.isPlaying)
                    Destroy(slot.gameObject);
                else
                    DestroyImmediate(slot.gameObject);
            }

            slots.Clear();
        }

    }
}
