using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;
using Systems.Statistics;
using UI.Player.Common;

namespace UI.Player.Statistics
{
    public class StatsListUI : MonoBehaviour
    {
        [SerializeField] private StatsController controller;
        [SerializeField] private GameObject statEntryPrefab;
        [SerializeField] private Transform statListParent;
        [SerializeField] private TextMeshProUGUI pendingText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resetButton;

        private readonly Dictionary<EStatistics, int> tempAllocations = new();
        private readonly List<StatViewModel> statBuffer = new();

        private DynamicListPool<StatEntryUI> entryPool;

        private void Awake()
        {
            entryPool = new DynamicListPool<StatEntryUI>(statEntryPrefab, statListParent);
        }

        private void OnEnable()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmAllocations);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetAllocations);

            RefreshUI();
        }

        private void OnDisable()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(ConfirmAllocations);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetAllocations);
        }

        public void RefreshUI()
        {
            if (controller == null || controller.Statistics == null || controller.Statistics.container == null)
            {
                entryPool?.Clear();
                if (pendingText != null)
                    pendingText.text = "Available points: 0";
                return;
            }

            var container = controller.Statistics.container;
            var stats = container.GetAll();
            EnsureAllocationEntries(stats);

            statBuffer.Clear();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                int temp = tempAllocations[stat.stat];
                statBuffer.Add(new StatViewModel(stat.stat, stat.value, temp));
            }

            int remaining = container.GetPendingPoints() - TotalAllocated();

            entryPool.Render(statBuffer, (item, data, _) =>
            {
                item.Configure(data.Stat, OnAddPointClicked, OnRemovePointClicked);
                item.UpdateValue(data.BaseValue, data.TempAllocation, remaining > 0, data.TempAllocation > 0);
            });

            if (pendingText != null)
                pendingText.text = $"Available points: {remaining}";
        }

        private void EnsureAllocationEntries(IReadOnlyList<(EStatistics stat, int value)> stats)
        {
            if (stats == null)
                return;

            for (int i = 0; i < stats.Count; i++)
            {
                var entry = stats[i];
                if (!tempAllocations.ContainsKey(entry.stat))
                    tempAllocations[entry.stat] = 0;
            }
        }

        private int TotalAllocated()
        {
            int sum = 0;
            foreach (var value in tempAllocations.Values)
                sum += value;
            return sum;
        }

        private void OnAddPointClicked(EStatistics stat)
        {
            var container = controller?.Statistics?.container;
            if (container == null)
                return;

            if (container.GetPendingPoints() - TotalAllocated() > 0)
            {
                tempAllocations[stat]++;
                RefreshUI();
            }
        }

        private void OnRemovePointClicked(EStatistics stat)
        {
            if (!tempAllocations.TryGetValue(stat, out int allocated) || allocated <= 0)
                return;

            tempAllocations[stat] = allocated - 1;
            RefreshUI();
        }

        private void ConfirmAllocations()
        {
            var container = controller?.Statistics?.container;
            if (container == null)
                return;

            foreach (var pair in tempAllocations)
            {
                for (int i = 0; i < pair.Value; i++)
                    container.AllocatePoint(pair.Key);
            }

            ResetAllocations();
            RefreshUI();
        }

        private void ResetAllocations()
        {
            var keys = new List<EStatistics>(tempAllocations.Keys);
            for (int i = 0; i < keys.Count; i++)
                tempAllocations[keys[i]] = 0;

            RefreshUI();
        }

        private readonly struct StatViewModel
        {
            public StatViewModel(EStatistics stat, int baseValue, int tempAllocation)
            {
                Stat = stat;
                BaseValue = baseValue;
                TempAllocation = tempAllocation;
            }

            public EStatistics Stat { get; }
            public int BaseValue { get; }
            public int TempAllocation { get; }
        }
    }
}
