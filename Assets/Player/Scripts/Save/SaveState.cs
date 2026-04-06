using System;
using System.Collections.Generic;
using UnityEngine;
using Player.Progress;
using Player.Statistics;
using Quests;
using Systems.SaveSystem;
using Systems.SaveSystem.SaveData;
using Inventory;
using Items;
using Common.World.SceneTransitions;

namespace Player.Save
{
    [DisallowMultipleComponent]
    public class SaveState : MonoBehaviour, ISaveable
    {
        public static event Action PlayerLoadedFromSave;
        public static bool IsRestoring { get; private set; }
        public static bool SuppressNextTransformRestore { get; set; }

        [Header("References")]
        [SerializeField] private ProgressController progressController;
        [SerializeField] private StatsController statsController;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private EquipmentController equipmentController;
        [Header("Items")]
        [Tooltip("Database of item definitions used to resolve saved itemIds.")]
        [SerializeField] private ItemDefinitionDatabase itemDatabase;
        [Tooltip("Fallback list of item definitions used to resolve saved itemIds.")]
        [SerializeField] private List<ItemDefinition> itemDefinitions = new();

        private readonly Dictionary<string, ItemDefinition> definitionLookup = new();

        private void Awake()
        {
            CacheReferences();
            BuildItemLookup();
        }

        private void OnEnable()
        {
            CacheReferences();
            SaveManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.Unregister(this);
        }

        public void OnSave(GameData data)
        {
            if (data == null)
                return;

            data.playerData ??= new PlayerStatisticsData();
            data.progressData ??= new PlayerProgressData();
            data.inventoryData ??= new PlayerInventoryData();

            WriteStats(data.playerData);
            WriteProgress(data.progressData);
            WriteInventory(data.inventoryData);
        }

        public void OnLoad(GameData data)
        {
            if (data == null)
                return;

            IsRestoring = true;
            BuildItemLookup();
            ReadInventory(data.inventoryData);
            ReadStats(data.playerData);
            ReadProgress(data.progressData);
            IsRestoring = false;
            PlayerLoadedFromSave?.Invoke();
        }

        private void CacheReferences()
        {
            if (progressController == null)
                progressController = GetComponentInParent<ProgressController>() ?? FindFirstObjectByType<ProgressController>();

            if (statsController == null)
                statsController = GetComponentInParent<StatsController>() ?? FindFirstObjectByType<StatsController>();

            if (questManager == null)
                questManager = progressController?.QuestManager ?? FindFirstObjectByType<QuestManager>();

            if (inventoryController == null)
                inventoryController = GetComponentInParent<InventoryController>() ?? FindFirstObjectByType<InventoryController>();

            if (equipmentController == null)
                equipmentController = GetComponentInParent<EquipmentController>() ?? FindFirstObjectByType<EquipmentController>();
        }

        private void WriteStats(PlayerStatisticsData snapshot)
        {
            if (snapshot == null)
                return;

            if (statsController != null)
            {
                snapshot.SetBasics(
                    statsController.CurrentHealth,
                    statsController.CurrentMana,
                    statsController.CurrentStamina,
                    ResolvePlayerLevel());

                var statsData = statsController.Statistics;
                if (statsData != null && statsData.container != null)
                {
                    snapshot.SetStats(
                        statsData.container.GetAll(),
                        statsData.container.GetPendingPoints());
                }
            }

            if (progressController != null)
            {
                snapshot.SetJobs(progressController.GetAllJobs());
            }
        }

        private void ReadStats(PlayerStatisticsData snapshot)
        {
            if (snapshot == null)
                return;

            if (progressController != null)
                progressController.ApplyJobsFromSnapshot(snapshot);

            if (statsController == null)
                return;

            var statsData = statsController.Statistics;
            if (statsData != null && statsData.container != null)
            {
                snapshot.GetStatsPairs(out var stats, out var pendingPoints);
                statsData.container.SetStats(stats, pendingPoints);
            }

            snapshot.GetBasics(out var health, out var mana, out var stamina, out _);
            statsController.OverrideResources(health, mana, stamina);
        }

        private void WriteProgress(PlayerProgressData snapshot)
        {
            if (snapshot == null)
                return;

            snapshot.flags ??= new List<SerializedFlagEntry>();
            snapshot.learnedSymbols ??= new List<int>();
            snapshot.quests ??= new List<SerializedQuestProgress>();

            snapshot.hasPlayerTransform = true;
            snapshot.playerPosition = transform.position;
            snapshot.playerRotation = transform.rotation;

            if (progressController == null)
                return;

            snapshot.availableExperience = progressController.AvailableExperience;

            snapshot.flags.Clear();
            foreach (var flag in progressController.ExportFlags())
            {
                snapshot.flags.Add(new SerializedFlagEntry(flag.Key, flag.Value));
            }

            snapshot.learnedSymbols.Clear();
            var symbols = progressController.ExportKnownSymbols();
            if (symbols != null && symbols.Count > 0)
                snapshot.learnedSymbols.AddRange(symbols);

            snapshot.quests.Clear();
            if (questManager != null)
            {
                foreach (var quest in questManager.ActiveQuests)
                {
                    var serialized = SerializeQuest(quest);
                    if (serialized != null)
                        snapshot.quests.Add(serialized);
                }
            }
        }

