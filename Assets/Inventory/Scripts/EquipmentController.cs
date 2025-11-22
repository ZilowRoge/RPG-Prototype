using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Handles equipping and unequipping items for a character.
    /// </summary>
    public class EquipmentController : MonoBehaviour
    {
        [SerializeField] private List<EquipmentSlotEntry> equipmentSlots = new();

        public IReadOnlyList<EquipmentSlotEntry> Slots => equipmentSlots;

        private void Awake()
        {
            EnsureSlotSetup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureSlotSetup();
        }
#endif

        public ItemInstance GetItem(EquipmentSlot slot)
        {
            var entry = GetEntry(slot);
            return entry?.ItemInstance;
        }

        public bool TryEquipItem(ItemInstance instance, out ItemInstance replacedItem)
        {
            replacedItem = null;
            if (instance == null || instance.IsEmpty || instance.Definition == null)
            {
                return false;
            }

            if (instance.Definition.Type != ItemType.Equipment)
            {
                return false;
            }

            if (!TryResolveTargetSlot(instance.Definition, out var slot))
            {
                return false;
            }

            return TryEquipItem(slot, instance, out replacedItem);
        }

        public bool TryEquipItem(EquipmentSlot slot, ItemInstance instance, out ItemInstance replacedItem)
        {
            replacedItem = null;
            if (instance == null || instance.IsEmpty || instance.Definition == null)
            {
                return false;
            }

            var entry = GetEntry(slot);
            if (entry == null)
            {
                return false;
            }

            if (!IsItemCompatibleWithSlot(instance, slot))
            {
                return false;
            }

            var currentItem = entry.ItemInstance;
            if (ReferenceEquals(currentItem, instance))
            {
                return true;
            }

            entry.SetItem(instance);
            replacedItem = currentItem;

            return true;
        }

        public bool TryUnequip(EquipmentSlot slot, out ItemInstance removedItem)
        {
            removedItem = null;
            var entry = GetEntry(slot);
            if (entry == null || entry.IsEmpty)
            {
                return false;
            }

            removedItem = entry.ItemInstance;
            entry.Clear();
            return true;
        }

        public bool TryResolveTargetSlot(ItemDefinition definition, out EquipmentSlot slot)
        {
            slot = default;
            if (definition == null)
            {
                return false;
            }

            var armorData = definition.GetStatBlock<ArmorItemData>();
            if (armorData != null)
            {
                slot = armorData.Slot;
                return true;
            }

            var weaponData = definition.GetStatBlock<WeaponItemData>();
            if (weaponData != null)
            {
                slot = weaponData.Slot;
                return true;
            }

            return false;
        }

        public bool IsSlotEmpty(EquipmentSlot slot)
        {
            var entry = GetEntry(slot);
            return entry == null || entry.IsEmpty;
        }

        private EquipmentSlotEntry GetEntry(EquipmentSlot slot)
        {
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                var entry = equipmentSlots[i];
                if (entry != null && entry.Slot == slot)
                {
                    return entry;
                }
            }

            return null;
        }

        private bool IsItemCompatibleWithSlot(ItemInstance instance, EquipmentSlot slot)
        {
            if (instance?.Definition == null)
            {
                return false;
            }

            var armorData = instance.Definition.GetStatBlock<ArmorItemData>();
            if (armorData != null)
            {
                return armorData.Slot == slot;
            }

            var weaponData = instance.Definition.GetStatBlock<WeaponItemData>();
            if (weaponData != null)
            {
                return weaponData.Slot == slot;
            }

            return false;
        }

        private void EnsureSlotSetup()
        {
            if (equipmentSlots == null)
            {
                equipmentSlots = new List<EquipmentSlotEntry>();
            }

            var seen = new HashSet<EquipmentSlot>();
            if (equipmentSlots.Count > 0)
            {
                for (int i = equipmentSlots.Count - 1; i >= 0; i--)
                {
                    var entry = equipmentSlots[i];
                    if (entry == null)
                    {
                        equipmentSlots.RemoveAt(i);
                        continue;
                    }

                    if (seen.Contains(entry.Slot))
                    {
                        equipmentSlots.RemoveAt(i);
                        continue;
                    }

                    seen.Add(entry.Slot);
                }
            }

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (!seen.Contains(slot))
                {
                    equipmentSlots.Add(new EquipmentSlotEntry(slot));
                }
            }

            equipmentSlots.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        }
    }

}
