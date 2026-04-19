using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "DurabilityData", menuName = "Items/Type Data/Durability")]
    public class DurabilityItemData : ItemTypeData
    {
        [Min(1)]
        [SerializeField] private int maxDurability = 100;

        public int MaxDurability => Mathf.Max(1, maxDurability);

        public override string GetString()
        {
            return $"Durability: {MaxDurability}";
        }
    }
}
