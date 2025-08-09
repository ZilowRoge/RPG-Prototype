using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Statistics;
using Player.Statistics;
using System.Collections.Generic;

namespace UI.Player.Statistics {
public class UIStatsPanel : MonoBehaviour
{
    [SerializeField] private StatsController statsController;
    [SerializeField] private GameObject statEntryPrefab;
    [SerializeField] private Transform statListParent;
    [SerializeField] private TextMeshProUGUI pendingText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;

    private Dictionary<EStatistics, StatEntryUI> entries = new();
    private Dictionary<EStatistics, int> tempAllocations = new();

    private void Start()
    {
        InitStatsUI();
        UpdateUI();

        confirmButton.onClick.AddListener(ConfirmAllocations);
        resetButton.onClick.AddListener(ResetAllocations);
    }

    private void InitStatsUI()
    {
        foreach (var stat in statsController.Data.stats.GetAll())
        {
            tempAllocations[stat.stat] = 0;
            var instance = Instantiate(statEntryPrefab, statListParent);
            var entryUI = instance.GetComponent<StatEntryUI>();
            entryUI.Setup(stat.stat, OnAddPointClicked, OnRemovePointClicked);
            entries[stat.stat] = entryUI;
        }
    }

    private void UpdateUI()
    {
        var container = statsController.Data.stats;
        int remaining = container.GetPendingPoints() - TotalAllocated();

        foreach (var stat in container.GetAll())
        {
            int baseValue = stat.value;
            int temp = tempAllocations[stat.stat];
            entries[stat.stat].UpdateValue(baseValue, temp, remaining > 0, temp > 0);
        }

        pendingText.text = $"Available points: {remaining}";
    }

    private int TotalAllocated()
    {
        int sum = 0;
        foreach (var pair in tempAllocations)
            sum += pair.Value;
        return sum;
    }

    private void OnAddPointClicked(EStatistics stat)
    {
        if (statsController.Data.stats.GetPendingPoints() - TotalAllocated() > 0)
        {
            tempAllocations[stat]++;
            UpdateUI();
        }
    }

    private void OnRemovePointClicked(EStatistics stat)
    {
        if (tempAllocations[stat] > 0)
        {
            tempAllocations[stat]--;
            UpdateUI();
        }
    }

    private void ConfirmAllocations()
    {
        foreach (var pair in tempAllocations)
        {
            for (int i = 0; i < pair.Value; i++)
                statsController.Data.stats.AllocatePoint(pair.Key);
        }

        ResetAllocations();
        UpdateUI();
    }

    private void ResetAllocations()
    {
        var keys = new List<EStatistics>(tempAllocations.Keys);
        foreach (var key in keys)
            tempAllocations[key] = 0;
        UpdateUI();
    }
}
}