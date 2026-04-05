using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "GatheringToolData", menuName = "Items/Type Data/Gathering Tool")]
    public class GatheringToolItemData : ItemTypeData
    {
        [SerializeField] private GatheringToolType toolType = GatheringToolType.Pickaxe;

        public GatheringToolType ToolType => toolType;

        public override string GetString()
        {
            return $"Tool: {toolType}";
        }
    }
}
