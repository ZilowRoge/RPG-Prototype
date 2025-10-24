using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Statistics;

namespace UI.Player.Statistics
{
    public class StatEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statNameText;
        [SerializeField] private TextMeshProUGUI statValueText;
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;

        private EStatistics stat;
        private Action<EStatistics> onAddCallback;
        private Action<EStatistics> onRemoveCallback;
        private bool listenersRegistered;

        private void Awake()
        {
            EnsureComponents();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        public void Configure(EStatistics stat, Action<EStatistics> onAdd, Action<EStatistics> onRemove)
        {
            EnsureComponents();
            this.stat = stat;
            onAddCallback = onAdd;
            onRemoveCallback = onRemove;

            if (statNameText != null)
                statNameText.text = stat.ToString();
        }

        public void UpdateValue(int baseValue, int tempAdd, bool canAdd, bool canRemove)
        {
            EnsureComponents();

            int total = baseValue + tempAdd;
            if (statValueText != null)
                statValueText.text = total.ToString();

            ToggleButton(addButton, canAdd);
            ToggleButton(removeButton, canRemove);
        }

        private void OnAddClicked()
        {
            onAddCallback?.Invoke(stat);
        }

        private void OnRemoveClicked()
        {
            onRemoveCallback?.Invoke(stat);
        }

        private void EnsureComponents()
        {
            if (statNameText == null)
                statNameText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (statValueText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 1)
                    statValueText = texts[1];
            }
            if (addButton == null)
                addButton = GetComponentInChildren<Button>(true);
            if (removeButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 1)
                    removeButton = buttons[1];
            }
        }

        private void RegisterListeners()
        {
            if (listenersRegistered)
                return;

            if (addButton != null)
                addButton.onClick.AddListener(OnAddClicked);
            if (removeButton != null)
                removeButton.onClick.AddListener(OnRemoveClicked);

            listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered)
                return;

            if (addButton != null)
                addButton.onClick.RemoveListener(OnAddClicked);
            if (removeButton != null)
                removeButton.onClick.RemoveListener(OnRemoveClicked);

            listenersRegistered = false;
        }

        private static void ToggleButton(Button button, bool visible)
        {
            if (button == null)
                return;

            var go = button.gameObject;
            if (go.activeSelf != visible)
                go.SetActive(visible);
        }
    }
}
