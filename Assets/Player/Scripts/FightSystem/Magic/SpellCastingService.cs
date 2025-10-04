using System.Collections.Generic;
using UnityEngine;
using Spells;

namespace Player.FightSystem.Magic
{
    public class SpellCastingService
    {
        private readonly SkillDatabase database;
        private readonly SpellValidator validator = new();

        public SpellCastingService(SkillDatabase database)
        {
            this.database = database;
        }

        public CastResult Activate(List<int> symbols, CasterData caster)
        {
            var spell = database.GetSpellBySymbols(symbols);
            Debug.Log($"Symbols: {symbols}, spell: {spell}");

            var result = validator.Validate(spell, caster);
            if (result != CastResult.Success) {
                Debug.LogWarning($"Can't cast symbol reason: {result}");
                return result;
            }

            spell.OnActivation(caster);
            return CastResult.Success;
        }

        public CastResult Cast(List<int> symbols, CasterData caster)
        {
            var spell = database.GetSpellBySymbols(symbols);

            var result = validator.Validate(spell, caster);
            if (result != CastResult.Success)
                return result;

            spell.OnCast(caster);
            return CastResult.Success;
        }

        public void Deactivate(List<int> symbols, CasterData caster)
        {
            var spell = database.GetSpellBySymbols(symbols);
            if (spell == null) return;

            spell.OnDeactivation(caster);
        }
    }
}
