using System.Collections.Generic;
using Inventory;
using Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Displays the contents of an <see cref="EquipmentController"/> in the UI.
    /// </summary>
    public class EquipmentPanelUI : MonoBehaviour
    {
        [Header("Equipment Source")]
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private InventoryPanelUI linkedInventoryPanel;

        [Header("UI")]
        [SerializeField] private List<EquipmentSlotUI> slotViews = new();
        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI tooltip;

        private readonly Dictionary<EquipmentSlot, EquipmentSlotUI> slotLookup = new();
        private System.Action refreshCallback;
        private bool isEquipmentDragging;

        private void Awake()
        {
            if (inventoryController == null && equipmentController != null)
            {
                inventoryController = equipmentController.GetComponent<InventoryController>();
            }

            if (linkedInventoryPanel == null)
            {
                var window = GetComponentInParent<PlayerInventoryWindow>();
                if (window != null)
                {
                    linkedInventoryPanel = window.GetComponentInChildren<InventoryPanelUI>(true);
                }
            }

            if (tooltip == null && linkedInventoryPanel != null)
            {
                tooltip = linkedInventoryPanel.GetComponentInChildren<ItemTooltipUI>(true);
            }
        }

        private void OnDisable()
        {
            tooltip?.Hide();
        }

        public void Refresh()
        {
            if (equipmentController == null)
            {
                ClearSlots();
                return;
            }

            tooltip?.Hide();
            EnsureSlotViews();
            if (equipmentController.Slots == null)
                return;

            foreach (var entry in equipmentController.Slots)
            {
                if (entry == null)
                    continue;

                if (!slotLookup.TryGetValue(entry.Slot, out var slotUI) || slotUI == null)
                    continue;

                var icon = entry.IsEmpty ? null : entry.ItemInstance?.Definition?.Icon;
                slotUI.SetIcon(icon);
                var stackCount = entry.IsEmpty ? 0 : entry.ItemInstance?.StackCount ?? 0;
                slotUI.SetStackCount(stackCount);
            }
        }

        private void EnsureSlotViews()
        {
            if (equipmentController == null)
                return;

            var sourceSlots = equipmentController.Slots;
            if (sourceSlots == null)
                return;

            if (slotViews == null)
                slotViews = new List<EquipmentSlotUI>();

            // Remove orphaned slot views (slots no longer defined).
            for (int i = slotViews.Count - 1; i >= 0; i--)
            {
                var view = slotViews[i];
                if (view == null)
                {
                    slotViews.RemoveAt(i);
                    continue;
                }

                if (!ContainsSlot(sourceSlots, view.Slot))
                {
                    if (Application.isPlaying)
                        Destroy(view.gameObject);
                    else
                        DestroyImmediate(view.gameObject);

                    slotViews.RemoveAt(i);
                }
            }

            slotLookup.Clear();
            foreach (var view in slotViews)
            {
                if (view == null)
                    continue;
                slotLookup[view.Slot] = view;
            }

            foreach (var entry in sourceSlots)
            {
                if (entry == null)
                    continue;

                if (slotLookup.TryGetValue(entry.Slot, out var existing) && existing != null)
                {
                    existing.Configure(entry.Slot, HandleSlotClick, HandleSlotDoubleClick, HandleBeginDrag, HandleSlotDrop, HandleEndDrag, HandleDrag, HandlePointerEnter, HandlePointerExit);
                    continue;
                }

                Debug.LogWarning($"Missing EquipmentSlotUI for slot {entry.Slot}. Please assign it.", this);
            }

            slotViews.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        }

        private static bool ContainsSlot(IEnumerable<EquipmentSlotEntry> entries, EquipmentSlot slot)
        {
            if (entries == null)
                return false;

            foreach (var entry in entries)
            {
                if (entry != null && entry.Slot == slot)
                    return true;
            }

            return false;
        }

        private void ClearSlots()
        {
            if (slotViews == null)
                return;

            for (int i = slotViews.Count - 1; i >= 0; i--)
            {
                var slotView = slotViews[i];
                if (slotView == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(slotView.gameObject);
                else
                    DestroyImmediate(slotView.gameObject);
            }

            slotViews.Clear();
            slotLookup.Clear();
            tooltip?.Hide();
        }

        private void HandleSlotClick(EquipmentSlotUI slotView)
        {
            // Reserved for future context actions.
        }

        private void HandleSlotDoubleClick(EquipmentSlotUI slotView)
        {
            if (slotView == null || inventoryController == null)
            {
                Debug.LogWarning("EquipmentPanelUI missing references for unequip.", this);
                return;
            }

            var item = equipmentController != null ? equipmentController.GetItem(slotView.Slot) : null;
            if (item != null && !item.IsEmpty && item.Definition != null && item.Definition.Type == ItemType.Consumable)
            {
                if (inventoryController.TryUseEquippedConsumable(slotView.Slot))
                {
                    RequestRefresh();
                }
                else
                {
                    Debug.LogWarning($"Failed to consume item from slot {slotView.Slot}.", this);
                }

                return;
            }

            if (inventoryController.TryUnequipItem(slotView.Slot))
            {
                RequestRefresh();
            }
            else
            {
                Debug.LogWarning($"Failed to unequip slot {slotView.Slot}.", this);
            }
        }

        private void HandleSlotDrop(EquipmentSlotUI slotView, PointerEventData eventData)
        {
            if (slotView == null || inventoryController == null || eventData == null)
                return;

            var inventorySlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
            if (inventorySlot == null)
                return;

            if (inventoryController.TryUseItem(inventorySlot.SlotId, slotView.Slot))
            {
                RequestRefresh();
            }
        }

        private void HandleBeginDrag(EquipmentSlotUI slotView, PointerEventData eventData)
        {
            if (slotView == null || equipmentController == null || linkedInventoryPanel == null)
                return;

            var item = equipmentController.GetItem(slotView.Slot);
            if (item == null || item.IsEmpty)
                return;

            tooltip?.Hide();
            isEquipmentDragging = true;
            linkedInventoryPanel.BeginExternalDrag(item.Definition?.Icon, eventData);
        }

        private void HandleDrag(PointerEventData eventData)
        {
            if (!isEquipmentDragging)
                return;

            linkedInventoryPanel?.UpdateExternalDrag(eventData);
        }

        private void HandleEndDrag()
        {
            if (!isEquipmentDragging)
                return;

            isEquipmentDragging = false;
            linkedInventoryPanel?.EndExternalDrag();
            tooltip?.Hide();
        }

        private void HandlePointerEnter(EquipmentSlotUI slotView, PointerEventData eventData)
        {
            if (tooltip == null || equipmentController == null || slotView == null)
                return;

            var item = equipmentController.GetItem(slotView.Slot);
            if (item == null || item.IsEmpty)
            {
                tooltip.Hide();
                return;
            }

            var position = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            tooltip.Show(item, position);
        }

        private void HandlePointerExit(EquipmentSlotUI slotView, PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        public void SetRefreshCallback(System.Action callback)
        {
            refreshCallback = callback;
        }

        public void SetControllers(EquipmentController equipment, InventoryController inventory, InventoryPanelUI inventoryPanel = null)
        {
            equipmentController = equipment;
            inventoryController = inventory != null ? inventory : equipmentController != null ? equipmentController.GetComponent<InventoryController>() : null;

            if (inventoryPanel != null)
                linkedInventoryPanel = inventoryPanel;

            if (tooltip == null && linkedInventoryPanel != null)
                tooltip = linkedInventoryPanel.GetComponentInChildren<ItemTooltipUI>(true);

            if (isActiveAndEnabled)
                Refresh();
        }

        private void RequestRefresh()
        {
            if (refreshCallback != null)
            {
                refreshCallback.Invoke();
            }
            else
            {
                Refresh();
                linkedInventoryPanel?.Refresh();
            }
        }
    }
}
