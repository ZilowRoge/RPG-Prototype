using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player.Quests
{
    public class QuestListItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button button;

        private string questId;
        private Action<string> clickCallback;
        private bool isCompleted;

        public string QuestId => questId;
        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            RegisterListener();
        }

        private void OnDisable()
        {
            UnregisterListener();
        }

        public void Bind(string questIdentifier, string title, bool completed, Action<string> onClicked)
        {
            EnsureComponents();

            questId = questIdentifier;
            isCompleted = completed;
            clickCallback = onClicked;

            if (titleLabel != null)
                titleLabel.text = title ?? string.Empty;

            RegisterListener();
        }

        public void UpdateVisualState(bool selected, Color selectedColor, Color activeColor, Color completedColor)
        {
            if (titleLabel == null)
                return;

            titleLabel.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            if (selected)
                titleLabel.color = selectedColor;
            else if (isCompleted)
                titleLabel.color = completedColor;
            else
                titleLabel.color = activeColor;
        }

        public void ApplyState(bool selected, bool completed, Color selectedColor, Color activeColor, Color completedColor)
        {
            isCompleted = completed;
            UpdateVisualState(selected, selectedColor, activeColor, completedColor);
        }

        private void OnClicked()
        {
            if (!string.IsNullOrEmpty(questId))
                clickCallback?.Invoke(questId);
        }

        private void EnsureComponents()
        {
            if (titleLabel == null)
                titleLabel = GetComponentInChildren<TMP_Text>(true);
            if (button == null)
                button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }

        private void RegisterListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        private void UnregisterListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClicked);
        }
    }
}
