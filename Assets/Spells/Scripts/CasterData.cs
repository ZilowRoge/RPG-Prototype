using UnityEngine;
using Player.Interfaces;

namespace Spells
{
    public class CasterData
    {
        public ISpellCasterStats stats;
        public Transform castOrigin;
        public Transform target;
        public ISpellPowerProvider perkRuntime;

        public float SpellPowerMultiplier =>
            perkRuntime != null ? perkRuntime.SpellPowerMultiplier : 1f;

        public CasterData(ISpellCasterStats stats, Transform castOrigin, Transform target = null, ISpellPowerProvider perkRuntime = null)
        {
            this.stats = stats;
            this.castOrigin = castOrigin;
            this.target = target;
            this.perkRuntime = perkRuntime;
        }
    }
}