        private void ReadProgress(PlayerProgressData snapshot)
        {
            if (snapshot == null)
                return;

            ApplySavedTransform(snapshot);

            if (progressController != null)
            {
                progressController.OverrideAvailableExperience(snapshot.availableExperience);

                var flagEntries = snapshot.flags ?? new List<SerializedFlagEntry>();
                var restoredFlags = new List<KeyValuePair<string, bool>>(flagEntries.Count);
                foreach (var flag in flagEntries)
                {
                    if (flag == null || string.IsNullOrEmpty(flag.key))
                        continue;
                    restoredFlags.Add(new KeyValuePair<string, bool>(flag.key, flag.value));
                }
                progressController.OverwriteFlags(restoredFlags);

                var learnedSymbols = snapshot.learnedSymbols ?? new List<int>();
                progressController.OverwriteKnownSymbols(learnedSymbols);
            }

            if (questManager != null)
            {
                var questEntries = snapshot.quests ?? new List<SerializedQuestProgress>();
                var restoredQuests = new List<QuestProgress>(questEntries.Count);
                foreach (var serialized in questEntries)
                {
                    var quest = DeserializeQuest(serialized);
                    if (quest != null)
                        restoredQuests.Add(quest);
                }

                questManager.OverwriteActiveQuests(restoredQuests);

                if (progressController != null)
                    progressController.EvaluateQuests();
            }

            PlayerLoadedFromSave?.Invoke();
        }

        private void WriteInventory(PlayerInventoryData snapshot)
        {
            if (snapshot == null)
                return;

            snapshot.inventorySlots ??= new List<SerializedInventorySlot>();
            snapshot.equipmentSlots ??= new List<SerializedEquipmentSlot>();
            snapshot.inventorySlots.Clear();
            snapshot.equipmentSlots.Clear();

            var inventory = inventoryController != null ? inventoryController.Inventory : null;
            if (inventory != null)
            {
                snapshot.inventorySlotCount = inventory.SlotCount;

                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    var slot = inventory.Slots[i];
                    if (slot == null || slot.IsEmpty)
                        continue;

                    var serializedItem = SerializeItem(slot.ItemInstance);
                    if (serializedItem == null)
                        continue;

                    snapshot.inventorySlots.Add(new SerializedInventorySlot
                    {
                        slotId = i,
                        item = serializedItem
                    });
                }
            }

            if (equipmentController != null && equipmentController.Slots != null)
            {
                foreach (var entry in equipmentController.Slots)
                {
                    if (entry == null || entry.IsEmpty)
                        continue;

                    var serializedItem = SerializeItem(entry.ItemInstance);
                    if (serializedItem == null)
                        continue;

                    snapshot.equipmentSlots.Add(new SerializedEquipmentSlot
                    {
                        slot = entry.Slot.ToString(),
                        item = serializedItem
                    });
                }
            }
        }

        private void ReadInventory(PlayerInventoryData snapshot)
        {
            if (snapshot == null)
                return;

            RestoreInventory(snapshot);
            RestoreEquipment(snapshot);
        }

        private void RestoreInventory(PlayerInventoryData snapshot)
        {
            if (inventoryController == null)
                return;

            var inventory = inventoryController.Inventory;
            if (inventory == null)
                return;

            var targetCount = snapshot.inventorySlotCount > 0 ? snapshot.inventorySlotCount : inventory.SlotCount;
            inventory.InitializeSlots(targetCount);

            var slots = inventory.Slots;
            if (slots == null)
                return;

            var savedSlots = snapshot.inventorySlots ?? new List<SerializedInventorySlot>();
            foreach (var saved in savedSlots)
            {
                if (saved == null || saved.slotId < 0 || saved.slotId >= slots.Count)
                    continue;

                var item = DeserializeItem(saved.item);
                if (item == null)
                    continue;

                slots[saved.slotId].SetItem(item);
            }
        }

