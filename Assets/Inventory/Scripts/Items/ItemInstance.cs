using System;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private int stackCount = 1;
        [SerializeField] private string instanceId;
        [SerializeField] private List<ItemStatModifier> modifiers = new();

        public ItemInstance(ItemDefinition definition, int stackCount = 1, string instanceId = null, IEnumerable<ItemStatModifier> initialModifiers = null)
        {
            this.definition = definition;
            this.instanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString() : instanceId;
            SetStackCount(stackCount);
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

        public void SetStackCount(int newCount)
        {
            if (definition == null)
            {
                stackCount = Mathf.Max(0, newCount);
                return;
            }

            stackCount = Mathf.Clamp(newCount, 0, definition.MaxStack);
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
    }
}
