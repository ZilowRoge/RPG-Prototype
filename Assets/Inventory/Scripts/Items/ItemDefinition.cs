using UnityEngine;
using System.Collections.Generic;

namespace Items
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Items/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identifiers")]
        [SerializeField] private string itemId = "item_000";

        [Header("Basic Info")]
        [SerializeField] private string itemName = "New Item";
        [SerializeField] private ItemType itemType = ItemType.Equipment;

        [TextArea]
        [SerializeField] private string description = "Item description";

        [SerializeField] private Sprite icon;

        [Min(1)]
        [SerializeField] private int maxStack = 1;

        [Header("Type Specific")]
        [SerializeField] private List<ItemTypeData> typeDatas;

        public string Name => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public string Id => itemId;
        public ItemType Type => itemType;

        public T GetStatBlock<T>() where T : ItemTypeData
        {
            if (typeDatas == null)
                return null;

            foreach (var data in typeDatas)
            {
                if (data is T typeData)
                    return typeData;
            }

            return null;
        }

        public bool TryGetStatBlock<T>(out T block) where T : ItemTypeData
        {
            block = GetStatBlock<T>();
            return block != null;
        }
    }
}
