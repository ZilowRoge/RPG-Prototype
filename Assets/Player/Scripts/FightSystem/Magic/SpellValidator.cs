using Spells;

namespace Player.FightSystem.Magic
{
    public enum CastResult
    {
        Success,
        NotEnoughMana,
        NoTarget,
        InvalidSymbol
    }

    public class SpellValidator
    {
        public CastResult Validate(Spell spell, CasterData caster, bool consumeResources = true)
        {
            if (spell == null)
                return CastResult.InvalidSymbol;

            if (caster.stats == null)
                return CastResult.NotEnoughMana;

            if (consumeResources)
            {
                if (!caster.stats.UseMana(spell.ManaCost))
                    return CastResult.NotEnoughMana;
            }
            else if (caster.stats.CurrentMana < spell.ManaCost)
            {
                return CastResult.NotEnoughMana;
            }

            if (caster.target == null)
                return CastResult.NoTarget;

            return CastResult.Success;
        }
    }
}

