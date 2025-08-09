using UnityEngine;
using Systems.Statistics;

namespace Systems.Perks{
[CreateAssetMenu(menuName = "Perk/Add Stat Effect")]
public class PerkAddStatEffect : PerkEffectBase
{
    public EStatistics stat;
    public int value;
}
}