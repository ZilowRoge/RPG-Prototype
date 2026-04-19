using UnityEngine;
using Systems.Statistics;

namespace Systems.Perks{
[CreateAssetMenu(menuName = "Perk/Add Stat Effect")]
public class PerkAddStatEffect : PerkEffectBase
{
    public EStatistics stat;
    public int value;

    public override string GetEffectString()
    {
        var statName = stat switch
        {
            EStatistics.INT => "Intelligence",
            EStatistics.POW => "Power",
            EStatistics.VIT => "Vitality",
            EStatistics.END => "Endurance",
            _ => SplitCamelCase(stat.ToString())
        };

        return $"{FormatSignedInt(value)} {statName}";
    }
}
}
