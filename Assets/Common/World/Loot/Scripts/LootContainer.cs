using System;
using System.Collections.Generic;
using Common.World.Interaction;
using Inventory;
using Items;
using UI.Player.Inventory;
using Systems.SaveSystem;
using Systems.SaveSystem.SaveData;
using UnityEngine;

namespace Common.World.Loot
{
    [AddComponentMenu("Game/World/Loot/Container")]
    [RequireComponent(typeof(Collider))]
    public class LootContainer : MonoBehaviour, IInteractable, ISaveable
    {
        [Header("Container")]
        [SerializeField] private string containerId = "container_001";
        [SerializeField] private string containerName = "Container";
        [SerializeField] private InventoryController inventoryController;
        [SerializeField, Min(1)] private int slotCount = 6;
        [SerializeField] private bool initializeOnAwake = true;

        [Header("Initial Loot")]
        [SerializeField] private List<ItemStack> initialItems = new();
        [Header("Definitions")]
        [SerializeField] private ItemDefinitionDatabase itemDatabase;

        [Header("UI")]
        [SerializeField] private LootContainerWindow containerWindowOverride;

        private void Awake()
        {
            EnsureColliderIsTrigger();

            if (inventoryController == null)
                inventoryController = GetComponent<InventoryController>();

            if (initializeOnAwake && inventoryController != null)
            {
                inventoryController.Initialize(slotCount);
                SeedInventory();
            }
        }

        private void OnEnable()
        {
            SaveManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.Unregister(this);
        }

        public void Interact(GameObject player)
        {
            if (inventoryController == null)
            {
                Debug.LogWarning("[LootContainer] Missing InventoryController.", this);
                return;
            }

            var playerInventory = ResolvePlayerInventory(player);
            if (playerInventory == null)
            {
                Debug.LogWarning("[LootContainer] Player InventoryController not found.", this);
                return;
            }

            var window = ResolveWindow();
            if (window == null)
            {
                Debug.LogWarning("[LootContainer] LootContainerWindow not found in the scene.", this);
                return;
            }

            window.Open(playerInventory, inventoryController, containerName);
        }

        public void OnSave(GameData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(containerId) || inventoryController == null || inventoryController.Inventory == null)
                return;

            data.containerInventories ??= new List<ContainerInventoryData>();
            data.containerInventories.RemoveAll(c => c != null && c.containerId == containerId);

            var snapshot = new ContainerInventoryData
            {
                containerId = containerId,
                slotCount = inventoryController.Inventory.SlotCount
            };

            for (int i = 0; i < inventoryController.Inventory.SlotCount; i++)
            {
                var slot = inventoryController.Inventory.Slots[i];
                if (slot == null || slot.IsEmpty)
                    continue;

                var serializedItem = SerializeItem(slot.ItemInstance);
                if (serializedItem == null)
                    continue;

                snapshot.slots.Add(new SerializedInventorySlot
                {
                    slotId = i,
                    item = serializedItem
                });
            }

            data.containerInventories.Add(snapshot);
        }

        public void OnLoad(GameData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(containerId) || inventoryController == null || inventoryController.Inventory == null)
                return;

            var list = data.containerInventories;
            if (list == null || list.Count == 0)
                return;

            ContainerInventoryData snapshot = null;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry != null && entry.containerId == containerId)
                {
                    snapshot = entry;
                    break;
                }
            }

            if (snapshot == null)
                return;

            int targetCount = snapshot.slotCount > 0 ? snapshot.slotCount : inventoryController.Inventory.SlotCount;
            inventoryController.Initialize(targetCount);

            var slots = inventoryController.Inventory.Slots;
            if (slots == null)
                return;

            foreach (var saved in snapshot.slots)
            {
                if (saved == null || saved.slotId < 0 || saved.slotId >= slots.Count)
                    continue;

                var item = DeserializeItem(saved.item);
                if (item == null)
                    continue;

                slots[saved.slotId].SetItem(item);
            }
        }

        private InventoryController ResolvePlayerInventory(GameObject player)
        {
            if (player == null)
                return null;

            return player.GetComponentInParent<InventoryController>();
        }

        private LootContainerWindow ResolveWindow()
        {
            if (containerWindowOverride != null)
                return containerWindowOverride;

            return FindFirstObjectByType<LootContainerWindow>(FindObjectsInactive.Include);
        }

        private void SeedInventory()
        {
            if (inventoryController?.Inventory == null || initialItems == null)
                return;

            foreach (var item in initialItems)
            {
                if (item.instance == null || item.instance.Definition == null)
                    continue;

                int targetAmount = item.amount > 0 ? item.amount : item.instance.StackCount;
                if (targetAmount <= 0)
                    targetAmount = 1;

                var copy = new ItemInstance(item.instance.Definition, targetAmount, null, item.instance.Modifiers);
                inventoryController.TryAddItemInstance(copy);
            }
        }

        private void EnsureColliderIsTrigger()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
                col.isTrigger = true;

        }

        private SerializedItemInstance SerializeItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return null;

            var serialized = new SerializedItemInstance
            {
                itemId = instance.Definition.Id,
                stackCount = instance.StackCount,
                instanceId = instance.InstanceId
            };

            if (instance.Modifiers != null)
            {
                foreach (var mod in instance.Modifiers)
                {
                    serialized.modifiers.Add(new SerializedItemModifier
                    {
                        stat = mod.Stat.ToString(),
                        mode = mod.Mode.ToString(),
                        value = mod.Value
                    });
                }
            }

            return serialized;
        }

        private ItemInstance DeserializeItem(SerializedItemInstance serialized)
        {
            if (serialized == null || string.IsNullOrWhiteSpace(serialized.itemId))
                return null;

            var definition = ResolveDefinition(serialized.itemId);
            if (definition == null)
            {
                Debug.LogWarning($"[LootContainer] Missing ItemDefinition for id '{serialized.itemId}' in container '{containerId}'. Item skipped.");
                return null;
            }

            var mods = DeserializeModifiers(serialized.modifiers);
            return new ItemInstance(definition, serialized.stackCount, serialized.instanceId, mods);
        }

        private IEnumerable<ItemStatModifier> DeserializeModifiers(List<SerializedItemModifier> serialized)
        {
            if (serialized == null)
                yield break;

            foreach (var mod in serialized)
            {
                if (mod == null || string.IsNullOrWhiteSpace(mod.stat) || string.IsNullOrWhiteSpace(mod.mode))
                    continue;

                if (!Enum.TryParse(mod.stat, out ItemStatType statType))
                    continue;

                if (!Enum.TryParse(mod.mode, out ModifierMode mode))
                    continue;

                yield return new ItemStatModifier(statType, mode, mod.value);
            }
        }

        private ItemDefinition ResolveDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (itemDatabase != null && itemDatabase.TryGetById(id, out var defFromDb))
                return defFromDb;

            return null;
        }

        [Serializable]
        public struct ItemStack
        {
            public ItemInstance instance;
            [Min(1)] public int amount;
        }
    }
}
