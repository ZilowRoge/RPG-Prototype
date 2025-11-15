using System;
using UnityEngine;

namespace Items
{
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private int stackCount = 1;
        [SerializeField] private string instanceId;

        public ItemInstance(ItemDefinition definition, int stackCount = 1, string instanceId = null)
        {
            this.definition = definition;
            this.instanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString() : instanceId;
            SetStackCount(stackCount);
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

        public void SetStackCount(int newCount)
        {
            if (definition == null)
            {
                stackCount = Mathf.Max(0, newCount);
                return;
            }

            stackCount = Mathf.Clamp(newCount, 0, definition.MaxStack);
        }
    }
}
