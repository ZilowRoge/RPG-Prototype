using UnityEngine;
using UnityEngine.Serialization;

namespace Items
{
    [CreateAssetMenu(fileName = "GatheringToolData", menuName = "Items/Type Data/Gathering Tool")]
    public class GatheringToolItemData : ItemTypeData
    {
        [SerializeField] private GatheringToolType toolType = GatheringToolType.Pickaxe;
        [FormerlySerializedAs("maxDurability")]
        [HideInInspector]
        [SerializeField] private int legacyMaxDurability;

        public GatheringToolType ToolType => toolType;
        internal int LegacyMaxDurability => Mathf.Max(0, legacyMaxDurability);

        public override string GetString()
        {
            return $"Tool: {toolType}";
        }
    }
}
