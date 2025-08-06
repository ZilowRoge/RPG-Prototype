using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Statistics;

public class StatEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;

    private EStatistics stat;
    private System.Action<EStatistics> onAddCallback;
    private System.Action<EStatistics> onRemoveCallback;

    public void Setup(EStatistics stat, System.Action<EStatistics> onAdd, System.Action<EStatistics> onRemove)
    {
        this.stat = stat;
        this.onAddCallback = onAdd;
        this.onRemoveCallback = onRemove;

        statNameText.text = stat.ToString();

        addButton.onClick.AddListener(() => onAddCallback?.Invoke(stat));
        removeButton.onClick.AddListener(() => onRemoveCallback?.Invoke(stat));
    }

    public void UpdateValue(int baseValue, int tempAdd, bool canAdd, bool canRemove)
    {
        int total = baseValue + tempAdd;
        statValueText.text = total.ToString();

        addButton.gameObject.SetActive(canAdd);
        removeButton.gameObject.SetActive(canRemove);
    }
}
