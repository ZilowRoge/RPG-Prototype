using TMPro;
using UnityEngine;

namespace UI.Player.Quests
{
    public class QuestObjectiveItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private string incompletePrefix = "[ ] ";
        [SerializeField] private string completedPrefix = "[x] ";

        private string baseText = string.Empty;
        private bool isCompleted;

        private void Awake()
        {
            if (descriptionLabel == null)
                descriptionLabel = GetComponentInChildren<TMP_Text>(true);
        }

        public void Configure(string text, bool completed)
        {
            baseText = text ?? string.Empty;
            isCompleted = completed;
            RefreshLabel();
        }

        public void SetText(string text)
        {
            baseText = text ?? string.Empty;
            RefreshLabel();
        }

        public void SetPrefixes(string incomplete, string completed)
        {
            if (!string.IsNullOrEmpty(incomplete))
                incompletePrefix = incomplete;
            if (!string.IsNullOrEmpty(completed))
                completedPrefix = completed;
            RefreshLabel();
        }

        public void SetCompletionState(bool completed)
        {
            isCompleted = completed;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (descriptionLabel == null)
                return;

            string prefix = isCompleted ? completedPrefix : incompletePrefix;
            descriptionLabel.text = prefix + baseText;
        }
    }
}
