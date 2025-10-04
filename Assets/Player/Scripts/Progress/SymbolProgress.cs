using System.Collections.Generic;
using UnityEngine;

namespace Player.Progress
{
    [System.Serializable]
    public class SymbolProgress : ISerializationCallbackReceiver
    {
        [Tooltip("Symbols the player starts knowing.")]
        [SerializeField] private List<int> knownSymbols = new();

        [System.NonSerialized]
        private HashSet<int> runtimeSymbols = new();

        public SymbolProgress()
        {
            SyncRuntimeSymbols();
        }

        public bool IsSymbolLearned(int symbolId)
        {
            return runtimeSymbols.Contains(symbolId);
        }

        public bool MarkSymbolLearned(int symbolId)
        {
            if (!runtimeSymbols.Add(symbolId))
                return false;

            if (!knownSymbols.Contains(symbolId))
                knownSymbols.Add(symbolId);

            return true;
        }

        public int LearnedSymbolCount => runtimeSymbols.Count;

        public IReadOnlyCollection<int> GetLearnedSymbols() => runtimeSymbols;

        public void OverwriteLearnedSymbols(IEnumerable<int> symbols)
        {
            runtimeSymbols.Clear();
            knownSymbols.Clear();

            if (symbols == null)
                return;

            foreach (var id in symbols)
            {
                if (runtimeSymbols.Add(id))
                    knownSymbols.Add(id);
            }
        }

        public void OnBeforeSerialize()
        {
            if (knownSymbols == null)
                knownSymbols = new List<int>();

            var unique = new HashSet<int>();
            for (int i = knownSymbols.Count - 1; i >= 0; i--)
            {
                if (!unique.Add(knownSymbols[i]))
                    knownSymbols.RemoveAt(i);
            }
        }

        public void OnAfterDeserialize()
        {
            SyncRuntimeSymbols();
        }

        private void SyncRuntimeSymbols()
        {
            if (runtimeSymbols == null)
                runtimeSymbols = new HashSet<int>();
            else
                runtimeSymbols.Clear();

            if (knownSymbols == null)
            {
                knownSymbols = new List<int>();
                return;
            }

            for (int i = 0; i < knownSymbols.Count; i++)
                runtimeSymbols.Add(knownSymbols[i]);
        }
    }
}
