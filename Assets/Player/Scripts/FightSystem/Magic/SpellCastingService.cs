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

        public CastResult TryPrepareSpell(List<int> symbols, CasterData caster, out Spell spell)
        {
            spell = database.GetSpellBySymbols(symbols);
            if (spell == null)
                return CastResult.InvalidSymbol;

            var result = validator.Validate(spell, caster, consumeResources: false);
            if (result != CastResult.Success)
                Debug.LogWarning($"[SpellCastingService] Can't prepare spell, reason: {result}");
            else
            {
                Debug.Log($"[SpellCastingService] Spell '{spell.name}' prepared successfully. Activating.");
                spell.OnActivation(caster);
                SpawnIndicator(spell, caster);
            }

            return result;
        }

        public CastResult Cast(List<int> symbols, CasterData caster)
        {
            var spell = database.GetSpellBySymbols(symbols);
            return Cast(spell, caster);
        }

        public CastResult Cast(Spell spell, CasterData caster)
        {
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

        private static void SpawnIndicator(Spell spell, CasterData caster)
        {
            if (spell == null || caster == null)
            {
                Debug.LogWarning("[SpellCastingService] Cannot spawn indicator: spell or caster is null.");
                return;
            }

            var prefab = spell.IndicatorPrefab;
            if (prefab == null)
            {
                Debug.Log($"[SpellCastingService] Spell '{spell.name}' has no indicator prefab assigned.");
                return;
            }

            Transform origin = caster.castOrigin != null ? caster.castOrigin : caster.stats != null ? caster.stats.transform : null;

            if (origin != null)
            {
                Debug.Log($"[SpellCastingService] Spawning indicator '{prefab.name}' at origin '{origin.name}'.");
                var instance = Object.Instantiate(prefab, origin.position, origin.rotation);
                if (instance != null)
                {
                    Debug.Log($"[SpellCastingService] Indicator '{instance.name}' instantiated. Parenting to origin.");
                    instance.transform.SetParent(origin, worldPositionStays: true);
                }
                else
                {
                    Debug.LogWarning("[SpellCastingService] Indicator instantiation returned null instance.");
                }
            }
            else
            {
                Debug.LogWarning("[SpellCastingService] No cast origin or stats transform found, spawning indicator at default position.");
                Object.Instantiate(prefab);
            }
        }
    }
}
