using System;
using Items;
using Player.Statistics;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Basic implementation that handles equipping gear and consuming consumables.
    /// </summary>
    public class ItemUseContext : MonoBehaviour, IItemUseContext
    {
        private const string LogPrefix = "[ItemUseContext]";

        [Header("Dependencies")]
        [SerializeField] private StatsController statsController;
        [SerializeField] private BuffController buffController;

        private void Awake()
        {
            CacheDependencies();
        }

        public bool TryUseItem(ItemUseRequest request)
        {
            if (request.InventoryData == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing inventory reference in request.");
                return false;
            }

            var inventory = request.InventoryData;
            var slotIndex = request.SlotIndex;

            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            {
                Debug.LogWarning($"{LogPrefix} Slot index {slotIndex} is out of range.");
                return false;
            }

            var slot = inventory.Slots[slotIndex];
            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"{LogPrefix} Slot {slotIndex} is empty.");
                return false;
            }

            var instance = slot.ItemInstance;
            var definition = instance?.Definition;
            if (definition == null)
            {
                Debug.LogWarning($"{LogPrefix} Item instance in slot {slotIndex} has no definition.");
                return false;
            }

            if (definition.Type == ItemType.Consumable)
            {
                if (request.PreferredEquipmentSlot.HasValue)
                {
                    var targetSlot = request.PreferredEquipmentSlot.Value;
                    if (!EquipmentController.IsConsumableSlot(targetSlot))
                    {
                        Debug.LogWarning($"{LogPrefix} Slot {targetSlot} does not accept consumables.");
                        return false;
                    }

                    Debug.Log($"{LogPrefix} Assigning consumable '{definition.Name}' to slot {targetSlot}.");
                    return HandleEquipmentUse(request, slot);
                }

                Debug.Log($"{LogPrefix} Trying to consume '{definition.Name}' from slot {slotIndex}.");
                return HandleConsumableUse(slot, definition, slotIndex);
            }

            Debug.Log($"{LogPrefix} Trying to use '{definition.Name}' of type {definition.Type} from slot {slotIndex}.");
            switch (definition.Type)
            {
                case ItemType.Equipment:
                    return HandleEquipmentUse(request, slot);
                default:
                    Debug.LogWarning($"{LogPrefix} Item type {definition.Type} not supported.");
                    return false;
            }
        }

        public void HandleItemEquipped(EquipmentSlot slot, ItemInstance item)
        {
            // Placeholder for stat/buff integration.
        }

        public void HandleItemUnequipped(EquipmentSlot slot, ItemInstance item)
        {
            // Placeholder for stat/buff removal.
        }

        public bool TryUseEquippedConsumable(EquipmentController equipmentController, EquipmentSlot slot)
        {
            if (equipmentController == null)
            {
                Debug.LogWarning($"{LogPrefix} No EquipmentController provided for consuming equipped item.");
                return false;
            }

            if (!EquipmentController.IsConsumableSlot(slot))
            {
                Debug.LogWarning($"{LogPrefix} Slot {slot} is not a consumable slot.");
                return false;
            }

            var instance = equipmentController.GetItem(slot);
            var definition = instance?.Definition;
            if (instance == null || definition == null || definition.Type != ItemType.Consumable)
            {
                Debug.LogWarning($"{LogPrefix} No consumable in equipment slot {slot}.");
                return false;
            }

            if (!definition.TryGetStatBlock<ConsumableItemData>(out var consumableData) || !consumableData.HasEffect)
            {
                Debug.LogWarning($"{LogPrefix} Consumable '{definition.Name}' has no effect data.");
                return false;
            }

            if (!TryApplyConsumableEffect(consumableData, definition.Name))
                return false;

            ConsumeInstance(instance, () => equipmentController.ClearSlot(slot));
            Debug.Log($"{LogPrefix} Consumed '{definition.Name}' from equipment slot {slot}. Remaining stack: {instance.StackCount}.");
            return true;
        }

        private bool HandleConsumableUse(Slot slot, ItemDefinition definition, int slotIndex)
        {
            var instance = slot.ItemInstance;
            if (instance == null)
                return false;

            if (!definition.TryGetStatBlock<ConsumableItemData>(out var consumableData) || !consumableData.HasEffect)
            {
                Debug.LogWarning($"{LogPrefix} Consumable '{definition.Name}' has no effect data.");
                return false;
            }

            if (!TryApplyConsumableEffect(consumableData, definition.Name))
                return false;

            ConsumeInstance(instance, slot.Clear);
            Debug.Log($"{LogPrefix} Consumed '{definition.Name}' from slot {slotIndex}. Remaining stack: {instance.StackCount}.");
            return true;
        }

        private static bool HandleEquipmentUse(ItemUseRequest request, Slot slot)
        {
            if (request.EquipmentController == null)
            {
                Debug.LogWarning($"{LogPrefix} No EquipmentController available, cannot equip item.");
                return false;
            }

            var instance = slot.ItemInstance;
            var definition = instance?.Definition;
            if (definition == null)
                return false;

            var targetSlot = request.PreferredEquipmentSlot ?? ResolveEquipmentSlot(definition);
            if (!targetSlot.HasValue)
            {
                Debug.LogWarning($"{LogPrefix} Could not resolve target equipment slot for '{definition.Name}'.");
                return false;
            }

            Debug.Log($"{LogPrefix} Equipping '{definition.Name}' into slot {targetSlot.Value}.");
            if (!request.EquipmentController.TryEquipItem(targetSlot.Value, instance, out var replaced))
            {
                Debug.LogWarning($"{LogPrefix} Failed to equip '{definition.Name}' into slot {targetSlot.Value}.");
                return false;
            }

            if (replaced != null && !replaced.IsEmpty)
            {
                slot.SetItem(replaced);
                Debug.Log($"{LogPrefix} Replaced existing item; returned to inventory slot.");
            }
            else
            {
                slot.Clear();
                Debug.Log($"{LogPrefix} Slot cleared after equipping '{definition.Name}'.");
            }

            return true;
        }

        private static EquipmentSlot? ResolveEquipmentSlot(ItemDefinition definition)
        {
            var armor = definition.GetStatBlock<ArmorItemData>();
            if (armor != null)
                return armor.Slot;

            var weapon = definition.GetStatBlock<WeaponItemData>();
            if (weapon != null)
                return weapon.Slot;

            return null;
        }

        private void CacheDependencies()
        {
            if (statsController == null)
                statsController = GetComponent<StatsController>() ?? GetComponentInParent<StatsController>();

            if (buffController == null)
                buffController = GetComponent<BuffController>() ?? GetComponentInParent<BuffController>();

        }

        private static void ConsumeInstance(ItemInstance instance, Action clearSlot)
        {
            instance.SetStackCount(instance.StackCount - 1);
            if (instance.StackCount <= 0)
            {
                clearSlot?.Invoke();
            }
        }

        private bool TryApplyConsumableEffect(ConsumableItemData data, string displayName)
        {
            return data.EffectType switch
            {
                ConsumableEffectType.Health => ApplyOverTimeEffect(data, displayName, BuffKey.ConsumableHealth, (stats, amount) => stats.RestoreHealth(amount)),
                ConsumableEffectType.Mana => ApplyOverTimeEffect(data, displayName, BuffKey.ConsumableMana, (stats, amount) => stats.RestoreMana(amount)),
                ConsumableEffectType.Stamina => ApplyOverTimeEffect(data, displayName, BuffKey.ConsumableStamina, (stats, amount) => stats.RestoreStamina(amount)),
                _ => LogUnsupported(data.EffectType)
            };
        }

        private bool ApplyOverTimeEffect(
            ConsumableItemData data,
            string displayName,
            BuffKey buffKey,
            Action<StatsController, float> applyAmount)
        {
            if (statsController == null)
            {
                Debug.LogWarning($"{LogPrefix} StatsController missing; cannot apply consumable.");
                return false;
            }

            float amount = Mathf.Max(0f, data.EffectValue);
            float duration = Mathf.Max(0f, data.EffectDuration);

            if (buffController == null)
            {
                Debug.LogWarning($"{LogPrefix} BuffController missing; cannot apply over-time consumable.");
                return false;
            }

            if (duration <= Mathf.Epsilon)
            {
                Debug.LogWarning($"{LogPrefix} Duration is zero for over-time consumable {data.EffectType}; skipping.");
                return false;
            }

            float ratePerSecond = amount / Mathf.Max(0.0001f, duration);
            var request = BuffRequestBuilder
                .Create(buffKey, duration)
                .WithDisplayName(displayName)
                .OnTick((context, delta) =>
                {
                    var stats = context.Stats;
                    if (stats != null)
                        applyAmount?.Invoke(stats, ratePerSecond * delta);
                })
                .Build();

            return buffController.ApplyBuff(request);
        }

        private bool LogUnsupported(ConsumableEffectType type)
        {
            Debug.LogWarning($"{LogPrefix} Consumable effect type {type} is not supported yet.");
            return false;
        }
    }
}
