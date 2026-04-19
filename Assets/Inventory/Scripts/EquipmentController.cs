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

        private IItemUseContext itemUseContext;

        public IReadOnlyList<EquipmentSlotEntry> Slots => equipmentSlots;

        private void Reset()
        {
        }

        private void Awake()
        {
            CacheItemUseContext();
            EnsureSlotSetup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheItemUseContext();
            EnsureSlotSetup();
        }
#endif

        public ItemInstance GetItem(EquipmentSlot slot)
        {
            var entry = GetEntry(slot);
            return entry?.ItemInstance;
        }

        public bool ClearSlot(EquipmentSlot slot)
        {
            var entry = GetEntry(slot);
            if (entry == null)
            {
                return false;
            }

            bool hadItem = !entry.IsEmpty;
            if (hadItem)
            {
                itemUseContext?.HandleItemUnequipped(slot, entry.ItemInstance);
            }

            entry.Clear();
            return hadItem;
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

            if (currentItem != null && !currentItem.IsEmpty)
            {
                itemUseContext?.HandleItemUnequipped(slot, currentItem);
            }

            entry.SetItem(instance);
            replacedItem = currentItem;

            itemUseContext?.HandleItemEquipped(slot, instance);

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
            itemUseContext?.HandleItemUnequipped(slot, removedItem);
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

            var toolData = definition.GetStatBlock<GatheringToolItemData>();
            if (toolData != null)
            {
                slot = EquipmentSlot.Weapon;
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

            if (instance.Definition.Type == ItemType.Consumable)
            {
                return IsConsumableSlot(slot);
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

            var toolData = instance.Definition.GetStatBlock<GatheringToolItemData>();
            if (toolData != null)
            {
                return slot == EquipmentSlot.Weapon;
            }

            return false;
        }

        public static bool IsConsumableSlot(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Consumable1 ||
                   slot == EquipmentSlot.Consumable2 ||
                   slot == EquipmentSlot.Consumable3;
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

        private void CacheItemUseContext()
        {
            itemUseContext = ResolveItemUseContext();
        }

        private IItemUseContext ResolveItemUseContext()
        {
            var context = GetComponent<IItemUseContext>();
            if (context == null)
                Debug.LogWarning("EquipmentController could not find IItemUseContext on the same GameObject.", this);
            return context;
        }
    }

}
