using UnityEngine;
using Player.Statistics;

namespace Spells{
public class CasterData
{
    public StatsController stats;
    public Transform castOrigin;
    public Transform target; 
    public CasterData(StatsController stats, Transform castOrigin, Transform target = null)
    {
        this.stats = stats;
        this.castOrigin = castOrigin;
        this.target = target;
    }
}
}