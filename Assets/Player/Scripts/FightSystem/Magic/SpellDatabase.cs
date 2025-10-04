using UnityEngine;
using System.Collections.Generic;
using Spells;

namespace Player.FightSystem.Magic
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Magic/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [System.Serializable]
        public class SpellEntry
        {
            public List<int> symbolSequence;
            public Spell spell;
        }

        [SerializeField] private List<SpellEntry> spells = new();

        private Dictionary<string, Spell> lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, Spell>();
            foreach (var entry in spells)
            {
                if (entry.symbolSequence == null || entry.symbolSequence.Count == 0 || entry.spell == null)
                    continue;

                string key = string.Join("-", entry.symbolSequence);
                if (!lookup.ContainsKey(key))
                {
                    lookup.Add(key, entry.spell);
                }
            }
        }

        public Spell GetSpellBySymbols(List<int> symbols)
        {
            if (symbols == null || symbols.Count == 0) return null;
            string key = string.Join("-", symbols);
            return lookup.TryGetValue(key, out var spell) ? spell : null;
        }

        public void AddSpell(List<int> symbols, Spell spell)
        {
            if (symbols == null || symbols.Count == 0 || spell == null) return;

            spells.Add(new SpellEntry { symbolSequence = new List<int>(symbols), spell = spell });
            string key = string.Join("-", symbols);
            if (!lookup.ContainsKey(key))
            {
                lookup.Add(key, spell);
            }
        }
    }
}
