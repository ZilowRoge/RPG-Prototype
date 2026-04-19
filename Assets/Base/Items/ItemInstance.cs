using System;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    public readonly struct ItemTooltipData
    {
        public ItemTooltipData(string name, string description, string statsText)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            StatsText = statsText ?? string.Empty;
        }

        public string Name { get; }
        public string Description { get; }
        public string StatsText { get; }
    }

    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private int stackCount = 1;
        [SerializeField] private string instanceId;
        [SerializeField] private int currentDurability = -1;
        [SerializeField] private List<ItemStatModifier> modifiers = new();

        public ItemInstance(
            ItemDefinition definition,
            int stackCount = 1,
            string instanceId = null,
            IEnumerable<ItemStatModifier> initialModifiers = null,
            int currentDurability = -1)
        {
            this.definition = definition;
            this.instanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString() : instanceId;
            SetStackCount(stackCount);
            SetCurrentDurability(currentDurability);
            SetModifiers(initialModifiers);
        }

        public ItemDefinition Definition => definition;
        public int StackCount => stackCount;
        public string InstanceId
        {
            get
            {
                if (string.IsNullOrEmpty(instanceId))
                {
                    instanceId = Guid.NewGuid().ToString();
                }

                return instanceId;
            }
        }
        public bool IsEmpty => definition == null || stackCount <= 0;
        public IReadOnlyList<ItemStatModifier> Modifiers => modifiers;
        public bool HasDurability => definition != null && ResolveMaxDurability() > 0;
        public int MaxDurability => ResolveMaxDurability();
        public bool IsBroken => HasDurability && CurrentDurability <= 0;
        public int CurrentDurability
        {
            get
            {
                EnsureDurabilityInitialized();
                return HasDurability ? currentDurability : -1;
            }
        }

        public void SetStackCount(int newCount)
        {
            if (definition == null)
            {
                stackCount = Mathf.Max(0, newCount);
                return;
            }

            stackCount = Mathf.Clamp(newCount, 0, definition.MaxStack);
        }

        public void SetCurrentDurability(int value)
        {
            if (!HasDurability)
            {
                currentDurability = -1;
                return;
            }

            int maxDurability = ResolveMaxDurability();
            if (value < 0)
            {
                currentDurability = stackCount > 0 ? maxDurability : 0;
                return;
            }

            currentDurability = Mathf.Clamp(value, 0, maxDurability);
        }

        public bool ConsumeDurability(int amount = 1)
        {
            if (!HasDurability || amount <= 0)
                return true;

            EnsureDurabilityInitialized();
            currentDurability = Mathf.Clamp(currentDurability - amount, 0, ResolveMaxDurability());
            return currentDurability > 0;
        }

        public void SetModifiers(IEnumerable<ItemStatModifier> source)
        {
            modifiers.Clear();
            if (source == null)
                return;

            foreach (var mod in source)
            {
                modifiers.Add(mod);
            }
        }

        public void AddModifier(ItemStatModifier modifier)
        {
            modifiers.Add(modifier);
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
        }

        public ItemTooltipData GetTooltip(
            string unknownNameFallback = "Unknown item",
            string emptyDescriptionFallback = "No description.")
        {
            string tooltipName = definition != null && !string.IsNullOrWhiteSpace(definition.Name)
                ? definition.Name
                : unknownNameFallback;

            string tooltipDescription = definition != null && !string.IsNullOrWhiteSpace(definition.Description)
                ? definition.Description
                : emptyDescriptionFallback;

            return new ItemTooltipData(tooltipName, tooltipDescription, BuildTooltipStatsText());
        }

        private void EnsureDurabilityInitialized()
        {
            if (!HasDurability || stackCount <= 0 || currentDurability >= 0)
                return;

            currentDurability = ResolveMaxDurability();
        }

        private string BuildTooltipStatsText()
        {
            var lines = new List<string>();

            if (HasDurability)
            {
                lines.Add($"Durability: {CurrentDurability}/{MaxDurability}");
            }

            if (definition != null)
            {
                foreach (var block in definition.GetAllStatBlocks())
                {
                    if (block == null || block is DurabilityItemData)
                        continue;

                    string text = block.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        lines.Add(text);
                    }
                }
            }

            if (modifiers != null)
            {
                foreach (var mod in modifiers)
                {
                    string text = FormatModifier(mod);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        lines.Add(text);
                    }
                }
            }

            return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
        }

        private static string FormatModifier(ItemStatModifier modifier)
        {
            string statName = modifier.Stat.ToString();
            switch (modifier.Mode)
            {
                case ModifierMode.Add:
                    if (Mathf.Approximately(modifier.Value, 0f))
                        return string.Empty;
                    return $"{statName}: {(modifier.Value >= 0f ? "+" : string.Empty)}{modifier.Value:0.#}";
                case ModifierMode.Multiply:
                    float pct = modifier.Value * 100f;
                    if (Mathf.Approximately(pct, 0f))
                        return string.Empty;
                    return $"{statName}: {(pct >= 0f ? "+" : string.Empty)}{pct:0.#}%";
                case ModifierMode.Override:
                    return $"{statName}: {modifier.Value:0.#}";
                default:
                    return string.Empty;
            }
        }

        private int ResolveMaxDurability()
        {
            if (definition == null)
                return 0;

            var durabilityData = definition.GetStatBlock<DurabilityItemData>();
            if (durabilityData != null)
                return durabilityData.MaxDurability;

            var toolData = definition.GetStatBlock<GatheringToolItemData>();
            return toolData != null ? toolData.LegacyMaxDurability : 0;
        }
    }
}
