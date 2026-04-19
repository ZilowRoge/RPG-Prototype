using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "ItemDefinitionDatabase", menuName = "Items/Item Definition Database")]
    public class ItemDefinitionDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> definitions = new();

        public IReadOnlyList<ItemDefinition> Definitions => definitions;

        private readonly Dictionary<string, ItemDefinition> lookup = new();
        private int cachedVersion = -1;

        private void OnEnable() => RebuildLookup();
        private void OnValidate() => RebuildLookup();

        public ItemDefinition GetById(string id)
        {
            return TryGetById(id, out var def) ? def : null;
        }

        public bool TryGetById(string id, out ItemDefinition definition)
        {
            EnsureLookup();
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            return lookup.TryGetValue(id, out definition);
        }

        private void EnsureLookup()
        {
            if (cachedVersion != definitions?.Count)
                RebuildLookup();
        }

        private void RebuildLookup()
        {
            lookup.Clear();
            if (definitions == null)
            {
                cachedVersion = 0;
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                    continue;

                lookup[def.Id] = def;
            }

            cachedVersion = definitions.Count;
        }
    }
}
