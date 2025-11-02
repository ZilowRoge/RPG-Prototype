using UnityEngine;
using Player.Statistics;
using Player.Perks;

namespace Spells
{
    public class CasterData
    {
        public StatsController stats;
        public Transform castOrigin;
        public Transform target;
        public PlayerPerkRuntime perkRuntime;

        public float SpellPowerMultiplier =>
            perkRuntime != null ? perkRuntime.SpellPowerMultiplier : 1f;

        public CasterData(StatsController stats, Transform castOrigin, Transform target = null, PlayerPerkRuntime perkRuntime = null)
        {
            this.stats = stats;
            this.castOrigin = castOrigin;
            this.target = target;
            this.perkRuntime = perkRuntime;
        }
    }
}