        private void RestoreEquipment(PlayerInventoryData snapshot)
        {
            if (equipmentController == null || equipmentController.Slots == null)
                return;

            var savedEquip = snapshot.equipmentSlots ?? new List<SerializedEquipmentSlot>();
            if (savedEquip.Count == 0)
                return;

            foreach (var entry in equipmentController.Slots)
            {
                entry?.Clear();
            }

            foreach (var saved in savedEquip)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.slot))
                    continue;

                if (!Enum.TryParse(saved.slot, out EquipmentSlot slot))
                    continue;

                var item = DeserializeItem(saved.item);
                if (item == null)
                    continue;

                var entry = GetEquipmentEntry(slot);
                if (entry != null)
                    entry.SetItem(item);
            }
        }

        private EquipmentSlotEntry GetEquipmentEntry(EquipmentSlot slot)
        {
            if (equipmentController?.Slots == null)
                return null;

            foreach (var entry in equipmentController.Slots)
            {
                if (entry != null && entry.Slot == slot)
                    return entry;
            }

            return null;
        }

        private SerializedItemInstance SerializeItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return null;

            var serialized = new SerializedItemInstance
            {
                itemId = instance.Definition.Id,
                stackCount = instance.StackCount,
                instanceId = instance.InstanceId,
                currentDurability = instance.CurrentDurability
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
                Debug.LogWarning($"[SaveState] Missing ItemDefinition for id '{serialized.itemId}'. Item skipped.");
                return null;
            }

            var mods = DeserializeModifiers(serialized.modifiers);
            int durability = serialized.currentDurability > 0 ? serialized.currentDurability : -1;
            return new ItemInstance(definition, serialized.stackCount, serialized.instanceId, mods, durability);
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

        private void BuildItemLookup()
        {
            definitionLookup.Clear();

            foreach (var def in EnumerateDefinitions())
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                    continue;
                definitionLookup[def.Id] = def;
            }
        }

        private IEnumerable<ItemDefinition> EnumerateDefinitions()
        {
            if (itemDatabase != null)
            {
                foreach (var def in itemDatabase.Definitions)
                    yield return def;
            }

            if (itemDefinitions != null)
            {
                foreach (var def in itemDefinitions)
                    yield return def;
            }
        }

        private ItemDefinition ResolveDefinition(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            if (definitionLookup.TryGetValue(itemId, out var found) && found != null)
                return found;

            return null;
        }

        private void ApplySavedTransform(PlayerProgressData snapshot)
        {
            if (snapshot == null || !snapshot.hasPlayerTransform)
                return;

            if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
            {
                return;
            }

            if (SuppressNextTransformRestore)
            {
                SuppressNextTransformRestore = false;
                return;
            }

            var target = transform;
            if (target == null)
                return;

            CharacterController controller = null;
            bool controllerWasEnabled = false;

            if (TryGetComponent(out controller))
            {
                controllerWasEnabled = controller.enabled;
                controller.enabled = false;
            }

            target.SetPositionAndRotation(snapshot.playerPosition, snapshot.playerRotation);

            if (controller != null)
                controller.enabled = controllerWasEnabled;
        }

        private int ResolvePlayerLevel()
        {
            if (progressController == null)
                return 1;

            int aggregatedLevel = 0;
            bool hasJob = false;
            foreach (var job in progressController.GetAllJobs())
            {
                if (job == null)
                    continue;
                hasJob = true;
                aggregatedLevel += Mathf.Max(1, job.CurrentLevel);
            }

            if (hasJob && aggregatedLevel > 0)
                return aggregatedLevel;

            return 1;
        }

        private static SerializedQuestProgress SerializeQuest(QuestProgress source)
        {
            if (source == null || string.IsNullOrEmpty(source.questId))
                return null;

            var serialized = new SerializedQuestProgress
            {
                questId = source.questId,
                stageIndex = source.stageIndex,
                state = source.state
            };

            if (source.stages == null)
                return serialized;

            foreach (var stage in source.stages)
            {
                if (stage == null)
                    continue;

                var stageData = new SerializedStageProgress
                {
                    stageId = stage.stageId,
                    completed = stage.completed
                };

                if (stage.objectives != null)
                {
                    foreach (var objective in stage.objectives)
                    {
                        if (objective == null)
                            continue;

                        stageData.objectives.Add(new SerializedObjectiveProgress
                        {
                            objectiveId = objective.objectiveId,
                            currentCount = objective.currentCount,
                            completed = objective.completed
                        });
                    }
                }

                serialized.stages.Add(stageData);
            }

            return serialized;
        }

        private static QuestProgress DeserializeQuest(SerializedQuestProgress source)
        {
            if (source == null || string.IsNullOrEmpty(source.questId))
                return null;

            var quest = new QuestProgress
            {
                questId = source.questId,
                stageIndex = source.stageIndex,
                state = source.state,
                stages = new List<StageProgress>()
            };

            if (source.stages == null)
                return quest;

            foreach (var stageData in source.stages)
            {
                if (stageData == null)
                    continue;

                var stage = new StageProgress
                {
                    stageId = stageData.stageId,
                    completed = stageData.completed,
                    objectives = new List<ObjectiveProgress>()
                };

                if (stageData.objectives != null)
                {
                    foreach (var objectiveData in stageData.objectives)
                    {
                        if (objectiveData == null)
                            continue;

                        stage.objectives.Add(new ObjectiveProgress
                        {
                            objectiveId = objectiveData.objectiveId,
                            currentCount = objectiveData.currentCount,
                            completed = objectiveData.completed
                        });
                    }
                }

                quest.stages.Add(stage);
            }

            return quest;
        }
    }
}
