using System;
using System.Collections.Generic;
using Common.UI;
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

        private readonly List<ViewModel> viewBuffer = new();
        private DynamicListPool<QuestListItemUI> itemPool;
        private string currentSelection;

        public event Action<string> QuestSelected;

        private void Awake()
        {
            EnsurePool();
        }

        public string Refresh(IReadOnlyList<QuestProgress> quests, QuestDatabase database, bool hideCompleted, string selectedQuestId)
        {
            EnsurePool();
            viewBuffer.Clear();

            if (quests != null)
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    var progress = quests[i];
                    if (progress == null || string.IsNullOrEmpty(progress.questId))
                        continue;
                    if (hideCompleted && progress.state == QuestState.Completed)
                        continue;

                    bool completed = progress.state == QuestState.Completed;
                    string title = ResolveQuestTitle(progress.questId, database);
                    if (completed)
                        title += " (Completed)";

                    viewBuffer.Add(new ViewModel(progress.questId, title, completed));
                }
            }

            SortViewBuffer();
            currentSelection = DetermineSelection(selectedQuestId);

            itemPool.Render(viewBuffer, (item, model, _) =>
            {
                item.Bind(model.QuestId, model.Title, model.Completed, OnItemClicked);
                item.UpdateVisualState(model.QuestId == currentSelection, selectedColor, activeColor, completedColor);
            });

            return currentSelection;
        }

        public void ApplySelection(string questId)
        {
            EnsurePool();
            currentSelection = questId;
            foreach (var item in itemPool.ActiveItems)
                item.UpdateVisualState(item.QuestId == currentSelection, selectedColor, activeColor, completedColor);
        }

        public bool Contains(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return false;

            EnsurePool();
            foreach (var item in itemPool.ActiveItems)
            {
                if (item.QuestId == questId)
                    return true;
            }

            return false;
        }

        private string DetermineSelection(string candidate)
        {
            if (!string.IsNullOrEmpty(candidate) && ContainsInBuffer(candidate))
                return candidate;

            if (autoSelectFirst && viewBuffer.Count > 0)
                return viewBuffer[0].QuestId;

            return null;
        }

        private void SortViewBuffer()
        {
            if (viewBuffer.Count <= 1)
                return;

            viewBuffer.Sort((a, b) =>
            {
                if (a.Completed == b.Completed)
                    return string.Compare(a.Title, b.Title, StringComparison.Ordinal);
                return a.Completed ? 1 : -1;
            });
        }

        private bool ContainsInBuffer(string questId)
        {
            for (int i = 0; i < viewBuffer.Count; i++)
            {
                if (viewBuffer[i].QuestId == questId)
                    return true;
            }
            return false;
        }

        private void OnItemClicked(string questId)
        {
            ApplySelection(questId);
            QuestSelected?.Invoke(questId);
        }

        private static string ResolveQuestTitle(string questId, QuestDatabase database)
        {
            if (database == null || string.IsNullOrEmpty(questId))
                return questId;

            var asset = database.Get(questId);
            return asset != null && !string.IsNullOrEmpty(asset.title) ? asset.title : questId;
        }

        private readonly struct ViewModel
        {
            public ViewModel(string questId, string title, bool completed)
            {
                QuestId = questId;
                Title = title;
                Completed = completed;
            }

            public string QuestId { get; }
            public string Title { get; }
            public bool Completed { get; }
        }

        private void EnsurePool()
        {
            if (itemPool == null)
                itemPool = new DynamicListPool<QuestListItemUI>(itemPrefab, contentRoot);
        }
    }
}
