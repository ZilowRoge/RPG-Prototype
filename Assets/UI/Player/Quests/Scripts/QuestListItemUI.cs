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

        public string QuestId => questId;

        private void Awake()
        {
            if (titleLabel == null)
                titleLabel = GetComponentInChildren<TMP_Text>(true);
            if (button == null)
                button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }

        private void OnEnable()
        {
            EnsureButtonListener();
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClicked);
        }

        public void Initialize(string questIdentifier, Action<string> onClicked)
        {
            questId = questIdentifier;
            clickCallback = onClicked;
            EnsureButtonListener();
        }

        public void SetTitle(string text)
        {
            if (titleLabel != null)
                titleLabel.text = text ?? string.Empty;
        }

        public void ApplyState(bool selected, bool completed, Color selectedColor, Color activeColor, Color completedColor)
        {
            if (titleLabel == null)
                return;

            titleLabel.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            if (selected)
                titleLabel.color = selectedColor;
            else if (completed)
                titleLabel.color = completedColor;
            else
                titleLabel.color = activeColor;
        }

        private void OnClicked()
        {
            if (string.IsNullOrEmpty(questId))
                return;

            clickCallback?.Invoke(questId);
        }

        private void EnsureButtonListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }
}
