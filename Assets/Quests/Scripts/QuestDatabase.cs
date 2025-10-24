using System.Collections.Generic;
using UnityEngine;

namespace Quests
{
    [CreateAssetMenu(menuName = "Quests/Quest Database", fileName = "QuestDatabase")]
    public class QuestDatabase : ScriptableObject
    {
        [SerializeField] List<QuestAsset> quests = new();

        Dictionary<string, QuestAsset> index;

        void OnEnable()
        {
            BuildIndex();
        }

        void OnValidate()
        {
            BuildIndex();
        }

        void BuildIndex()
        {
            index = new Dictionary<string, QuestAsset>();
            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                if (q == null || string.IsNullOrEmpty(q.questId)) continue;
                if (!index.ContainsKey(q.questId))
                    index.Add(q.questId, q);
                else
                    index[q.questId] = q;
            }
        }

        public bool TryGet(string questId, out QuestAsset asset)
        {
            if (index == null || index.Count != quests.Count) BuildIndex();
            return index.TryGetValue(questId, out asset);
        }

        public QuestAsset Get(string questId)
        {
            TryGet(questId, out var a);
            return a;
        }

        public IReadOnlyList<QuestAsset> All => quests;

        public void SetQuests(List<QuestAsset> list)
        {
            quests = list ?? new List<QuestAsset>();
            BuildIndex();
        }
    }
}
