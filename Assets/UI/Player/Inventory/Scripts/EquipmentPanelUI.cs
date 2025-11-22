using System.Collections.Generic;
using Inventory;
using Items;
using UnityEngine;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Displays the contents of an <see cref="EquipmentController"/> in the UI.
    /// </summary>
    public class EquipmentPanelUI : MonoBehaviour
    {
        [Header("Equipment Source")]
        [SerializeField] private EquipmentController equipmentController;

        [Header("UI")]
        [SerializeField] private List<EquipmentSlotUI> slotViews = new();

        private readonly Dictionary<EquipmentSlot, EquipmentSlotUI> slotLookup = new();

        public void Refresh()
        {
            if (equipmentController == null)
            {
                ClearSlots();
                return;
            }

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
                    existing.Configure(entry.Slot);
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
        }
    }
}
