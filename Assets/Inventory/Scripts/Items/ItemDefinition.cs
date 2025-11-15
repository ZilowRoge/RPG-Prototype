using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Items/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identifiers")]
        [SerializeField] private string itemId = "item_000";

        [Header("Basic Info")]
        [SerializeField] private string itemName = "New Item";

        [TextArea]
        [SerializeField] private string description = "Item description";

        [SerializeField] private Sprite icon;

        [Min(1)]
        [SerializeField] private int maxStack = 1;

        public string Name => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public string Id => itemId;
    }
}
