using System.Collections.Generic;
using UnityEngine;

namespace Common.Progress
{
    [CreateAssetMenu(menuName = "Game/Progress/Flag Registry", fileName = "FlagRegistry")]
    public class FlagRegistry : ScriptableObject
    {
        [System.Serializable]
        public class FlagDef
        {
            public string key;
            [TextArea]
            public string description;
            public bool defaultValue;
        }

        [SerializeField] private List<FlagDef> flags = new List<FlagDef>();

        public IReadOnlyList<FlagDef> Flags => flags;

        public IEnumerable<string> GetKeys()
        {
            foreach (var f in flags)
            {
                if (!string.IsNullOrEmpty(f.key))
                    yield return f.key;
            }
        }
    }
}

