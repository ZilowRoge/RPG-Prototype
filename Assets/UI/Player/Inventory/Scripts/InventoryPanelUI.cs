using System.Collections.Generic;
using Inventory;
using Items;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [Header("Drag Visuals")]
        [SerializeField] private InventoryDragVisual dragVisual;
        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI tooltip;
        private System.Action refreshCallback;

        public IReadOnlyList<InventorySlotUI> SlotViews => slots;
        private int dragSourceIndex = -1;
        private bool isDragging;

        private void Awake()
        {
            EnsureSlotsRoot();
        }

        private void OnDisable()
        {
            tooltip?.Hide();
        }

        public void Refresh()
        {
            EnsureSlotsRoot();
            tooltip?.Hide();
            var inventory = inventoryController != null ? inventoryController.Inventory : null;

            if (inventory == null || slotPrefab == null || slotsRoot == null)
            {
                ClearSlots();
                dragSourceIndex = -1;
                isDragging = false;
                return;
            }

            EnsureSlotCount(inventory.SlotCount);
            var slotData = inventory.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                var slotView = slots[i];
                if (slotView == null)
                    continue;

                slotView.Configure(i, HandleSlotClick, HandleSlotDoubleClick, HandleBeginDrag, HandleDrop, HandleEndDrag, HandleDrag, HandlePointerEnter, HandlePointerExit);

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
                newSlot.Configure(slots.Count, HandleSlotClick, HandleSlotDoubleClick, HandleBeginDrag, HandleDrop, HandleEndDrag, HandleDrag, HandlePointerEnter, HandlePointerExit);
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
            ResetDragState();
        }

        internal void HandleSlotClick(InventorySlotUI slotView)
        {
            // Optional: could use clicks for context menus. Currently left empty.
        }

        private void HandleSlotDoubleClick(InventorySlotUI slotView)
        {
            if (inventoryController == null || slotView == null)
                return;

            var inventory = inventoryController.Inventory;
            if (inventory == null)
                return;

            int slotIndex = slotView.SlotId;
            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
                return;

            if (inventoryController.TryUseItem(slotIndex))
                RequestRefresh();
        }

        private void HandlePointerEnter(InventorySlotUI slotView, PointerEventData eventData)
        {
            if (tooltip == null || inventoryController == null || slotView == null)
                return;

            var inventory = inventoryController.Inventory;
            var item = GetItemFromSlot(inventory, slotView.SlotId);
            if (item == null)
            {
                tooltip.Hide();
                return;
            }

            var position = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            tooltip.Show(item, position);
        }

        private void HandlePointerExit(InventorySlotUI slotView, PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        private void HandleBeginDrag(InventorySlotUI slotView, PointerEventData eventData)
        {
            if (inventoryController == null || slotView == null)
                return;

            var inventory = inventoryController.Inventory;
            if (inventory == null)
                return;

            int index = slotView.SlotId;
            if (index < 0 || index >= inventory.SlotCount)
                return;

            var slotData = inventory.Slots[index];
            if (slotData.IsEmpty)
                return;

            tooltip?.Hide();
            dragSourceIndex = index;
            isDragging = true;
            var icon = slotData.ItemInstance.Definition?.Icon;
            dragVisual?.Show(icon);
            dragVisual?.UpdatePosition(eventData);
        }

        private void HandleDrop(InventorySlotUI targetSlot, PointerEventData eventData)
        {
            if (inventoryController == null || targetSlot == null)
            {
                ResetDragState();
                return;
            }

            var pointer = eventData?.pointerDrag;
            if (pointer == null)
            {
                ResetDragState();
                return;
            }

            var inventorySource = pointer.GetComponent<InventorySlotUI>();
            if (inventorySource != null)
            {
                HandleInventoryDrop(targetSlot, inventorySource);
                return;
            }

            var equipmentSource = pointer.GetComponent<EquipmentSlotUI>();
            if (equipmentSource != null)
            {
                HandleEquipmentItemDrop(equipmentSource, targetSlot);
                return;
            }

            ResetDragState();
        }

        private void HandleInventoryDrop(InventorySlotUI targetSlot, InventorySlotUI sourceSlot)
        {
            if (!isDragging)
            {
                ResetDragState();
                return;
            }

            var inventory = inventoryController.Inventory;
            if (inventory == null)
            {
                ResetDragState();
                return;
            }

            int targetIndex = targetSlot.SlotId;
            int sourceIndex = sourceSlot.SlotId;
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            {
                ResetDragState();
                return;
            }

            bool moved = inventoryController.TryMoveItem(sourceIndex, targetIndex);
            ResetDragState();

            if (moved)
                RequestRefresh();
        }

        private void HandleEquipmentItemDrop(EquipmentSlotUI equipmentSlot, InventorySlotUI targetSlot)
        {
            if (inventoryController == null || equipmentSlot == null || targetSlot == null)
                return;

            if (inventoryController.TryUnequipItem(equipmentSlot.Slot, targetSlot.SlotId))
            {
                RequestRefresh();
            }
        }

        private void HandleEndDrag()
        {
            ResetDragState();
        }

        private void HandleDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            dragVisual?.UpdatePosition(eventData);
        }

        private void ResetDragState()
        {
            dragSourceIndex = -1;
            isDragging = false;
            dragVisual?.Hide();
            tooltip?.Hide();
        }

        public void SetRefreshCallback(System.Action callback)
        {
            refreshCallback = callback;
        }

        public void BeginExternalDrag(Sprite icon, PointerEventData eventData)
        {
            if (icon == null)
            {
                dragVisual?.Hide();
                return;
            }

            dragVisual?.Show(icon);
            dragVisual?.UpdatePosition(eventData);
        }

        public void UpdateExternalDrag(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            dragVisual?.UpdatePosition(eventData);
        }

        public void EndExternalDrag()
        {
            dragVisual?.Hide();
        }

        private static ItemInstance GetItemFromSlot(InventoryData inventory, int slotIndex)
        {
            if (inventory == null || slotIndex < 0 || slotIndex >= inventory.SlotCount)
                return null;

            var slot = inventory.Slots[slotIndex];
            return slot != null && !slot.IsEmpty ? slot.ItemInstance : null;
        }

        private void RequestRefresh()
        {
            if (refreshCallback != null)
                refreshCallback.Invoke();
            else
                Refresh();
        }
    }
}
