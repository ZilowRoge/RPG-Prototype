using System;
using System.Collections.Generic;
using Quests;
using UnityEngine;

namespace UI.Player.Quests
{
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private bool autoSelectFirst = true;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color completedColor = new Color(0.65f, 0.65f, 0.65f);

        private readonly List<Entry> entries = new();

        public event Action<string> QuestSelected;

        public string Refresh(IReadOnlyList<QuestProgress> quests, QuestDatabase database, bool hideCompleted, string selectedQuestId)
        {
            Clear();

            if (contentRoot == null || itemPrefab == null)
            {
                Debug.LogWarning("[QuestListUI] References not assigned.", this);
                return null;
            }

            if (quests != null)
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    var progress = quests[i];
                    if (progress == null) continue;
                    if (string.IsNullOrEmpty(progress.questId)) continue;
                    if (hideCompleted && progress.state == QuestState.Completed) continue;

                    var item = CreateItem();
                    item.Initialize(progress.questId, OnItemClicked);

                    bool completed = progress.state == QuestState.Completed;
                    string title = ResolveQuestTitle(progress.questId, database);
                    if (completed)
                        title += " (Completed)";
                    item.SetTitle(title);

                    entries.Add(new Entry(item, progress.questId, completed));
                }
            }

            string resolvedSelection = selectedQuestId;
            if (string.IsNullOrEmpty(resolvedSelection) || !Contains(resolvedSelection))
            {
                resolvedSelection = autoSelectFirst && entries.Count > 0 ? entries[0].QuestId : null;
            }

            ApplySelection(resolvedSelection);
            return resolvedSelection;
        }

        public void ApplySelection(string questId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool selected = entry.QuestId == questId;
                entry.Item.ApplyState(selected, entry.Completed, selectedColor, activeColor, completedColor);
            }
        }

        public bool Contains(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].QuestId == questId)
                    return true;
            return false;
        }

        private void OnItemClicked(string questId)
        {
            ApplySelection(questId);
            QuestSelected?.Invoke(questId);
        }

        private QuestListItemUI CreateItem()
        {
            var instance = Instantiate(itemPrefab, contentRoot);
            var item = instance.GetComponent<QuestListItemUI>() ?? instance.GetComponentInChildren<QuestListItemUI>(true);
            if (item == null)
                item = instance.AddComponent<QuestListItemUI>();
            return item;
        }

        private void Clear()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Item != null)
                    DestroyItem(entry.Item.gameObject);
            }
            entries.Clear();
        }

        private void DestroyItem(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
#endif
                Destroy(go);
        }

        private static string ResolveQuestTitle(string questId, QuestDatabase database)
        {
            if (database == null || string.IsNullOrEmpty(questId))
                return questId;

            var asset = database.Get(questId);
            return asset != null && !string.IsNullOrEmpty(asset.title) ? asset.title : questId;
        }

        private readonly struct Entry
        {
            public Entry(QuestListItemUI item, string questId, bool completed)
            {
                Item = item;
                QuestId = questId;
                Completed = completed;
            }

            public QuestListItemUI Item { get; }
            public string QuestId { get; }
            public bool Completed { get; }
        }
    }
}
